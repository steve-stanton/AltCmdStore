using System;

namespace AltLib
{
    /// <summary>
    /// A numeric range associated with an ID.
    /// </summary>
    /// <remarks>
    /// When fetching from an upstream store, the downstream store initially supplies
    /// the number of commands it already has in every branch. The upstream can use that
    /// to determine the additional commands that need to be sent.
    /// <para/>
    /// Similarly, when the downstream pushes, it first tells the upstream what it
    /// has. The upstream responds with a list of the branch ranges that it does not
    /// already have.
    /// </remarks>
    public struct IdRange
    {
        /// <summary>
        /// The ID.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// The lower bound of the range.
        /// </summary>
        public uint Min { get; }

        /// <summary>
        /// The upper bound of the range.
        /// </summary>
        public uint Max { get; }

        /// <summary>
        /// The size of the range (inclusive of both min and max).
        /// </summary>
        public uint Size => Max - Min + 1;

        /// <summary>
        /// Creates an instance of <see cref="IdRange"/>
        /// </summary>
        /// <param name="id">The ID.</param>
        /// <param name="min">The lower bound of the range.</param>
        /// <param name="max">The upper bound of the range.</param>
        /// <exception cref="ArgumentException">The supplied max is less than the min.</exception>
        public IdRange(Guid id, uint min, uint max)
        {
            if (min > max)
                throw new ArgumentException();

            Id = id;
            Min = min;
            Max = max;
        }
    }
}
