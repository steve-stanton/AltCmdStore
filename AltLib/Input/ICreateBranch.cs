using System;

namespace AltLib
{
    /// <summary>
    /// Parameters for creating a new branch
    /// </summary>
    public interface ICreateBranch : ICmdInput
    {
        /// <summary>
        /// The user-perceived name for the new branch
        /// </summary>
        /// <remarks>
        /// The same branch name may be repeated throughout a
        /// command store. But for a given parent branch, all
        /// immediate child branches must have distinct names.
        /// </remarks>
        string Name { get; }

        /// <summary>
        /// The number of commands that should be inherited by the new branch.
        /// </summary>
        uint CommandCount { get; }
    }
}
