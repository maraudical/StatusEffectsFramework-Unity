using System.Text;

namespace StatusEffectsFramework
{
    /// <summary>
    /// Helpers for <see cref="string"/>s.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Converts text to PascalCase, for example <c>max health</c> becomes <c>MaxHealth</c> and <c>basic_stun-v2</c>
        /// becomes <c>BasicStunV2</c>. Anything that isn't a letter or digit separates words and is removed, and the rest
        /// of each word keeps its case, so <c>HTTPServer</c> stays as it is. Null or empty text is returned as is.
        /// </summary>
        public static string ToPascalCase(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var builder = new StringBuilder(input.Length);
            bool startOfWord = true;
            foreach (char c in input)
            {
                if (!char.IsLetterOrDigit(c))
                {
                    startOfWord = true;
                    continue;
                }

                builder.Append(startOfWord ? char.ToUpperInvariant(c) : c);
                startOfWord = false;
            }

            return builder.ToString();
        }
    }
}
