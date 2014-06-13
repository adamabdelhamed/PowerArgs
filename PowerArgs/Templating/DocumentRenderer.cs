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

    public class DocumentTemplateInfo
    {
        public string Value { get; set; }
        public string SourceLocation { get; set; }
    }

    public class DocumentRenderer
    {
        public Dictionary<string, DocumentTemplateInfo> NamedTemplates { get; private set; }

        public DocumentRenderer()
        {
            NamedTemplates = new Dictionary<string, DocumentTemplateInfo>();
        }

        public ConsoleString Render(string template, object data, string sourceFileLocation = null)
        {
            return Render(template, new DataContext(data) { DocumentRenderer = this }, sourceFileLocation);
        }

        public ConsoleString Render(string template, DataContext context, string sourceFileLocation = null)
        {
            List<DocumentToken> tokens = DocumentToken.Tokenize(template, sourceFileLocation);
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
