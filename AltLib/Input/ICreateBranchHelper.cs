namespace AltLib
{
    /// <summary>
    /// Property access helper for an instance of <see cref="CmdData"/>
    /// where <see cref="CmdData.CmdName"/> has a value of "ICreateBranch".
    /// </summary>
    public partial class CmdData : ICreateBranch
    {
        /// <summary>
        /// The user-perceived name for the new branch
        /// </summary>
        /// <remarks>
        /// The same branch name may be repeated throughout a
        /// command store. But for a given parent branch, all
        /// immediate child branches must have distinct names.
        /// </remarks>
        string ICreateBranch.Name => this.GetValue<string>(nameof(ICreateBranch.Name));

        /// <summary>
        /// Identifier that denotes where the branch should
        /// be taken from.
        /// </summary>
        /// <remarks>
        /// For the time being, this must hold the 0-based
        /// sequence number of the command to branch from
        /// within the parent branch.
        /// </remarks>
        uint ICreateBranch.CommandCount => this.GetValue<uint>(nameof(ICreateBranch.CommandCount));
    }
}
