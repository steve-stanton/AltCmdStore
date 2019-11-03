using System;
using System.Collections.Generic;
using System.IO;
using AltLib;
using CommandLine;

namespace AltCmd
{
    /// <summary>
    /// Command to clone an instance of <see cref="CmdStore"/>.
    /// </summary>
    [Verb("clone", HelpText = "Clones a repository into a newly created directory")]
    class CloneCmdLine : AltCmdLine
    {
        /// <summary>
        /// The (possibly remote) store to clone from.
        /// </summary>
        /// <remarks>For the time being, this is limited to stores
        /// that exist on the local machine. Specify the full path for
        /// the root folder of the store.</remarks>
        [Value(
            0,
            Required = true,
            HelpText = "The (possibly remote) store to clone from",
            MetaName = "origin")]
        public string Origin { get; private set; }

        /// <summary>
        /// The full path for a new directory where the clone should
        /// be created.
        /// </summary>
        /// <remarks>Must be a folder on a local hard drive.</remarks>
        [Value(
            1,
            Required = true,
            HelpText = "The command store to create",
            MetaName = "to")]
        public string To { get; private set; }

        /*
        /// <summary>
        /// The persistence mechanism to be used for saving command data
        /// in the clone store.
        /// </summary>
        [Option(
            't',
            "type",
            Default = StoreType.File,
            HelpText = "Specify how command data should be persisted by the clone")]
        public StoreType Type { get; set; }
        */

        /// <summary>
        /// The unique ID to use for the cloned store.
        /// </summary>
        public Guid StoreId { get; } = Guid.NewGuid();

        /// <summary>
        /// Obtains a string that contains all command line parameters.
        /// </summary>
        /// <returns>The command line text (without any abbreviations)</returns>
        public override string ToString()
        {
            return $"clone {Origin} {To}";
        }

        protected override ICmdHandler GetCommandHandler(ExecutionContext context)
        {
            var data = new CmdData(
                        cmdName: nameof(ICloneStore),
                        sequence: 0,
                        createdAt: DateTime.UtcNow);

            data.Add(nameof(ICreateStore.StoreId), StoreId);
            data.Add(nameof(ICreateStore.Name), To);
            data.Add(nameof(ICloneStore.Origin), Origin);

            // For the time being, only clone to the same type as the origin (you should
            // really be allowed to clone from one type into another)
            StoreType type = StoreType.None;

            if (Directory.Exists(Origin))
                type = StoreType.File;
            else
            {
                string dbSpec = Path.HasExtension(Origin) ? Origin : Origin + ".ac-sqlite";
                if (File.Exists(dbSpec))
                    type = StoreType.SQLite;
            }

            if (type == StoreType.None)
                throw new ArgumentException("Cannot determine store type for " + Origin);

            data.Add(nameof(ICreateStore.Type), type);

            return new CloneStoreHandler(data);
        }
    }
}
