using System;
using System.Collections.Generic;
using System.Linq;
using AltLib;
using CommandLine;

namespace AltCmd
{
    /// <summary>
    /// Lists recently entered commands (primarily to help with debugging).
    /// </summary>
    [Verb("recall", HelpText = "Recalls recent command lines")]
    class RecallCmdLine : AltCmdLine
    {
        [Value(
            0,
            Required = false,
            //Default = 20, -- not implemented by CommandLine package
            HelpText = "The number of recent commands to display",
            MetaName = "command-count")]
        public uint Count { get; set; }

        [Option(
            'a',
            "all",
            HelpText = "Recalls across all branches",
            Default = false)]
        public bool All { get; private set; }

        public override string ToString()
        {
            return $"recall {Count}";
        }

        public override bool Execute(ExecutionContext context)
        {
            if (Count == 0)
                Count = 20;

            CmdStore cs = context.Store;

            if (All)
            {
                // Display with most recent first
                LinkedListNode<Cmd> cNode = cs.Stream.Cmds.Last;

                for (int i = 0; cNode != null && i < Count; i++, cNode = cNode.Previous)
                {
                    Cmd c = cNode.Value;
                    string branchPath = c.Branch.GetBranchPath(false);
                    CmdData cd = c.Data;
                    string summary = GetCmdSummary(cs, cd, c.Branch);
                    Console.WriteLine($"{branchPath}[{cd.Sequence}] = {summary}");
                }
            }
            else
            {
                BranchInfo ac = cs.Current.Info;
                uint minSeq = Count < ac.CommandCount ? ac.CommandCount - Count : 0;
                uint maxSeq = ac.CommandCount - 1;
                CmdData[] data = cs.ReadData(cs.Current, minSeq, maxSeq).ToArray();

                foreach (CmdData cd in data.Reverse())
                {
                    string summary = GetCmdSummary(cs, cd, cs.Current);
                    Console.WriteLine($"[{cd.Sequence}] = {summary}");
                }
            }

            return true;
        }

        protected override ICmdHandler GetCommandHandler(ExecutionContext context)
        {
            throw new NotSupportedException();
        }

        string GetCmdSummary(CmdStore cs, CmdData data, Branch branch)
        {
            if (data.CmdName == nameof(NameCmdLine))
                return NameCmdLine.GetCommandLine(data);

            if (data.CmdName == nameof(ICreateBranch))
            {
                string name = (data as ICreateBranch).Name;
                uint cc = (data as ICreateBranch).CommandCount;
                string result = $"branch {name} {cc}";
                if (!name.Equals(branch.Name))
                    result += $" (now called {branch.Name})";

                return result;
            }

            if (data.CmdName == nameof(IMerge))
            {
                IMerge m = (data as IMerge);
                string result = "merge ";
                Guid fromId = m.FromId;
                Branch fromBranch = cs.FindBranch(fromId);
                if (ReferenceEquals(fromBranch, branch.Parent))
                    result += "..";
                else
                    result += fromBranch.Name;

                result += $" [{m.MinCmd},{m.MaxCmd}]";

                return result;
            }

            return data.CmdName;
        }
    }
}
