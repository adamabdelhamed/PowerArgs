using System;
using System.Collections.Generic;

namespace PowerArgs
{
    public class TemplateExpression : IDocumentExpression
    {
        public DocumentToken IdToken { get; private set; }

        public DocumentToken EvalToken { get; private set; }

        public TemplateExpression(DocumentToken idToken, DocumentToken evalToken)
        {
            this.IdToken = idToken;
            this.EvalToken = evalToken;
        }

        public ConsoleString Evaluate(DataContext context)
        {
            DocumentTemplateInfo target;
            if(context.DocumentRenderer.NamedTemplates.TryGetValue(this.IdToken.Value,out target) == false)
            {
                throw new DocumentRenderException("There is no template named '" + IdToken.Value + "'", IdToken);
            }

            var eval = context.EvaluateExpression(this.EvalToken.Value);

            return context.DocumentRenderer.Render(target.Value, eval, target.SourceLocation);
        }
    }

    public class TemplateExpressionProvider : IDocumentExpressionProvider
    {
        public IDocumentExpression CreateExpression(DocumentToken replacementKeyToken, List<DocumentToken> parameters, List<DocumentToken> body)
        {
            if (body.Count > 0)
            {
                throw new DocumentRenderException("template tags can't have a body", replacementKeyToken);
            }

            TokenReader<DocumentToken> reader = new TokenReader<DocumentToken>(parameters);

            DocumentToken idToken;

            if (reader.TryAdvance(out idToken, skipWhitespace: true) == false)
            {
                throw new DocumentRenderException("missing template Id", replacementKeyToken);
            }

            DocumentToken evalToken;

            if (reader.TryAdvance(out evalToken, skipWhitespace: true) == false)
            {
                throw new DocumentRenderException("missing eval token", idToken);
            }

            var ret = new TemplateExpression(idToken, evalToken);

            return ret;
        }
    }
}
