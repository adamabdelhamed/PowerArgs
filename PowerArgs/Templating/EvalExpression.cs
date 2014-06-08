using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    public class EvalExpression : IDocumentExpression
    {
        public DocumentToken EvalToken { get; private set; }

        public EvalExpression(DocumentToken evalToken)
        {
            this.EvalToken = evalToken;
        }

        public string Evaluate(DataContext context)
        {
            var eval = context.EvaluateExpression(this.EvalToken.Value);
            if (eval == null) return "";
            else return eval.ToString();
        }
    }

    public class EvalExpressionProvider : IDocumentExpressionProvider
    {
        public IDocumentExpression CreateExpression(List<DocumentToken> parameters, List<DocumentToken> body)
        {
            TokenReader<DocumentToken> reader = new TokenReader<DocumentToken>(parameters);

            if (reader.CanAdvance(skipWhitespace: true) == false)
            {
                throw new InvalidOperationException("missing variable expression");
            }

            var variableExpressionToken = reader.Advance(skipWhitespace: true);

            if (reader.CanAdvance(skipWhitespace: true))
            {
                throw new InvalidOperationException("unexpected parameters after if expression at " + variableExpressionToken.Position);
            }

            if(body.Count > 0)
            {
                throw new InvalidOperationException("eval tags can't have a body");
            }

            return new EvalExpression(variableExpressionToken);
        }
    }
}
