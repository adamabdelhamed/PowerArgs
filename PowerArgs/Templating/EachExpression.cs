using System.Collections;
using System.Collections.Generic;

namespace PowerArgs
{
    public class EachExpression : IDocumentExpression
    {
        public DocumentToken IterationVariableNameToken { get; private set; }

        public DocumentToken CollectionVariableExpressionToken { get; private set; }

        public List<DocumentToken> Body { get; private set; }

        public EachExpression(DocumentToken iterationVariable, DocumentToken collectionVariable, List<DocumentToken> body)
        {
            this.IterationVariableNameToken = iterationVariable;
            this.CollectionVariableExpressionToken = collectionVariable;
            this.Body = body;
        }

        public ConsoleString Evaluate(DataContext context)
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
                ret+= DocumentRenderer.Render(this.Body, context);
                context.LocalVariables.Remove(this.IterationVariableNameToken);
                context.LocalVariables.ForceClear(iterationVariableName);
                index++;
            }
            return ret;
        }
    }

    public class EachExpressionProvider : IDocumentExpressionProvider
    {
        public IDocumentExpression CreateExpression(DocumentToken replacementKeyToken, List<DocumentToken> parameters, List<DocumentToken> body)
        {
            if(body.Count == 0)
            {
                throw new DocumentRenderException("Each tag has no body", replacementKeyToken);
            }

            TokenReader<DocumentToken> reader = new TokenReader<DocumentToken>(parameters);

            DocumentToken variableName;
            if (reader.TryAdvance(out variableName, skipWhitespace: true) == false)
            {
                throw new DocumentRenderException("missing variable name in each tag", replacementKeyToken);
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

            return new EachExpression(variableName, collectionExpressionToken, body);
        }
    }
}
