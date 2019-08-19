using System;
using AltLib;
using NLog;

namespace AltCmd
{
    /// <summary>
    /// Base class for the command verbs supported by the AltCmd application.
    /// </summary>
    /// <remarks>
    /// A command line effectively provides a kind of user interface to the
    /// actual command, it is not responsible for executing the command
    /// itself. That said, each derived command line class does need to
    /// be able to obtain a suitable handler via an implementation
    /// for <see cref="GetCommandHandler(ExecutionContext)"/>.
    /// <para/>
    /// The handler is responsible for performing the work of the command,
    /// which means that it will usually be supplied with an instance of
    /// <see cref="CmdData"/>, which will hold input parameters
    /// present on the command line.
    /// </remarks>
    abstract class AltCmdLine : ICmdInput
    {
        static Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Executes this command.
        /// </summary>
        /// <param name="context">The processing context</param>
        /// <returns>True if the command executed as expected. False if
        /// something went wrong (in that case, the user will be informed
        /// and details will be written to the log file).</returns>
        public virtual bool Execute(ExecutionContext context)
        {
            try
            {
                // Assume that each derived class will implement
                // ToString with a description of what's in the
                // command line.
                Log.Trace(ToString());

                ICmdHandler handler = GetCommandHandler(context);

                if (handler == null)
                    throw new ApplicationException("Cannot obtain a command handler");

                handler.Process(context);
                return true;
            }

            catch (Exception ex)
            {
                ShowError(ex);
            }

            return false;
        }

        protected void ShowError(Exception ex)
        {
            ShowError(ex, ex.Message);
        }

        protected void ShowError(Exception ex, string message)
        {
            Console.WriteLine(message);
            Log.Trace(ex.StackTrace);
            Log.Error(ex, "Command failed");
        }

        /// <summary>
        /// Obtains the command handler that should be used to execute
        /// this command line.
        /// </summary>
        /// <param name="context">The state of the system prior to command execution.</param>
        /// <returns>The handler that may be used to process the command (null if
        /// the current command line is incomplete or inconsistent)</returns>
        /// <remarks>
        /// This method will be called by <see cref="Execute"/> and,
        /// to have reached that stage, the command line should probably
        /// have been validated. As such, getting back a null in that
        /// situation would be unexpected.
        /// </remarks>
        abstract internal ICmdHandler GetCommandHandler(ExecutionContext context);
    }
}