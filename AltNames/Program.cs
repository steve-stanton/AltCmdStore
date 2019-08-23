using System;
using System.IO;
using AltLib;
using CommandLine;
using NLog;

namespace AltNames
{
    class Program
    {
        static Logger Log = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            try
            {
                if (args.Length > 1)
                {
                    Console.WriteLine("Usage: AltNames {folder-name}");
                    return;
                }

                string branchRef = args.Length == 0 ? String.Empty : args[0];

                // Establish the execution context
                var ec = GetContext(branchRef);
                if (ec == null)
                    return;

                // Register the command processor for dealing with a list of names
                ec.Processors.Add(new NameProcessor());

                // Initialize model(s) by loading commands related to the
                // current branch (plus branches that have been merged into it)
                ec.InitializeModels();

                RunSession(ec);
            }

            catch (Exception ex)
            {
                ShowError(ex);
                Console.WriteLine("Hit RETURN to close");
                Console.Read();
            }
        }

        /// <summary>
        /// Runs a command session (by letting the user enter a series
        /// of commands without re-starting the application).
        /// </summary>
        /// <param name="ec"></param>
        static void RunSession(ExecutionContext ec)
        {
            for (; ; )
            {
                Console.Write($"{ec}> ");
                string cmd = Console.ReadLine();
                if (cmd.StartsWithIgnoreCase("ex") || cmd.StartsWithIgnoreCase("q"))
                    return;

                try
                {
                    string[] args = cmd.Split(' ');
                    Process(ec, args);
                }

                catch (Exception ex)
                {
                    ShowError(ex);
                }
            }
        }

        /// <summary>
        /// Analyzes the command line for the AltCmd application, to determine
        /// which store (if any) should be initialized.
        /// </summary>
        /// <param name="branchRef">The reference to the branch (either the path
        /// to a folder that holds an AC file, or the AC file itself).
        /// </param>
        /// <param name="args">The command line arguments.</param>
        /// <returns>The execution context (wraps a reference to the store that
        /// the user has specified)</returns>
        static ExecutionContext GetContext(string branchRef)
        {
            string acSpec = null;

            if (String.IsNullOrEmpty(branchRef))
            {
                // If nothing was specified, look in the current folder
                string curDir = Directory.GetCurrentDirectory();
                acSpec = AltCmdFile.GetAcPath(curDir);

                // If nothing in current folder, check last used branch
                if (acSpec == null)
                {
                    string progData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                    string last = Path.Combine(progData, "AltCmd", "last");
                    if (File.Exists(last))
                    {
                        string lastSpec = File.ReadAllText(last);
                        if (File.Exists(lastSpec))
                            acSpec = lastSpec;
                    }
                }

                if (acSpec == null)
                    throw new ArgumentException("Cannot locate last used command branch");
            }
            else
            {
                if (File.Exists(branchRef))
                {
                    acSpec = branchRef;
                }
                else if (Directory.Exists(branchRef))
                {
                    acSpec = AltCmdFile.GetAcPath(branchRef);
                    if (acSpec == null)
                        throw new ArgumentException("Specified folder does not contain any command data");
                }
                else
                {
                    throw new ArgumentException("No such file: " + branchRef);
                }
            }

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
        /// Processes a command line
        /// </summary>
        /// <param name="ec">The execution context (wrapping any command store that is currently active)</param>
        /// <param name="args">The words in the entered command line (either entered from the operating system
        /// command line, or in response to a prompt issued by this application).</param>
        static void Process(ExecutionContext ec, string[] args)
        {
            var parser = new Parser(c =>
            {
                c.CaseInsensitiveEnumValues = true;
                c.HelpWriter = Console.Out;
                c.IgnoreUnknownArguments = false;
                c.AutoVersion = false;
            });

            parser.ParseArguments<AddCmdLine
                                 ,CutCmdLine
                                 ,ListCmdLine>(args)
                  .WithParsed<AltCmdLine>(x => x.Execute(ec));
        }

        static void ShowError(Exception ex)
        {
            Console.WriteLine(ex.Message);
            Log.Error(ex, ex.Message);
        }
    }
}
