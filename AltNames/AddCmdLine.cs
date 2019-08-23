using System;
using AltLib;
using CommandLine;

namespace AltNames
{
    [Verb("add", HelpText = "Adds a name for use in testing")]
    class AddCmdLine : AltCmdLine, ICmdHandler
    {
        internal const string CmdName = "NameCmdLine";

        /// <summary>
        /// The name to be added
        /// </summary>
        [Value(
            1,
            Required = false,
            HelpText = "A name to associate with the command",
            MetaName = "name")]
        public string Name { get; set; }

        public override string ToString()
        {
            return $"add {Name}";
        }

        protected override ICmdHandler GetCommandHandler(ExecutionContext context)
        {
            // Ensure the name has been defined
            if (String.IsNullOrEmpty(Name))
                Name = context.ToString();

            // Just do things via the Process method below
            return this;
        }

        public void Process(ExecutionContext context)
        {
            Branch branch = context.Store.Current;

            if (branch.IsRemote)
                branch = branch.CreateLocal(context);

            var cmd = new CmdData(cmdName: CmdName,
                                  sequence: branch.Info.CommandCount,
                                  createdAt: DateTime.UtcNow);

            cmd.Add(nameof(AddCmdLine.Name), Name);

            // Update relevant model(s)
            if (context.Apply(cmd))
            {
                branch.SaveData(cmd);
            }
            else
            {
                ProcessorException pex = context.LastProcessingError;
                if (pex == null)
                    Console.WriteLine("Handler failed");
                else
                    ShowError(pex);
            }
        }
    }
}
