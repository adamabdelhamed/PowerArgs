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
            return Render(tokens, new DataContext(data));
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
    }
}
