using System.Collections.Generic;
using System.Linq;
using AltLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AltCmdTests
{
    [TestClass]
    public class MergeTests : TestBase
    {
        [TestMethod]
        public void MergeFromParent()
        {
            var s = CreateSession("A");

            s.Execute(new string[]
            {
                "name A1", "name A2", "branch B",
                "cd B", "name B1",
                "cd ..", "name A3",
                "cd B", "merge .."
            });

            Branch branch = s.Current;
            Assert.AreEqual<string>("B", branch.Info.BranchName);

            // The first command in the stream should always be the ICreateStore command
            Cmd first = s.Stream.Cmds.First.Value;
            Assert.AreEqual<string>(nameof(ICreateStore), first.Data.CmdName);

            // Check the whole stream
            var expect = new string[] { "A1", "A2", "B1", "A3" };
            CheckNames(s.Stream, expect);
        }

        [TestMethod]
        public void MergeToEmptyParent()
        {
            var s = CreateSession("A");

            s.Execute(new string[]
            {
                "checkout B --branch",
                "name B1", "name B2",
                "cd ..", "merge B",
            });

            Branch branch = s.Current;
            Assert.AreEqual<string>("A", branch.Info.BranchName);

            // Check the whole stream
            var expect = new string[] { "B1", "B2" };
            CheckNames(s.Stream, expect);
        }


        [TestMethod]
        public void MergeToEmptyChild()
        {
            var s = CreateSession("A");

            s.Execute(new string[]
            {
                "mkdir B",
                "name A1", "name A2",
                "cd B", "merge ..",
            });

            Branch branch = s.Current;
            Assert.AreEqual<string>("B", branch.Info.BranchName);

            // Check the whole stream
            var expect = new string[] { "A1", "A2" };
            CheckNames(s.Stream, expect);
        }

        IEnumerable<string> GetNames(CmdStream s)
        {
            foreach (Cmd c in s.Cmds.Where(x => x.Data.CmdName == "NameCmdLine"))
                yield return c.Data.GetValue<string>("Name");
        }

        [TestMethod]
        public void MergeFromChild()
        {
            var s = CreateSession("A");

            s.Execute(new string[]
            {
                "name A1",
                "checkout B --branch", "name B1",
                "cd ..", "merge B",
            });

            Branch b = s.Current;
            Assert.AreEqual<string>("A", b.Info.BranchName);

            // The child should come at the end
            var actual = GetNames(s.Stream).ToArray();
            Assert.AreEqual<int>(2, actual.Length);
            Assert.AreEqual<string>("A1", actual[0]);
            Assert.AreEqual<string>("B1", actual[1]);
        }

        [TestMethod]
        public void SplitMerge()
        {
            var s = CreateSession("A");

            s.Execute(new string[]
            {
                "name A1", "mkdir B", "name A2", "name A3",
                "cd B", "name B1", "name B2", "merge ..",
                "cd ..", "name A4", "name A5",
                "cd B", "name B4", "merge ..", "name B6",
                "cd ..", "name A6", "merge B", "name A8",
                "cd B", "merge ..",
                "cd .."
            });

            Branch a = s.Current;
            Assert.AreEqual<string>("A", a.Info.BranchName);

            Branch b = a.GetChild("B");
            Assert.AreEqual<uint>(0, b.AheadCount);
            Assert.AreEqual<uint>(0, b.BehindCount);

            var expect = new string[] { "A1", "B1", "B2", "A2", "A3", "B4",
                                        "A4", "A5", "A6", "B6", "A8" };
            CheckNames(s.Stream, expect);

            // It should be the same when we switch branches
            s.Execute("cd B");
            CheckNames(s.Stream, expect);
        }

        /// <summary>
        /// Tests a merge where the start of a merge range has to be
        /// injected prior to the start of a child branch.
        /// </summary>
        [TestMethod]
        public void ParentSpanBeforeZero()
        {
            var s = CreateSession("A");

            s.Execute(new string[]
            {
                "name A1", "mkdir B", "name A2", "name A3",
                "cd B", "name B1", "name B2",
                "cd ..", "merge B", "name A5",
                "cd B", "name B3", "merge ..", "name B5",
                "cd ..", "name A6",
                "cd B"
            });

            Branch b = s.Current;
            Assert.AreEqual<string>("B", b.Info.BranchName);
            Assert.AreEqual<uint>(2, b.AheadCount); // A doesn't have B3 or B5
            Assert.AreEqual<uint>(1, b.BehindCount); // B doesn't have A6

            var expectA = new string[] { "A1", "A2", "A3", "B1", "B2", "A5", "A6" };
            var expectB = new string[] { "A1", "A2", "A3", "B1", "B2", "B3", "A5", "B5" };

            CheckNames(s.Stream, expectB);

            s.Execute("cd ..");
            CheckNames(s.Stream, expectA);
        }

        /// <summary>
        /// Something quite complicated
        /// </summary>
        [TestMethod]
        public void MultiLevelMerge()
        {
            var s = CreateSession("A");

            s.Execute(new string[]
            {
                "name A1", "name A2", "name A3", "name A4", "name A5",
                "mkdir B", "cd B", "name B1", "name B2", "merge ..",
                "cd ..", "mkdir C", "cd C", "name C1", "name C2", "merge ..",
                "name C4", "name C5",
                "cd ../B", "name B4", "name B5", "name B6",
                "cd ..", "name A6", "name A7", "name A8",
                "cd C", "merge ..", "mkdir D",
                "cd D", "name D1", "name D2", "name D3",
                "cd ..", "merge ..", "name C8",
                "cd D", "merge ..", "name D5", "name D6", "name D7",
                "cd ..", "merge D",
                "cd ..", "name A9",
                "cd B", "merge ..", "name B8", "name B9", "name B10",
                "cd ..", "name A10", "merge B", "merge C", "name A11",
                "cd C", "name C10", "merge .."
            });

            var st = s.Stream;
            //var expect = new string[] {  };
            //CheckNames(s.Stream, expect);
        }

        void CheckNames(CmdStream stream, string[] expect)
        {
            var actual = GetNames(stream).ToArray();

            Assert.AreEqual<int>(expect.Length, actual.Length);

            for (int i = 0; i < actual.Length; i++)
                Assert.AreEqual<string>(expect[i], actual[i]);
        }
    }
}