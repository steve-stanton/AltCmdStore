using System;
using AltLib;
using CommandLine;

namespace AltCmd
{
    /// <summary>
    /// Marks a branch as completed
    /// </summary>
    [Verb("complete", HelpText = "Marks a branch as completed")]
    class CompleteCmdLine : AltCmdLine
    {
        /// <summary>
        /// Obtains a string that contains all command line parameters.
        /// </summary>
        /// <returns>The command line text</returns>
        public override string ToString()
        {
            return "complete";
        }
        public override bool Execute(ExecutionContext context)
        {
            // Disallow if the current branch is ahead of the parent
            Branch b = context.Store.Current;
            if (b.IsRemote)
            {
                Console.WriteLine("You cannot complete a remote branch");
                return false;
            }

            // Disallow if there are commands that haven't been merged into the parent
            if (b.AheadCount > 0)
            {
                Console.WriteLine($"Cannot complete ({b.AheadCount} command`s not merged)".TrimExtras());
                return false;
            }

            return base.Execute(context);
        }

        protected override ICmdHandler GetCommandHandler(ExecutionContext context)
        {
            CmdData data = context.CreateCmdData(nameof(IComplete));
            return new CompleteHandler(data);
        }
    }
}
