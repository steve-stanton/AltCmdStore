using System;

namespace AltLib
{
    /// <summary>
    /// An exception that has arisen on a call to an implementation
    /// of a method specified by <see cref="ICmdProcessor"/>.
    /// </summary>
    public class ProcessorException : Exception
    {
        /// <summary>
        /// The processor that failed (not null).
        /// </summary>
        public ICmdProcessor Processor { get; }

        /// <summary>
        /// Creates a new instance of <see cref="ProcessorException"/>
        /// </summary>
        /// <param name="processor">The processor that failed (not null).</param>
        /// <param name="message">A message to explain the failure.</param>
        /// <param name="ex">Any inner exception (may be null).</param>
        /// <exception cref="ArgumentNullException">Undefined processor</exception>
        public ProcessorException(ICmdProcessor processor, string message, Exception ex = null)
            : base(message, ex)
        {
            Processor = processor ?? throw new ArgumentNullException("Undefined processor");
        }
    }
}
