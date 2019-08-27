using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using NLog;

namespace AltLib
{
    /// <summary>
    /// A branch within the command model.
    /// </summary>
    public class Branch
    {
        static Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The command store that this branch is part of.
        /// </summary>
        public CmdStore Store { get; }

        /// <summary>
        /// Metadata relating to the branch.
        /// </summary>
        public AltCmdFile Info { get; internal set; }

        /// <summary>
        /// The parent branch (if any).
        /// </summary>
        public Branch Parent { get; internal set; }

        /// <summary>
        /// Any child branches.
        /// </summary>
        public List<Branch> Children { get; }

        /// <summary>
        /// The commands that have been appended to this branch.
        /// </summary>
        public List<CmdData> Commands { get; }

        /// <summary>
        /// The unique ID for the branch.
        /// </summary>
        public Guid Id => Info.BranchId;

        /// <summary>
        /// Creates a new instance of <see cref="Branch"/> (but does
        /// not load it).
        /// </summary>
        /// <param name="store">The command store that this branch is part of (not null)</param>
        /// <param name="ac">Metadata relating to the branch (not null)</param>
        /// <exception cref="ArgumentNullException">
        /// The supplied command store or metadata is undefined</exception>
        internal Branch(CmdStore store, AltCmdFile ac)
        {
            Store = store ?? throw new ArgumentNullException(nameof(store));
            Info = ac ?? throw new ArgumentNullException(nameof(ac));
            Commands = new List<CmdData>();
            Parent = null;
            Children = new List<Branch>();
        }

        /// <summary>
        /// Deserializes all commands for this branch.
        /// </summary>
        /// <exception cref="InvalidOperationException">At least one command
        /// has already been loaded.</exception>
        internal void LoadAll()
        {
            if (Commands.Count > 0)
                throw new InvalidOperationException("Branch already loaded");

            // I would expect the branch to always contain something
            if (Info.CommandCount == 0)
            {
                Log.Warn($"Attempt to load empty branch {Info.BranchName}");
                return;
            }

            Log.Trace($"Loading {Info.CommandCount} commands for {ToString()}");
            Commands.AddRange(ReadData(0, Info.CommandCount - 1));
        }

        /// <summary>
        /// Loads commands for this branch.
        /// </summary>
        /// <param name="cmdCount">The total number of commands in this branch
        /// that need to be loaded (any value greater than zero up to
        /// <see cref="Info.CommandCount"/>)</param>
        /// <remarks>
        /// If the commands have already been loaded, they do not get
        /// loaded again.
        /// </remarks>
        internal void Load(uint cmdCount)
        {
            // Just return if the required number of commands has already been loaded
            if (cmdCount > Commands.Count)
            {
                Log.Trace($"Loading commands [{Commands.Count},{cmdCount - 1}] for {ToString()}");
                Commands.AddRange(ReadData((uint)Commands.Count, cmdCount - 1));
            }
        }

        /// <summary>
        /// Clears command data previously read in by a call to <see cref="Load"/>
        /// </summary>
        internal void Unload()
        {
            Log.Trace($"Unloading commands for {ToString()}");
            Commands.Clear();
        }

        /// <summary>
        /// Retrieves the data at a specific command sequence (ensuring that
        /// all commands prior to that are also loaded).
        /// </summary>
        /// <param name="sequence">The data entry sequence number for the
        /// command within this branch.</param>
        /// <returns>The data for the command at the specified sequence.</returns>
        internal CmdData Get(uint sequence)
        {
            if (sequence >= (uint)Commands.Count)
                Load(sequence + 1);

            return Commands[(int)sequence];
        }

        /// <summary>
        /// Locates the root branch.
        /// </summary>
        /// <returns>The AC info for the master branch (in
        /// the root folder for the store).</returns>
        public Branch GetRoot()
        {
            for (Branch b = this; b != null; b = b.Parent)
            {
                if (b.Parent == null)
                    return b;
            }

            throw new ApplicationException("Cannot obtain root for " + Info.BranchName);
        }

        /// <summary>
        /// Attempts to locate a child branch given its name
        /// (relative to this branch).
        /// </summary>
        /// <param name="name">The name of the child branch (not
        /// case sensitive).</param>
        /// <returns>The first child with the specified name (null
        /// if not found).</returns>
        public Branch GetChild(string name)
        {
            return Children.FirstOrDefault(x => x.Info.BranchName.EqualsIgnoreCase(name));
        }

        /// <summary>
        /// The number of commands that this branch is behind the parent.
        /// </summary>
        /// <remarks>
        /// This is the number of commands that need to be merged from the
        /// parent branch, excluding recent merges that the parent has made
        /// from this branch.
        /// </remarks>
        public uint BehindCount
        {
            get
            {
                if (Parent == null)
                    return 0;
                else
                    return Parent.Info.CommandCount -
                           Info.RefreshDiscount -
                           Info.RefreshCount;
            }
        }

        /// <summary>
        /// The number of commands that this branch is ahead of the parent.
        /// </summary>
        /// <remarks>
        /// This is the number of commands that need to be merged into
        /// the parent, excluding recent merges from the parent.
        /// </remarks>
        public uint AheadCount
            => Info.CommandCount - Info.ParentDiscount - Info.ParentCount;

        /// <summary>
        /// The path for the folder that contains the AC file,
        /// relative to the root folder for the store.
        /// </summary>
        /// <param name="excludeRoot">Should the name of the store
        /// be excluded from the path (by default, the name of
        /// the store will be excluded).</param>
        /// <returns>
        /// If the root folder for the store is "C:\MyStores\Project123",
        /// and <see cref="FileName"/> is
        /// "C:\MyStores\Project123\MyBranch\Test\42.ac", you
        /// get "MyBranch/Test" by default. With 
        /// <paramref name="excludeRoot"/> set to <c>false</c>,
        /// that will be prefixed with "Project123/".
        /// </returns>
        /// <remarks>
        /// Use <see cref="FileName"/> if you want the absolute path.
        /// </remarks>
        public string GetBranchPath(bool excludeRoot = true)
        {
            var names = new List<string>();

            for (var b = this; b != null; b = b.Parent)
            {
                if (b.Parent == null)
                {
                    if (!excludeRoot)
                        names.Add(b.Info.BranchName);
                }
                else
                {
                    names.Add(b.Info.BranchName);
                }
            }

            names.Reverse();
            string result = String.Join("/", names);

            if (excludeRoot)
                return result;
            else
                return "/" + result;
        }

        /// <summary>
        /// Saves command data relating to this branch.
        /// </summary>
        /// <param name="data">The data to be written.</param>
        /// <exception cref="ApplicationException">Attempt to save new data
        /// to a branch imported from a remote store.</exception>
        /// <exception cref="ArgumentException">The supplied data does not
        /// have a sequence number that comes at the end of this branch.
        /// </exception>
        /// <remarks>
        /// This method can be used only to append data to a branch was created
        /// as part of the local store. As well as saving the command data, it
        /// will mutate the AC metadata for the branch.
        /// <para/>
        /// TODO: At the present time, a merge from a child branch will also
        /// mutate the AC metadata for the child. This is bad because the
        /// child could be a remote, but it would be wise to disallow any
        /// attempt to mutate anything relating to a remote branch.
        /// </remarks>
        public void SaveData(CmdData data)
        {
            if (IsRemote)
                throw new ApplicationException("Attempt to mutate remote branch");

            // The data must come at the end of the current branch
            if (data.Sequence != Info.CommandCount)
                throw new ArgumentException(
                                $"Unexpected command sequence {data.Sequence} " +
                                $"(should be {Info.CommandCount})");

            // Persist the command data
            Store.WriteData(this, data);

            // Update the AC file to reflect the latest command
            Info.CommandCount = data.Sequence + 1;

            // Update the appropriate merge count if we've just done a merge
            if (data.CmdName == nameof(IMerge))
            {
                Guid fromId = (data as IMerge).FromId;

                if (fromId.Equals(Info.ParentId))
                {
                    // Just done a merge from the parent (this is the child,
                    // now matches the parent)
                    this.Info.RefreshCount = Parent.Info.CommandCount;
                    this.Info.RefreshDiscount = 0;

                    // The parent doesn't need to consider the merge
                    // from this child, so increment the number of commands
                    // the parent can ignore
                    this.Info.ParentDiscount++;
                }
                else
                {
                    // TODO: Updating the child AC isn't ideal, is there another way?
                    // One potential issue is that when a store merges from a child
                    // that has been pushed from a remote clone, we end up mutating
                    // the AC of the child. Although we don't append any extra commands
                    // to the child, it could potentially confuse push/fetch logic.

                    // Just done a merge from a child, so update the child so
                    // that it knows how much the parent has merged
                    Branch child = Children.FirstOrDefault(x => x.Id.Equals(fromId));
                    if (child == null)
                        throw new ApplicationException($"Cannot locate child {fromId}");

                    // Update the child to signify that the parent now has everything
                    child.Info.ParentCount = child.Info.CommandCount;
                    child.Info.ParentDiscount = 0;

                    // The child doesn't need to consider the merge 
                    // that the parent has done, so increment the number of
                    // parent commands the child can ignore
                    child.Info.RefreshDiscount++;

                    child.Store.Save(child.Info);
                }
            }

            // Save the mutated branch metadata
            Store.Save(Info);
        }

        /// <summary>
        /// Reads the command data for a range of commands in the current branch.
        /// </summary>
        /// <param name="minSeq">The sequence number of the first command to be read.</param>
        /// <param name="maxSeq">The sequence number of the last command to be read</param>
        /// <returns>The commands in the specified range (ordered by
        /// their data entry sequence).
        /// </returns>
        public IEnumerable<CmdData> ReadData(uint minSeq, uint maxSeq)
        {
            // If the store is a relational database, an override that performs
            // a single range query would likely be better.

            for (uint i = minSeq; i <= maxSeq; i++)
                yield return Store.ReadData(this, i);
        }

        /// <summary>
        /// Obtains a subset of the commands in this branch (reading them in
        /// if they have not been loaded already).
        /// </summary>
        /// <param name="minSeq">The sequence number of the first command in the range.</param>
        /// <param name="maxSeq">The sequence number of the last command in the range.</param>
        /// <returns>The commands in the specified range (ordered by
        /// their data entry sequence).
        /// </returns>
        internal IEnumerable<CmdData> TakeRange(uint minSeq, uint maxSeq)
        {
            // Ensure the complete range has been loaded
            Load(maxSeq + 1);

            for (uint i = minSeq; i <= maxSeq; i++)
                yield return Commands[(int)i];
        }

        /// <summary>
        /// Obtains a string that represents this object.
        /// </summary>
        /// <returns>The string returned by <see cref="GetBranchPath"/></returns>
        public override string ToString()
        {
            return GetBranchPath(false);
        }

        /// <summary>
        /// Is this a copy of a remote branch?
        /// </summary>
        /// <remarks>
        /// Remote branches should only be mutated by the original store.
        /// </remarks>
        public bool IsRemote => Store.Id.Equals(Info.StoreId) == false;

        /// <summary>
        /// Is creation of a child branch allowed?
        /// </summary>
        /// <remarks>
        /// Branches that have been pushed from a clone can only be
        /// modified by that clone. Although the originating store can
        /// merge commands from such a branch (pulling them back up to
        /// the parent in the origin), it cannot change the pushed branch
        /// in any way.
        /// <para/>
        /// It would be theoretically possible to create a child of the
        /// pushed branch (since that would not modify the pushed branch
        /// in any way). But the commands in such a child would not be able
        /// to get merged back, since merging them back up would require an
        /// extra command in the pushed branch.
        /// </remarks>
        public bool CanBranch
        {
            get
            {
                // You can branch if this is a local branch, or this branch
                // and all ancestors are remote. In other words, you cannot
                // branch if this branch came from a remote store, and any
                // ancestor is local.

                if (IsRemote)
                {
                    for (Branch b = Parent; b != null; b = b.Parent)
                    {
                        if (!b.IsRemote)
                            return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Creates a local branch from this remote branch.
        /// </summary>
        /// <param name="ec">The current execution context</param>
        /// <returns>The newly created branch which the execution context
        /// now refers to.</returns>
        /// <exception cref="ApplicationException">This is not a remote branch,
        /// or the branch already has at least one local child branch.</exception>
        public Branch CreateLocal(ExecutionContext ec)
        {
            if (!IsRemote)
                throw new ApplicationException("Branch is already local");

            if (!CanBranch)
                throw new ApplicationException("A branch received from a clone cannot be branched");

            // Search for a local child
            if (Children.Any(x => !x.IsRemote))
                throw new ApplicationException("Remote branch already has at least one child");

            // Create a new child branch
            var data = new CmdData(
                        cmdName: nameof(ICreateBranch),
                        sequence: 0,
                        createdAt: DateTime.UtcNow);

            // + is a valid folder name, but may well not work in other types of store
            string childName = "+";
            data.Add(nameof(ICreateBranch.Name), childName);
            data.Add(nameof(ICreateBranch.CommandCount), Info.CommandCount);

            var cb = new CreateBranchHandler(data);
            cb.Process(ec);
            Branch result = GetChild(childName);

            // And switch to it
            ec.Store.SwitchTo(result);
            Log.Info($"Created (and switched to) {result}");

            return result;
        }
    }
}
