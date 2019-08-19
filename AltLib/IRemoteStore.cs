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
        AltCmdFile GetBranchInfo(Guid branchId);

        /// <summary>
        /// Obtains a collection of all branches known to the supplier.
        /// </summary>
        /// <returns>The branches known to the supply
        /// (the order you get items back is not specified, it could be
        /// entirely random).</returns>
        IEnumerable<AltCmdFile> GetBranches();

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
        /// <param name="existing">The number of commands that are currently present
        /// in each branch of another store (in any order).</param>
        /// <param name="wantNew">Should new branches in this store (i.e. branches
        /// that are unknown in the other store) be included in the results.
        /// Specify <c>true</c> when fetching from the remote, or <c>false</c> when
        /// pushing to the remote.</param>
        /// <returns>The commands in this store that are not in the other store,
        /// or vice versa.</returns>
        IdRange[] GetMissingRanges(IdCount[] existing, bool wantNew);
    }
}
