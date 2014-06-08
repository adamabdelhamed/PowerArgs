using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

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

        public string Evaluate(DataContext context)
        {
            var collection = context.EvaluateExpression(this.CollectionVariableExpressionToken.Value);
            if (collection == null) return "";

            if(collection is IEnumerable == false)
            {
                throw new InvalidCastException("'" + this.CollectionVariableExpressionToken.Value + "' does not resolve to a collection at " + this.CollectionVariableExpressionToken.Position);
            }
            var ret = "";
            foreach(var item in (IEnumerable)collection)
            {
                context.LocalVariables.Add(this.IterationVariableNameToken, item);
                ret+= DocumentRenderer.Render(this.Body, context);
                context.LocalVariables.Remove(this.IterationVariableNameToken);
            }
            return ret;
        }
    }

    public class EachExpressionProvider : IDocumentExpressionProvider
    {
        public IDocumentExpression CreateExpression(List<DocumentToken> parameters, List<DocumentToken> body)
        {
            if(body.Count == 0)
            {
                throw new InvalidOperationException("each tags must contain a body");
            }

            TokenReader<DocumentToken> reader = new TokenReader<DocumentToken>(parameters);

            DocumentToken variableName;
            if (reader.TryAdvance(out variableName, skipWhitespace: true) == false)
            {
                throw new InvalidOperationException("missing variable name");
            }

            DocumentToken inToken;

            if (reader.TryAdvance(out inToken, skipWhitespace: true) == false)
            {
                throw new InvalidOperationException("Expected 'in' after "+variableName.Position);
            }

            if (inToken.Value != "in")
            {
                throw new InvalidOperationException("Expected 'in', got '" + inToken.Value + "' at " + inToken.Position);
            }

            DocumentToken collectionExpressionToken;

            if(reader.TryAdvance(out collectionExpressionToken, skipWhitespace: true) == false)
            {
                throw new InvalidOperationException("Expected collection expression after 'in' at "+inToken.Position);
            }

            DocumentToken unexpected;
            if(reader.TryAdvance(out unexpected, skipWhitespace: true))
            {
                throw new InvalidOperationException("Unexpected parameter '" + unexpected.Value + "' after collection at " + unexpected.Position);
            }

            return new EachExpression(variableName, collectionExpressionToken, body);
        }
    }
}
