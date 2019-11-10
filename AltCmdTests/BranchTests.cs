using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AltLib;

namespace AltCmdTests
{
    [TestClass]
    public class BranchTests : TestBase
    {
        [TestMethod]
        public void BranchFromEmptyRoot()
        {
            var cmds = new string[]
            {
                "branch a",
                "checkout a",
                "name a1",
                "name a2"
            };

            var s = CreateSession();

            foreach (string c in cmds)
                s.Execute(c);

            Branch b = s.Current;

            Assert.AreEqual<int>(b.Commands.Count, 3);
            Assert.AreEqual<string>(b.Name, "a");
            Assert.AreEqual<uint>(b.Info.CommandCount, 3);

            // The ICreateBranch is the one and only command
            // that can be ignored by the parent
            Assert.AreEqual<uint>(b.Info.CommandDiscount, 1);

            // The child knows about 1 command in the parent
            // (the command that created the store)
            Assert.AreEqual<uint>(b.Info.RefreshCount, 1);

            // The parent hasn't yet merged from the child
            Assert.AreEqual<uint>(b.Info.RefreshDiscount, 0);
        }
    }
}
