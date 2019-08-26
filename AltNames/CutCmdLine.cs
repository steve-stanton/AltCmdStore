using System;
using AltLib;
using CommandLine;

namespace AltNames
{
    [Verb("cut", HelpText = "Removes a name from the list")]
    class CutCmdLine : AltCmdLine, ICmdHandler
    {
        internal const string CmdName = "CutName";

        /// <summary>
        /// The name to be removed
        /// </summary>
        [Value(
            1,
            Required = true,
            HelpText = "The name to be removed",
            MetaName = "name")]
        public string Name { get; set; }

        public override string ToString()
        {
            return $"cut {Name}";
        }

        protected override ICmdHandler GetCommandHandler(ExecutionContext context)
        {
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

            cmd.Add(nameof(CutCmdLine.Name), Name);

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
