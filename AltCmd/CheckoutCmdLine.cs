using System;
using AltLib;
using CommandLine;

namespace AltCmd
{
    [Verb("checkout", HelpText = "Specify the current working branch")]
    class CheckoutCmdLine : AltCmdLine
    {
        /// <summary>
        /// The name of the branch to be checked out (may be a relative
        /// branch specification)
        /// </summary>
        [Value(
            0,
            Required = true,
            HelpText = "The name of the branch to be checked out",
            MetaName = "branch-name")]
        public string BranchName { get; set; }

        
        [Option(
            'b',
            "branch",
            HelpText = "Creates a new branch, then checks it out",
            Default = false)]
        public bool Branch { get; set; }

        public override string ToString()
        {
            string result = $"checkout {BranchName}";

            if (Branch)
                result += " --branch";

            return result;
        }

        public override bool Execute(ExecutionContext context)
        {
            if (Branch)
            {
                var cmd = new BranchCmdLine { Name = BranchName };
                if (!cmd.Execute(context))
                    return false;
            }

            try
            {
                Branch result = context.Store.SwitchTo(BranchName);
                if (result == null)
                {
                    Console.WriteLine($"No such branch: {BranchName}");
                    return false;
                }

                return true;
            }

            catch (Exception ex)
            {
                ShowError(ex);
                return false;
            }
        }

        protected override ICmdHandler GetCommandHandler(ExecutionContext context)
        {
            throw new NotSupportedException();
        }
    }
}
