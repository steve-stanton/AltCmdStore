namespace AltLib
{
    /// <summary>
    /// Something that consumes instances of <see cref="CmdData"/> (usually
    /// to help build some sort of model).
    /// </summary>
    /// <seealso cref="ProcessorException"/>
    public interface ICmdProcessor
    {
        /// <summary>
        /// A filter that can identify the commands that are relevant
        /// to this processor (null if all commands are relevant).
        /// </summary>
        ICmdFilter Filter { get; }

        /// <summary>
        /// Performs any processing of the command data.
        /// </summary>
        /// <param name="data">The data to be processed.</param>
        /// <remarks>The supplied data is expected to be relevant to this
        /// processor (the caller should have already applied any filter
        /// to the command stream).
        /// <para/>
        /// Command processing is expected to be atomic (i.e. if a processor
        /// fails part-way, it should rollback any changes already made to its
        /// associated data model up to that point). In a situation where a
        /// command is handled by 'n' processors, a failure handling
        /// processor 'm' (where m&lt;n) will normally be followed by
        /// calls to <see cref="Undo"/> for processors 0 through (m-1).
        /// </remarks>
        void Process(CmdData data);

        /// <summary>
        /// Reverts any changes that were made via the last call to <see cref="Process"/>
        /// </summary>
        /// <param name="data">The data that was previously supplied to the
        /// <see cref="Process"/> method.</param>
        void Undo(CmdData data);
    }
}
