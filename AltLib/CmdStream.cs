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

            // Link the main branch (this may well load data from other branches
            // that merge into the main)
            var data = new Dictionary<Guid, CmdData[]>();
            Link(data, Main, 0, Main.Info.CommandCount - 1);

            // Work through the ancestors as well
            for (Branch b = Main; b != null; b = b.Parent)
            {
                if (b.Parent != null)
                {
                    CmdData cd = b.Commands[0];
                    Debug.Assert(cd.CmdName == nameof(ICreateBranch));
                    ICreateBranch cb = (cd as ICreateBranch);
                    Link(data, b.Parent, 0, cb.CommandCount - 1);
                }
            }

            // Confirm that all data has been accounted for
            foreach (KeyValuePair<Guid, CmdData[]> kvp in data)
            {
                if (!kvp.Value.All(x => x == null))
                {
                    Branch b = Main.Store.FindBranch(kvp.Key);
                    Log.Error($"Not all command data accounted for in {b}");
                }
            }

            // Apply any updates

            //Log.Info($"Loaded stream with {Cmds.Count} commands");
        }

        /// <summary>
        /// Links command data in a branch (including data that has been merged
        /// from other branches)
        /// </summary>
        /// <param name="allData">The command data for all branches that contribute
        /// to the stream, keyed by the branch ID.</param>
        /// <param name="branch">The branch to walk through</param>
        /// <param name="minCmd">The earliest command in the branch to consider</param>
        /// <param name="maxCmd">The latest command if the branch to consider</param>
        void Link(Dictionary<Guid, CmdData[]> allData, Branch branch, uint minCmd, uint maxCmd)
        {
            if (!allData.ContainsKey(branch.Id))
            {
                // Ensure the branch has been loaded up to the high water mark
                branch.Load(maxCmd + 1);

                // The branch may already hold a larger cache of command data. Take a
                // copy of the references we need to build this stream (since we're
                // walking backwards, we should encounter the max on the first call)
                CmdData[] data = branch.Commands.Take((int)maxCmd + 1).ToArray();
                allData.Add(branch.Id, data);
            }

            CmdData[] branchData = allData[branch.Id];

            if (maxCmd >= branchData.Length)
                throw new IndexOutOfRangeException($"{branch}[{maxCmd}] out of range (max is {branchData.Length - 1})");

            for (int i = (int)maxCmd; i >= (int)minCmd; i--)
            {
                CmdData cd = branchData[i];

                if (cd != null)
                {
                    if (cd.CmdName == nameof(IMerge))
                    {
                        // TODO: Is this really preserving the entry order
                        // across branches? I think it needs to return as soon as
                        // it hits a merge from the calling branch (that's behind
                        // the idea of nulling out stuff as we go). But how would
                        // it know where to continue the merge??

                        IMerge m = (cd as IMerge);
                        Branch fromBranch = branch.Store.FindBranch(m.FromId);

                        // The merge command itself will precede the things
                        // that the merge brought in (reads better that way)
                        Link(allData, fromBranch, m.MinCmd, m.MaxCmd);
                    }

                    Cmds.AddFirst(new Cmd(branch, cd));

                    // And null it out. As we walk back down the chain of commands,
                    // we may end up merging from a branch that merges in turn from
                    // the calling branch. We want to make sure we don't include it
                    // a second time in the stream.

                    // This also makes it possible to confirm that all command data
                    // has been accounted for when we have finished building the
                    // stream.

                    branchData[i] = null;
                }
            }
        }
    }
}
