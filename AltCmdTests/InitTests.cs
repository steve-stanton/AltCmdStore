using System;
using System.IO;
using AltLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AltCmdTests
{
    [TestClass]
    public class InitTests
    {
        [TestMethod]
        public void InitMemoryStore()
        {
            const string storeName = nameof(InitMemoryStore);
            CmdStore cs = Init(storeName, StoreType.Memory);
            Assert.AreEqual<string>(storeName, cs.Name);
        }

        [TestMethod]
        public void InitFileStore()
        {
            const string storeName = nameof(InitFileStore);
            string folder = Path.Combine(Path.GetTempPath(), storeName);

            try
            {
                CmdStore cs = Init(folder, StoreType.File);

                Assert.IsTrue(Directory.Exists(folder));
                Assert.IsTrue(File.Exists(Path.Combine(folder, "0.json")));
                Assert.IsTrue(File.Exists(Path.Combine(folder, ".root")));
                Assert.AreEqual<int>(Directory.GetFiles(folder).Length, 3);
                Assert.AreEqual<string>(storeName, cs.Name);
                Assert.AreEqual<string>(folder, cs.Root.DirectoryName);
            }

            finally
            {
                Directory.Delete(folder, true);
            }
        }

        CmdStore Init(string storeName, StoreType t)
        {
            CmdStore result = CmdStore.Create(storeName, t);
            Assert.IsNotNull(result);
            Assert.AreEqual<int>(1, result.Branches.Count);
            return result;
        }

    }
}
