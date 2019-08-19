using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NLog;

namespace AltLib
{
    public class CloneStoreHandler : ICmdHandler
    {
        static Logger Log = LogManager.GetCurrentClassLogger();

        CmdData Input { get; }

        public CloneStoreHandler(CmdData input)
        {
            Input = input ?? throw new ArgumentNullException();

            if (Input.CmdName != nameof(ICloneStore))
                throw new ArgumentException(nameof(Input.CmdName));
        }

        public void Process(ExecutionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Create the new store
            var store = CmdStore.CreateStore(Input);

            // Write the command data
            // TODO: Does it need to be written anywhere? Perhaps to root metadata?
            // We shouldn't really touch any of the branches that have just been copied.
            //store.Current.SaveData(Input);
            context.Store = store;
        }
    }
}
