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
            // store, but any attempt to change them made elsewhere should be
            // disallowed.

            IdCount[] have = cs.Branches.Values
                                .Where(x => x.IsRemote)
                                .Select(x => new IdCount(x.Id, x.Info.CommandCount))
                                .ToArray();

            // Open a channel to the upstream store
            IRemoteStore rs = context.GetRemoteStore(upLoc);

            // We do care about new branches in the remote
            IdRange[] toFetch = rs.GetMissingRanges(have, true);

            // How many commands do we need to fetch
            uint total = (uint)toFetch.Sum(x => x.Size);
            Log.Info($"To fetch {total} commands from {toFetch.Length} branches");

            /*
            // Keep track of any new branches
            var newBranches = new List<AltCmdFile>();

            // As well as the latest state of the AC metadata on the remote
            var updBranches = new List<AltCmdFile>();

            foreach (IdRange idr in toFetch)
            {
                // Fetch the AC file, but don't copy over command data just yet
                // (it's important to create new branches in their creation order)
                AltCmdFile ac = rs.GetBranchInfo(idr.Id);
                if (ac == null)
                    throw new ApplicationException("Could not locate remote branch " + idr.Id);

                Branch b = cs.FindBranch(idr.Id);

                if (b == null)
                {
                    newBranches.Add(ac);
                }
                else
                {
                    updBranches.Add(ac);

                    Log.Info($"Fetch [{idr.Min},{idr.Max}] into {b}");
                    CmdData[] data = rs.GetData(idr).ToArray();

                    // TODO: This will fail sometimes because it expects any child to
                    // be there (and that could be a new branch).
                    foreach (CmdData cd in data)
                        b.SaveData(cd);
                }
            }

            // Process any new branches (in the order they were created)
            foreach (AltCmdFile ac in newBranches.OrderBy(x => x.CreatedAt))
            {
                Log.Info("New branch " + ac.BranchId);

                // Transfer the command data
                var idr = new IdRange(ac.BranchId, 0, ac.CommandCount - 1);
                CmdData[] data = rs.GetData(idr).ToArray();
            }

            // Process any branches that have been updated
            foreach (AltCmdFile ac in updBranches)
            {
            }
            */
        }
    }
}
