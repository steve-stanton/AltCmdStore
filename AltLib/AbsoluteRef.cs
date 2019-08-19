using System;

namespace AltLib
{
    /// <summary>
    /// A reference that includes the ID of the branch involved.
    /// </summary>
    public class AbsoluteRef : AltRef
    {
        /// <summary>
        /// The ID of the branch
        /// </summary>
        public Guid BranchId { get; }

        /// <summary>
        /// Creates a new instance of <see cref="AltRef"/>
        /// </summary>
        /// <param name="branchId">The ID of the branch</param>
        /// <param name="sequence">The 0-based sequence number of a command within the branch.</param>
        /// <param name="item">The number of an item that was created by the command (0 refers
        /// to the overall command).</param>
        /// <param name="propertyName">The (optional) name of a property within the referenced object</param>
        public AbsoluteRef(Guid branchId, uint sequence, uint item = 0, string propertyName = "")
            : base(sequence, item, propertyName)
        {
            BranchId = branchId;
        }

        public override string ToString()
        {
            if (Item == 0)
                return $"{{{BranchId}}}[{Sequence}]{PropertyName}";
            else
                return $"{{{BranchId}}}[{Sequence}.{Item}]{PropertyName}";
        }

        /// <summary>
        /// Obtains the branch that is referenced by this instance.
        /// </summary>
        /// <param name="from">The branch that holds this instance</param>
        /// <returns>The branch that is being referred to.</returns>
        protected override Branch GetReferencedBranch(Branch from)
        {
            return from?.Store.FindBranch(BranchId);
        }
    }
}
