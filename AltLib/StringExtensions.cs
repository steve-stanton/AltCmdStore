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

        /// <summary>
        /// Checks whether a string is defined (satisfying !String.IsNullOrEmpty).
        /// </summary>
        /// <param name="s">The string to check</param>
        /// <returns><c>true</c> if String.IsNullOrEmpty returns false.</returns>
        public static bool IsDefined(this string s)
        {
            return !String.IsNullOrEmpty(s);
        }

        /// <summary>
        /// Produces a string that does not contains unnecessary plurals.
        /// </summary>
        /// <param name="s">The string to de-pluralize</param>
        /// <param name="pluralPrefix">The marker that precedes characters that may need to be stripped.</param>
        /// <returns>A potentially modified string with any unnecessary characters removed.</returns>
        /// <remarks>
        /// This is a matter of looking for a word that contains <paramref name="pluralPrefix"/>,
        /// then looking for a previous word that can be converted into an integer. If that
        /// number is "1", the letter(s) following the marker will be stripped out.
        /// </remarks>
        public static string TrimExtras(this string s, char pluralPrefix = '`')
        {
            if (String.IsNullOrEmpty(s))
                return s;

            string[] words = s.Split(' ');

            for (int i = 0; i < words.Length; i++)
            {
                int pp = words[i].IndexOf(pluralPrefix);

                if (pp > 0)
                {
                    bool strip = false;

                    for (int j = i - 1; j >= 0; j--)
                    {
                        if (Int32.TryParse(words[j], out int number))
                        {
                            strip = number == 1;
                            break;
                        }
                    }

                    if (strip)
                        words[i] = words[i].Substring(0, pp);
                    else
                        words[i] = words[i].Substring(0, pp) + words[i].Substring(pp + 1);
                }
            }

            return String.Join(" ", words);
        }
    }
}
