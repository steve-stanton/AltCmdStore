using System;
using System.IO;

namespace AltLib
{
    /// <summary>
    /// Extension methods for instances of <see cref="String"/>
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Checks whether two strings are equal,
        /// using <see cref="StringComparison.InvariantCultureIgnoreCase"/>
        /// </summary>
        /// <param name="a">The string to be checked</param>
        /// <param name="b">The string to compare with (could be null)</param>
        /// <returns>True if the strings are equal (ignoring case)</returns>
        public static bool EqualsIgnoreCase(this string a, string b)
        {
            return String.Equals(a, b, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Checks whether a string starts with a specified substring,
        /// using <see cref="StringComparison.InvariantCultureIgnoreCase"/>
        /// </summary>
        /// <param name="fullString">The string to be checked.</param>
        /// <param name="prefix">The string to look for at the start of <paramref name="fullString"/></param>
        /// <returns>True if <paramref name="fullString"/> begins with
        /// <paramref name="prefix"/> (ignoring case). False if there is
        /// no match, or either string is null or empty.</returns>
        public static bool StartsWithIgnoreCase(this string fullString, string prefix)
        {
            if (String.IsNullOrEmpty(fullString) || String.IsNullOrEmpty(prefix))
                return false;
            else
                return fullString.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
