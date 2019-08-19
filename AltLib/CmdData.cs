using System;
using System.Collections.Generic;

namespace AltLib
{
    /// <summary>
    /// The data input for a command.
    /// </summary>
    public partial class CmdData : Dictionary<string, object>
    {
        /// <summary>
        /// Default constructor (for use by JSON deserializer).
        /// </summary>
        public CmdData()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CmdData"/> class.
        /// </summary>
        /// <param name="cmdName">The name of the command</param>
        /// <param name="sequence">The sequence number for this command (in the context of
        /// the branch where the command was first executed).</param>
        /// <param name="createdAt">The time (UTC) when the command was originally created.</param>
        public CmdData(string cmdName,
                       uint sequence,
                       DateTime createdAt)
        {
            CmdName = cmdName;
            Sequence = sequence;
            CreatedAt = createdAt;
        }

        // Provide convenience methods for accessing properties
        // that should always be present. The JSON serializer
        // is smart enough to only output these once (no need
        // to tag with [JsonIgnore]).

        /// <summary>
        /// A name for the command (providing some way to
        /// tell how this command should be executed).
        /// </summary>
        /// <remarks>
        /// This will typically be the name of the class (or
        /// interface for a class) that handles command execution.
        /// </remarks>
        public string CmdName
        {
            get { return this.GetValue<string>(nameof(CmdName)); }
            set { this[nameof(CmdName)] = value; }
        }

        /// <summary>
        /// The sequence number for the command (in the context of
        /// the branch where the command was first executed).
        /// </summary>
        public uint Sequence
        {
            get { return this.GetValue<uint>(nameof(Sequence)); }
            set { this[nameof(Sequence)] = value; }
        }

        /// <summary>
        /// The time (UTC) when the command was originally created.
        /// </summary>
        public DateTime CreatedAt
        {
            get { return this.GetDateTime(nameof(CreatedAt)); }
            set { this[nameof(CreatedAt)] = value; }
        }
    }
}
