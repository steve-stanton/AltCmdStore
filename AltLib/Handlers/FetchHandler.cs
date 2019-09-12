using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NLog;

namespace AltLib
{
    /// <summary>
    /// Command handler for fetching the latest changes from the upstream store
    /// </summary>
    public class FetchHandler : ICmdHandler
    {
        static Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The input parameters for the command (not null).
        /// </summary>
        CmdData Input { get; }

        /// <summary>
        /// Creates a new instance of <see cref="FetchHandler"/>
        /// </summary>
        /// <param name="input">The input parameters for the command.</param>
        /// <exception cref="ArgumentNullException">
        /// Undefined value for <paramref name="input"/></exception>
        /// <exception cref="ArgumentException">
        /// The supplied input has an unexpected value for <see cref="CmdData.CmdName"/>
        /// </exception>
        public FetchHandler(CmdData input)
        {
            Input = input ?? throw new ArgumentNullException(nameof(Input));

            if (Input.CmdName != nameof(IFetch))
                throw new ArgumentException(nameof(Input.CmdName));
        }

        public void Process(ExecutionContext context)
        {
            CmdStore cs = context.Store;
            RootFile root = cs.Root;
            string upLoc = root.UpstreamLocation;
            if (String.IsNullOrEmpty(upLoc))
                throw new ApplicationException("There is no upstream location");

            // Collate how much we already have in all remote branches.
            // We may have previously pushed some local branches to the remote
            // store, but nothing is supposed to mutate those remote copies.

            IdCount[] have = cs.Branches.Values
                               .Where(x => x.IsRemote)
                               .Select(x => new IdCount(x.Id, x.Info.CommandCount))
                               .ToArray();

            // Open a channel to the upstream store
            IRemoteStore rs = context.GetRemoteStore(upLoc);

            // Determine what we are missing (including any new branches in the remote)
            IdRange[] toFetch = rs.GetMissingRanges(cs.Id, have, true);

            // How many commands do we need to fetch
            uint total = (uint)toFetch.Sum(x => x.Size);
            Log.Info($"To fetch {total} command`s from {toFetch.Length} branch`es".TrimExtras());

            // Retrieve the command data from the remote, keeping new branches
            // apart from appends to existing branches.

            var newBranchData = new Dictionary<AltCmdFile, CmdData[]>();
            var moreBranchData = new Dictionary<AltCmdFile, CmdData[]>();

            foreach (IdRange idr in toFetch)
            {
                // Fetch the remote AC file
                AltCmdFile ac = rs.GetBranchInfo(idr.Id);
                if (ac == null)
                    throw new ApplicationException("Could not locate remote branch " + idr.Id);

                // And the relevant data
                Log.Info($"Fetch [{idr.Min},{idr.Max}] for {ac.BranchName} ({ac.BranchId})");
                CmdData[] branchData = rs.GetData(idr).ToArray();

                if (cs.FindBranch(ac.BranchId) == null)
                    newBranchData.Add(ac, branchData);
                else
                    moreBranchData.Add(ac, branchData);
            }

            // All done with the remote store

            // Copy any brand new branches (ensuring they get created in the
            // right order so that parent/child relationships can be formed
            // as we go).

            foreach (KeyValuePair<AltCmdFile, CmdData[]> kvp in newBranchData.OrderBy(x => x.Key.CreatedAt))
                cs.CopyIn(kvp.Key, kvp.Value);

            // Append command data for branches we previously had (the order
            // shouldn't matter)

            foreach (KeyValuePair<AltCmdFile, CmdData[]> kvp in moreBranchData)
                cs.CopyIn(kvp.Key, kvp.Value);

            Log.Info("Fetch completed");

            // Reload the current command stream (from scratch, kind of brute force,
            // not sure whether appending the new commands would really be sufficient)
            // TODO: Is this really necessary? Perhaps only if the current branch has
            // been fetched (the stuff we're fetching should only come from remotes,
            // but the current branch could be one of those remotes)
            //cs.Stream = new CmdStream(cs.Current);
        }
    }
}
