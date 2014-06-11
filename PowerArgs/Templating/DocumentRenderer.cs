using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    public class DocumentRenderedException : Exception
    {
        public DocumentRenderedException(string msg) : base(msg) { }
    }

    public static class DocumentRenderer
    {
        public static ConsoleString Render(string template, object data)
        {
            List<DocumentToken> tokens = DocumentToken.Tokenize(template);
            List<DocumentToken> filtered = RemoveLinesThatOnlyContainReplacements(tokens);
            return Render(filtered, new DataContext(data));
        }

        public static ConsoleString Render(string template, DataContext context)
        {
            List<DocumentToken> tokens = DocumentToken.Tokenize(template);
            return Render(tokens, context);
        }

        internal static ConsoleString Render(List<DocumentToken> tokens, DataContext context)
        {
            DocumentExpressionParser parser = new DocumentExpressionParser();
            var expressions = parser.Parse(tokens);
            var ret = DocumentRenderer.Evaluate(expressions, context);
            return ret;
        }

        private static ConsoleString Evaluate(List<IDocumentExpression> expressions, DataContext context)
        {
            ConsoleString ret = new ConsoleString();

            foreach (var expression in expressions)
            {
                ret += expression.Evaluate(context);
            }

            return ret;
        }

        private static List<DocumentToken> RemoveLinesThatOnlyContainReplacements(List<DocumentToken> tokens)
        {
            var currentLine = 1;
            int numContentTokensOnCurrentLine = 0;
            int numReplacementTokensOnCurrentLine = 0;

            List<DocumentToken> filtered = new List<DocumentToken>();
            foreach(var token in tokens)
            {
                if(token.Line != currentLine)
                {
                    currentLine = token.Line;

                    if(numContentTokensOnCurrentLine == 0 && numReplacementTokensOnCurrentLine > 0)
                    {
                        if(filtered.Count >= 2 && filtered[filtered.Count-2].Value == "\r" && filtered[filtered.Count-1].Value == "\n")
                        {
                            // this line only had replacements so remove the trailing carriage return and newline
                            filtered.RemoveAt(filtered.Count - 1);
                            filtered.RemoveAt(filtered.Count - 1);
                            //Console.WriteLine("removed CR+NL as 2 tokens");
                        }
                        if (filtered.Count >= 1 && filtered[filtered.Count - 1].Value == "\r\n")
                        {
                            // this line only had replacements so remove the trailing carriage return and newline (in the same token)
                            filtered.RemoveAt(filtered.Count - 1);
                            //Console.WriteLine("removed CR+NL as 1 token");
                        }
                        else if (filtered.Count >= 1 && filtered[filtered.Count - 1].Value == "\n")
                        {
                            // this line only had replacements so remove the trailing newline
                            filtered.RemoveAt(filtered.Count - 1);
                            //Console.WriteLine("removed NL token");
                        }
                    }

                    numReplacementTokensOnCurrentLine = 0;
                    numContentTokensOnCurrentLine = 0;
                }

                if(string.IsNullOrWhiteSpace(token.Value))
                {
                    // do nothing
                }
                else if(IsReplacementToken(token))
                {
                    numReplacementTokensOnCurrentLine++;
                }
                else
                {
                    numContentTokensOnCurrentLine++;
                }

                filtered.Add(token);
            }

            return filtered;
        }

        private static bool IsReplacementToken(DocumentToken token)
        {
            return token.TokenType == DocumentTokenType.BeginReplacementSegment ||
                    token.TokenType == DocumentTokenType.BeginTerminateReplacementSegment ||
                    token.TokenType == DocumentTokenType.EndReplacementSegment ||
                    token.TokenType == DocumentTokenType.QuickTerminateReplacementSegment ||
                    token.TokenType == DocumentTokenType.ReplacementKey ||
                    token.TokenType == DocumentTokenType.ReplacementParameter;
        }
    }
}
