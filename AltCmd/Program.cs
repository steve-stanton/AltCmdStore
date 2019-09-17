using System;
using System.Collections.Generic;
using System.IO;
using AltLib;
using CommandLine;
using NLog;

namespace AltCmd
{
    /// <summary>
    /// Console application for managing command stores.
    /// </summary>
    public class Program
    {
        static Logger Log = LogManager.GetCurrentClassLogger();

        public static void Main(string[] args)
        {
            try
            {
                // Establish the execution context
                var ec = GetContext(args);

                // Register any command processors
                //ec.Processors.Add(new NameProcessor());

                // TODO: There should likely be a processor that handles the command
                // model itself (which would always get processed last)...

                // Initialize model(s) by loading commands related to the
                // current branch (plus branches that have been merged into it)
                ec.InitializeModels();

                var session = new AltCmdSession(ec);

                if (ec.Store != null && args.Length <= 1)
                {
                    // Start an interactive command line session if the
                    // command store has been determined, but there are
                    // no other command line arguments.

                    RunSession(session);
                }
                else
                {
                    // Process the command line. Start an interactive session
                    // if that command line leads to a memory store.

                    session.Execute(String.Join(" ", args));

                    if (ec.Store is MemoryStore)
                        RunSession(session);
                }
            }

            catch (NotImplementedException nex)
            {
                Console.WriteLine(nex.StackTrace);
                Log.Warn("Not implemented: " + nex.StackTrace);
            }

            catch (Exception ex)
            {
                ShowError(ex);
                Console.WriteLine("Hit RETURN to close");
                Console.Read();
            }
        }

        /// <summary>
        /// Analyzes the command line for the AltCmd application, to determine
        /// which store (if any) should be initialized.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /// <returns>The execution context (wraps a reference to the store that
        /// the user has specified)</returns>
        static ExecutionContext GetContext(string[] args)
        {
            // If the user is trying to initialize (or clone), don't try to determine
            // the initial store
            if (args.Length > 0)
            {
                string verb = args[0];

                if (verb.EqualsIgnoreCase("init") || verb.EqualsIgnoreCase("clone"))
                    return new ExecutionContext();
            }

            string acSpec = null;
            string curDir = null;

            // If we've been given nothing, start by looking for
            // an AC file in the current folder. If we've been
            // given a specific folder, look in there instead.

            if (args.Length == 0)
                curDir = Directory.GetCurrentDirectory();
            else if (args.Length == 1 && Directory.Exists(args[0]))
                curDir = args[0];

            if (curDir != null)
                acSpec = AltCmdFile.GetAcPath(curDir);

            // If we couldn't determine an initial branch based
            // on folder, see if we've been supplied with an AC file

            if (acSpec == null)
            {
                if (args.Length == 1 && File.Exists(args[0]))
                {
                    string fileName = args[0];

                    if (Path.GetExtension(fileName) == ".ac")
                        acSpec = fileName;
                    else
                        throw new ArgumentException("Unrecognized file type");
                }
            }

            // If that doesn't give us anything, look in C:\ProgramData\AltCmd
            // to obtain the last one we opened

            string progData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string last = Path.Combine(progData, "AltCmd", "last");

            if (acSpec == null && File.Exists(last))
            {
                string lastSpec = File.ReadAllText(last);
                if (File.Exists(lastSpec))
                    acSpec = lastSpec;
            }

            if (acSpec == null)
                return new ExecutionContext();

            // Load branch metadata for the store
            var cs = FileStore.Load(acSpec);
            Log.Info($"Opened {cs.Name} (current branch is {cs.Current.Info.BranchName})");

            // Remember the store we opened as the last one
            cs.SaveCurrent();

            // Load up the command stream as well
            cs.Stream = new CmdStream(cs.Current);

            return new ExecutionContext(cs);
        }

        /// <summary>
        /// Runs a command session (by letting the user enter a series
        /// of commands without re-starting the application).
        /// </summary>
        /// <param name="session">The session controller</param>
        static void RunSession(AltCmdSession session)
        {
            ExecutionContext ec = session.Context;

            for (; ; )
            {
                Console.Write($"{ec}> ");
                string cmd = Console.ReadLine();
                if (cmd.StartsWithIgnoreCase("ex") || cmd.StartsWithIgnoreCase("q"))
                    return;

                try
                {
                    if (cmd.StartsWith("@"))
                        ProcessCmdFile(session, cmd.Substring(1));
                    else
                        session.Execute(cmd);
                }

                catch (Exception ex)
                {
                    ShowError(ex);
                }
            }
        }

        /// <summary>
        /// Processes commands listed in a command file.
        /// </summary>
        /// <param name="session">The command session</param>
        /// <param name="fileName">The path for a text file that holds the commands
        /// to be executed (one command per line).</param>
        static void ProcessCmdFile(AltCmdSession session, string fileName)
        {
            // if the file doesn't have an extension, assume ".cmd"
            bool isExisting = File.Exists(fileName);
            if (!isExisting && String.IsNullOrEmpty(Path.GetExtension(fileName)))
            {
                fileName = fileName + ".cmd";
                isExisting = File.Exists(fileName);
            }

            if (!isExisting)
                throw new ArgumentException("No such file: " + fileName);

            string[] cmds = File.ReadAllLines(fileName);
            uint numDone = 0;
            Log.Trace($"Processing commands read from {fileName}");

            foreach (string cmd in cmds)
            {
                // Ignore blank lines, as well as lines that start with
                // recognized comment markers
                string c = cmd.Trim();
                if (c.StartsWith("!") || c.StartsWith("--") || c.StartsWith("//"))
                    continue;

                try
                {
                    session.Execute(cmd);
                    numDone++;
                }

                catch
                {
                    Log.Error($"Failed to process command: {c}");
                    throw;
                }
            }

            Log.Info($"Processed {numDone} commands");
        }

        static void ShowError(Exception ex, string message = null)
        {
            // Log the error along with the message held in the exception, but
            // allow for a possible message override
            Log.Error(ex, message ?? ex.Message);
            //Console.WriteLine(message);
        }
    }
}
