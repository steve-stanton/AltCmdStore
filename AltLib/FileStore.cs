using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NLog;

namespace AltLib
{
    public class FileStore : CmdStore
    {
        /// <summary>
        /// The name of the file that holds root metadata for the command store.
        /// </summary>
        const string RootName = ".root";

        static Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Attempts to locate a single AC file in a specific folder.
        /// </summary>
        /// <param name="folderName">The folder to check.</param>
        /// <returns>The path for a single AC file present in the specified folder</returns>
        /// <exception cref="ApplicationException">The specified folder contains more than one AC file</exception>
        public static string GetAcFilePath(string folderName)
        {
            string[] acFiles = Directory.GetFiles(folderName, "*.ac", SearchOption.TopDirectoryOnly);
            if (acFiles.Length > 1)
                throw new ApplicationException("Unexpected number of AC files in " + folderName);

            if (acFiles.Length == 1)
                return acFiles[0];
            else
                return null;
        }

        internal static FileStore Create(CmdData args)
        {
            // Expand the supplied name to include the current working directory (or
            // expand a relative path)
            string enteredName = args.GetValue<string>(nameof(ICreateStore.Name));
            string name = Path.GetFileNameWithoutExtension(enteredName);
            string folderName = Path.GetFullPath(enteredName);

            // Disallow if the folder name already exists.

            // It may be worth relaxing this rule at some future date. The
            // reason for disallowing it is because an existing folder may
            // well contain sub-folders, but we also use sub-folders to
            // represent the branch hierarchy. So things would be a bit
            // mixed up. That said, it would be perfectly plausible to
            // place branch sub-folders under a separate ".ac" folder (in
            // much the same way as git). That might be worth considering
            // if we want to support a "working directory" like that
            // provided by git.

            if (Directory.Exists(folderName))
                throw new ApplicationException($"{folderName} already exists");

            // Confirm the folder is on a local drive
            if (!IsLocalDrive(folderName))
                throw new ApplicationException("Command stores can only be initialized on a local fixed drive");

            // Create the folder for the store (but if the folder already exists,
            // confirm that it does not already hold any AC files).
            if (Directory.Exists(folderName))
            {
                if (GetAcFilePath(folderName) != null)
                    throw new ApplicationException($"{folderName} has already been initialized");
            }
            else
            {
                Log.Info("Creating " + folderName);
                Directory.CreateDirectory(folderName);
            }

            Guid storeId = args.GetGuid(nameof(ICreateStore.StoreId));
            FileStore result = null;

            // If we're cloning, copy over the source data
            if (args.CmdName == nameof(ICloneStore))
            {
                // TODO: When cloning from a real remote, using wget might be a
                // good choice (but that doesn't really fit with what's below)

                ICloneStore cs = (args as ICloneStore);
                IRemoteStore rs = GetRemoteStore(cs);

                // Retrieve metadata for all remote branches
                BranchInfo[] acs = rs.GetBranches()
                                     .OrderBy(x => x.CreatedAt)
                                     .ToArray();

                var branchFolders = new Dictionary<Guid, string>();

                // Copy over all branches
                foreach (BranchInfo ac in acs)
                {
                    // Get the data for the branch (do this before we do any tweaking
                    // of the folder path from the AC file -- if the command supplier is
                    // a local file store, we won't be able to read the command data files
                    // after changing the folder name recorded in the AC)
                    IdRange range = new IdRange(ac.BranchId, 0, ac.CommandCount - 1);
                    CmdData[] data = rs.GetData(range).ToArray();

                    // TODO: what follows is very similar to the CopyIn method.
                    // Consider using that instead (means an empty FileStore needs
                    // to be created up front)

                    // Determine the output location for the AC file (relative to the
                    // location that should have already been defined for the parent)

                    if (!branchFolders.TryGetValue(ac.ParentId, out string parentFolder))
                    {
                        Debug.Assert(ac.ParentId.Equals(Guid.Empty));
                        parentFolder = folderName;
                    }

                    string dataFolder = Path.Combine(parentFolder, ac.BranchName);
                    branchFolders[ac.BranchId] = dataFolder;
                    SaveBranchInfo(dataFolder, ac);

                    // Copy over the command data
                    foreach (CmdData cd in data)
                        FileStore.WriteData(dataFolder, cd);
                }

                // If the origin is a folder on the local file system, ensure it's
                // saved as an absolute path (relative specs may confuse directory
                // navigation, depending on what the current directory is at the time)

                string origin = cs.Origin;
                if (Directory.Exists(origin))
                    origin = Path.GetFullPath(origin);

                // Save the root metadata
                var root = new RootInfo(storeId, name, rs.Id, origin);
                SaveRoot(folderName, root);

                // And suck it back up again
                string acSpec = Path.Combine(folderName, acs[0].BranchId + ".ac");
                result = FileStore.Load(acSpec);
            }
            else
            {
                // Create the AC file that represents the store root branch
                var ac = new BranchInfo(storeId: storeId,
                                        parentId: Guid.Empty,
                                        branchId: storeId,
                                        branchName: name,
                                        createdAt: args.CreatedAt);

                // Create a new root
                var root = new RootInfo(storeId, name, Guid.Empty);

                // Create the store and save it
                result = new FileStore(folderName,
                                       root,
                                       new BranchInfo[] { ac },
                                       ac.BranchId);

                // Save the AC file plus the root metadata
                FileStore.SaveBranchInfo(folderName, ac);
                result.SaveRoot();
            }

            return result;
        }

        /// <summary>
        /// The folder containing the master branch of this store.
        /// </summary>
        public string RootDirectoryName => Home;

        /// <summary>
        /// Copies in data from a remote branch.
        /// </summary>
        /// <param name="ac">The branch metadata received from a remote store
        /// (this will be used to replace any metadata already held locally).</param>
        /// <param name="data">The command data to append to the local branch.</param>
        /// <param name="altName">An alternative name to assign to the branch.</param>
        /// <exception cref="ApplicationException">
        /// Attempt to import remote data into a local branch</exception>
        public override void CopyIn(BranchInfo ac, CmdData[] data, string altName = null)
        {
            // The incoming data can only come from a remote store
            Debug.Assert(!ac.StoreId.Equals(this.Id));

            // Define the local location for the AC file (relative to the
            // location that should have already been defined for the parent).
            // There could be no parent if we're copying in command data from
            // the root branch.

            string dataFolder;
            Branch parent;

            if (ac.ParentId.Equals(Guid.Empty))
            {
                parent = null;
                dataFolder = RootDirectoryName;
            }
            else
            {
                parent = FindBranch(ac.ParentId);
                if (parent == null)
                    throw new ApplicationException("Cannot find parent branch " + ac.ParentId);

                string parentDir = GetBranchDirectoryName(parent);
                dataFolder = Path.Combine(parentDir, altName ?? ac.BranchName);
            }

            // Save the supplied AC in its new location (if the branch already
            // exists locally, this will overwrite the AC)
            SaveBranchInfo(dataFolder, ac);

            Branch branch = FindBranch(ac.BranchId);

            if (branch == null)
            {
                Log.Trace($"Copying {data.Length} commands to {dataFolder}");

                Debug.Assert(data[0].Sequence == 0);
                Debug.Assert(data[0].CmdName == nameof(ICreateBranch));
                Debug.Assert(parent != null);

                // Create the new branch
                branch = new Branch(this, ac) {Parent = parent};
                parent.Children.Add(branch);
                Branches.Add(ac.BranchId, branch);
            }
            else
            {
                Log.Trace($"Appending {data.Length} commands to {dataFolder}");

                if (!branch.IsRemote)
                    throw new ApplicationException("Attempt to import remote data into a local branch");

                // Replace the cached metadata
                branch.Info = ac;
            }

            // Copy over the command data
            foreach (CmdData cd in data)
                WriteData(dataFolder, cd);
        }

        /// <summary>
        /// Obtains some sort of channel to the store that acts
        /// as the origin for the clone.
        /// </summary>
        /// <returns>Something that provides the command structure
        /// for a store. This implementation just returns a supplier
        /// that corresponds to a local file store.</returns>
        static IRemoteStore GetRemoteStore(ICloneStore cs)
        {
            string origin = cs.Origin;
            string acPath = GetAcFilePath(origin);

            if (acPath == null)
                throw new ApplicationException("Cannot locate any AC file in source");

            var result = FileStore.Load(acPath);
            Log.Info($"Cloning from {result.Name}");
            return result;
        }

        /// <summary>
        /// Loads a command store using the metadata for one of the branches
        /// in the store.
        /// </summary>
        /// <param name="acSpec">The path to an AC file within the store (to
        /// define as the "current" branch in the returned store).</param>
        /// <returns>The command store that contains the specified AC file.</returns>
        public static FileStore Load(string acSpec)
        {
            // Confirm that the supplied file really is an AC file.
            var ac = ReadAc(acSpec);

            // Locate root metadata
            string dirPath = Path.GetDirectoryName(acSpec);
            var dirInfo = new DirectoryInfo(dirPath);
            if (dirInfo == null)
                throw new ApplicationException("Cannot locate root folder");

            var root = ReadRoot(dirInfo.FullName);

            // Load all AC files in the tree
            BranchInfo[] acs = Load(dirInfo).OrderBy(x => x.CreatedAt).ToArray();
            //Log.Info($"Loaded {acs.Length} AC files");

            return new FileStore(dirInfo.FullName, root, acs, ac.BranchId);
        }

        /// <summary>
        /// Attempts to locate root metadata (by walking up the directory tree).
        /// </summary>
        /// <param name="dirInfo">Information for the directory to check first</param>
        /// <returns>Information for the directory that contains root metadata (null
        /// if it could not be found).</returns>
        static DirectoryInfo FindRootDirectory(DirectoryInfo dirInfo)
        {
            for (var d = dirInfo; d != null; d = d.Parent)
            {
                string fileName = Path.Combine(d.FullName, RootName);
                if (File.Exists(fileName))
                    return d;
            }

            return null;
        }

        /// <summary>
        /// Loads root metadata from the specified folder.
        /// </summary>
        /// <param name="folderName">The path for the root folder of a command store.</param>
        /// <returns>The root metadata in the specified folder.</returns>
        static RootInfo ReadRoot(string folderName)
        {
            string fileName = Path.Combine(folderName, RootName);
            string data = File.ReadAllText(fileName);
            return JsonConvert.DeserializeObject<RootInfo>(data);
        }

        /// <summary>
        /// Locates all AC files within a command store.
        /// </summary>
        /// <param name="rootFolder">The root directory for the store.
        /// </param>
        /// <returns>
        /// The AC files present in the directory tree beneath the specified root.
        /// </returns>
        static IEnumerable<BranchInfo> Load(DirectoryInfo rootFolder)
        {
            foreach (FileInfo f in rootFolder.EnumerateFiles("*.ac", SearchOption.AllDirectories))
            {
                yield return ReadAc(f.FullName);
            }
        }

        /// <summary>
        /// Reads a file containing an instance of <see cref="BranchInfo"/>
        /// </summary>
        /// <param name="fileName">The file to read (could
        /// potentially be a relative file specification)</param>
        /// <returns>The content of the file</returns>
        static BranchInfo ReadAc(string fileName)
        {
            string data = File.ReadAllText(fileName);
            return JsonConvert.DeserializeObject<BranchInfo>(data);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileStore"/> class.
        /// </summary>
        /// <param name="rootFolder">The folder containing the master branch of this store.</param>
        /// <param name="rootInfo">Root metadata for this store.</param>
        /// <param name="branches">The branches in the store (not null or empty)</param>
        /// <param name="currentId">The ID of the currently checked out branch</param>
        internal FileStore(string rootFolder,
                           RootInfo rootInfo,
                           BranchInfo[] branches,
                           Guid currentId)
            : base(rootFolder, rootInfo, branches, currentId)
        {
        }

        /// <summary>
        /// Reads the command data for a range of commands in a specific branch.
        /// </summary>
        /// <param name="branch">Details for the branch to read from.</param>
        /// <param name="minCmd">The sequence number of the first command to be read.</param>
        /// <param name="maxCmd">The sequence number of the last command to be read</param>
        /// <returns>The commands in the specified range (ordered by their data entry sequence).
        /// </returns>
        public override IEnumerable<CmdData> ReadData(Branch branch, uint minCmd, uint maxCmd)
        {
            string dataFolder = GetBranchDirectoryName(branch);

            for (uint i = minCmd; i <= maxCmd; i++)
            {
                string filePath = Path.Combine(dataFolder, $"{i}.json");
                if (!File.Exists(filePath))
                    throw new ArgumentException("No such file: " + filePath);

                string data = File.ReadAllText(filePath);
                yield return JsonConvert.DeserializeObject<CmdData>(data);
            }
        }

        /// <summary>
        /// Persists command data as part of a specific branch.
        /// </summary>
        /// <param name="branch">The branch the data relates to</param>
        /// <param name="data">The data to be written</param>
        internal override void WriteData(Branch branch, CmdData data)
        {
            string dataFolder = GetBranchDirectoryName(branch);
            WriteData(dataFolder, data);
        }

        /// <summary>
        /// Persists command data as part of a specific folder.
        /// </summary>
        /// <param name="dataFolder">The path for the folder to write to</param>
        /// <param name="data">The data to be written</param>
        public static void WriteData(string dataFolder, CmdData data)
        {
            string dataPath = Path.Combine(dataFolder, $"{data.Sequence}.json");
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(dataPath, json);
        }

        /// <summary>
        /// Saves the metadata for a branch that is part of this store.
        /// </summary>
        /// <param name="branch">The branch to be saved.</param>
        public override void SaveBranchInfo(Branch branch)
        {
            string folderName = GetBranchDirectoryName(branch);
            SaveBranchInfo(folderName, branch.Info);
        }

        /// <summary>
        /// Saves the metadata for a branch that is part of this store.
        /// </summary>
        /// <param name="folderName">The path for the folder that holds branch data.</param>
        /// <param name="ac">The branch metadata to be saved.</param>
        internal static void SaveBranchInfo(string folderName, BranchInfo ac)
        {
            Directory.CreateDirectory(folderName);
            string data = JsonConvert.SerializeObject(ac, Formatting.Indented);
            string fileName = Path.Combine(folderName, ac.BranchId + ".ac");
            File.WriteAllText(fileName, data);
        }

        /// <summary>
        /// Saves the supplied root metadata as part of this store.
        /// </summary>
        public override void SaveRoot()
        {
            SaveRoot(RootDirectoryName, Root);
        }

        static void SaveRoot(string rootDirectoryName, RootInfo root)
        {
            if (rootDirectoryName == null)
                throw new ArgumentNullException("Cannot save root because location is undefined");

            Directory.CreateDirectory(rootDirectoryName);
            string data = JsonConvert.SerializeObject(root, Formatting.Indented);
            string fileName = Path.Combine(rootDirectoryName, RootName);
            File.WriteAllText(fileName, data);
        }

        /// <summary>
        /// Obtains the full path for the AC file used to hold metadata for a branch.
        /// </summary>
        /// <param name="b">The branch of interest.</param>
        /// <returns>The full path for the AC file</returns>
        internal string GetAcPath(Branch b)
        {
            return Path.Combine(GetBranchDirectoryName(b), b.Id + ".ac");
        }

        /// <summary>
        /// Obtains the full path for the folder that holds branch data.
        /// </summary>
        /// <param name="b">The branch of interest.</param>
        /// <returns>The folder that contains the branch</returns>
        internal string GetBranchDirectoryName(Branch b)
        {
            return Path.Combine(RootDirectoryName, b.GetBranchPath(true, @"\"));
        }

        /// <summary>
        /// Remembers <see cref="Current"/> as the most recently loaded branch.
        /// </summary>
        public override void SaveCurrent()
        {
            // Remember the current branch for the current store
            // in app data (last branch, regardless of which store)
            string progData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string last = Path.Combine(progData, "AltCmd", "last");
            string acSpec = GetAcPath(Current);
            File.WriteAllText(last, acSpec);

            Log.Trace($"Current branch set to {Current}");
        }
    }
}
