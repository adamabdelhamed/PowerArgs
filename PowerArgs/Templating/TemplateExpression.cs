using System;
using System.Collections.Generic;

namespace PowerArgs
{
    /// <summary>
    /// A document expression that can render a named template
    /// </summary>
    public class TemplateExpression : IDocumentExpression
    {
        /// <summary>
        /// A token containing the id of the template to render
        /// </summary>
        public DocumentToken IdToken { get; private set; }

        /// <summary>
        /// A token containing an expression to be evaluated.  The result of the evaluation
        /// will be used as the root data object to be bound to the named template.
        /// </summary>
        public DocumentToken EvalToken { get; private set; }

        /// <summary>
        /// Creates a new template expression given an id token and a data evaluation token.
        /// </summary>
        /// <param name="idToken">A token containing the id of the template to render</param>
        /// <param name="evalToken">A token containing an expression to be evaluated.  The result of the evaluation will be
        /// used as the root data object to be bound to the named template.</param>
        public TemplateExpression(DocumentToken idToken, DocumentToken evalToken)
        {
            this.IdToken = idToken;
            this.EvalToken = evalToken;
        }

        /// <summary>
        /// Finds the matching template from the data context, evaluates the data expression, then renders
        /// the template against the data.  The rendered document is inserted into the parent document.
        /// </summary>
        /// <param name="context">The data context used to find the named template and to evaluate the data expression</param>
        /// <returns>The rendered child document to be inserted into the parent document</returns>
        public ConsoleString Evaluate(DocumentRendererContext context)
        {
            DocumentTemplateInfo target = context.DocumentRenderer.GetTemplate(this.IdToken);
            var eval = context.EvaluateExpression(this.EvalToken.Value);
            return context.DocumentRenderer.Render(target.Value, eval, target.SourceLocation);
        }
    }

    /// <summary>
    /// A provider that can create a template expression from a replacement token and parameters.
    /// </summary>
    public class TemplateExpressionProvider : IDocumentExpressionProvider
    {
        /// <summary>
        /// Creates a template expression given a replacement token and parameters.
        /// </summary>
        /// <param name="context">Context about the expression being parsed</param>
        /// <returns>a template expression</returns>
        public IDocumentExpression CreateExpression(DocumentExpressionContext context)
        {
            if (context.Body.Count > 0)
            {
                throw new DocumentRenderException("template tags can't have a body", context.ReplacementKeyToken);
            }

            TokenReader<DocumentToken> reader = new TokenReader<DocumentToken>(context.Parameters);

            DocumentToken idToken;

            if (reader.TryAdvance(out idToken, skipWhitespace: true) == false)
            {
                throw new DocumentRenderException("missing template Id", context.ReplacementKeyToken);
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
