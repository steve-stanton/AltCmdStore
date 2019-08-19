using System;

namespace AltLib
{
    /// <summary>
    /// Parameters for creating a command store that is a clone of an existing store.
    /// </summary>
    /// <remarks>
    /// Additional parameters relating to the creation of the clone are specified
    /// as part of the <see cref="ICreateStore"/> interface.
    /// </remarks>
    public interface ICloneStore : ICreateStore
    {
        /// <summary>
        /// The name of the store to clone from (possibly including a folder path).
        /// </summary>
        /// <remarks>When cloning from another local store, this provides
        /// the path to the root folder for the store.</remarks>
        string Origin { get; }

        /// <summary>
        /// Should the clone be regarded as a mirror of the origin?
        /// </summary>
        /// <remarks>
        /// A mirror acts as a backup copy that downstream stores can use as
        /// an alternative for fetch and push operations. Mirror copies may
        /// be out of sync at any one time, but should ultimately become
        /// identical.
        /// <para/>
        /// Mirrors will have the same store ID as the origin.
        /// </remarks>
        //bool Mirror { get; }

        /// <summary>
        /// Will the clone be used only to extend the content of the origin?
        /// </summary>
        /// <remarks>
        /// An extend-only clone is allowed to create additional branches, but it
        /// will not be allowed to push upstream.
        /// <para/>
        /// TODO: Is this worth burning into the metadata of the clone? If the
        /// user wants it to be extend-only, they just need to avoid the push command.
        /// </remarks>
        //bool ExtendOnly { get; }
    }
}
