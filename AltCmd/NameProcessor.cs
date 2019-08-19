using System;
using System.Collections.Generic;
using System.Diagnostics;
using AltLib;

namespace AltCmd
{
    /// <summary>
    /// A command processor that builds a set of names, based
    /// on the names supplied via <see cref="NameCmdLine"/>.
    /// </summary>
    class NameProcessor : ICmdProcessor
    {
        /// <summary>
        /// The names that are already known (may contain duplicates).
        /// </summary>
        /// <remarks>
        /// This forms the model for this processor. In a more realistic situation,
        /// the model would likely be defined using a separate model class.
        /// </remarks>
        List<string> Names { get; }

        /// <summary>
        /// The filter that identifies the commands consumed by this processor.
        /// </summary>
        SimpleCmdFilter CmdFilter { get; }

        /// <summary>
        /// Creates a new instance of <see cref="NameProcessor"/> with
        /// an empty set of names.
        /// </summary>
        /// <remarks>
        /// Names can only be added to the model via calls to <see cref="Process"/>.
        /// </remarks>
        internal NameProcessor()
        {
            Names = new List<string>();
            CmdFilter = new SimpleCmdFilter(new string[] { nameof(NameCmdLine) });
        }

        /// <summary>
        /// A filter that can identify the commands that
        /// are relevant to this processor.
        /// </summary>
        ICmdFilter ICmdProcessor.Filter => CmdFilter;

        /// <summary>
        /// Performs any processing of the command data.
        /// </summary>
        /// <param name="data">The data to be processed.</param>
        /// <remarks>The supplied data is expected to be relevant to this
        /// processor (the caller should have already applied any filter
        /// to the command stream).</remarks>
        void ICmdProcessor.Process(CmdData data)
        {
            Debug.Assert(data.CmdName == nameof(NameCmdLine));
            string name = data.GetValue<string>(nameof(NameCmdLine.Name));
            Names.Add(name);
        }

        /// <summary>
        /// Reverts any changes that were made via the last call to <see cref="Process"/>
        /// </summary>
        /// <param name="data">The data that was previously supplied to the
        /// <see cref="Process"/> method.</param>
        void ICmdProcessor.Undo(CmdData data)
        {
            Debug.Assert(data.CmdName == nameof(NameCmdLine));
            string name = data.GetValue<string>(nameof(NameCmdLine.Name));

            // I would expect it to be the last item in our list
            int lastIndex = Names.Count - 1;
            if (lastIndex < 0 || Names[lastIndex] != name)
                throw new ApplicationException("Not the last name added: " + name);

            Names.RemoveAt(lastIndex);
        }
    }
}
