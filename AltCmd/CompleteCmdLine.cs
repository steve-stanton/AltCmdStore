using System;
using AltLib;
using CommandLine;

namespace AltCmd
{
    /// <summary>
    /// Marks the current branch as completed
    /// </summary>
    [Verb("complete", HelpText = "Marks the current branch as completed")]
    class CompleteCmdLine : AltCmdLine
    {
        [Option(
            'f',
            "force",
            HelpText = "Force completion even if the branch contains un-merged commands",
            Default = false)]
        public bool Force { get; private set; }

        /*
        [Option(
            'c',
            "cascade",
            HelpText = "Complete all descendants as well",
            Default = false)]
        public bool Cascade { get; private set; }
        */

        /// <summary>
        /// Obtains a string that contains all command line parameters.
        /// </summary>
        /// <returns>The command line text</returns>
        public override string ToString()
        {
            if (Force)
                return "complete --force";
            else
                return "complete";
        }

        public override bool Execute(ExecutionContext context)
        {
            // Disallow if we're working with a remote
            Branch b = context.Store.Current;
            if (b.IsRemote)
            {
                Console.WriteLine("You cannot complete a remote branch");
                return false;
            }

            // Disallow if there are commands that haven't been merged into the parent
            if (b.AheadCount > 0 && !Force)
            {
                Console.WriteLine($"Cannot complete ({b.AheadCount} command`s not merged)".TrimExtras());
                return false;
            }

            // TODO: Check all descendants too

            return base.Execute(context);
        }

        protected override ICmdHandler GetCommandHandler(ExecutionContext context)
        {
            CmdData data = context.CreateCmdData(nameof(IComplete));
            return new CompleteHandler(data);
        }
    }
}
