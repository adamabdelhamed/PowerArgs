namespace PowerArgs
{
    /// <summary>
    /// An interface that defines a signature for evaluating an expression against a data context to render a ConsoleString.
    /// </summary>
    public interface IDocumentExpression
    {
        /// <summary>
        /// The expression should use it's metadata to evaluate itself against the given data context.
        /// </summary>
        /// <param name="context">The data context</param>
        /// <returns>The evaluated ConsoleString.</returns>
        ConsoleString Evaluate(DocumentRendererContext context);
    }
}
