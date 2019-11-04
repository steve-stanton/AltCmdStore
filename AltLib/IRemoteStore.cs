using System;
using System.Collections.Generic;

namespace AltLib
{
    public interface IRemoteStore
    {
        /// <summary>
        /// The ID of the remote store.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Obtains metadata for a specific remote branch.
        /// </summary>
        /// <param name="branchId">The ID of the branch to retrieve.</param>
        /// <returns>Metadata for the branch.</returns>
        BranchInfo GetBranchInfo(Guid branchId);

        /// <summary>
        /// Obtains a collection of all branches known to the supplier.
        /// </summary>
        /// <returns>The branches known to the supply
        /// (the order you get items back is not specified, it could be
        /// entirely random).</returns>
        IEnumerable<BranchInfo> GetBranches();

        /// <summary>
        /// Obtains the command data for a branch.
        /// </summary>
        /// <param name="range">The range of data to retrieve from a branch.</param>
        /// <returns>
        /// The command data for the branch (in the order they were created).
        /// </returns>
        IEnumerable<CmdData> GetData(IdRange range);

        /// <summary>
        /// Obtains command ranges that are missing in another store.
        /// </summary>
        /// <param name="callerId">The ID of the command store making the request</param>
        /// <param name="callerHas">The number of commands that are currently present
        /// in each branch of the calling store (in any order).</param>
        /// <param name="isFetch">Specify <c>true</c> if the request is to satisfy a fetch
        /// request (in that case, new branches in this store should be included in the
        /// results). Specify <c>false</c> if the request is for a push.
        /// </param>
        /// <returns>The commands in this store that are not in the other store,
        /// or vice versa.</returns>
        IdRange[] GetMissingRanges(Guid callerId, IdCount[] callerHas, bool isFetch);

        /// <summary>
        /// Accepts data from another command store.
        /// </summary>
        /// <param name="source">A name that identifies the command store that is
        /// the source of the data.</param>
        /// <param name="ac">The metadata for the branch the commands are part of.</param>
        /// <param name="data">The command data to be appended to the remote branch.</param>
        void Push(string source, BranchInfo ac, CmdData[] data);
    }
}
