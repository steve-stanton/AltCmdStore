namespace AltLib
{
    /// <summary>
    /// Parameters for pushing changes to an upstream store.
    /// </summary>
    public interface IPush : ICmdHandler
    {
        // No parameters (for the time being anyway)
        /// <summary>
        /// The ID of the remote store
        /// </summary>
        //Guid RemoteId { get; }
    }
}
