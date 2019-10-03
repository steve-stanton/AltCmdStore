using System;
using System.Collections.Generic;
using NLog;

namespace AltLib
{
    /// <summary>
    /// An association of an instance of <see cref="CmdData"/>
    /// with its enclosing <see cref="Branch"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="CmdStream"/> class can be used to obtain a linked
    /// list of <see cref="Cmd"/> that reflects the execution sequence.
    /// </remarks>
    public class Cmd
    {
        static Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The branch that the command is part of (not null).
        /// </summary>
        public Branch Branch { get; }

        /// <summary>
        /// The data for the command (not null).
        /// </summary>
        /// <remarks>
        /// This corresponds to Branch.Commands[Data.Sequence]
        /// </remarks>
        public CmdData Data { get; }

        /// <summary>
        /// Any other commands that make reference to this
        /// command (null if there are no references).
        /// </summary>
        public List<Cmd> References { get; internal set; }

        /// <summary>
        /// Creates a new instance of <see cref="Cmd"/>
        /// that is not linked to any other command.
        /// </summary>
        /// <param name="branch">The branch that the command is part of (not null).</param>
        /// <param name="data">The data for the command (not null).</param>
        /// <exception cref="ArgumentNullException">One of the supplied parameters
        /// is undefined.</exception>
        public Cmd(Branch branch, CmdData data)
        {
            Branch = branch ?? throw new ArgumentNullException(nameof(Branch));
            Data = data ?? throw new ArgumentNullException(nameof(Data));
            References = null;
        }

        public override string ToString()
        {
            return $"{Branch.GetBranchPath()}[{Data.Sequence}]";
        }
    }
}
