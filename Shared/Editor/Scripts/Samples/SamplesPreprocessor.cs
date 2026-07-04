// DefinePreprocessor.cs
// Minimal conditional-compilation resolver for #if / #elif / #else / #endif.
// Lines outside any #if block are always kept (your shared "base" code).
// The directive lines themselves are stripped from the output -- only the
// code that would actually compile for the given defined symbols survives.

using System.Collections.Generic;
using System.Text;

namespace StatusEffectsFramework.Editor
{
    public static class SamplesPreprocessor
    {
        private struct Frame
        {
            public bool CondTrue;
            public bool BranchTaken;
            public bool ParentEnabled;
            public bool Enabled => ParentEnabled && CondTrue;
        }

        public static string Resolve(string source, HashSet<string> definedSymbols)
        {
            var lines = source.Replace("\r\n", "\n").Split('\n');
            var output = new StringBuilder();
            var stack = new Stack<Frame>();

            foreach (var rawLine in lines)
            {
                string trimmed = rawLine.TrimStart();

                if (trimmed.StartsWith("#if "))
                {
                    string expr = trimmed.Substring(4).Trim();
                    bool parentEnabled = stack.Count == 0 || stack.Peek().Enabled;
                    bool condTrue = parentEnabled && SamplesExpressionEvaluator.Evaluate(expr, definedSymbols);
                    stack.Push(new Frame { BranchTaken = condTrue, CondTrue = condTrue, ParentEnabled = parentEnabled });
                    continue;
                }

                if (trimmed.StartsWith("#elif "))
                {
                    if (stack.Count == 0) continue; // malformed input, ignore rather than throw
                    var frame = stack.Pop();
                    bool condTrue;
                    if (frame.BranchTaken)
                    {
                        condTrue = false;
                    }
                    else
                    {
                        string expr = trimmed.Substring(6).Trim();
                        condTrue = frame.ParentEnabled && SamplesExpressionEvaluator.Evaluate(expr, definedSymbols);
                        frame.BranchTaken = condTrue;
                    }
                    frame.CondTrue = condTrue;
                    stack.Push(frame);
                    continue;
                }

                if (trimmed.StartsWith("#else"))
                {
                    if (stack.Count == 0) continue;
                    var frame = stack.Pop();
                    frame.CondTrue = frame.ParentEnabled && !frame.BranchTaken;
                    frame.BranchTaken = true;
                    stack.Push(frame);
                    continue;
                }

                if (trimmed.StartsWith("#endif"))
                {
                    if (stack.Count > 0)
                        stack.Pop();
                    continue;
                }

                bool enabled = stack.Count == 0 || stack.Peek().Enabled;
                if (enabled)
                    output.AppendLine(rawLine);
            }

            return output.ToString();
        }
    }
}