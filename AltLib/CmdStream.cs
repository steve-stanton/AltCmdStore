using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NLog;

namespace AltLib
{
    public class CmdStream
    {
        static Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The main branch for this stream (not null).
        /// </summary>
        public Branch Main { get; }

        public LinkedList<Cmd> Cmds { get; }

        /// <summary>
        /// Creates a new instance of <see cref="CmdStream"/> and loads it.
        /// </summary>
        /// <param name="main">The main branch for this stream (not null).</param>
        /// <exception cref="ArgumentNullException">The specified main branch is undefined.</exception>
        public CmdStream(Branch main)
        {
            Main = main ?? throw new ArgumentNullException(nameof(main));
            Cmds = new LinkedList<Cmd>();

            // Load the target branch (plus any stuff merged into it)
            Link(Main, 0, Main.Info.CommandCount - 1, Guid.Empty);

            // Work through the ancestors as well
            for (Branch b = Main; b != null; b = b.Parent)
            {
                if (b.Parent != null)
                {
                    CmdData cd = b.Commands[0];
                    Debug.Assert(cd.CmdName == nameof(ICreateBranch));
                    ICreateBranch cb = (cd as ICreateBranch);
                    Link(b.Parent, 0, cb.CommandCount - 1, Guid.Empty);
                }
            }

            Log.Info($"Loaded stream with {Cmds.Count} commands");
        }

        /// <summary>
        /// Links commands in a branch, recursively linking with additional
        /// commands that have been merged in from elsewhere.
        /// </summary>
        /// <param name="branch">The branch to walk through</param>
        /// <param name="minCmd">The earliest command in the branch to consider</param>
        /// <param name="maxCmd">The latest command if the branch to consider</param>
        /// <param name="mergeFromId">The ID of the branch containing a merge that
        /// led to this call (<see cref="Guid.Empty"/> if the call did not originate
        /// from a merge).</param>
        void Link(Branch branch, uint minCmd, uint maxCmd, Guid mergeFromId)
        {
            // Ensure the branch data we need is all loaded (the commands
            // may have been loaded already if we have created more than
            // one command stream)
            branch.Load(maxCmd + 1);

            Guid parentId = branch.Info.ParentId;

            // Work back from the end
            for (int i = (int)maxCmd; i >= (int)minCmd; i--)
            {
                CmdData cd = branch.Commands[i];

                // Don't include merges in the result (they don't add anything further,
                // and can look a bit confused when you dump it out)

                if (cd.CmdName == nameof(IMerge))
                {
                    IMerge m = (cd as IMerge);
                    Branch fromBranch = null;

                    if (m.FromId.Equals(parentId))
                        fromBranch = branch.Parent;
                    else
                        fromBranch = branch.Store.FindBranch(m.FromId);

                    // If we're already doing a merge, ignore merges from the
                    // branch we came from (we will reach them in their original
                    // place after we have returned to the caller).

                    if (!fromBranch.Id.Equals(mergeFromId))
                        Link(fromBranch, m.MinCmd, m.MaxCmd, branch.Id);
                }
                else
                {
                    Cmds.AddFirst(new Cmd(branch, cd));
                }
            }
        }
    }
}
