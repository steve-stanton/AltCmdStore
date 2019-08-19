using System;

namespace AltLib
{
    /// <summary>
    /// Property access helper for an instance of <see cref="CmdData"/>
    /// where <see cref="CmdData.CmdName"/> has a value of "IMerge".
    /// </summary>
    public partial class CmdData : IMerge
    {
        /// <summary>
        /// The ID of the branch to merge into the current branch.
        /// </summary>
        /// <remarks>
        /// The ID must refer to either the parent of the current branch,
        /// or one of its child branches.
        /// <para/>
        /// The ID of the branch we are merging into is not one of the
        /// input parameters. That is known via the branch that the
        /// merge command gets saved to.
        /// </remarks>
        Guid IMerge.FromId => this.GetGuid(nameof(IMerge.FromId));

        /// <summary>
        /// The 0-based sequence number of the first command that
        /// needs to be merged.
        /// </summary>
        uint IMerge.MinCmd => this.GetValue<uint>(nameof(IMerge.MinCmd));

        /// <summary>
        /// The 0-based sequence number of the last command that
        /// needs to be merged.
        /// </summary>
        uint IMerge.MaxCmd => this.GetValue<uint>(nameof(IMerge.MaxCmd));
    }
}
