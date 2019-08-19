using System;
using System.IO;

namespace AltLib
{
    /// <summary>
    /// Command handler for creating a brand new (empty) store.
    /// </summary>
    public class CreateStoreHandler : ICmdHandler
    {
        /// <summary>
        /// The input parameters for the command.
        /// </summary>
        CmdData Input { get; }

        /// <summary>
        /// Creates a new instance of <see cref="CreateStoreHandler"/>
        /// </summary>
        /// <param name="input">The input parameters for the command.</param>
        /// <exception cref="ArgumentNullException">
        /// Undefined value for <paramref name="input"/>input</exception>
        public CreateStoreHandler(CmdData input)
        {
            Input = input ?? throw new ArgumentNullException(nameof(Input));

            if (Input.CmdName != nameof(ICreateStore))
                throw new ArgumentException(nameof(Input.CmdName));
        }

        /// <summary>
        /// Performs the data processing associated with a command.
        /// </summary>
        /// <remarks>If a call to this method completes without any exception,
        /// a reference to the new store can be obtained using the
        /// <see cref="ExecutionContext.Store"/> property.
        /// </remarks>
        public void Process(ExecutionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // Create the physical manifestation (if any) for the new store
            var store = CmdStore.CreateStore(Input);

            // Write the command data
            store.Current.SaveData(Input);
            context.Store = store;
        }

    }
}
