using System;
using System.Collections.Generic;

namespace PowerArgs
{
    public class DocumentRenderException : Exception
    {
        public enum NoTokenReason
        {
            EndOfString,
        }

        public DocumentRenderException(string msg, DocumentToken offendingToken) : base(msg + ": " + offendingToken.Position) { }
        public DocumentRenderException(string msg, NoTokenReason reason) : base(msg + ": " + LookupReason(reason)) { }

        private static string LookupReason(NoTokenReason reason)
        {
            if(reason == NoTokenReason.EndOfString)
            {
                return "End of string";
            }
            else
            {
                throw new ArgumentException("Unknown reason: " + reason);
            }
        }
    }

    public static class DocumentRenderer
    {
        public static ConsoleString Render(string template, object data)
        {
            return Render(template, new DataContext(data));
        }

        public static ConsoleString Render(string template, DataContext context)
        {
            List<DocumentToken> tokens = DocumentToken.Tokenize(template);
            List<DocumentToken> filtered = DocumentToken.RemoveLinesThatOnlyContainReplacements(tokens);
            return Render(filtered, context);
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
    }
}
