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
        public void MultiLevel()
        {
            var s = CreateSession("A");

            s.Execute(new string[]
            {
                // [1], [2], =>B, =>C, [3], [4], [5]
                "name", "name", "mkdir B", "mkdir C", "name", "name", "name",
                // B[1], B[2], B[3]=merge A[3,5]
                "cd B", "name", "name", "merge ..",
                // C[1], C[2], C[3]=merge A[3,5], C[4], C[5]
                "cd ../C", "name", "name", "merge ..", "name", "name",
                // B[4], B[5], B[6]
                "cd ../B", "name", "name", "name",
                // cd A, [6], [7], [8]
                "cd ..", "name", "name", "name",
                // C[6]=merge A[6,8], C[6]=>D
                "cd C", "merge ..", "mkdir D",
                // D[1], D[2], D[3]
                "cd D", "name", "name", "name",
                // cd C, C[7]=merge D[1,3], C[8]
                "cd ..", "merge D", "name",
                // D[4]=merge C[7,8], D[5], D[6], D[7]
                "cd D", "merge ..", "name", "name", "name",
                // cd C, C[9]=merge D[4,7]
                "cd ..", "merge D",
                // cd A, [9]
                "cd ..", "name",
                // B[7]=merge A[6,9], B[8], B[9], B[10]
                "cd B", "merge ..", "name", "name", "name",
                // cd A, [10], [11]=merge B[0,10], [12]=merge C[0,9], [13]
                "cd ..", "name", "merge B", "merge C", "name",
                // C[10], C[11]=merge A[9,13]
                "cd C", "name", "merge .."
            });

            var st = s.Stream;
            var expectC = new string[]
            {
                "[1]", "[2]", "C[1]", "C[2]", "B[1]", "B[2]", "[3]", "[4]", "[5]",
                "C[4]", "C[5]", "B[4]", "B[5]", "B[6]", "[6]", "[7]", "[8]", "[9]",
                "[10]", "B[8]", "B[9]", "B[10]", "C/D[1]", "C/D[2]", "C/D[3]", "C[8]",
                "C/D[5]", "C/D[6]", "C/D[7]", "C[10]", "[13]",
            };
            CheckNames(s.Stream, expectC);

            s.Execute("cd D");
            var expectD = new string[]
            {
                "[1]", "[2]", "C[1]", "C[2]", "[3]", "[4]",
                "[5]", "C[4]", "C[5]", "[6]", "[7]", "[8]",
                "C/D[1]", "C/D[2]", "C/D[3]", "C[8]", "C/D[5]", "C/D[6]",
                "C/D[7]"
            };
            CheckNames(s.Stream, expectD);

            s.Execute("cd ../.."); // A
            var expectA = new string[]
            {
                "[1]", "[2]", "B[1]", "B[2]", "C[1]",
                "C[2]", "[3]", "[4]", "[5]", "B[4]", "B[5]", "B[6]", "C[4]",
                "C[5]", "[6]", "[7]", "[8]", "[9]", "[10]", "B[8]", "B[9]",
                "B[10]", "C/D[1]", "C/D[2]", "C/D[3]", "C[8]",
                "C/D[5]", "C/D[6]", "C/D[7]", "[13]"
            };
            CheckNames(s.Stream, expectA);

            s.Execute("cd B");
            var expectB = new string[]
            {
                "[1]", "[2]", "B[1]", "B[2]", "[3]", "[4]",
                "[5]", "B[4]", "B[5]", "B[6]", "[6]", "[7]", "[8]", "[9]",
                "B[8]", "B[9]", "B[10]"
            };
            CheckNames(s.Stream, expectB);

            // Bring everything into sync

            s.Execute(new string[]
            {
                "merge ..",
                "cd ../C/D", "merge ..",
                "cd ../..", "merge C",
                "cd B", "merge ..",
                "cd .."
            });

            // All four branches should now have identical streams

            var expectAll = new string[]
            {
                "[1]", "[2]", "B[1]", "B[2]", "C[1]", "C[2]", "[3]", "[4]",
                "[5]", "B[4]", "B[5]", "B[6]", "C[4]", "C[5]", "[6]", "[7]",
                "[8]", "[9]", "[10]", "B[8]", "B[9]", "B[10]", "C/D[1]", "C/D[2]",
                "C/D[3]", "C[8]", "C/D[5]", "C/D[6]", "C/D[7]", "C[10]", "[13]"
            };

            Assert.AreEqual<uint>(0, s.Current.AheadCount);
            Assert.AreEqual<uint>(0, s.Current.BehindCount);
            CheckNames(s.Stream, expectAll);

            s.Execute("cd B");
            Assert.AreEqual<uint>(0, s.Current.AheadCount);
            Assert.AreEqual<uint>(0, s.Current.BehindCount);
            CheckNames(s.Stream, expectAll);

            s.Execute("cd ../C");
            Assert.AreEqual<uint>(0, s.Current.AheadCount);
            Assert.AreEqual<uint>(0, s.Current.BehindCount);
            CheckNames(s.Stream, expectAll);

            s.Execute("cd D");
            Assert.AreEqual<uint>(0, s.Current.AheadCount);
            Assert.AreEqual<uint>(0, s.Current.BehindCount);
            CheckNames(s.Stream, expectAll);
        }

        void CheckNames(CmdStream stream, string[] expect)
        {
            var actual = GetNames(stream).ToArray();

            Assert.AreEqual<int>(expect.Length, actual.Length);

            for (int i = 0; i < actual.Length; i++)
                Assert.AreEqual<string>(expect[i], actual[i]);
        }

        /// <summary>
        /// A pair of child branches with the same command sequences
        /// </summary>
        [TestMethod]
        public void SimilarBranches()
        {
            var s = CreateSession("A");

            s.Execute(new string[]
            {
                "name", "mkdir B", "mkdir C", "name", "name", "name",
                "cd C", "name", "name", "merge ..", "name", "name",
                "cd ../B", "name", "name", "merge ..", "name", "name",
                "cd ..", "name", "merge B", "merge C",
                "cd B", "merge ..",
                "cd ../C", "merge ..",
                "cd .."
            });

            var expect = new string[]
            {
                "[1]", "B[1]", "B[2]", "C[1]", "C[2]", "[2]", "[3]",
                "[4]", "[5]", "B[4]", "B[5]", "C[4]", "C[5]"
            };

            Assert.AreEqual<uint>(0, s.Current.AheadCount);
            Assert.AreEqual<uint>(0, s.Current.BehindCount);
            CheckNames(s.Stream, expect);

            s.Execute("cd B");
            Assert.AreEqual<uint>(0, s.Current.AheadCount);
            Assert.AreEqual<uint>(0, s.Current.BehindCount);
            CheckNames(s.Stream, expect);

            s.Execute("cd ../C");
            Assert.AreEqual<uint>(0, s.Current.AheadCount);
            Assert.AreEqual<uint>(0, s.Current.BehindCount);
            CheckNames(s.Stream, expect);

        }

        /// <summary>
        /// A child branch that frequently merges from the parent, and
        /// which only makes occasional changes.
        /// </summary>
        [TestMethod]
        public void LazyChild()
        {
            var s = CreateSession("A");

            s.Execute(new string[]
            {
                "name", "name", "mkdir B", "name", "name",
                "cd B", "merge ..",
                "cd ..", "name",
                "cd B", "merge ..",
                "cd ..", "name",
                "cd B", "name", "merge ..",
                "cd ..", "name", "merge B", "name",
                "cd B", "merge .."
            });

            var expect = new string[]
            {
                "[1]", "[2]", "[3]", "[4]", "[5]",
                "B[3]", "[6]", "[7]", "[9]"
            };

            Assert.AreEqual<uint>(0, s.Current.AheadCount);
            Assert.AreEqual<uint>(0, s.Current.BehindCount);
            CheckNames(s.Stream, expect);

            s.Execute("cd ..");
            CheckNames(s.Stream, expect);
        }

        /// <summary>
        /// A parent branch that only makes occasional changes, but frequently
        /// merges from a child branch.
        /// </summary>
        [TestMethod]
        public void LazyParent()
        {
            var s = CreateSession("A");

            s.Execute(new string[]
            {
                "checkout B -b", "name", "name",
                "cd ..", "merge B",
                "cd B", "name",
                "cd ..", "merge B",
                "cd B", "name",
                "cd ..", "merge B", "name",
                "cd B", "merge ..", "name",
                "cd ..", "merge B",
            });

            var expect = new string[]
            {
                "B[1]", "B[2]", "B[3]", "B[4]", "[4]", "B[6]"
            };

            Assert.AreEqual<uint>(0, s.Current.AheadCount);
            Assert.AreEqual<uint>(0, s.Current.BehindCount);
            CheckNames(s.Stream, expect);

            s.Execute("cd B");
            CheckNames(s.Stream, expect);
        }
    }
}