using System;
using System.Diagnostics;

namespace AltLib
{
    /// <summary>
    /// Information relating to merges with immediate child branches.
    /// </summary>
    public class MergeInfo
    {
        /// <summary>
        /// The total number of commands in the child branch at the
        /// time of the last merge.
        /// </summary>
        public uint ChildCount { get; internal set; }

        /// <summary>
        /// The number of merges from the parent that the child had done at the
        /// time of the last merge.
        /// </summary>
        /// <remarks>Technically, this is the number of merges, plus 1 to account
        /// for the ICreateBranch command (which acts much like a merge).</remarks>
        public uint ChildDiscount { get; internal set; }

        /// <summary>
        /// The number of merges the <b>parent</b> has taken from the child.
        /// </summary>
        public uint ParentDiscount { get; internal set; }

        /// <summary>
        /// Creates a new instance of <see cref="MergeInfo"/>
        /// </summary>
        /// <param name="childCount">The total number of commands in the child branch at the
        /// time of the last merge.</param>
        /// <param name="childDiscount">The number of merges from the parent that the child had done at the
        /// time of the last merge.</param>
        /// <param name="parentDiscount">The number of merges the parent has taken from the child.</param>
        public MergeInfo(uint childCount, uint childDiscount, uint parentDiscount)
        {
            Debug.Assert(childDiscount <= childCount);

            ChildCount = childCount;
            ChildDiscount = childDiscount;
            ParentDiscount = parentDiscount;
        }
    }
}
