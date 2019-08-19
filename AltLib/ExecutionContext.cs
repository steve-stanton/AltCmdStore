using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace AltLib
{
    public class ExecutionContext
    {
        static Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The current store (null if a store still needs to be initialized).
        /// </summary>
        public CmdStore Store { get; internal set; }

        /// <summary>
        /// Processors that consume commands in the store.
        /// </summary>
        public List<ICmdProcessor> Processors { get; }

        /// <summary>
        /// An exception that has arisen on a call to a method implemented
        /// by an instance of <see cref="ICmdProcessor"/>
        /// </summary>
        /// <remarks>
        /// Cleared at the start of each call to <see cref="Apply"/>.
        /// </remarks>
        public ProcessorException LastProcessingError { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionContext"/> class
        /// in preparation for the creation of a new store.
        /// </summary>
        public ExecutionContext()
        {
            Store = null;
            Processors = new List<ICmdProcessor>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionContext"/> class.
        /// </summary>
        /// <param name="store">The command store (not null).</param>
        /// <exception cref="ArgumentNullException">
        /// The specified store is undefined.</exception>
        public ExecutionContext(CmdStore store)
            : this()
        {
            Store = store ?? throw new ArgumentNullException();
        }

        public override string ToString()
        {
            string result = String.Empty;

            if (Store == null)
                return "no store";

            Branch b = Store.Current;
            string leader = b.IsRemote ? "^" : String.Empty;

            return $"{leader}{b.GetBranchPath()}[{b.Info.CommandCount}]";
        }

        /// <summary>
        /// Prepares an instance of <see cref="CmdData"/> for
        /// the next command that will be appended to the
        /// current branch.
        /// </summary>
        /// <param name="commandName">A name that identifies
        /// the command type (a class name is one of potentially
        /// many choices)</param>
        /// <returns>An instance of command data that can
        /// be appended to the current branch.</returns>
        /// <remarks>
        /// The caller will usually append additional properties
        /// that are specific to the command.
        /// </remarks>
        public CmdData CreateCmdData(string commandName)
        {
            AltCmdFile ac = Store.Current.Info;

            return new CmdData(
                        cmdName: commandName,
                        sequence: ac.CommandCount,
                        createdAt: DateTime.UtcNow);
        }

        /// <summary>
        /// Applies a command to the relevant processors.
        /// </summary>
        /// <param name="data">The data for the command to apply.</param>
        /// <returns>True if the command was applied as expected. False if
        /// an error arose. Any error will be written to the log file. The caller
        /// can also obtain details via the <see cref="LastProcessingError"/>
        /// property.
        /// </returns>
        public bool Apply(CmdData data)
        {
            LastProcessingError = null;
            ICmdProcessor[] todo = Processors.Where(x => x.Filter.IsRelevant(data))
                                             .ToArray();

            try
            {
                Apply(data, todo);
                return true;
            }

            catch (ProcessorException pex)
            {
                LastProcessingError = pex;
                Log.Error(pex, pex.Message);

                // Attempt to undo anything we've done (up to and
                // including the processor where the failure arose)
                foreach (ICmdProcessor p in todo)
                {
                    try { p.Undo(data); }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Undo failed for " + p.GetType().Name);
                    }

                    // Break if we have just undone stuff for the processor that failed
                    // (the Apply method breaks on the first failure)
                    if (Object.ReferenceEquals(p, pex.Processor))
                        break;
                }
            }

            return false;
        }

        /// <summary>
        /// Applies a command to the relevant processors.
        /// </summary>
        /// <param name="data">The data for the command to apply.</param>
        /// <param name="todo">The processors that are relevant to the command.</param>
        /// <returns>The number of processors that successfully
        /// handled the command.</returns>
        void Apply(CmdData data, ICmdProcessor[] todo)
        {
            foreach (ICmdProcessor p in todo)
            {
                try
                {
                    p.Process(data);
                }

                catch (ProcessorException)
                {
                    throw;
                }

                catch (Exception ex)
                {
                    throw new ProcessorException(p, "Apply failed for " + p.GetType().Name, ex);
                }
            }
        }

        /// <summary>
        /// Initializes data models by loading and processing the commands
        /// from the current branch.
        /// </summary>
        public void InitializeModels()
        {
        }

        /// <summary>
        /// Opens a channel to something that acts as a remote store.
        /// </summary>
        /// <param name="storeUrl">The URL of the remote store.</param>
        /// <returns>A reference to the remote store</returns>
        /// <remarks>
        /// TODO: This just returns a local FileStore (<paramref name="storeUrl"/> should
        /// just be the path to the root folder for the store).
        /// </remarks>
        public IRemoteStore GetRemoteStore(string storeUrl)
        {
            string acPath = AltCmdFile.GetAcPath(storeUrl);

            if (acPath == null)
                throw new ApplicationException("Cannot locate " + storeUrl);

            return FileStore.Load(acPath);
        }
    }
}
