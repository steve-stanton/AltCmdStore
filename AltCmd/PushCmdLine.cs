using System;
using AltLib;
using CommandLine;

namespace AltCmd
{
    /// <summary>
    /// Command to push changes to the upstream store.
    /// </summary>
    [Verb("push", HelpText = "Push all changes to the upstream store")]
    class PushCmdLine : AltCmdLine
    {
        /// <summary>
        /// Obtains a string that contains all command line parameters.
        /// </summary>
        /// <returns>The command line text (without any abbreviations)</returns>
        public override string ToString()
        {
            return "push";
        }

        internal override ICmdHandler GetCommandHandler(ExecutionContext context)
        {
            CmdData data = context.CreateCmdData(nameof(IPush));
            return new PushHandler(data);
        }
    }
}
