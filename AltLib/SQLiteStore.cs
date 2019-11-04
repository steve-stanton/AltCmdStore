using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NLog;

namespace AltLib
{
    // See https://stackoverflow.com/questions/51933421/system-data-sqlite-vs-microsoft-data-sqlite
    // See https://www.c-sharpcorner.com/article/getting-started-with-sqlite/
    public class SQLiteStore : CmdStore
    {
        // Names for AltCmd system tables
        const string PropertiesTableName = "Properties";
        const string BranchesTableName = "Branches";

        static Logger Log = LogManager.GetCurrentClassLogger();

        internal static SQLiteStore Create(CmdData args)
        {
            // Disallow store names that correspond to tables in the database (bear
            // in mind that the entered name may or may not include a directory path)
            string enteredName = args.GetValue<string>(nameof(ICreateStore.Name));
            string name = Path.GetFileNameWithoutExtension(enteredName);
            if (IsReservedName(name))
                throw new ApplicationException("Store name not allowed");

            // Expand the supplied name to include the current working directory (or
            // expand a relative path)
            string fullSpec = Path.GetFullPath(enteredName);
            string fileType = Path.GetExtension(fullSpec);
            if (String.IsNullOrEmpty(fileType))
                fullSpec += ".ac-sqlite";

            // Disallow if the database file already exists
            if (File.Exists(fullSpec))
                throw new ApplicationException($"{fullSpec} already exists");

            // Confirm the file is on a local drive
            if (!IsLocalDrive(fullSpec))
                throw new ApplicationException("Command stores can only be initialized on a local fixed drive");

            // Ensure the folder exists
            string folderName = Path.GetDirectoryName(fullSpec);
            if (!Directory.Exists(folderName))
            {
                Log.Trace("Creating " + folderName);
                Directory.CreateDirectory(folderName);
            }

            Guid storeId = args.GetGuid(nameof(ICreateStore.StoreId));
            SQLiteStore result = null;

            if (args.CmdName == nameof(ICloneStore))
            {
                // Copy the SQLite database
                // TODO: To handle database files on remote machines
                ICloneStore cs = (args as ICloneStore);
                Log.Info($"Copying {cs.Origin} to {fullSpec}");
                File.Copy(cs.Origin, fullSpec);

                // Load the copied store
                result = SQLiteStore.Load(fullSpec, cs);
            }
            else
            {
                Log.Info("Creating " + fullSpec);
                SQLiteDatabase db = CreateDatabase(fullSpec);

                // Create the AC file that represents the store root branch
                var ac = new AltCmdFile(storeId: storeId,
                    parentId: Guid.Empty,
                    branchId: storeId,
                    createdAt: args.CreatedAt);

                // Fake a file name
                ac.FileName = Path.Combine(folderName, name, $"{storeId}.ac");

                // Create a new root
                var root = new RootFile(storeId, Guid.Empty);
                root.DirectoryName = Path.Combine(folderName, name);

                // Create the store and save it
                result = new SQLiteStore(
                                root,
                                new AltCmdFile[] { ac },
                                ac.BranchId, 
                                db);

                // Save the info for the master branch plus the root metadata
                result.Save(ac);
                result.SaveRoot();

                // The last branch is the root of the new database
                result.SaveProperty(PropertyNaming.LastBranch, storeId.ToString());
            }

            return result;
        }

        SQLiteDatabase Database { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLiteStore"/> class.
        /// </summary>
        /// <param name="rootInfo">Root metadata for this store.</param>
        /// <param name="branches">The branches in the store (not null or empty)</param>
        /// <param name="currentId">The ID of the currently checked out branch</param>
        /// <param name="db">The database that holds the store content</param>
        internal SQLiteStore(RootFile rootInfo,
                             AltCmdFile[] branches,
                             Guid currentId,
                             SQLiteDatabase db)
            : base(rootInfo, branches, currentId)
        {
            Database = db;
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
            var q = new CmdDataQuery(branch.Id, minCmd, maxCmd);

            foreach (CmdData result in Database.ExecuteQuery(q))
                yield return result;
        }

        /// <summary>
        /// Saves the supplied branch metadata as part of this store.
        /// </summary>
        /// <param name="ac">The metadata to be saved</param>
        /// <exception cref="ArgumentNullException">Cannot save because name is undefined</exception>
        public override void Save(AltCmdFile ac)
        {
            if (ac.FileName == null)
                throw new ArgumentNullException("Cannot save because name is undefined");

            string data = JsonConvert.SerializeObject(ac, Formatting.Indented);
            string sql;
            int nRows = 0;

            if (ac.CommandCount == 0)
            {
                Database.ExecuteTransaction(() =>
                {
                    // Create the data table for the new branch
                    sql = $"CREATE TABLE [{ac.BranchId}] " +
                          "(Sequence INTEGER NOT NULL PRIMARY KEY" +
                          ",Data JSON NOT NULL)";

                    Database.ExecuteNonQuery(sql);

                    // Create a view with a matching name (this is not needed by the
                    // software, but may make it easier to debug things)
                    string viewName = GetDataViewName(ac.BranchName);
                    sql = $"CREATE VIEW [{viewName}] AS SELECT * FROM [{ac.BranchId}]";
                    Database.ExecuteNonQuery(sql);

                    // Record branch metadata
                    sql = $"INSERT INTO Branches (Id,Name,CreatedAt,Data) VALUES " +
                          $"('{ac.BranchId}', '{ac.BranchName}', '{ac.CreatedAt:o}', '{data}')";

                    nRows = Database.ExecuteNonQuery(sql);
                });
            }
            else
            {
                sql = $"UPDATE Branches SET Name='{ac.BranchName}', Data='{data}' " +
                      $"WHERE Id='{ac.BranchId}'";
                nRows = Database.ExecuteNonQuery(sql);
            }

            if (nRows != 1)
                throw new ApplicationException($"Update for branch changed {nRows} row`s".TrimExtras());
        }

        /// <summary>
        /// Obtains the name for a database view of the data table for a new branch.
        /// </summary>
        /// <param name="branchName">The name of the new branch</param>
        /// <returns></returns>
        string GetDataViewName(string branchName)
        {
            // When dealing with a branch new store, the new branch is already
            // recorded. But for subsequent branch creation, the new branch only
            // gets added after the fact.

            if (Branches.Count == 1)
                return branchName;

            // The new branch should already be in the Branches dictionary
            int numBranch = Branches.Values
                                    .Count(x => x.Info.BranchName.EqualsIgnoreCase(branchName));

            if (numBranch == 0)
                return branchName;
            else
                return $"{branchName}({numBranch + 1})";
        }

        /// <summary>
        /// Saves root metadata as part of this store.
        /// </summary>
        public override void SaveRoot()
        {
            Database.ExecuteTransaction(() =>
            {
                SaveProperty(PropertyNaming.StoreId, Root.StoreId.ToString());

                if (Root.UpstreamId.Equals(Guid.Empty))
                {
                    Debug.Assert(String.IsNullOrEmpty(Root.UpstreamLocation));
                }
                else
                {
                    SaveProperty(PropertyNaming.UpstreamId, Root.UpstreamId.ToString());
                    SaveProperty(PropertyNaming.UpstreamLocation, Root.UpstreamLocation);
                }

                if (Root.PushTimes != null)
                {
                    // To store as json 
                    throw new NotImplementedException();
                }
            });
        }

        /// <summary>
        /// Persists command data as part of a specific branch.
        /// </summary>
        /// <param name="branch">The branch the data relates to</param>
        /// <param name="data">The data to be written</param>
        internal override void WriteData(Branch branch, CmdData data)
        {
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);

            Database.ExecuteNonQuery(
                $"INSERT INTO [{branch.Id}] (Sequence,Data) " +
                $"VALUES ({data.Sequence}, '{json}')");
        }

        /// <summary>
        /// Creates a SQLite database to be used by a new <see cref="SQLiteStore"/>
        /// </summary>
        /// <param name="sqliteFileName">The file specification for the SQLite database file.</param>
        static SQLiteDatabase CreateDatabase(string sqliteFileName)
        {
            var db = new SQLiteDatabase(sqliteFileName);

            db.ExecuteTransaction(() =>
            {
                // Create the catalog for branch metadata
                db.ExecuteNonQuery(
                    $"CREATE TABLE {BranchesTableName}" +
                    " (Id GUID NOT NULL PRIMARY KEY" +
                    ", Name TEXT NOT NULL" +
                    ", CreatedAt DATETIME NOT NULL" +
                    ", Data JSON NOT NULL)");

                // Miscellaneous store properties
                db.ExecuteNonQuery(
                    $"CREATE TABLE {PropertiesTableName}" +
                    " (Name TEXT NOT NULL PRIMARY KEY" +
                    ", Value TEXT NOT NULL)");
            });

            return db;
        }

        /// <summary>
        /// Saves a property value to the database.
        /// </summary>
        /// <param name="property">The property to be saved</param>
        /// <param name="value">The value to save for the property (not null)</param>
        /// <exception cref="ArgumentNullException">The specified value is undefined.</exception>
        /// <remarks>Once added, you cannot delete it using this method.</remarks>
        void SaveProperty(PropertyNaming property, string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            string propertyName = property.ToString();
            string sql;

            // TODO: may want to escape chars in the value

            if (Database.IsExisting(PropertiesTableName, $"Name='{propertyName}'"))
                sql = $"UPDATE {PropertiesTableName} " +
                      $"SET Value='{value}' " +
                      $"WHERE Name='{propertyName}'";
            else
                sql = $"INSERT INTO {PropertiesTableName} (Name,Value) " +
                      $"VALUES ('{propertyName}', '{value}')";

            int nRows = Database.ExecuteNonQuery(sql);
            Debug.Assert(nRows == 1);
        }


        /// <summary>
        /// Loads a command store using the metadata for one of the branches
        /// in the store.
        /// </summary>
        /// <param name="sqliteFilename">The file specification of the SQLite database
        /// to load.</param>
        /// <param name="cs">Details for a clone command that should be used
        /// to initialize the root metadata for a newly cloned store (specify
        /// null if this store previously existed).</param>
        /// <returns>The loaded command store.</returns>
        public static SQLiteStore Load(string sqliteFilename, ICloneStore cs = null)
        {
            var db = new SQLiteDatabase(sqliteFilename);

            // Load misc properties
            Dictionary<string,object> props = db.ExecuteQuery(new PropertiesQuery())
                                                .ToDictionary(x => x.Key, x => (object)x.Value);

            // Amend root details if we're loading up a new clone
            if (cs != null)
            {
                Guid upstreamId = props.GetGuid(PropertyNaming.StoreId.ToString());
                props[PropertyNaming.UpstreamId.ToString()] = upstreamId;
                props[PropertyNaming.UpstreamLocation.ToString()] = cs.Origin;
                props[PropertyNaming.StoreId.ToString()] = cs.StoreId;
            }

            var root = new RootFile(
                storeId: props.GetGuid(PropertyNaming.StoreId.ToString()),
                upstreamId: props.GetGuid(PropertyNaming.UpstreamId.ToString()),
                upstreamLocation: props.GetValue<string>(PropertyNaming.UpstreamLocation.ToString()),
                pushTimes: null);

            // Fake a directory name that will provide a value for CmdStore.Name that
            // corresponds to the name of the database
            root.DirectoryName = Path.ChangeExtension(sqliteFilename, null);

            // What was the last branch the user was working with? (it should be defined,
            // go with the master branch if not)
            var curId = props.GetGuid(PropertyNaming.LastBranch.ToString());
            if (Guid.Empty.Equals(curId))
                curId = root.StoreId;

            // Load metadata for all branches in the store
            AltCmdFile[] acs = db.ExecuteQuery(new BranchesQuery())
                                 .OrderBy(x => x.CreatedAt)
                                 .ToArray();

            Debug.Assert(acs.Length > 0);
            Debug.Assert(acs.Any(x=> x.BranchId.Equals(curId)));

            var result = new SQLiteStore(root, acs, curId, db);

            // If we amended the store properties for a new clone, save
            // them back to the database
            if (cs != null)
                result.SaveRoot();
            
            // At this stage, the FileName property held in branch metadata (as returned
            // by BranchesQuery) is simply the name. To ensure things mimic the logic in
            // FileStore, we need to define a fake FileName property that reflects the
            // branch hierarchy
            var branchNames = acs.ToDictionary(x => x.BranchId, x => x.FileName);

            foreach (Branch branch in result.Branches.Values)
            {
                var names = new List<string> {root.DirectoryName};

                for (var b = branch; b != null; b = b.Parent)
                    names.Add(branchNames[b.Id]);

                branch.Info.FileName = String.Join(@"\", names);
                //Log.Info(branch.Info.FileName);
            }

            return result;
        }

        /// <summary>
        /// Checks whether the name supplied for a new branch is valid.
        /// </summary>
        /// <param name="name">The name to be checked.</param>
        /// <returns>True if the specified name only contains characters
        /// that are valid for file names, and which is not a reserved
        /// name.</returns>
        /// <remarks>
        /// This does not check whether the name is a duplicate of
        /// another sibling branch.
        /// </remarks>
        public override bool IsValidBranchName(string name)
        {
            if (IsReservedName(name))
                return false;

            return base.IsValidBranchName(name);
        }

        /// <summary>
        /// Checks whether a branch name corresponds to an AltCmd system table.
        /// </summary>
        /// <param name="name">The branch name to be tested</param>
        /// <returns>True if the specified branch name corresponds to the
        /// name of an AltCmd system table.</returns>
        static bool IsReservedName(string name)
        {
            return name.EqualsIgnoreCase(BranchesTableName) ||
                   name.EqualsIgnoreCase(PropertiesTableName);
        }
    }
}
