using System;

namespace AltLib
{
    abstract public class AltRef
    {
        /// <summary>
        /// Attempts to parse a string that is expected to correspond
        /// to some sort of reference.
        /// </summary>
        /// <param name="s">The string to be parsed.</param>
        /// <param name="result">The parsed reference (or null if the
        /// supplied string could not be parsed)</param>
        /// <returns>True if the supplied string was parsed without error.</returns>
        public static bool TryParse(string s, out AltRef result)
        {
            try
            {
                result = Create(s);
                return true;
            }

            catch { }
            result = null;
            return false;
        }

        /// <summary>
        /// Parses a string representation as an instance of <see cref="AltRef"/>
        /// (as produced by a prior call to <see cref="ToString"/>).
        /// </summary>
        /// <param name="s">The string to be parsed.</param>
        /// <returns>The corresponding instance</returns>
        public static AltRef Create(string s)
        {
            try
            {
                int sBracket = s.IndexOf('[');
                int eBracket = s.IndexOf(']', sBracket);

                string inner = s.Substring(sBracket + 1, eBracket - sBracket - 1);
                int dotPos = inner.IndexOf('.');
                uint sequence;
                uint item;

                if (dotPos < 0)
                {
                    sequence = UInt32.Parse(inner);
                    item = 0;
                }
                else
                {
                    sequence = UInt32.Parse(inner.Substring(0, dotPos));
                    item = UInt32.Parse(inner.Substring(dotPos + 1));
                }

                string propName = (eBracket + 1) == s.Length ? String.Empty : s.Substring(eBracket + 1);

                if (s.StartsWith("{..}"))
                    return new ParentRef(sequence, item, propName);

                if (s.StartsWith("{"))
                {
                    int eBrace = s.IndexOf('}');
                    Guid branchId = Guid.Parse(s.Substring(1, eBrace - 1));
                    return new AbsoluteRef(branchId, sequence, item, propName);
                }

                return new LocalRef(sequence, item, propName);
            }

            catch (Exception e)
            {
                throw new FormatException("Cannot parse reference", e);
            }
        }

        /// <summary>
        /// The 0-based sequence number of a command within the branch.
        /// </summary>
        public uint Sequence { get; }

        /// <summary>
        /// The number of an item that was created by the command (0 refers
        /// to the overall command).
        /// </summary>
        /// <remarks>
        /// A value of 1 would typically be used for the first output object,
        /// 2 for the second, and so on. However, the way that output objects
        /// are numbered is unspecified (there may even be gaps in the sequence
        /// if it makes sense to do so).
        /// <para/>
        /// The command itself must know how to relate each item number to the
        /// corresponding output object. Bear in mind that references are
        /// immutable. If a command supports mutation via a subsequent update
        /// command (which could conceivably change the number of output
        /// objects) it must also ensure that the original item numbering is
        /// maintained.
        /// </remarks>
        public uint Item { get; }

        /// <summary>
        /// The name of a specific property within the referenced item (blank
        /// if the reference is to the item as a while)
        /// </summary>
        /// <remarks>
        /// There is perhaps no essential reason why this needs to be a property
        /// name. It might be better to think of this as some sort of identifier
        /// that the command knows the meaning of.
        /// </remarks>
        public string PropertyName { get; }

        /// <summary>
        /// Creates a new instance of <see cref="AltRef"/>
        /// </summary>
        /// <param name="sequence">The 0-based branch sequence number of a command that is being referenced.</param>
        /// <param name="item">The number of an item that was created by the command (0 refers
        /// to the overall command).</param>
        /// <param name="propertyName">The (optional) name of a property within the referenced object</param>
        protected AltRef(uint sequence, uint item, string propertyName)
        {
            Sequence = sequence;
            Item = item;
            PropertyName = propertyName;
        }

        /// <summary>
        /// Obtains the branch that is referenced by this instance.
        /// </summary>
        /// <param name="from">The branch that holds this instance</param>
        /// <returns>The branch that is being referred to.</returns>
        abstract protected Branch GetReferencedBranch(Branch from);
    }
}
