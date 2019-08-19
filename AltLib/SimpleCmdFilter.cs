using System;
using System.Collections.Generic;

namespace AltLib
{
    /// <summary>
    /// A filter for commands that is based on command names.
    /// </summary>
    public class SimpleCmdFilter : ICmdFilter
    {
        /// <summary>
        /// The values for <see cref="CmdData.CmdName"/> that are relevant.
        /// </summary>
        HashSet<string> Names { get; }

        /// <summary>
        /// Creates a new instance of <see cref="SimpleCmdFilter"/>
        /// </summary>
        /// <param name="cmdNames">The values for <see cref="CmdData.CmdName"/>
        /// that are relevant (these are the commands that should not be filtered
        /// out).</param>
        public SimpleCmdFilter(IEnumerable<string> cmdNames)
        {
            Names = new HashSet<string>(cmdNames);

            if (Names.Count == 0)
                throw new ArgumentException("Filter refers to an empty set");
        }

        /// <summary>
        /// Is an instance of <see cref="CmdData"/> relevant?
        /// </summary>
        /// <param name="data">The command to be checked.</param>
        /// <returns>True if the command is relevant (i.e. should not be
        /// filtered out). False if the command can be ignored.</returns>
        public bool IsRelevant(CmdData data)
        {
            return Names.Contains(data.CmdName);
        }
    }
}
