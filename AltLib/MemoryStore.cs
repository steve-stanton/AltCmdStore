using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using NLog;

namespace AltLib
{
    public class MemoryStore : CmdStore
    {
        static Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Serialized metadata for the branches in this store,
        /// keyed by <see cref="AltCmdFile.FileName"/>
        /// </summary>
        /// <remarks>The value is held as a string to mimic the data
        /// that would be written to disk as part of a file-based store.
        /// </remarks>
        Dictionary<string, string> AcFiles { get; }

        /// <summary>
        /// Data for commands in this store.
        /// </summary>
        /// <remarks>The key is the path for a file that is imagined
        /// to be present on a fake "m:" drive. This path corresponds
        /// to the value of <see cref="AltCmdFile.FileName"/>. Thus,
        /// if the store is called "MyStore", the key for the command
        /// that created the store (command 0) will be "m:\MyStore\0.json".
        /// <para/>
        /// The value is the command data that has been
        /// serialized to json.
        /// </remarks>
        Dictionary<string, string> Data { get; }

        static internal MemoryStore Create(CmdData args)
        {
            // Disallow an attempt to clone another memory store
            // TODO: How should the ICloneStore input reference another memory store?
            if (args.CmdName == nameof(ICloneStore))
                throw new NotImplementedException(nameof(MemoryStore));

            // Create the AC file that represents the store root branch
            Guid storeId = args.GetGuid(nameof(ICreateStore.StoreId));

            var ac = new AltCmdFile(storeId: storeId,
                                    parentId: Guid.Empty,
                                    branchId: storeId,
                                    createdAt: args.CreatedAt);

            string name = args.GetValue<string>(nameof(ICreateStore.Name));
            ac.FileName = $@"m:\{name}\{storeId}.ac";
            return new MemoryStore(ac);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryStore"/> class.
        /// </summary>
        /// <param name="rootAc">Metadata for the root branch</param>
        MemoryStore(AltCmdFile rootAc)
            : base(new RootFile(rootAc),
                   new AltCmdFile[] { rootAc },
                   rootAc.BranchId)
        {
            AcFiles = new Dictionary<string, string>();
            Data = new Dictionary<string, string>();

            Save(rootAc);
        }

        /// <summary>
        /// Reads the command data for a specific command in
        /// the current branch.
        /// </summary>
        /// <param name="branch">Details for the branch to read from.</param>
        /// <param name="sequence">The 0-based sequence number of
        /// the command to read.</param>
        /// <returns>The corresponding command data</returns>
        CmdData ReadData(Branch branch, uint sequence)
        {
            string dataPath = Path.Combine(branch.Info.DirectoryName, $"{sequence}.json");

            if (Data.ContainsKey(dataPath))
                return JsonConvert.DeserializeObject<CmdData>(Data[dataPath]);

            throw new ArgumentException("No such file: " + dataPath);
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
            for (uint i = minCmd; i <= maxCmd; i++)
                yield return ReadData(branch, i);
        }

        /// <summary>
        /// Persists command data as part of the current branch.
        /// </summary>
        /// <param name="branch">The branch the data relates to</param>
        /// <param name="data">The data to be written</param>
        internal override void WriteData(Branch branch, CmdData data)
        {
            string dataPath = Path.Combine(branch.Info.DirectoryName, $"{data.Sequence}.json");

            if (Data.ContainsKey(dataPath))
                throw new ApplicationException($"Data already recorded for {dataPath}");

            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            Data.Add(dataPath, json);
        }

        /// <summary>
        /// Saves the supplied branch metadata as part of this store.
        /// </summary>
        /// <param name="ac">The metadata to be saved</param>
        public override void Save(AltCmdFile ac)
        {
            if (ac.FileName == null)
                throw new ArgumentNullException("Cannot save AC file because name is undefined");

            string data = JsonConvert.SerializeObject(ac, Formatting.Indented);
            AcFiles[ac.FileName] = data;
        }

        /// <summary>
        /// Saves the supplied root metadata as part of this store.
        /// </summary>
        public override void SaveRoot()
        {
            if (Root.DirectoryName == null)
                throw new ArgumentNullException("Cannot save root because location is undefined");

            // Root metadata for a memory store doesn't need to be saved
            // (for now, I don't see any need to read back the serialized
            // form while an application is running)

            //string data = JsonConvert.SerializeObject(Root, Formatting.Indented);
        }
    }
}
