using System;
using AltLib;
using CommandLine;

namespace AltCmd
{
    /// <summary>
    /// Command to fetch changes from the upstream store.
    /// </summary>
    [Verb("fetch", HelpText = "Fetch the latest changes from the upstream store")]
    class FetchCmdLine : AltCmdLine
    {
        /// <summary>
        /// Obtains a string that contains all command line parameters.
        /// </summary>
        /// <returns>The command line text (without any abbreviations)</returns>
        public override string ToString()
        {
            return "fetch";
        }

        internal override ICmdHandler GetCommandHandler(ExecutionContext context)
        {
            CmdData data = context.CreateCmdData(nameof(IFetch));
            return new FetchHandler(data);
        }
    }
}
