using System;

namespace AltLib
{
    /// <summary>
    /// Property access helper for an instance of <see cref="CmdData"/>
    /// where <see cref="CmdData.CmdName"/> has a value of "ICreateStore".
    /// </summary>
    public partial class CmdData : ICreateStore
    {
        /// <summary>
        /// The unique ID that should be used to identify the store.
        /// </summary>
        Guid ICreateStore.StoreId => this.GetGuid(nameof(ICreateStore.StoreId));

        /// <summary>
        /// The user-perceived name for the store.
        /// </summary>
        string ICreateStore.Name => this.GetValue<string>(nameof(ICreateStore.Name));

        /// <summary>
        /// The persistence mechanism to be used for saving command data.
        /// </summary>
        StoreType ICreateStore.Type => this.GetEnum<StoreType>(nameof(ICreateStore.Type));
    }
}
