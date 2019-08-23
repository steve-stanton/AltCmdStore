using System;
using AltLib;
using CommandLine;

namespace AltCmd
{
    /// <summary>
    /// Command to initialize a brand new instance of <see cref="CmdStore"/>
    /// </summary>
    [Verb("init", HelpText = "Initialize a new command store")]
    class InitCmdLine : AltCmdLine, ICreateStore
    {
        /// <summary>
        /// The name of the new store (may include a folder path).
        /// </summary>
        [Value(
            0,
            Required = false,
            Default = "New Store",
            HelpText = "The name for the new store",
            MetaName = "name")]
        public string Name { get; private set; }

        /// <summary>
        /// The persistence mechanism to be used for saving command data.
        /// </summary>
        [Option(
            't',
            "type",
            Default = StoreType.File,
            HelpText = "Specify how command data should be persisted")]
        public StoreType Type { get; set; }

        /// <summary>
        /// The unique ID for the newly created store.
        /// </summary>
        public Guid StoreId { get; } = Guid.NewGuid();

        /// <summary>
        /// Obtains a string that contains all command line parameters.
        /// </summary>
        /// <returns>The command line text (without any abbreviations)</returns>
        public override string ToString()
        {
            return $"init {Name} --type {Type}";
        }

        protected override ICmdHandler GetCommandHandler(ExecutionContext context)
        {
            var data = new CmdData(
                        cmdName: nameof(ICreateStore),
                        sequence: 0,
                        createdAt: DateTime.UtcNow);

            data.Add(nameof(ICreateStore.StoreId), StoreId);
            data.Add(nameof(ICreateStore.Name), Name);
            data.Add(nameof(ICreateStore.Type), Type.ToString());

            return new CreateStoreHandler(data);
        }
    }
}
