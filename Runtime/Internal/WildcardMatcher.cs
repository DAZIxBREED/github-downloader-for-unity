using System;
using System.Text;
using System.Text.RegularExpressions;

namespace DAZIxBREED.GitHubDownloader.Internal
{
    internal static class WildcardMatcher
    {
        public static bool IsMatch(string value, string pattern, bool ignoreCase = true)
        {
            if (value == null || pattern == null)
            {
                return false;
            }

            var regex = new StringBuilder("^");
            foreach (char character in pattern)
            {
                switch (character)
                {
                    case '*':
                        regex.Append(".*");
                        break;
                    case '?':
                        regex.Append('.');
                        break;
                    default:
                        regex.Append(Regex.Escape(character.ToString()));
                        break;
                }
            }

            regex.Append('$');
            RegexOptions options = RegexOptions.CultureInvariant;
            if (ignoreCase)
            {
                options |= RegexOptions.IgnoreCase;
            }

            return Regex.IsMatch(value, regex.ToString(), options);
        }
    }
}
