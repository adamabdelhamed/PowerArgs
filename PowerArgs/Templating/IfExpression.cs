using System.Collections.Generic;

namespace PowerArgs
{
    /// <summary>
    /// An expression that allows a portion of a document template to be rendered only if some condition is true
    /// </summary>
    public class IfExpression : IDocumentExpression
    {
        /// <summary>
        /// The token representing the conditional expression.  
        /// </summary>
        public DocumentToken IfExpressionToken { get; private set; }

        /// <summary>
        /// The contents of the expression to render only if the condition is true
        /// </summary>
        public IEnumerable<DocumentToken> Body { get; private set; }

        /// <summary>
        /// Creates a new if expression given a conditional token and a body
        /// </summary>
        /// <param name="ifExpressionToken">The token containing the conditional expression</param>
        /// <param name="body">The body of the document to render only if the condition evaluates to true</param>
        public IfExpression(DocumentToken ifExpressionToken, IEnumerable<DocumentToken> body)
        {
            this.IfExpressionToken = ifExpressionToken;
            this.Body = body;
        }

        /// <summary>
        /// Evaluates the conditional expression against the given data context, rendering the body only if it is true.
        /// </summary>
        /// <param name="context">The data context to use when evaluating the conditional expression</param>
        /// <returns>The rendered body if the conditional was true, an empty string otherwise</returns>
        public virtual ConsoleString Evaluate(DocumentRendererContext context)
        {
            var eval = context.EvaluateExpression(this.IfExpressionToken.Value);
            if(true.Equals(eval) || 1.Equals(eval))
            {
                return context.RenderBody(this.Body);
            }
            else
            {
                return ConsoleString.Empty;
            }
        }
    }

    /// <summary>
    /// An expression that allows a portion of a document template to be rendered only if some condition is not true
    /// </summary>
    public class IfNotExpression : IfExpression
    {
        /// <summary>
        /// Creates a new ifnot expression given a conditional token and a body
        /// </summary>
        /// <param name="ifExpressionToken">The token containing the conditional expression</param>
        /// <param name="body">The body of the document to render only if the condition evaluates to false</param>
        public IfNotExpression(DocumentToken ifExpressionToken, IEnumerable<DocumentToken> body) : base(ifExpressionToken, body) { }

        /// <summary>
        /// Evaluates the conditional expression against the given data context, rendering the body only if it is false.
        /// </summary>
        /// <param name="context">The data context to use when evaluating the conditional expression</param>
        /// <returns>The rendered body if the conditional was false, an empty string otherwise</returns>
        public override ConsoleString Evaluate(DocumentRendererContext context)
        {
            var eval = context.EvaluateExpression(this.IfExpressionToken.Value);
            if (false.Equals(eval) == true || 0.Equals(eval))
            {
                return context.RenderBody(this.Body);
            }
            else
            {
                return ConsoleString.Empty;
            }
        }
    }

    /// <summary>
    /// An expression provider that can provide either an If expresison or an IfNot expression.
    /// </summary>
    public class IfExpressionProvider : IDocumentExpressionProvider
    {
        bool not;

        /// <summary>
        /// Creates a new provider, indicating whether or not this provider should provide if or ifnot expressions.
        /// </summary>
        /// <param name="not">If true, this provider will provide if expressions, otherwise it will provide ifnot expressions</param>
        public IfExpressionProvider(bool not = false)
        {
            this.not = not;
        }

        /// <summary>
        /// Creates either an if or an ifnot expression, based on its configuration, using the given document info.
        /// </summary>
        /// <param name="context">The context that contains information about the document being rendered</param>
        /// <returns>The expression, either an if or an ifnot expression</returns>
        public IDocumentExpression CreateExpression(DocumentExpressionContext context)
        {
            TokenReader<DocumentToken> reader = new TokenReader<DocumentToken>(context.Parameters);

            DocumentToken ifExpressionToken;            
            if(reader.TryAdvance(out ifExpressionToken, skipWhitespace: true) == false)
            {
                throw new DocumentRenderException("missing if expression", context.ReplacementKeyToken);
            }

            if (reader.CanAdvance(skipWhitespace: true))
            {
                throw new DocumentRenderException("unexpected parameters after if expression", reader.Advance(skipWhitespace: true));
            }

            return not ? new IfNotExpression(ifExpressionToken, context.Body) : new IfExpression(ifExpressionToken, context.Body);
        }
    }
}
