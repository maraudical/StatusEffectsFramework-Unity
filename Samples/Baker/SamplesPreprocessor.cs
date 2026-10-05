using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StatusEffectsFramework.Editor
{
    // Resolves #if blocks that only test sample defines, keeping the branches for the defines that are on
    // and dropping the directives themselves. Blocks that test anything else (e.g. UNITY_2023_1_OR_NEWER)
    // are left untouched. Mixing the two in one block is an error since it can't be resolved when baking.
    internal static class SamplesPreprocessor
    {
        private static readonly Regex Directive = new(@"^\s*#\s*(if|elif|else|endif)\b(.*)$");
        private static readonly Regex Token = new(@"\|\||&&|[()!]|\w+|\S");

        private class Block
        {
            public bool Resolved;     // Tests sample defines, so its directives get stripped
            public bool ParentActive;
            public bool Active;       // The current branch is kept
            public bool Taken;        // An earlier branch was already kept
        }

        public static IEnumerable<string> DefinesIn(string source) =>
            source.Split('\n')
                .Select(line => Directive.Match(line))
                .Where(match => match.Success)
                .SelectMany(match => Symbols(StripComment(match.Groups[2].Value)))
                .Distinct();

        public static string Resolve(string source, HashSet<string> sampleDefines, HashSet<string> enabled)
        {
            string newline = source.Contains("\r\n") ? "\r\n" : "\n";
            string[] lines = source.Replace("\r\n", "\n").Split('\n');
            var output = new List<string>();
            var blocks = new Stack<Block>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                bool active = blocks.Count == 0 || blocks.Peek().Active;
                var match = Directive.Match(line);
                if (!match.Success)
                {
                    if (active)
                        output.Add(line);
                    continue;
                }

                int lineNumber = i + 1;
                string expression = StripComment(match.Groups[2].Value);
                Block block;
                switch (match.Groups[1].Value)
                {
                    case "if":
                        block = new Block { Resolved = IsSampleCondition(expression, sampleDefines, lineNumber), ParentActive = active };
                        block.Active = block.Resolved ? active && Evaluate(expression, enabled) : active;
                        block.Taken = block.Active;
                        blocks.Push(block);
                        break;
                    case "elif":
                        block = Current(blocks, lineNumber);
                        if (IsSampleCondition(expression, sampleDefines, lineNumber) != block.Resolved)
                            throw new FormatException($"Line {lineNumber}: #elif has to test the same kind of defines as its #if.");
                        if (block.Resolved)
                        {
                            block.Active = block.ParentActive && !block.Taken && Evaluate(expression, enabled);
                            block.Taken |= block.Active;
                        }
                        break;
                    case "else":
                        block = Current(blocks, lineNumber);
                        if (block.Resolved)
                        {
                            block.Active = block.ParentActive && !block.Taken;
                            block.Taken = true;
                        }
                        break;
                    default:
                        block = Current(blocks, lineNumber);
                        blocks.Pop();
                        break;
                }

                if (!block.Resolved && block.ParentActive)
                    output.Add(line);
            }

            if (blocks.Count > 0)
                throw new FormatException("An #if is missing its #endif.");

            return string.Join(newline, output);
        }

        private static Block Current(Stack<Block> blocks, int lineNumber) =>
            blocks.Count > 0 ? blocks.Peek() : throw new FormatException($"Line {lineNumber}: directive has no matching #if.");

        private static bool IsSampleCondition(string expression, HashSet<string> sampleDefines, int lineNumber)
        {
            var symbols = Symbols(expression).ToList();
            int sampleCount = symbols.Count(sampleDefines.Contains);
            if (sampleCount > 0 && sampleCount < symbols.Count)
                throw new FormatException($"Line {lineNumber}: '{expression}' mixes sample defines with other defines, which can't be resolved when baking.");
            return sampleCount > 0;
        }

        private static bool Evaluate(string expression, HashSet<string> enabled)
        {
            var tokens = Tokenize(expression);
            int position = 0;
            bool result = Or();
            if (position < tokens.Count)
                throw new FormatException($"Unexpected '{tokens[position]}' in '{expression}'.");
            return result;

            bool Or()
            {
                bool value = And();
                while (Accept("||"))
                    value |= And();
                return value;
            }

            bool And()
            {
                bool value = Unary();
                while (Accept("&&"))
                    value &= Unary();
                return value;
            }

            bool Unary() => Accept("!") ? !Unary() : Primary();

            bool Primary()
            {
                if (Accept("("))
                {
                    bool value = Or();
                    if (!Accept(")"))
                        throw new FormatException($"Missing ')' in '{expression}'.");
                    return value;
                }
                if (position >= tokens.Count || !IsSymbol(tokens[position]) && tokens[position] != "true" && tokens[position] != "false")
                    throw new FormatException($"Can't evaluate '{expression}'.");

                string token = tokens[position++];
                return token == "true" || enabled.Contains(token);
            }

            bool Accept(string token)
            {
                if (position >= tokens.Count || tokens[position] != token)
                    return false;
                position++;
                return true;
            }
        }

        private static List<string> Tokenize(string expression) =>
            Token.Matches(expression).Cast<Match>().Select(match => match.Value).ToList();

        private static IEnumerable<string> Symbols(string expression) => Tokenize(expression).Where(IsSymbol);

        private static bool IsSymbol(string token) =>
            (char.IsLetter(token[0]) || token[0] == '_') && token != "true" && token != "false";

        private static string StripComment(string expression)
        {
            int comment = expression.IndexOf("//", StringComparison.Ordinal);
            return (comment >= 0 ? expression.Substring(0, comment) : expression).Trim();
        }
    }
}
