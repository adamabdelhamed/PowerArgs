using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    public class IfExpression : IDocumentExpression
    {
        public DocumentToken IfExpressionToken { get; private set; }

        public List<DocumentToken> Body { get; private set; }

        public IfExpression(DocumentToken ifExpressionToken, List<DocumentToken> body)
        {
            this.IfExpressionToken = ifExpressionToken;
            this.Body = body;
        }

        public string Evaluate(DataContext context)
        {
            var eval = context.EvaluateExpression(this.IfExpressionToken.Value);
            if(true.Equals(eval) || 1.Equals(eval))
            {
                return DocumentRenderer.Render(this.Body, context);
            }
            else
            {
                return string.Empty;
            }
        }
    }

    public class IfExpressionProvider : IDocumentExpressionProvider
    {
        public IDocumentExpression CreateExpression(List<DocumentToken> parameters, List<DocumentToken> body)
        {
            TokenReader<DocumentToken> reader = new TokenReader<DocumentToken>(parameters);

            if (reader.CanAdvance(skipWhitespace: true) == false)
            {
                throw new InvalidOperationException("missing if expression");
            }

            var ifExpressionToken = reader.Advance(skipWhitespace: true);

            if (reader.CanAdvance(skipWhitespace: true))
            {
                throw new InvalidOperationException("unexpected parameters after if expression at " + ifExpressionToken.Position);
            }

            return new IfExpression(ifExpressionToken, body);
        }
    }
}
