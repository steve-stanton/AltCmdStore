using System;

namespace AltLib
{
    /// <summary>
    /// A reference to something in the current branch.
    /// </summary>
    public class LocalRef : AltRef
    {
        /// <summary>
        /// Creates a new instance of <see cref="LocalRef"/>
        /// </summary>
        /// <param name="sequence">The 0-based sequence number of a command within the branch.</param>
        /// <param name="item">The number of an item that was created by the command (0 refers
        /// to the overall command).</param>
        /// <param name="propertyName">The (optional) name of a property within the referenced object</param>
        public LocalRef(uint sequence, uint item = 0, string propertyName = "")
            : base(sequence, item, propertyName)
        {
        }

        public override string ToString()
        {
            if (Item == 0)
                return $"[{Sequence}]{PropertyName}";
            else
                return $"[{Sequence}.{Item}]{PropertyName}";
        }

        /// <summary>
        /// Obtains the branch that is referenced by this instance.
        /// </summary>
        /// <param name="from">The branch that holds this instance</param>
        /// <returns>The branch that is being referred to.</returns>
        protected override Branch GetReferencedBranch(Branch from)
        {
            return from;
        }
    }
}
