using System;
using System.IO;
using System.Linq;

namespace AltLib
{
    /// <summary>
    /// Command handler for creating a new branch.
    /// </summary>
    public class CreateBranchHandler : ICmdHandler
    {
        /// <summary>
        /// The input parameters for the command (not null).
        /// </summary>
        CmdData Input { get; }

        /// <summary>
        /// Creates a new instance of <see cref="CreateBranchHandler"/>
        /// </summary>
        /// <param name="input">The input parameters for the command.</param>
        /// <exception cref="ArgumentNullException">
        /// Undefined value for <paramref name="input"/></exception>
        /// <exception cref="ArgumentException">
        /// The supplied input has an unexpected value for <see cref="CmdData.CmdName"/>
        /// </exception>
        public CreateBranchHandler(CmdData input)
        {
            Input = input ?? throw new ArgumentNullException(nameof(Input));

            if (Input.CmdName != nameof(ICreateBranch))
                throw new ArgumentException(nameof(Input.CmdName));
        }

        /// <summary>
        /// Performs the data processing associated with a command.
        /// </summary>
        public void Process(ExecutionContext context)
        {
            CmdStore cs = context?.Store ?? throw new ApplicationException("Undefined store");

            uint numCmd = (Input as ICreateBranch).CommandCount;
            if (numCmd == 0)
                throw new ApplicationException(nameof(numCmd));

            // Confirm that the name for the new branch is not a
            // duplicate (considering just the children of the
            // current branch)
            string name = (Input as ICreateBranch).Name;
            Branch parent = cs.Current;
            Branch oldBranch = parent.GetChild(name);
            if (oldBranch != null)
                throw new ArgumentException(
                    $"Branch {name} previously created at {oldBranch.Info.CreatedAt}");

            // Confirm that the new branch name is acceptable to the command store
            if (!cs.IsValidBranchName(name))
                throw new ArgumentException($"Branch name '{name}' is not allowed");

            var ac = new AltCmdFile(
                        storeId: cs.Id,
                        parentId: parent.Id,
                        branchId: Guid.NewGuid(),
                        createdAt: Input.CreatedAt,
                        commandCount: 0,
                        commandDiscount: 1,
                        refreshCount: numCmd,
                        refreshDiscount: 0,
                        lastMerge: null);

            // Save the AC file in the store
            string parentPath = Path.GetDirectoryName(parent.Info.FileName);
            string newPath = Path.Combine(parentPath, name);
            ac.FileName = Path.Combine(newPath, ac.BranchId + ".ac");
            cs.Save(ac);

            var newBranch = new Branch(cs, ac);
            newBranch.SaveData(Input);

            // Update internal structure to include the new branch
            newBranch.Parent = parent;
            parent.Children.Add(newBranch);
            cs.Branches.Add(ac.BranchId, newBranch);
        }
    }
}
