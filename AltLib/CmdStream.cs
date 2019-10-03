using System;
using System.Collections.Generic;

namespace AltLib
{
    public class CmdStream
    {
        /// <summary>
        /// The main branch for this stream (not null).
        /// </summary>
        public Branch Main { get; }

        public LinkedList<Cmd> Cmds { get; }

        /// <summary>
        /// Creates a new instance of <see cref="CmdStream"/> and loads it.
        /// </summary>
        /// <param name="main">The main branch for this stream (not null).</param>
        /// <exception cref="ArgumentNullException">The specified main branch is undefined.</exception>
        public CmdStream(Branch main, IEnumerable<Cmd> commands)
        {
            Main = main ?? throw new ArgumentNullException(nameof(main));
            Cmds = new LinkedList<Cmd>(commands);
        }
    }
}
