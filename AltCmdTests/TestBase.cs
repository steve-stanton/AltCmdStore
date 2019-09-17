using System;
using AltCmd;
using AltLib;

namespace AltCmdTests
{
    public class TestBase
    {
        /// <summary>
        /// Creates an instance of <see cref="AltCmdSession"/>
        /// that refers to a new instance of <see cref="MemoryStore"/>,
        /// with a store name that matches the name of the
        /// derived test class.
        /// </summary>
        /// <returns>The newly created command store</returns>
        protected AltCmdSession CreateSession()
        {
            return CreateSession(GetType().Name);
        }

        /// <summary>
        /// Creates an instance of <see cref="AltCmdSession"/>
        /// that refers to a new instance of <see cref="MemoryStore"/>.
        /// </summary>
        /// <param name="storeName">The name for the new store.</param>
        /// <returns>The newly created command store</returns>
        protected AltCmdSession CreateSession(string storeName)
        {
            var cs = CmdStore.Create(storeName, StoreType.Memory);
            var ec = new ExecutionContext(cs);
            return new AltCmdSession(ec);
        }
    }
}
