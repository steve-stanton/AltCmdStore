using System;
using System.Collections.Generic;
using System.Diagnostics;
using AltLib;

namespace AltNames
{
    /// <summary>
    /// A command processor that builds a set of names, based
    /// on the names supplied via <see cref="AddCmdLine"/>.
    /// </summary>
    class NameProcessor : ICmdProcessor
    {
        /// <summary>
        /// The number of names that were removed on a cut.
        /// </summary>
        /// <remarks>This demonstrates how the processor might append additional
        /// command parameters derived from the processing.</remarks>
        const string CutCount = null;

        /// <summary>
        /// The names that are already known (may contain duplicates).
        /// </summary>
        /// <remarks>
        /// This forms the model for this processor. In a more realistic situation,
        /// the model would likely be defined using a separate model class.
        /// </remarks>
        internal List<string> Names { get; }

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
            CmdFilter = new SimpleCmdFilter(new string[]
                            { AddCmdLine.CmdName, CutCmdLine.CmdName });
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
            // The command names handled here need to be included
            // via the CmdFilter property

            if (data.CmdName == AddCmdLine.CmdName)
            {
                string name = data.GetValue<string>(nameof(AddCmdLine.Name));
                Names.Add(name);
            }
            else if (data.CmdName == CutCmdLine.CmdName)
            {
                string name = data.GetValue<string>(nameof(CutCmdLine.Name));
                int nRem = Names.RemoveAll(x => x == name);
                data.Add(nameof(NameProcessor.CutCount), nRem);
            }
            else
            {
                throw new ProcessorException(this, "Unexpected command: " + data.CmdName);
            }
        }

        /// <summary>
        /// Reverts any changes that were made via the last call to <see cref="Process"/>
        /// </summary>
        /// <param name="data">The data that was previously supplied to the
        /// <see cref="Process"/> method.</param>
        void ICmdProcessor.Undo(CmdData data)
        {
            if (data.CmdName == AddCmdLine.CmdName)
            {
                string name = data.GetValue<string>(nameof(AddCmdLine.Name));

                // I would expect it to be the last item in our list
                int lastIndex = Names.Count - 1;
                if (lastIndex < 0 || Names[lastIndex] != name)
                    throw new ApplicationException("Not the last name added: " + name);

                Names.RemoveAt(lastIndex);
            }
            else if (data.CmdName == CutCmdLine.CmdName)
            {
                string name = data.GetValue<string>(nameof(AddCmdLine.Name));
                int nCut = data.GetValue<int>(nameof(NameProcessor.CutCount));

                for (int i = 0; i < nCut; i++)
                    Names.Add(name);
            }
            else
            {
                throw new ProcessorException(this, "Unexpected command: " + data.CmdName);
            }
        }
    }
}
