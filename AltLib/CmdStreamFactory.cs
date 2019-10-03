using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NLog;

namespace AltLib
{
    class CmdStreamFactory
    {
        static Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The main branch for this stream (not null).
        /// </summary>
        Branch Main { get; }

        /// <summary>
        /// The commands in the branches that contribute to the stream.
        /// </summary>
        Dictionary<Branch, CmdData[]> Data { get; }

        Stack<Cmd> Result { get; }

        internal CmdStreamFactory(Branch main)
        {
            Main = main ?? throw new ArgumentNullException();
            Data = new Dictionary<Branch, CmdData[]>();
            Result = new Stack<Cmd>();
        }

        internal CmdStream Create()
        {
            // Start out by faking a merge of the whole main branch into itself
            var ms = new MergeSpan(Main, 0, Main.Info.CommandCount - 1, Main);
            Link(ms);

            // Follow ancestors as well (up to the branch point)
            for (Branch b = Main; b != null; b = b.Parent)
            {
                Debug.Assert(b.Commands.Count > 0);

                if (b.Parent != null)
                {
                    uint maxCmd = 0;

                    // The ICreateBranch command may not be the last
                    // command pushed to the results (it's possible that
                    // a parent span has been injected prior to the branch
                    // creation step). But if that does happen, confirm
                    // that the last command we do have is immediately
                    // after the branch point.

                    Debug.Assert(Result.Count > 0);
                    Cmd last = Result.Peek();

                    if (last.Data.Sequence == 0)
                    {
                        Debug.Assert(Object.ReferenceEquals(last.Branch, b));
                        Debug.Assert(last.Data.CmdName == nameof(ICreateBranch));
                        Debug.Assert(Object.ReferenceEquals(b.Commands[0], last.Data));

                        maxCmd = (last.Data as ICreateBranch).CommandCount - 1;
                    }
                    else
                    {
                        Debug.Assert(Object.ReferenceEquals(last.Branch, b.Parent));
                        Debug.Assert(b.Commands[0].CmdName == nameof(ICreateBranch));
                        Debug.Assert((b.Commands[0] as ICreateBranch).CommandCount == last.Data.Sequence);

                        maxCmd = last.Data.Sequence - 1;
                    }

                    ms = new MergeSpan(b.Parent, 0, maxCmd, b);
                    Link(ms);
                }
            }

            // Confirm that all merge spans have been processed
            foreach (CmdData cd in GetAllData())
            {
                MergeSpan[] spans = PopMergeSpans(cd).ToArray();
                if (spans.Length > 0)
                    throw new ApplicationException("Not all merge spans were processed");
            }

            // Apply any updates

            Log.Info($"Loaded stream with {Result.Count} commands");

            return new CmdStream(Main, Result);
        }

        IEnumerable<CmdData> GetAllData()
        {
            foreach (CmdData[] data in Data.Values)
            {
                foreach (CmdData d in data)
                    yield return d;
            }
        }

        CmdData[] GetSourceData(MergeSpan ms)
        {
            Branch source = ms.Source;
            uint numCmd = ms.MaxCmd + 1;

            if (Data.TryGetValue(source, out CmdData[] data))
            {
                // Since we scan from the end, we should have asked for the
                // high water mark on the first access attempt
                if (data.Length < numCmd)
                    throw new ApplicationException($"Only {data.Length} commands loaded (need {numCmd})");

                return data;
            }

            // Ensure the source branch has been loaded (it's quite possible that
            // the branch has already been loaded -- it may even hold more commands
            // than we need now)

            source.Load(numCmd);
            data = source.Commands.Take((int)numCmd).ToArray();
            Data.Add(source, data);
            return data;
        }

        /// <summary>
        /// Links the commands in a merge range
        /// </summary>
        /// <param name="ms">The range of commands to be linked</param>
        void Link(MergeSpan ms)
        {
            // Get the command data for the branch we need to walk
            CmdData[] sourceData = GetSourceData(ms);

            // Walk back through the commands in the merge range
            for (int i = (int)ms.MaxCmd; i >= (int)ms.MinCmd; i--)
            {
                CmdData cd = sourceData[i];

                if (cd.CmdName == nameof(IMerge))
                {
                    // Continue with any truncated merges that were previously
                    // attached to the command data
                    foreach (MergeSpan m in PopMergeSpans(cd))
                        Link(m);

                    IMerge merge = cd as IMerge;

                    // If we have reached a reverse merge (the source has merged
                    // from the target), attach a truncated span at the start of
                    // the merge range in the target. When we ultimately reach that
                    // command, we will continue the merge.

                    if (merge.FromId.Equals(ms.Target.Id))
                    {
                        // Define the portion of the source span that needs to be handled later
                        var truncatedRange = new MergeSpan(
                            ms.Source, ms.MinCmd, cd.Sequence - 1, ms.Target);

                        // Attach the span somewhere in the range of the merge
                        AddMergeSpan(merge.MinCmd, merge.MaxCmd, truncatedRange);
                        return;
                    }

                    // Express the merge as a span & link that
                    Branch newSource = ms.Source.Store.FindBranch(merge.FromId);
                    var span = new MergeSpan(newSource, merge.MinCmd, merge.MaxCmd, ms.Source);
                    Link(span);
                }
                else
                {
                    // Include the command in the chain
                    Result.Push(new Cmd(ms.Source, cd));

                    // Continue with any truncated merges that were previously
                    // attached to the command data
                    foreach (MergeSpan m in PopMergeSpans(cd))
                        Link(m);
                }
            }
        }

        /// <summary>
        /// Injects a merge span at a suitable place in a range of commands.
        /// </summary>
        /// <param name="minCmd">The sequence number of the earliest target command to consider</param>
        /// <param name="maxCmd">The sequence number of the latest target command to consider</param>
        /// <param name="ms">The span the needs to be injected.</param>
        void AddMergeSpan(uint minCmd, uint maxCmd, MergeSpan ms)
        {
            // If there are no further merges from the source, inject the span
            // at the start of the target range. But if there are any later
            // merges, attach to that merge.

            CmdData[] targetData = Data[ms.Target];
            int injectAt = -1;

            for (int i = (int)maxCmd; injectAt < 0 && i >= (int)minCmd; i--)
            {
                CmdData cd = targetData[i];

                if (cd.CmdName == nameof(IMerge))
                {
                    if ((cd as IMerge).FromId.Equals(ms.Source.Id))
                        injectAt = i;
                }
            }

            // If no reverse merges, inject at the start of the range
            if (injectAt < 0)
                injectAt = (int)minCmd;

            // It's conceivable that more than one span may get injected
            // at the same place
            CmdData data = targetData[injectAt];
            var spanList = data.GetObject<List<MergeSpan>>(nameof(MergeSpan));
            if (spanList == null)
            {
                spanList = new List<MergeSpan>();
                data.Add(nameof(MergeSpan), spanList);
            }

            spanList.Add(ms);
        }

        /// <summary>
        /// Retrieves any merge spans that have been attached to a
        /// instance of command data (removing it from the data if it
        /// was there).
        /// </summary>
        /// <param name="data">The command data to check for merge spans</param>
        /// <returns>Any merge spans that were attached to the supplied
        /// command data (not null, but may be an empty set)</returns>
        static IEnumerable<MergeSpan> PopMergeSpans(CmdData data)
        {
            var result = data.GetObject<List<MergeSpan>>(nameof(MergeSpan));
            if (result == null)
                return Enumerable.Empty<MergeSpan>();

            data.Remove(nameof(MergeSpan));
            return result;
        }
    }
}
