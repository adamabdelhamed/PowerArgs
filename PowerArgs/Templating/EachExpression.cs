using System.Collections;
using System.Collections.Generic;

namespace PowerArgs
{
    /// <summary>
    /// An expression used to expand a portion of a document template for each element in a collection
    /// </summary>
    public class EachExpression : IDocumentExpression
    {
        /// <summary>
        /// Gets the token in the document that represents the iteration variable name (e.g. 'element' in {{each element in collection}})
        /// </summary>
        public DocumentToken IterationVariableNameToken { get; private set; }

        /// <summary>
        /// Gets the token in the document that represents the collection evaluation expression (e.g. 'collection' in {{each element in collection}})
        /// </summary>
        public DocumentToken CollectionVariableExpressionToken { get; private set; }

        /// <summary>
        /// Gets the body of the each expression.  This body will be evaluated once fore each element in the collection.
        /// </summary>
        public IEnumerable<DocumentToken> Body { get; private set; }

        /// <summary>
        /// Creates a new each expression given an iteration variable name, a collection expression, and a body.
        /// </summary>
        /// <param name="iterationVariable">The name to assign to ther variable representing the current element in the template</param>
        /// <param name="collectionExpression">The expression used to determine the collection to enumerate</param>
        /// <param name="body">The body of the each loop</param>
        public EachExpression(DocumentToken iterationVariable, DocumentToken collectionExpression, IEnumerable<DocumentToken> body)
        {
            this.IterationVariableNameToken = iterationVariable;
            this.CollectionVariableExpressionToken = collectionExpression;
            this.Body = body;
        }

        /// <summary>
        /// Evaluates the each loop
        /// </summary>
        /// <param name="context">The context that contains information about the document being rendered</param>
        /// <returns>The rendered contents of the each loop</returns>
        public ConsoleString Evaluate(DocumentRendererContext context)
        {
            var collection = context.EvaluateExpression(this.CollectionVariableExpressionToken.Value);
            if (collection == null)
            {
                throw new DocumentRenderException("'" + this.CollectionVariableExpressionToken.Value + "' resolved to a null reference", this.CollectionVariableExpressionToken);
            }

            if(collection is IEnumerable == false)
            {
                throw new DocumentRenderException("'" + this.CollectionVariableExpressionToken.Value + "' does not resolve to a collection", this.CollectionVariableExpressionToken);
            }
            ConsoleString ret = ConsoleString.Empty;
            int index = 0;
            var iterationVariableName = this.IterationVariableNameToken.Value + "-index";
            foreach(var item in (IEnumerable)collection)
            {
                context.LocalVariables.Add(this.IterationVariableNameToken, item);
                context.LocalVariables.Force(iterationVariableName, index);
                ret+= context.RenderBody(this.Body);
                context.LocalVariables.Remove(this.IterationVariableNameToken);
                context.LocalVariables.ForceClear(iterationVariableName);
                index++;
            }
            return ret;
        }
    }

    /// <summary>
    /// A class that can take in document replacement info and convert it into a document expression that represents an each loop.
    /// </summary>
    public class EachExpressionProvider : IDocumentExpressionProvider
    {
        /// <summary>
        /// Takes in document replacement info and converts it into a document expression that represents an each loop.
        /// </summary>
        /// <param name="context">Context about the expression being parsed</param>
        /// <returns>The parsed each expression</returns>
        public IDocumentExpression CreateExpression(DocumentExpressionContext context)
        {
            if(context.Body.Count == 0)
            {
                throw new DocumentRenderException("Each tag has no body", context.ReplacementKeyToken);
            }

            TokenReader<DocumentToken> reader = new TokenReader<DocumentToken>(context.Parameters);

            DocumentToken variableName;
            if (reader.TryAdvance(out variableName, skipWhitespace: true) == false)
            {
                throw new DocumentRenderException("missing variable name in each tag", context.ReplacementKeyToken);
            }

            DocumentToken inToken;

            if (reader.TryAdvance(out inToken, skipWhitespace: true) == false)
            {
                throw new DocumentRenderException("Expected 'in' after iteration variable", variableName);
            }

            if (inToken.Value != "in")
            {
                throw new DocumentRenderException("Expected 'in', got '" + inToken.Value + "'", inToken);
            }

            DocumentToken collectionExpressionToken;

            if(reader.TryAdvance(out collectionExpressionToken, skipWhitespace: true) == false)
            {
                throw new DocumentRenderException("Expected collection expression after 'in' ", inToken);
            }

            DocumentToken unexpected;
            if(reader.TryAdvance(out unexpected, skipWhitespace: true))
            {
                throw new DocumentRenderException("Unexpected parameter '" + unexpected.Value + "' after collection", unexpected);
            }

            return new EachExpression(variableName, collectionExpressionToken, context.Body);
        }
    }
}
