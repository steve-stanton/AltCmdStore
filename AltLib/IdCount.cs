using System;

namespace AltLib
{
    /// <summary>
    /// A count associated with an ID.
    /// </summary>
    public struct IdCount
    {
        /// <summary>
        /// The ID.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// The value for the count.
        /// </summary>
        public uint Count { get; }

        /// <summary>
        /// Creates an instance of <see cref="IdCount"/>
        /// </summary>
        /// <param name="id">The ID.</param>
        /// <param name="count"></param>
        public IdCount(Guid id, uint count)
        {
            Id = id;
            Count = count;
        }
    }
}
