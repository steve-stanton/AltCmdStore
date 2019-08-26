using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;

namespace AltLib
{
    public class FileStore : CmdStore
    {
        static Logger Log = LogManager.GetCurrentClassLogger();

        static internal FileStore Create(CmdData args)
        {
            // Expand the supplied name to include the current working directory (or
            // expand a relative path)
            string name = args.GetValue<string>(nameof(ICreateStore.Name));
            string folderName = Path.GetFullPath(name);

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
                if (AltCmdFile.GetAcPath(folderName) != null)
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
                AltCmdFile[] acs = rs.GetBranches()
                                     .OrderBy(x => x.CreatedAt)
                                     .ToArray();

                var acsByBranch = acs.ToDictionary(x => x.BranchId);

                // Copy over all branches
                foreach (AltCmdFile ac in acs)
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

                    // Define the output location for the AC file (relative to the
                    // location that should have already been defined for the parent)

                    string dataFolder = null;
                    if (acsByBranch.TryGetValue(ac.ParentId, out AltCmdFile parentAc))
                    {
                        dataFolder = Path.Combine(parentAc.DirectoryName, ac.BranchName);
                        Directory.CreateDirectory(dataFolder);
                    }
                    else
                    {
                        Debug.Assert(ac.ParentId.Equals(Guid.Empty));
                        dataFolder = folderName;
                    }

                    ac.FileName = Path.Combine(dataFolder, ac.BranchId + ".ac");
                    string acData = JsonConvert.SerializeObject(ac, Formatting.Indented);
                    File.WriteAllText(ac.FileName, acData);

                    // Copy over the command data
                    foreach (CmdData cd in data)
                        FileStore.WriteData(dataFolder, cd);
                }

                // Save the root metadata
                var root = new RootFile(storeId, rs.Id, cs.Origin);
                root.DirectoryName = folderName;
                FileStore.SaveRoot(root);

                // And suck it back up again
                result = FileStore.Load(acs[0].FileName);
            }
            else
            {
                // Create the AC file that represents the store root branch
                var ac = new AltCmdFile(storeId: storeId,
                                        branchId: storeId,
                                        createdAt: args.CreatedAt,
                                        commandCount: 0,
                                        parentId: Guid.Empty,
                                        refreshCount: 0,
                                        refreshDiscount: 0,
                                        parentCount: 0,
                                        parentDiscount: 0);

                // Create a new root
                var root = new RootFile(storeId, Guid.Empty);

                // Create the store and save it
                ac.FileName = Path.Combine(folderName, storeId + ".ac");
                root.DirectoryName = folderName;

                result = new FileStore(root,
                                       new AltCmdFile[] { ac },
                                       ac.BranchId);

                // Save the AC file plus the root metadata
                result.Save(ac);
                result.SaveRoot();
            }

            return result;
        }

        /// <summary>
        /// Copies in data from a remote branch.
        /// </summary>
        /// <param name="ac">The branch metadata received from a remote store
        /// (this will be used to replace any metadata already held locally).</param>
        /// <param name="data">The command data to append to the local branch.</param>
        /// <exception cref="ApplicationException">
        /// Attempt to import remote data into a local branch</exception>
        public override void CopyIn(AltCmdFile ac, CmdData[] data)
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
                dataFolder = Root.DirectoryName;
            }
            else
            {
                parent = FindBranch(ac.ParentId);
                if (parent == null)
                    throw new ApplicationException("Cannot find parent branch " + ac.ParentId);

                dataFolder = Path.Combine(parent.Info.DirectoryName, ac.BranchName);
            }

            // Save the supplied AC in its new location (if the branch already
            // exists locally, this will overwrite the AC)
            ac.FileName = Path.Combine(dataFolder, ac.BranchId + ".ac");
            Save(ac);

            Branch branch = FindBranch(ac.BranchId);

            if (branch == null)
            {
                Log.Trace($"Copying {data.Length} commands to {ac.DirectoryName}");

                Debug.Assert(data[0].Sequence == 0);
                Debug.Assert(data[0].CmdName == nameof(ICreateBranch));
                Debug.Assert(parent != null);

                // Create the new branch
                branch = new Branch(this, ac);
                branch.Parent = parent;
                parent.Children.Add(branch);
                Branches.Add(ac.BranchId, branch);
            }
            else
            {
                Log.Trace($"Appending {data.Length} commands to {ac.DirectoryName}");

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
            string acPath = AltCmdFile.GetAcPath(origin);

            if (acPath == null)
                throw new ApplicationException("Cannot locate any AC file in source");

            var result = FileStore.Load(acPath);
            Log.Info($"Cloning from {result.Name}");
            return result;
        }

        /// <summary>
        /// Checks whether a folder is on a local fixed drive.
        /// </summary>
        /// <param name="folderPath">The path for the folder to be checked.</param>
        /// <returns>
        /// True if the specified folder is on a local fixed drive.
        /// </returns>
        static bool IsLocalDrive(string folderPath)
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
        /// Loads a command store using the metadata for one of the branches
        /// in the store.
        /// </summary>
        /// <param name="acSpec">The path to an AC file within the store (to
        /// define as the "current" branch in the returned store).</param>
        /// <returns>The command store that contains the specified AC file.</returns>
        static public FileStore Load(string acSpec)
        {
            // Confirm that the supplied file really is an AC file.
            var ac = ReadAc(acSpec);

            // Locate root metadata
            var dirInfo = new DirectoryInfo(ac.DirectoryName);
            dirInfo = FindRootDirectory(dirInfo);
            var root = FindRoot(dirInfo);

            // Load all AC files in the tree
            AltCmdFile[] acs = Load(dirInfo).OrderBy(x => x.CreatedAt).ToArray();
            //Log.Info($"Loaded {acs.Length} AC files");

            return new FileStore(root, acs, ac.BranchId);
        }

        static RootFile FindRoot(DirectoryInfo dirInfo)
        {
            DirectoryInfo rootDir = FindRootDirectory(dirInfo);

            if (rootDir == null)
                return null;
            else
                return ReadRoot(rootDir.FullName);
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
                string fileName = Path.Combine(d.FullName, RootFile.Name);
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
        static RootFile ReadRoot(string folderName)
        {
            string fileName = Path.Combine(folderName, RootFile.Name);
            string data = File.ReadAllText(fileName);
            var result = JsonConvert.DeserializeObject<RootFile>(data);
            result.DirectoryName = folderName;
            return result;
        }

        /// <summary>
        /// Locates all AC files within a command store.
        /// </summary>
        /// <param name="rootFolder">The root directory for the store.
        /// </param>
        /// <returns>
        /// The AC files present in the directory tree beneath the specified root.
        /// </returns>
        static IEnumerable<AltCmdFile> Load(DirectoryInfo rootFolder)
        {
            foreach (FileInfo f in rootFolder.EnumerateFiles("*.ac", SearchOption.AllDirectories))
            {
                yield return ReadAc(f.FullName);
            }
        }

        /// <summary>
        /// Reads a file containing an instance of <see cref="AltCmdFile"/>
        /// </summary>
        /// <param name="fileName">The file to read (could
        /// potentially be a relative file specification)</param>
        /// <returns>The content of the file</returns>
        static public AltCmdFile ReadAc(string fileName)
        {
            string data = File.ReadAllText(fileName);
            var result = JsonConvert.DeserializeObject<AltCmdFile>(data);
            result.FileName = Path.GetFullPath(fileName);
            return result;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileStore"/> class.
        /// </summary>
        /// <param name="rootInfo">Root metadata for this store.</param>
        /// <param name="branches">The branches in the store (not null or empty)</param>
        /// <param name="currentId">The ID of the currently checked out branch</param>
        internal FileStore(RootFile rootInfo,
                           AltCmdFile[] branches,
                           Guid currentId)
            : base(rootInfo, branches, currentId)
        {
        }

        /// <summary>
        /// Reads the command data for a specific command in
        /// the current branch.
        /// </summary>
        /// <param name="branch">Details for the branch to read from.</param>
        /// <param name="sequence">The 0-based sequence number of
        /// the command to read.</param>
        /// <returns>The corresponding command data</returns>
        public override CmdData ReadData(Branch branch, uint sequence)
        {
            string dataPath = Path.Combine(branch.Info.DirectoryName, $"{sequence}.json");
            if (!File.Exists(dataPath))
                throw new ArgumentException("No such file: " + dataPath);

            string data = File.ReadAllText(dataPath);
            return JsonConvert.DeserializeObject<CmdData>(data);
        }

        /// <summary>
        /// Persists command data as part of the current branch.
        /// </summary>
        /// <param name="branch">The branch the data relates to</param>
        /// <param name="data">The data to be written</param>
        internal override void WriteData(Branch branch, CmdData data)
        {
            WriteData(branch.Info.DirectoryName, data);
        }

        /// <summary>
        /// Persists command data as part of a specific folder.
        /// </summary>
        /// <param name="dataFolder">The path for the folder to write to</param>
        /// <param name="data">The data to be written</param>
        static void WriteData(string dataFolder, CmdData data)
        {
            string dataPath = Path.Combine(dataFolder, $"{data.Sequence}.json");
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(dataPath, json);
        }

        /// <summary>
        /// Saves the supplied branch metadata as part of this store.
        /// </summary>
        /// <param name="ac">The metadata to be saved</param>
        public override void Save(AltCmdFile ac)
        {
            if (ac.FileName == null)
                throw new ArgumentNullException("Cannot save AC file because name is undefined");

            Directory.CreateDirectory(ac.DirectoryName);
            string data = JsonConvert.SerializeObject(ac, Formatting.Indented);
            File.WriteAllText(ac.FileName, data);
        }

        /// <summary>
        /// Saves the supplied root metadata as part of this store.
        /// </summary>
        public override void SaveRoot()
        {
            SaveRoot(Root);
        }

        static void SaveRoot(RootFile root)
        {
            if (root.DirectoryName == null)
                throw new ArgumentNullException("Cannot save root because location is undefined");

            Directory.CreateDirectory(root.DirectoryName);
            string data = JsonConvert.SerializeObject(root, Formatting.Indented);
            string fileName = Path.Combine(root.DirectoryName, RootFile.Name);
            File.WriteAllText(fileName, data);
        }
    }
}
