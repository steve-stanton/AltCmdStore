using System;
using NLog;

namespace AltLib
{
    /// <summary>
    /// Command handler to mark a branch as completed.
    /// </summary>
    public class CompleteHandler : ICmdHandler
    {
        static Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The input parameters for the command (not null).
        /// </summary>
        CmdData Input { get; }

        /// <summary>
        /// Creates a new instance of <see cref="CompleteHandler"/>
        /// </summary>
        /// <param name="input">The input parameters for the command.</param>
        /// <exception cref="ArgumentNullException">
        /// Undefined value for <paramref name="input"/></exception>
        /// <exception cref="ArgumentException">
        /// The supplied input has an unexpected value for <see cref="CmdData.CmdName"/>
        /// </exception>
        public CompleteHandler(CmdData input)
        {
            Input = input ?? throw new ArgumentNullException(nameof(Input));

            if (Input.CmdName != nameof(IComplete))
                throw new ArgumentException(nameof(Input.CmdName));
        }

        public void Process(ExecutionContext context)
        {
            AltCmdFile ac = context.Store.Current.Info;
            ac.IsCompleted = true;
            context.Store.Save(ac);
        }
    }
}
