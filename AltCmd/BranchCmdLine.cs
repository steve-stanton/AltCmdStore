using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AltLib;
using CommandLine;

namespace AltCmd
{
    /// <summary>
    /// Command line for creating a new branch. For consistency
    /// with the git command line, this also provides the ability
    /// to just list branches.
    /// </summary>
    [Verb("branch", HelpText = "List or create branches")]
    class BranchCmdLine : AltCmdLine, ICreateBranch
    {
        /// <summary>
        /// The user-perceived name for the new branch
        /// </summary>
        /// <remarks>
        /// The same branch name may be repeated throughout a
        /// command store. But for a given parent branch, all
        /// immediate child branches must have distinct names.
        /// </remarks>
        [Value(
            0,
            Required = false,
            HelpText = "The name for the new branch",
            MetaName = "branch-name")]
        public string Name { get; internal set; }

        /// <summary>
        /// The number of commands to inherit from the parent.
        /// </summary>
        [Value(
            1,
            Required = false,
            HelpText = "The number of commands to inherit (omit to branch from the end)",
            MetaName = "command-count")]
        public uint CommandCount { get; private set; }

        [Option(
            'l',
            "list",
            HelpText = "Lists branches",
            Default = false)]
        public bool List { get; private set; }

        [Option(
            'a',
            "all",
            HelpText = "List both local and remote branches",
            Default = false)]
        public bool All { get; private set; }

        [Option(
            'r',
            "remotes",
            HelpText = "List only remote branches",
            Default = false)]
        public bool Remotes { get; private set; }

        public override bool Execute(ExecutionContext context)
        {
            if (List)
            {
                ListBranches(context);
                return true;
            }

            if (!Name.IsDefined())
            {
                Console.WriteLine("You need to specify a name for the new branch");
                return false;
            }

            // If a starting point has been specified, confirm that
            // it is valid. If nothing specified, define it to point
            // to the end of the current branch.

            uint cc = context.Store.Current.Info.CommandCount;
            if (CommandCount == 0)
            {
                CommandCount = cc;
            }
            else
            {
                if (CommandCount > cc)
                {
                    Console.WriteLine($"'{CommandCount}' is too big. It must not be bigger than {cc}");
                    return false;
                }
            }

            bool result = base.Execute(context);

            if (result)
            {
                Branch newBranch = context.Store.Current.GetChild(Name);
                Console.WriteLine($"Created {newBranch}");
            }

            return result;
        }

        protected override ICmdHandler GetCommandHandler(ExecutionContext context)
        {
            // Don't use context.CreateCmdData here because the command
            // will get added to the new branch (not the current one).

            var data = new CmdData(
                        cmdName: nameof(ICreateBranch),
                        sequence: 0,
                        createdAt: DateTime.UtcNow);

            data.Add(nameof(ICreateBranch.Name), Name);
            data.Add(nameof(ICreateBranch.CommandCount), CommandCount);

            return new CreateBranchHandler(data);
        }

        public override string ToString()
        {
            string result = "branch";

            if (String.IsNullOrEmpty(Name))
            {
                result += " --list";
            }
            else
            {
                result += $" {Name}";

                if (CommandCount > 0)
                    result += $" {CommandCount}";
            }

            return result;
        }

        void ListBranches(ExecutionContext ec)
        {
            if (ec.Store == null)
                throw new ApplicationException("Store is undefined");

            CmdStore store = ec.Store;

            // Apply optional filter
            Guid curBranchId = store.Current.Info.BranchId;
            IEnumerable<Branch> toList = ApplyFilter(store.Branches.Values);
            uint numLocal = 0;
            uint numRemote = 0;
            uint totLocal = 0;
            uint totRemote = 0;

            foreach (Branch b in toList.OrderBy(x => x.GetBranchPath()))
            {
                if (b.IsRemote)
                {
                    numRemote++;
                    totRemote += b.Info.CommandCount;
                }
                else
                {
                    numLocal++;
                    totLocal += b.Info.CommandCount;
                }

                if (!All)
                {
                    if (Remotes != b.IsRemote)
                        continue;
                }

                string prefix = b.Info.BranchId.Equals(curBranchId) ? "*" : " ";
                string suffix = String.Empty;
                string isRemote = " ";

                if (b.IsRemote)
                {
                    // If every ancestor is remote, it's an upstream branch. Otherwise
                    // its a downstream branch that has been pushed back to its origin
                    isRemote = b.CanBranch ? "^" : ".";
                }

                if (b.Parent != null)
                {
                    if (b.BehindCount > 0)
                        suffix = $" (behind parent by {b.BehindCount}";

                    if (b.AheadCount > 0)
                    {
                        if (suffix.Length > 0)
                            suffix += ", ";
                        else
                            suffix = " (";

                        suffix += $"ahead of parent by {b.AheadCount}";

                        // If we've pushed, show where we got to (can't easily show a
                        // number that's a subset of the AheadCount, since the branch
                        // metadata doesn't hold the discount value as well)
                        // TODO: Need to show how many within the AheadCount have
                        // actually been pushed
                        if (b.Info.LastPush != 0)
                            suffix += $" - pushed to [{b.Info.LastPush}]";
                    }

                    if (suffix.Length > 0)
                        suffix += ")";
                }

                Console.WriteLine($"{prefix} {isRemote}{b}{suffix}");
            }

            Console.WriteLine();
            string localMsg = $"{totLocal} command`s in {numLocal} local branch`es".Strips();
            string remotesMsg = $"{totRemote} command`s in {numRemote} remote branch`es".Strips();

            if (!All)
            {
                if (Remotes)
                    localMsg += " (not listed)";
                else
                    remotesMsg += " (not listed)";
            }

            Console.WriteLine(localMsg);

            if (totRemote > 0)
                Console.WriteLine(remotesMsg);
        }

        IEnumerable<Branch> ApplyFilter(IEnumerable<Branch> branches)
        {
            if (String.IsNullOrEmpty(Name))
                return branches;

            string pat = $"^{Regex.Escape(Name).Replace("\\?", ".").Replace("\\*", ".*")}$";
            return branches.Where(x => Regex.IsMatch(x.GetBranchPath(true), pat));
        }
    }
}
