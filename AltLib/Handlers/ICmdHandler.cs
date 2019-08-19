namespace AltLib
{
    /// <summary>
    /// Handles the logic for processing of a command.
    /// </summary>
    public interface ICmdHandler
    {
        /// <summary>
        /// Performs the data processing associated with a command.
        /// </summary>
        /// <param name="context">The execution context (not null).
        /// This may be modified to reflect changes caused by the
        /// processing.
        /// </param>
        /// <exception cref="ArgumentNullException">Undefined execution context</exception>
        void Process(ExecutionContext context);
    }
}
