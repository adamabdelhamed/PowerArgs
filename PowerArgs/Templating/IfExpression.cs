using System.Collections.Generic;

namespace PowerArgs
{
    /// <summary>
    /// An expression that allows a portion of a document template to be rendered only if some condition is true
    /// </summary>
    public class IfExpression : IDocumentExpression, IBufferedDocumentExpression
    {
        /// <summary>
        /// The token representing the conditional expression.  
        /// </summary>
        public DocumentToken IfExpressionToken { get; private set; }

        /// <summary>
        /// The contents of the expression to render only if the condition is true
        /// </summary>
        public IEnumerable<DocumentToken> Body { get; private set; }

        internal IReadOnlyList<IDocumentExpression> ParsedBody { get; set; }

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
            var characters = new List<ConsoleCharacter>();
            EvaluateInto(context, characters);
            return new ConsoleString(characters);
        }

        void IBufferedDocumentExpression.EvaluateInto(DocumentRendererContext context, List<ConsoleCharacter> output) => EvaluateInto(context, output);

        protected virtual bool ShouldRender(object value) => true.Equals(value) || 1.Equals(value);

        private void EvaluateInto(DocumentRendererContext context, List<ConsoleCharacter> output)
        {
            if (ShouldRender(context.EvaluateExpression(this.IfExpressionToken.Value)) == false) return;
            if (ParsedBody == null) output.AddRange(context.RenderBody(this.Body));
            else context.AppendExpressions(ParsedBody, output);
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
            return base.Evaluate(context);
        }

        protected override bool ShouldRender(object value) => false.Equals(value) || 0.Equals(value);
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

            var expression = not ? new IfNotExpression(ifExpressionToken, context.Body) : new IfExpression(ifExpressionToken, context.Body);
            expression.ParsedBody = context.ParsedBody;
            return expression;
        }
    }
}
