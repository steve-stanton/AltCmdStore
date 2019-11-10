using System;
using System.Linq;
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
                Branch b = GetBranch(BranchName, context.Store.Current);
                if (b == null)
                {
                    Console.WriteLine($"No such branch: {BranchName}");
                    return false;
                }

                context.Store.SwitchTo(b);
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

        /// <summary>
        /// Converts the entered branch path into the corresponding branch.
        /// </summary>
        /// <param name="branchPath">The path for the branch
        /// to switch to (not case sensitive)</param>
        /// <returns>The branch that the path refers to (null if
        /// the child was not found)</returns>
        /// <remarks>
        /// The value for <paramref name="branchPath"/> works
        /// like a folder specification, and may be specified
        /// either in an absolute form (relative to the store
        /// root), or relative to the current branch.
        /// <para/>
        /// A branch path of ".." may be used as a shortcut for
        /// the parent of the current branch. A branch path of "/"
        /// may be used as a shortcut for the root branch.
        /// </remarks>
        Branch GetBranch(string branchPath, Branch curBranch)
        {
            // Treat ".." as a switch to the parent branch
            if (branchPath == "..")
                return curBranch.Parent;

            // Treat an undefined branch as a switch to the root branch
            if (!branchPath.IsDefined())
                return curBranch.GetRoot();

            // Treat a path that starts with a "/" as an absolute path (otherwise
            // it's relative to the current branch)
            Branch result = branchPath.StartsWith("/") ? curBranch.GetRoot() : curBranch;

            foreach (string name in branchPath.Split('/').Where(x => x.IsDefined()))
            {
                if (result == null)
                    return null;

                result = name == ".." ? result.Parent : result.GetChild(name);
            }

            return result;
        }
    }
}
