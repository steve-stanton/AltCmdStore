﻿using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace AltLib
{
    /// <summary>
    /// Command handler for merging commands from a parent branch
    /// to a child branch (and vice versa).
    /// </summary>
    public class MergeHandler : ICmdHandler
    {
        /// <summary>
        /// The input parameters for the command.
        /// </summary>
        CmdData Input { get; }

        /// <summary>
        /// Creates a new instance of <see cref="MergeHandler"/>
        /// </summary>
        /// <param name="input">The input parameters for the command.</param>
        /// <exception cref="ArgumentNullException">
        /// Undefined value for <paramref name="input"/>input</exception>
        /// <exception cref="ArgumentException">
        /// The supplied input has an unexpected value for <see cref="CmdData.CmdName"/>
        /// </exception>
        public MergeHandler(CmdData input)
        {
            Input = input ?? throw new ArgumentNullException(nameof(Input));

            if (Input.CmdName != nameof(IMerge))
                throw new ArgumentException(nameof(Input.CmdName));
        }

        public void Process(ExecutionContext context)
        {
            Guid sourceId = (Input as IMerge).FromId;
            CmdStore cs = context.Store;
            Branch target = cs.Current;
            Branch source = cs.FindBranch(sourceId);

            if (source == null)
                throw new ApplicationException($"Cannot locate branch {sourceId}");

            // Merges can only be done between branches that are part
            // of the local store (fetches from remote stores should
            // involve a totally different set of branches)
            //if (target.Info.StoreId != source.Info.StoreId)
            //    throw new NotSupportedException("Attempt to merge with a remote store");

            // Determine the first command that needs to be merged from
            // the source branch. The source branch doesn't know anything
            // about merges that have already been done, so figuring this
            // out involves scanning back from the end of the target branch
            // to locate the last merge (if any).

            // How far we go back depends on whether we're merging from
            // a child branch to its parent, or vice versa. If the target
            // is the child we go back as far as command 0. If the target
            // is the parent, we go back as far the command that followed
            // the branch start point.

            bool targetIsParent = target.Id.Equals(source.Info.ParentId);

            // Define the range of commands to be merged from the source

            uint minCmd = 0;

            if (targetIsParent)
            {
                // Merging into the parent, so start from the command immediately
                // after the last command that was previously merged (or 0 if this
                // is the first time the parent has merged from the child).

                if (target.Info.LastMerge.TryGetValue(sourceId, out MergeInfo mi))
                    minCmd = mi.ChildCount;
            }
            else
            {
                // Merging from the parent into the child, so start from the command
                // immediately after the last command that was previously merged (it
                // could potentially be 0)

                minCmd = target.Info.RefreshCount;
            }

            uint maxCmd = (uint)source.Info.CommandCount - 1;

            // TODO: Round about here we need to actually include the new stuff
            // as part of the current stream. Replaying the commands may then
            // lead to some sort of conflict (things are not guaranteed to work),
            // so there needs to be some way to preview the results.

            // Write the command data
            Input.Add(nameof(IMerge.MinCmd), minCmd);
            Input.Add(nameof(IMerge.MaxCmd), maxCmd);
            target.SaveData(Input);
        }
    }
}
