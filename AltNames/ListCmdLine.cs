using System;
using System.Linq;
using AltLib;
using CommandLine;

namespace AltNames
{
    [Verb("list", HelpText = "List all names")]
    class ListCmdLine : AltCmdLine
    {
        public override bool Execute(ExecutionContext context)
        {
            var np = context.Processors.OfType<NameProcessor>().FirstOrDefault();
            if (np == null)
                throw new ApplicationException("Could not locate name processor");

            foreach (string s in np.Names)
            {
                Console.WriteLine(s);
            }

            return true;
        }

        protected override ICmdHandler GetCommandHandler(ExecutionContext context)
        {
            // This method should not be called because the Execute
            // override did everything
            throw new NotImplementedException();
        }
    }
}
