using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;

namespace AltLib
{
    /// <summary>
    /// The content of an AltCmd "AC" file.
    /// </summary>
    /// <remarks>
    /// An AC file contains a collection of metadata for a branch.
    /// Unlike command data, the content of an AC file is mutable,
    /// and will be updated and rewritten each time a command is
    /// appended to the branch.
    /// <para/>
    /// The AC file does not contain a user-perceived branch name.
    /// The branch name is obtained from the name of the folder that
    /// contains the AC file. When you create a brand new store,
    /// the root directory for the store itself is regarded as the
    /// "master" branch. As such, the user-perceived name of the
    /// initial branch will match the name of the store.
    /// <para/>
    /// The user is at liberty to rename the root directory for
    /// a command store, as well as sub-folders that represent
    /// further branches within that store.
    /// </remarks>
    public class AltCmdFile : IEquatable<AltCmdFile>
    {
        /// <summary>
        /// The unique ID for the store that contains this branch.
        /// </summary>
        public Guid StoreId { get; }

        /// <summary>
        /// The ID of the parent branch (if any).
        /// </summary>
        /// <remarks>
        /// This could potentially be a branch that is part of a different command store.
        /// <para/>
        /// If there is no parent (the AC appears in the root
        /// folder of a new store), this will be <see cref="Guid.Empty"/>.
        /// </remarks>
        public Guid ParentId { get; private set; }

        /// <summary>
        /// The unique ID for the branch.
        /// </summary>
        /// <remarks>
        /// The root branch for a store will have a branch ID that equals <see cref="StoreId"/>.
        /// </remarks>
        public Guid BranchId { get; }

        /// <summary>
        /// The time (UTC) when the branch was originally created.
        /// </summary>
        /// <remarks>
        /// This time is obtained from the local system time, and so
        /// may not be entirely accurate.
        /// </remarks>
        public DateTime CreatedAt { get; }

        /// <summary>
        /// The number of commands that have been appended to the branch.
        /// </summary>
        public uint CommandCount { get; internal set; }

        /// <summary>
        /// The number of merges from the parent that are included in
        /// the value for <see cref="CommandCount"/>
        /// </summary>
        /// <remarks>This excludes merges from any child branches.</remarks>
        public uint CommandDiscount { get; internal set; }

        /// <summary>
        /// The number of commands in the parent branch that are known to this branch.
        /// </summary>
        /// <remarks>
        /// When a branch is created, this will correspond to the value for
        /// <see cref="ICreateBranch.CommandCount"/> (i.e. it refers to the
        /// number of commands inherited from the parent).
        /// <para/>
        /// If the child branch later merges from the parent, it will increase
        /// to match the new parent total.
        /// <para/>
        /// If this value is less than <see cref="Branch.Parent.CommandCount"/>,
        /// it therefore means the parent has appended commands that the child
        /// is not aware of. However, this does not necessarily mean that the
        /// child is out of date, because the extra commands in the parent could
        /// be merges from the same child.
        /// <para/>
        /// The <see cref="RefreshDiscount"/> property is an adjustment used
        /// to account for commands that are of no significance to the child.
        /// </remarks>
        /// <seealso cref="Branch.BehindCount"/>
        public uint RefreshCount { get; internal set; }

        /// <summary>
        /// The number of merges that the parent has made from this child branch.
        /// </summary>
        /// <remarks>
        /// This has a value of 0 for a brand new branch. When a child merges from its parent,
        /// this value is defined to match the total number of merges that the parent has made
        /// from the child (which may be 0).
        /// </remarks>
        public uint RefreshDiscount { get; internal set; }

        /// <summary>
        /// Information relating to merges from child branches, keyed by the branch ID.
        /// </summary>
        /// <remarks>This excludes child branches that the parent has never merged from.</remarks>
        public Dictionary<Guid, MergeInfo> LastMerge { get; }

        /// <summary>
        /// The number of commands in this branch that have been pushed upstream
        /// (applies only to clones).
        /// </summary>
        /// <remarks>In a situation where the upstream exists in more than one
        /// location (i.e. there is more than one copy), this does not tell
        /// us which location actually received the last push. Some sort of
        /// overall controller is expected to keep the copies in sync.
        /// </remarks>
        public uint LastPush { get; internal set; }

        /// <summary>
        /// Has the branch been tagged as completed?
        /// </summary>
        /// TODO: Make it a BranchState property (Active, Completed, Deleted)
        public bool IsCompleted { get; internal set; }

        /// <summary>
        /// The AC file name (including the full path).
        /// </summary>
        [JsonIgnore]
        public string FileName { get; internal set; }

        /// <summary>
        /// The directory name for <see cref="FileName"/> (or null if
        /// that is not defined).
        /// </summary>
        [JsonIgnore]
        public string DirectoryName => Path.GetDirectoryName(FileName);

        /// <summary>
        /// Creates a new instance of <see cref="AltCmdFile"/>
        /// </summary>
        /// <param name="storeId">The unique ID for the store that contains this branch.</param>
        /// <param name="parentId">The ID of the parent branch (or <see cref="Guid.Empty"/> if
        /// there is no parent).</param>
        /// <param name="branchId">The unique ID for the branch.</param>
        /// <param name="createdAt">The time (UTC) when the branch was originally created.</param>
        /// <param name="commandCount">The number of commands that have been appended to the branch.</param>
        /// <param name="commandDiscount">The number of merges from the parent that are
        /// included in <paramref name="commandCount"/></param>
        /// <param name="refreshCount">The number of commands in this child branch
        /// that have been merged from the parent.</param>
        /// <param name="refreshDiscount">The number of merges that the parent has made
        /// from this child branch (at the time this child last merged from the parent)</param>
        /// <param name="lastPush">The number of commands in this branch that have
        /// been pushed upstream (applies only to clones)</param>
        /// <param name="lastMerge">Information relating to merges from child branches,
        /// keyed by the branch ID.</param>
        /// <param name="isCompleted">Has the branch been tagged as completed?</param>
        [JsonConstructor]
        internal AltCmdFile(Guid storeId,
                            Guid parentId,
                            Guid branchId,
                            DateTime createdAt,
                            uint commandCount = 0,
                            uint commandDiscount = 0,
                            uint refreshCount = 0,
                            uint refreshDiscount = 0,
                            uint lastPush = 0,
                            Dictionary<Guid,MergeInfo> lastMerge = null,
                            bool isCompleted = false)
        {
            StoreId = storeId;
            ParentId = parentId;
            BranchId = branchId;
            CreatedAt = createdAt;
            CommandCount = commandCount;
            CommandDiscount = commandDiscount;
            RefreshCount = refreshCount;
            RefreshDiscount = refreshDiscount;
            LastPush = lastPush;
            LastMerge = lastMerge ?? new Dictionary<Guid, MergeInfo>();
            IsCompleted = isCompleted;
        }

        /// <summary>
        /// The name of the folder that contains the AC file.
        /// </summary>
        /// <remarks>
        /// For a <see cref="FileName"/> of
        /// "C:\MyStores\Project123\MyBranch\42.ac",
        /// you get "MyBranch".
        /// </remarks>
        [JsonIgnore]
        public string BranchName
        {
            get
            {
                if (FileName == null)
                    return String.Empty;

                string dirName = Path.GetDirectoryName(FileName);
                var dirInfo = new DirectoryInfo(dirName);

                return dirInfo.Name;
            }
        }

        public override string ToString()
        {
            return $"{BranchName}[{CommandCount}]";
        }


        /// <summary>
        /// Attempts to locate a single AC file in a specific folder.
        /// </summary>
        /// <param name="folderName">The folder to check.</param>
        /// <returns>The path for a single AC file present in the specified folder</returns>
        /// <exception cref="ApplicationException">The specified folder contains more than one AC file</exception>
        public static string GetAcPath(string folderName)
        {
            string[] acFiles = Directory.GetFiles(folderName, "*.ac", SearchOption.TopDirectoryOnly);
            if (acFiles.Length > 1)
                throw new ApplicationException("Unexpected number of AC files in " + folderName);

            if (acFiles.Length == 1)
                return acFiles[0];
            else
                return null;
        }

        public bool Equals(AltCmdFile that)
        {
            return this.BranchId.Equals(that?.BranchId);
        }

        public override int GetHashCode()
        {
            return BranchId.GetHashCode();
        }

        public AltCmdFile CreateCopy()
        {
            return (AltCmdFile)MemberwiseClone();
        }
    }
}
