using System;
using System.Collections.Generic;
using System.Linq;
using NLog;

namespace AltLib
{
    /// <summary>
    /// Command handler for pushing change to an upstream store.
    /// </summary>
    public class PushHandler : ICmdHandler
    {
        static Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The input parameters for the command (not null).
        /// </summary>
        CmdData Input { get; }

        /// <summary>
        /// Creates a new instance of <see cref="PushHandler"/>
        /// </summary>
        /// <param name="input">The input parameters for the command.</param>
        /// <exception cref="ArgumentNullException">
        /// Undefined value for <paramref name="input"/></exception>
        /// <exception cref="ArgumentException">
        /// The supplied input has an unexpected value for <see cref="CmdData.CmdName"/>
        /// </exception>
        public PushHandler(CmdData input)
        {
            Input = input ?? throw new ArgumentNullException(nameof(Input));

            if (Input.CmdName != nameof(IPush))
                throw new ArgumentException(nameof(Input.CmdName));
        }

        public void Process(ExecutionContext context)
        {
            CmdStore cs = context.Store;
            RootInfo root = cs.Root;
            string upLoc = root.UpstreamLocation;
            if (String.IsNullOrEmpty(upLoc))
                throw new ApplicationException("There is no upstream location");

            // Collate the number of commands we have in local branches
            IdCount[] have = cs.Branches.Values
                               .Where(x => !x.IsRemote)
                               .Select(x => new IdCount(x.Id, x.Info.CommandCount))
                               .ToArray();

            IRemoteStore rs = context.GetRemoteStore(upLoc);

            // We don't care about new branches in the origin, but we do care
            // about local branches that have been created since the last push
            IdRange[] toPush = rs.GetMissingRanges(cs.Id, have, false).ToArray();

            // How many commands do we need to push
            uint total = (uint)toPush.Sum(x => x.Size);

            Log.Info($"To push {total} command`s in {toPush.Length} branch`es".TrimExtras());

            foreach (IdRange idr in toPush)
            {
                Branch b = cs.FindBranch(idr.Id);
                if (b == null)
                    throw new ApplicationException("Cannot locate branch " + idr.Id);

                Log.Info($"Push [{idr.Min},{idr.Max}] from {b}");

                CmdData[] data = b.TakeRange(idr.Min, idr.Max).ToArray();
                rs.Push(cs.Name, b.Info, data);

                // Remember how much we were ahead at the time of the push
                b.Info.LastPush = idr.Max + 1;
                b.Store.Save(b.Info);
            }

            Log.Info("Push completed");
        }
    }
}
