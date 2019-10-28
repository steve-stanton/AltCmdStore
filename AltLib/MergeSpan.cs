using System;

namespace AltLib
{
    /// <summary>
    /// Defines a range of commands involved in a merge.
    /// </summary>
    /// <remarks>This is a helper class utilized by <see cref="CmdStreamFactory"/></remarks>
    class MergeSpan
    {
        /// <summary>
        /// The branch that the commands are being merged from
        /// </summary>
        internal Branch Source { get; }

        /// <summary>
        /// The sequence number of the first command in the source branch.
        /// </summary>
        internal uint MinCmd { get; }

        /// <summary>
        /// The sequence number of the last command in the source branch.
        /// </summary>
        internal uint MaxCmd { get; }

        /// <summary>
        /// The branch that the commands are being merged into
        /// </summary>
        internal Branch Target { get; }

        /// <summary>
        /// The sequence number of the merge command (in the target branch)
        /// that produced this span.
        /// </summary>
        internal uint Sequence { get; }

        /// <summary>
        /// Creates an instance of <see cref="MergeSpan"/>
        /// </summary>
        /// <param name="source">The branch that the commands are being merged from</param>
        /// <param name="minCmd">The sequence number of the first command in the source branch.</param>
        /// <param name="maxCmd">The sequence number of the last command in the source branch.</param>
        /// <param name="target">The branch that the commands are being merged into</param>
        /// <param name="targetSequence">The sequence number of the merge command (in the target branch)</param>
        internal MergeSpan(Branch source, uint minCmd, uint maxCmd,
                           Branch target, uint targetSequence)
        {
            if (maxCmd < minCmd)
                throw new ArgumentException();

            Source = source;
            MinCmd = minCmd;
            MaxCmd = maxCmd;
            Target = target;
            Sequence = targetSequence;
        }
    }
}
