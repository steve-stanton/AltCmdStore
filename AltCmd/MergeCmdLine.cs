using System;
using System.Diagnostics;
using AltLib;
using CommandLine;

namespace AltCmd
{
    /// <summary>
    /// Command to merge commands from a parent to a child branch (and vice versa).
    /// </summary>
    [Verb("merge", HelpText = "Import commands from a neighbouring branch")]
    class MergeCmdLine : AltCmdLine
    {
        /// <summary>
        /// The name of the branch to merge into the current branch.
        /// </summary>
        [Value(
            0,
            Required = true,
            HelpText = "The name of the branch to be merged into the current branch",
            MetaName = "from")]
        public string From { get; private set; }

        /// <summary>
        /// The ID of the branch to merge into the current branch.
        /// </summary>
        /// <remarks>
        /// The ID must refer to either the parent of the current branch,
        /// or one of its child branches.
        /// </remarks>
        public Guid FromId { get; private set; }

        /// <summary>
        /// Obtains a string that contains all command line parameters.
        /// </summary>
        /// <returns>The command line text (without any abbreviations)</returns>
        public override string ToString()
        {
            return $"merge {From}(={FromId})";
        }

        public override bool Execute(ExecutionContext context)
        {
            // Convert the name of the branch we're merging from into it's
            // internal ID (the name might later change, but the ID won't)

            CmdStore cs = context.Store;

            if (From == "..")
            {
                if (cs.Current.Parent == null)
                {
                    Console.WriteLine("The current branch does not have a parent branch");
                    return false;
                }

                // Confirm that we are not already up to date
                if (cs.Current.BehindCount == 0)
                {
                    Console.WriteLine("Nothing to merge");
                    return false;
                }

                FromId = cs.Current.Info.ParentId;
                Debug.Assert(!FromId.Equals(Guid.Empty));
            }
            else
            {
                // TODO: There is a bit of a problem with this. If a new branch has been
                // created in a clone, it will be regarded as a remote as soon as you do a
                // push (since I expect the push to update root metadata with a new store ID).
                // So if you make any further changes, yet another branch will be needed.

                // This makes sense, because a branch that has been pushed to the remote
                // could be modified there. That isn't necessarily bad (a new branch for
                // every push), but it may need to be presented to the user in another way.

                // Perhaps a push could have a --private option that would let you send
                // the command data, while disallowing any changes on the remote. You
                // would need to send a "release" command to make the pushes available.

                // Or, do it the other way around: make all pushed branches private by
                // default. The remote would still be able to merge from them, but would
                // not be able to mutate them. Which means there needs to be a property
                // in the Branch class to say it can be used only in a certain context.
                // But how to specify the context -- is it a machine name? If so, it would
                // be more tricky to anticipate copies of any one store.

                if (cs.Current.IsRemote)
                {
                    Console.WriteLine("You are currently on a remote branch (use the push command instead)");
                    return false;
                }

                Branch child = cs.Current.GetChild(From);
                if (child == null)
                {
                    Console.WriteLine($"Cannot locate child branch called '{From}'");
                    return false;
                }

                // Confirm the parent does not already have everything from the child
                if (child.AheadCount == 0)
                {
                    Console.WriteLine("Nothing to merge");
                    return false;
                }

                FromId = child.Id;
            }

            return base.Execute(context);
        }

        internal override ICmdHandler GetCommandHandler(ExecutionContext context)
        {
            CmdData data = context.CreateCmdData(nameof(IMerge));
            data.Add(nameof(IMerge.FromId), FromId);
            return new MergeHandler(data);
        }
    }
}
