// https://gist.github.com/Sup3rc4l1fr4g1l1571c3xp14l1d0c10u5/3341dba6a53d7171fe3397d13d00ee3f

using System;
using System.Collections.Generic;
using System.Linq;

namespace AltLib
{
    /*
    static class Program {
        static void Main() {
            //
            // digraph G {
            //   "7"  -> "11"
            //   "7"  -> "8"
            //   "5"  -> "11"
            //   "3"  -> "8"
            //   "3"  -> "10"
            //   "11" -> "2"
            //   "11" -> "9"
            //   "11" -> "10"
            //   "8"  -> "9"
            // }

            var ret = TopologicalSort(
                new HashSet<int>(new[] {7, 5, 3, 8, 11, 2, 9, 10}),
                new HashSet<Tuple<int, int>>(
                    new [] {
                        Tuple.Create(7, 11),
                        Tuple.Create(7, 8),
                        Tuple.Create(5, 11),
                        Tuple.Create(3, 8),
                        Tuple.Create(3, 10),
                        Tuple.Create(11, 2),
                        Tuple.Create(11, 9),
                        Tuple.Create(11, 10),
                        Tuple.Create(8, 9)
                    }
                )
            );
            System.Diagnostics.Debug.Assert(ret.SequenceEqual(new[] {7, 5, 11, 2, 3, 8, 9, 10}));
        }
        */

    public static class Util
    {
        public static void Test()
        {
            var edges = new HashSet<Tuple<string, string>>(new Tuple<string, string>[]
            {
                Tuple.Create("A0", "A1"),
                Tuple.Create("A1", "B0"),
                Tuple.Create("A1", "C0"),
                Tuple.Create("A1", "A2"),
                Tuple.Create("A2", "A4"),
                Tuple.Create("A4", "A5"),
                Tuple.Create("A4", "B4"),
                Tuple.Create("A4", "C4"),
                Tuple.Create("A5", "B0"),

                Tuple.Create("B0", "B2"),
                Tuple.Create("B2", "A2"),
                Tuple.Create("B4", "B5"),
                Tuple.Create("B5", "A5"),

                Tuple.Create("C0", "C2"),
                Tuple.Create("C2", "A2"),
                Tuple.Create("C4", "C5"),
                Tuple.Create("C5", "A5"),
            });

            var nodes = new HashSet<string>();

            foreach (var t in edges)
            {
                nodes.Add(t.Item1);
                nodes.Add(t.Item2);
            }

            try
            {
                string[] result = TopologicalSort<string>(nodes, edges)?.ToArray();
                if (result == null)
                    Console.WriteLine("no result");
                else
                    Console.WriteLine(String.Join(", ", result));
            }

            catch (Exception ex)
            {
            }
        }

        /// <summary>
        /// Topological Sorting (Kahn's algorithm) 
        /// </summary>
        /// <remarks>https://en.wikipedia.org/wiki/Topological_sorting</remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="nodes">All nodes of directed acyclic graph.</param>
        /// <param name="edges">All edges of directed acyclic graph.</param>
        /// <returns>Sorted node in topological order.</returns>
        public static List<T> TopologicalSort<T>(HashSet<T> nodes, HashSet<Tuple<T, T>> edges) where T : IEquatable<T> {
            // Empty list that will contain the sorted elements
            var L = new List<T>();

            // Set of all nodes with no incoming edges
            var S = new HashSet<T>(nodes.Where(n => edges.All(e => e.Item2.Equals(n) == false)));

            // while S is non-empty do
            while (S.Any()) {

                //  remove a node n from S
                var n = S.First();
                S.Remove(n);

                // add n to tail of L
                L.Add(n);
                Console.WriteLine(n);

                // for each node m with an edge e from n to m do
                foreach (var e in edges.Where(e => e.Item1.Equals(n)).ToList()) {
                    var m = e.Item2;

                    // remove edge e from the graph
                    edges.Remove(e);

                    // if m has no other incoming edges then
                    if (edges.All(me => me.Item2.Equals(m) == false)) {
                        // insert m into S
                        S.Add(m);
                    }
                }
            }

            // if graph has edges then
            if (edges.Any()) {
                // return error (graph has at least one cycle)
                return null;
            } else {
                // return L (a topologically sorted order)
                return L;
            }
        }
    }
}
