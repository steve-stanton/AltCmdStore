using System;

namespace AltLib
{
    /// <summary>
    /// Property access helper for an instance of <see cref="CmdData"/>
    /// where <see cref="CmdData.CmdName"/> has a value of "ICloneStore".
    /// </summary>
    /// <remarks>
    /// Additional properties relating to cloning are specified as
    /// part of the <see cref="ICreateStore"/> interface.
    /// </remarks>
    public partial class CmdData : ICloneStore
    {
        /// <summary>
        /// The name of the store to clone from (possibly including a folder path).
        /// </summary>
        /// <remarks>When cloning from another local store, this provides
        /// the path to the root folder for the store.</remarks>
        string ICloneStore.Origin => this.GetValue<string>(nameof(ICloneStore.Origin));
    }
}
