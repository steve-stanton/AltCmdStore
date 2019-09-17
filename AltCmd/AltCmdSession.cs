using System;
using System.Collections.Generic;
using AltLib;
using CommandLine;

namespace AltCmd
{
    /// <summary>
    /// Handles commands entered during an AltCmd session.
    /// </summary>
    public class AltCmdSession
    {
        /// <summary>
        /// The execution context (defining the command store involved)
        /// </summary>
        public ExecutionContext Context { get; }

        /// <summary>
        /// The current branch
        /// </summary>
        public Branch Current => Context.Store.Current;

        /// <summary>
        /// Creates a new instance of <see cref="AltCmdSession"/>
        /// </summary>
        /// <param name="ec">The execution context (defining the command store involved)</param>
        public AltCmdSession(ExecutionContext ec)
        {
            Context = ec ?? throw new ArgumentNullException(nameof(Context));
        }

        /// <summary>
        /// Processes a command line
        /// </summary>
        /// <param name="cmd">The command line to execute</param>
        public void Execute(string cmd)
        {
            string[] args = cmd.Split(' ');

            // Handle typical aliases
            if (args.Length > 0)
            {
                var argList = new List<string>(args);

                if (args[0].StartsWith("d") || args[0] == "ls")
                {
                    argList[0] = "branch";
                    argList.Add("--list");
                }
                else if (args[0] == "cd" || args[0] == "cb")
                {
                    argList[0] = "checkout";
                }
                else if (args[0] == "mkdir" || args[0] == "md")
                {
                    argList[0] = "branch";
                }
                else if (args[0].StartsWith("m"))
                {
                    argList[0] = "merge";
                }

                args = argList.ToArray();
            }

            var parser = new Parser(c =>
            {
                c.CaseInsensitiveEnumValues = true;
                c.HelpWriter = Console.Out;
                c.IgnoreUnknownArguments = false;
                c.AutoVersion = false;
            });

            var result = parser.ParseArguments
                <InitCmdLine,
                CloneCmdLine,
                BranchCmdLine,
                CheckoutCmdLine,
                MergeCmdLine,
                FetchCmdLine,
                PushCmdLine,
                RecallCmdLine,
                NameCmdLine,
                CompleteCmdLine>(args)
                .WithParsed<AltCmdLine>(x => x.Execute(Context));
        }
    }
}
