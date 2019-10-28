namespace AltLib
{
    enum PropertyNaming
    {
        /// <summary>
        /// The ID of the current store.
        /// </summary>
        StoreId,

        /// <summary>
        /// The ID of the upstream store.
        /// </summary>
        /// <remarks>If there is no upstream, this property will not be present.</remarks>
        UpstreamId,

        /// <summary>
        /// The location of the upstream store.
        /// </summary>
        /// <remarks>If there is no upstream, this property will not be present.</remarks>
        UpstreamLocation,

        //LastMerge

        /// <summary>
        /// The ID of the branch that was last checked out.
        /// </summary>
        LastBranch,
    }
}
