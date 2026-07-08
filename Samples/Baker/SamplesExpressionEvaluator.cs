// ExpressionEvaluator.cs
// Evaluates simple preprocessor-style boolean expressions built from:
// identifiers, true/false literals, !, &&, ||, and parentheses.
// e.g. "SAMPLE_A", "!SAMPLE_A", "SAMPLE_A && !LEGACY", "(A || B) && !C"

using System.Collections.Generic;

namespace StatusEffectsFramework.Editor
{
    internal static class SamplesExpressionEvaluator
    {
        public static bool Evaluate(string expression, HashSet<string> definedSymbols)
        {
            var tokens = Tokenize(expression);
            int pos = 0;
            return ParseOr(tokens, ref pos, definedSymbols);
        }

        private static bool ParseOr(List<string> tokens, ref int pos, HashSet<string> defined)
        {
            bool left = ParseAnd(tokens, ref pos, defined);
            while (pos < tokens.Count && tokens[pos] == "||")
            {
                pos++;
                bool right = ParseAnd(tokens, ref pos, defined);
                left = left || right;
            }
            return left;
        }

        private static bool ParseAnd(List<string> tokens, ref int pos, HashSet<string> defined)
        {
            bool left = ParseUnary(tokens, ref pos, defined);
            while (pos < tokens.Count && tokens[pos] == "&&")
            {
                pos++;
                bool right = ParseUnary(tokens, ref pos, defined);
                left = left && right;
            }
            return left;
        }

        private static bool ParseUnary(List<string> tokens, ref int pos, HashSet<string> defined)
        {
            if (pos < tokens.Count && tokens[pos] == "!")
            {
                pos++;
                return !ParseUnary(tokens, ref pos, defined);
            }
            return ParsePrimary(tokens, ref pos, defined);
        }

        private static bool ParsePrimary(List<string> tokens, ref int pos, HashSet<string> defined)
        {
            if (pos >= tokens.Count)
                return false;

            string token = tokens[pos];

            if (token == "(")
            {
                pos++;
                bool value = ParseOr(tokens, ref pos, defined);
                if (pos < tokens.Count && tokens[pos] == ")")
                    pos++;
                return value;
            }

            pos++;
            if (token == "true") return true;
            if (token == "false") return false;
            return defined.Contains(token);
        }

        private static List<string> Tokenize(string expr)
        {
            var tokens = new List<string>();
            int i = 0;
            while (i < expr.Length)
            {
                char c = expr[i];

                if (char.IsWhiteSpace(c)) { i++; continue; }
                if (c == '(' || c == ')') { tokens.Add(c.ToString()); i++; continue; }
                if (c == '!') { tokens.Add("!"); i++; continue; }
                if (c == '&' && i + 1 < expr.Length && expr[i + 1] == '&') { tokens.Add("&&"); i += 2; continue; }
                if (c == '|' && i + 1 < expr.Length && expr[i + 1] == '|') { tokens.Add("||"); i += 2; continue; }

                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    int start = i;
                    while (i < expr.Length && (char.IsLetterOrDigit(expr[i]) || expr[i] == '_'))
                        i++;
                    tokens.Add(expr.Substring(start, i - start));
                    continue;
                }

                i++; // skip anything unrecognized rather than throw
            }
            return tokens;
        }
    }
}