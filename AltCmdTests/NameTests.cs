using System;
using AltCmd;
using AltLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AltCmdTests
{
    [TestClass]
    public class NameTests : TestBase
    {
        /// <summary>
        /// Adds a single name to the root branch of a new store.
        /// </summary>
        [TestMethod]
        public void AddNameToRoot()
        {
            const string testName = "Test";
            var s = CreateSession();
            s.Execute("name " + testName);
            Branch b = s.Current;
            Assert.AreEqual<int>(b.Commands.Count, 2);
            string resultName = b.Commands[1].GetValue<string>("Name");
            Assert.AreEqual<string>(testName, resultName);
        }

    }
}
