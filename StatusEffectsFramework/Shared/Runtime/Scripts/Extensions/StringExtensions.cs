using System.Text.RegularExpressions;

namespace StatusEffectsFramework
{
    /// <summary>
    /// Helpers for <see cref="string"/>s.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Converts text to lowercase words separated by dashes, for example <c>HTTPServer Name</c> becomes <c>http-server-name</c>.
        /// Null or empty text is returned as is.
        /// </summary>
        public static string ToKebabCase(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Insert a dash between a lowercase/digit and a following uppercase letter
            // e.g. abcDef -> abc-Def
            string result = Regex.Replace(input, "(?<=[a-z0-9])(?=[A-Z])", "-");

            // Insert a dash before the last uppercase letter of a run when it's
            // followed by a lowercase letter (splits acronyms from the word after them)
            // e.g. HTTPServer -> HTTP-Server
            result = Regex.Replace(result, "(?<=[A-Z])(?=[A-Z][a-z])", "-");

            // Collapse any whitespace runs into a single dash
            result = Regex.Replace(result, @"\s+", "-");

            // Collapse multiple consecutive dashes into one
            result = Regex.Replace(result, "-+", "-");

            // Trim leading/trailing dashes and lowercase everything
            return result.Trim('-').ToLowerInvariant();
        }
    }
}
