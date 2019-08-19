using System;

namespace AltLib
{
    /// <summary>
    /// Parameters for creating a new command store that is initially empty.
    /// </summary>
    /// <seealso cref="ICloneStore"/>
    public interface ICreateStore : ICmdInput
    {
        /// <summary>
        /// The unique ID that should be used to identify the store.
        /// </summary>
        Guid StoreId { get; }

        /// <summary>
        /// The user-perceived name for the store.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The persistence mechanism to be used for saving command data.
        /// </summary>
        StoreType Type { get; }
    }
}
