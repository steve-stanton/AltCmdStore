using System;

namespace AltLib
{
    /// <summary>
    /// Input parameters for merging branches.
    /// </summary>
    public interface IMerge : ICmdInput
    {
        /// <summary>
        /// The ID of the branch to merge into the current branch.
        /// </summary>
        /// <remarks>
        /// The ID must refer to either the parent of the current branch,
        /// or one of its child branches.
        /// <para/>
        /// The ID of the branch we are merging into is not one of the
        /// input parameters. That is known via the branch that the
        /// merge command gets saved to.
        /// </remarks>
        Guid FromId { get; }

        /// <summary>
        /// The 0-based sequence number of the first command that
        /// needs to be merged.
        /// </summary>
        uint MinCmd { get; }

        /// <summary>
        /// The 0-based sequence number of the last command that
        /// needs to be merged.
        /// </summary>
        uint MaxCmd { get; }
    }
}
