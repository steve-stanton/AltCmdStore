﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NLog;

namespace AltLib
{
    abstract public class CmdStore : IRemoteStore
    {
        static Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Some sort of identifier relating to the place where the
        /// store resides (derived classes know what this actually means)
        /// </summary>
        protected string Home { get; }

        /// <summary>
        /// Store metadata
        /// </summary>
        public StoreInfo Store { get; }

        /// <summary>
        /// The unique ID for this store.
        /// </summary>
        public Guid Id => Store.StoreId;

        /// <summary>
        /// A user-perceived name for this store.
        /// </summary>
        public string Name => Store.Name;

        /// <summary>
        /// The local branches known to this store (keyed by the branch ID).
        /// </summary>
        /// <remarks>
        /// Refers only to those branches that have been checked out
        /// locally (excludes branches cloned from a remote).
        /// </remarks>
        public Dictionary<Guid, Branch> Branches { get; }

        /// <summary>
        /// The branch that is currently checked out (not null)
        /// </summary>
        public Branch Current { get; internal set; }

        /// <summary>
        /// The command stream corresponding to the <see cref="Current"/> branch.
        /// </summary>
        public CmdStream Stream { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="CmdStore"/> that
        /// represents a brand new command store.
        /// </summary>
        /// <param name="storeName">The name for the new store (could be a directory
        /// path if <paramref name="storeType"/> is <see cref="StoreType.File"/>)</param>
        /// <param name="storeType">The type of store to create.</param>
        /// <returns>The newly created command store.</returns>
        public static CmdStore Create(string storeName, StoreType storeType)
        {
            Guid storeId = Guid.NewGuid();

            var c = new CmdData(nameof(ICreateStore), 0, DateTime.UtcNow);
            c.Add(nameof(ICreateStore.StoreId), storeId);
            c.Add(nameof(ICreateStore.Name), storeName);
            c.Add(nameof(ICreateStore.Type), storeType);

            var handler = new CreateStoreHandler(c);
            var ec = new ExecutionContext();
            handler.Process(ec);
            return ec.Store;
        }

        public static CmdStore CreateStore(CmdData args)
        {
            StoreType type = args.GetEnum<StoreType>(nameof(ICreateStore.Type));

            if (type == StoreType.File)
                return FileStore.Create(args);

            if (type == StoreType.SQLite)
                return SQLiteStore.Create(args);

            if (type == StoreType.Memory)
                return MemoryStore.Create(args);

            throw new NotImplementedException(type.ToString());
        }

        /// <summary>
        /// Checks whether a folder is on a local fixed drive.
        /// </summary>
        /// <param name="folderPath">The path for the folder to be checked.</param>
        /// <returns>
        /// True if the specified folder is on a local fixed drive.
        /// </returns>
        protected static bool IsLocalDrive(string folderPath)
        {
            string root = Path.GetPathRoot(folderPath);

            foreach (DriveInfo d in DriveInfo.GetDrives())
            {
                if (d.DriveType == DriveType.Fixed)
                {
                    // The folder name may have been supplied by the user
                    if (d.Name.EqualsIgnoreCase(root))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CmdStore"/> class.
        /// </summary>
        /// <param name="home">Some sort of identifier relating to the place where
        /// the store resides</param>
        /// <param name="storeInfo">Store metadata.</param>
        /// <param name="acInfo">Metadata for the branches in the store</param>
        /// <param name="currentId">The ID of the currently checked out branch</param>
        protected CmdStore(string home,
                           StoreInfo storeInfo,
                           BranchInfo[] acInfo,
                           Guid currentId)
        {
            if (acInfo == null || acInfo.Length == 0)
                throw new ArgumentException(nameof(acInfo));

            Home = home;

            // Obtain the store name from the root folder
            Store = storeInfo;
            Log.Trace($"Initializing store {Id}, Name={Name}");

            Branches = acInfo.ToDictionary(x => x.BranchId,
                                           x => new Branch(this, x));

            // Create branch objects and form parent/child relationships
            foreach (Branch b in Branches.Values)
            {
                Guid parentId = b.Info.ParentId;

                if (Branches.TryGetValue(parentId, out Branch p))
                {
                    b.Parent = p;
                    p.Children.Add(b);
                }
                else
                {
                    // We should have located the parent for all branches
                    // except for the root branch
                    if (!Guid.Empty.Equals(parentId))
                        throw new ApplicationException($"Could not locate parent for {b.Info.BranchId}");
                }
            }

            if (!Branches.TryGetValue(currentId, out Branch current))
                throw new ApplicationException($"Cannot locate current branch");

            Current = current;
        }

        /// <summary>
        /// Switches to (checks out) a different branch.
        /// </summary>
        public void SwitchTo(Branch branch)
        {
            Current = branch;
            SaveCurrent();
            Stream = branch.CreateStream();
        }

        /// <summary>
        /// Remembers <see cref="Current"/> as the most recently loaded branch.
        /// </summary>
        public abstract void SaveCurrent();

        /// <summary>
        /// Reads the command data for a range of commands in a specific branch.
        /// </summary>
        /// <param name="branch">Details for the branch to read from.</param>
        /// <param name="minCmd">The sequence number of the first command to be read.</param>
        /// <param name="maxCmd">The sequence number of the last command to be read</param>
        /// <returns>The commands in the specified range (ordered by their data entry sequence).
        /// </returns>
        public abstract IEnumerable<CmdData> ReadData(Branch branch, uint minCmd, uint maxCmd);

        /// <summary>
        /// Persists command data as part of the current branch.
        /// </summary>
        /// <param name="branch">The branch the data relates to</param>
        /// <param name="data">The data to be written</param>
        /// <remarks>
        /// To be called only by <see cref="Branch.SaveData"/>
        /// </remarks>
        internal abstract void WriteData(Branch branch, CmdData data);

        /// <summary>
        /// Checks whether the name supplied for a new branch is valid.
        /// </summary>
        /// <param name="name">The name to be checked.</param>
        /// <returns>True if the specified name only contains characters
        /// that are valid for file names.</returns>
        /// <remarks>
        /// This does not check whether the name is a duplicate of
        /// another sibling branch.
        /// </remarks>
        public virtual bool IsValidBranchName(string name)
        {
            // Only allow names that are valid file names
            char[] badChars = Path.GetInvalidFileNameChars();
            return name.Any(x => !badChars.Contains(x));
        }

        /// <summary>
        /// Saves the metadata for a branch that is part of this store.
        /// </summary>
        /// <param name="branch">The branch to be saved.</param>
        public abstract void SaveBranchInfo(Branch branch);

        /// <summary>
        /// Saves the store metadata.
        /// </summary>
        abstract public void SaveStoreInfo();

        /// <summary>
        /// Attempts to locate a branch given it's internal branch ID.
        /// </summary>
        /// <param name="branchId">The internal ID of the branch to find.</param>
        /// <returns>The branch with the specified ID (null if not found)</returns>
        public Branch FindBranch(Guid branchId)
        {
            if (Branches.TryGetValue(branchId, out Branch result))
                return result;
            else
                return null;
        }

        /// <summary>
        /// Obtains metadata for a specific branch.
        /// </summary>
        /// <param name="branchId">The ID of the branch to retrieve.</param>
        /// <returns>Metadata for the branch (null if not found).</returns>
        BranchInfo IRemoteStore.GetBranchInfo(Guid branchId)
        {
            return FindBranch(branchId)?.Info;
        }

        /// <summary>
        /// Obtains a collection of all branches known to
        /// the supplier.
        /// </summary>
        /// <returns>The branches known to the supply
        /// (the order you get items back is not specified, it could be
        /// entirely random).</returns>
        IEnumerable<BranchInfo> IRemoteStore.GetBranches()
        {
            return Branches.Values.Select(x => x.Info);
        }

        /// <summary>
        /// Obtains the command data for a branch.
        /// </summary>
        /// <param name="range">The range of data to retrieve from a branch.</param>
        /// <returns>
        /// The command data for the branch (in the order they were created).
        /// </returns>
        IEnumerable<CmdData> IRemoteStore.GetData(IdRange range)
        {
            Branch b = FindBranch(range.Id);
            if (b == null)
                throw new ArgumentException(nameof(range.Id));

            // If the branch hasn't been loaded already, do it now
            bool doLoad = b.Commands.Count == 0;

            // Ensure we've got a sufficient number of commands loaded
            // TODO: When doing a fetch, we will seldom need to load from the beginning
            b.Load(range.Max + 1);

            // Cycle through the commands in the branch
            for (uint i = range.Min; i <= range.Max; i++)
                yield return b.Commands[(int)i];

            // Release the command data if we had to load it
            if (doLoad)
                b.Unload();
        }

        /// <summary>
        /// Obtains the number of commands that exist in each branch of this store
        /// </summary>
        /// <returns>The command counts for every branch.</returns>
        IEnumerable<IdCount> GetBranchCounts()
        {
            return Branches.Values
                           .Select(x => new IdCount(x.Id, x.Info.CommandCount));
        }

        /// <summary>
        /// Obtains command ranges that are missing in another store.
        /// </summary>
        /// <param name="callerId">The ID of the command store making the request</param>
        /// <param name="callerHas">The number of commands that are currently present
        /// in each branch of the calling store (in any order).</param>
        /// <param name="isFetch">Specify <c>true</c> if the request is to satisfy a fetch
        /// request (in that case, new branches in this store should be included in the
        /// results). Specify <c>false</c> if the request is for a push.
        /// </param>
        /// <returns>The commands in this store that are not in the other store,
        /// and vice versa.</returns>
        IdRange[] IRemoteStore.GetMissingRanges(Guid callerId, IdCount[] callerHas, bool isFetch)
        {
            return GetMissingRanges(callerId, callerHas, isFetch).ToArray();
        }

        IEnumerable<IdRange> GetMissingRanges(Guid callerId, IdCount[] callerHas, bool isFetch)
        {
            Dictionary<Guid, uint> callerCounts = callerHas.ToDictionary(x => x.Id, x => x.Count);

            // Cycle through every branch in the current store
            foreach (Branch b in Branches.Values)
            {
                if (callerCounts.TryGetValue(b.Id, out uint callerCount))
                {
                    callerCounts.Remove(b.Id);

                    // The current store contains the same branch. If fetching, return the
                    // extra stuff present in this store. If pushing, return the extra stuff
                    // in the store that's pushing.

                    uint localCount = b.Info.CommandCount;

                    if (isFetch && localCount > callerCount)
                        yield return new IdRange(b.Id, callerCount - 1, localCount - 1);
                    else if (!isFetch && localCount < callerCount)
                        yield return new IdRange(b.Id, localCount - 1, callerCount - 1);
                }
                else
                {
                    // The current store has a new branch (not present as a remote branch
                    // in the other store). Return it if we're handling a fetch, but only
                    // if it doesn't originate with the requesting store.

                    if (isFetch && !b.Info.StoreId.Equals(callerId))
                        yield return new IdRange(b.Id, 0, b.Info.CommandCount - 1);
                }
            }

            // If we're handling a push request, return info for the branches
            // that are present in the calling store, but not in the local store.
            if (!isFetch)
            {
                foreach (KeyValuePair<Guid, uint> kvp in callerCounts)
                    yield return new IdRange(kvp.Key, 0, kvp.Value - 1);
            }
        }

        /// <summary>
        /// Copies in data from a new remote branch.
        /// </summary>
        /// <param name="ac">The branch metadata received from a remote store.</param>
        /// <param name="data">The command data for the branch</param>
        /// <param name="altName">An alternative name to assign to the branch.</param>
        public virtual void CopyIn(BranchInfo ac, CmdData[] data, string altName = null)
        {
            // Currently implemented only by FileStore
            throw new NotImplementedException();
        }

        /// <summary>
        /// Accepts data from another command store.
        /// </summary>
        /// <param name="source">A name that identifies the command store that is
        /// the source of the data.</param>
        /// <param name="ac">The metadata for the branch the commands are part of.</param>
        /// <param name="data">The command data to be appended to the remote branch.</param>
        void IRemoteStore.Push(string source, BranchInfo ac, CmdData[] data)
        {
            // Clone the supplied metadata (if the call actually comes from
            // the current application, we don't want to mutate the metadata)
            BranchInfo acCopy = ac.CreateCopy();

            string altName = acCopy.BranchName == "+" ? source : null;
            CopyIn(acCopy, data, altName);
        }
    }
}
