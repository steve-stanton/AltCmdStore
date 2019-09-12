using System;
using System.Diagnostics;
using AltLib;
using CommandLine;

namespace AltCmd
{
    // TODO: This is now covered through the separate AltNames test program.
    // But leave it here for now.
    [Verb("name", HelpText = "Save a name for use in testing")]
    class NameCmdLine : AltCmdLine, ICmdHandler
    {
        /// <summary>
        /// The name to associate with this command
        /// </summary>
        [Value(
            1,
            Required = false,
            HelpText = "A name to associate with the command",
            MetaName = "name")]
        public string Name { get; set; }

        public override string ToString()
        {
            return $"name {Name}";
        }

        internal static string GetCommandLine(CmdData data)
        {
            Debug.Assert(data.CmdName == nameof(NameCmdLine));
            string name = data.GetValue<string>(nameof(Name));
            return $"name {name}";
        }

        protected override ICmdHandler GetCommandHandler(ExecutionContext context)
        {
            // Ensure the name has been defined
            if (String.IsNullOrEmpty(Name))
                Name = context.ToString();

            return this;
        }

        public void Process(ExecutionContext context)
        {
            Branch branch = context.Store.Current;

            if (branch.IsRemote)
                branch = branch.CreateLocal(context);


            CmdData cmd = context.CreateCmdData(nameof(NameCmdLine));
            cmd.Add(nameof(NameCmdLine.Name), Name);

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
