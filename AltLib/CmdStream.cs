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

            // Load the target branch (plus stuff merged into it)
            Load(Main, 0, Main.Info.CommandCount - 1);

            // Load ancestors as well
            for (Branch b = Main; b != null; b = b.Parent)
            {
                if (b.Parent != null)
                {
                    CmdData cd = b.Commands[0];
                    Debug.Assert(cd.CmdName == nameof(ICreateBranch));
                    ICreateBranch cb = (cd as ICreateBranch);
                    Load(b.Parent, 0, cb.CommandCount - 1);
                }
            }

            Console.WriteLine("Loaded " + Cmds.Count);
        }

        void Load(Branch branch, uint minCmd, uint maxCmd)
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

                if (cd.CmdName == nameof(IMerge))
                {
                    IMerge m = (cd as IMerge);
                    Branch fromBranch = null;

                    if (m.FromId.Equals(parentId))
                        fromBranch = branch.Parent;
                    else
                        fromBranch = branch.Store.FindBranch(m.FromId);

                    Load(fromBranch, m.MinCmd, m.MaxCmd);
                }

                Cmds.AddFirst(new Cmd(branch, cd));
            }
        }
    }
}
