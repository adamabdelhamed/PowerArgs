using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    public class VarExpression : IDocumentExpression
    {
        public DocumentToken NameToken { get; private set; }
        public DocumentToken ValueToken { get; private set; }

        public VarExpression(DocumentToken name, DocumentToken value)
        {
            this.NameToken = name;
            this.ValueToken = value;
        }

        public ConsoleString Evaluate(DataContext context)
        {
            var value = context.EvaluateExpression(ValueToken.Value);

           // TODO - if console color match then parse it before storing
            
            context.LocalVariables.Add(NameToken, value);
            return ConsoleString.Empty;
        }
    }

    public class ClearVarExpression : IDocumentExpression
    {
        public DocumentToken NameToken { get; private set; }

        public ClearVarExpression(DocumentToken name)
        {
            this.NameToken = name;
        }

        public ConsoleString Evaluate(DataContext context)
        {
            context.LocalVariables.Remove(NameToken);
            return ConsoleString.Empty;
        }
    }

    public class VarExpressionProvider : IDocumentExpressionProvider
    {
        public IDocumentExpression CreateExpression(List<DocumentToken> parameters, List<DocumentToken> body)
        {
            if (body.Count > 0)
            {
                throw new InvalidOperationException("var tags can't have a body");
            }

            TokenReader<DocumentToken> reader = new TokenReader<DocumentToken>(parameters);

            DocumentToken variableName;

            if(reader.TryAdvance(out variableName,skipWhitespace:true) == false)
            {
                throw new InvalidOperationException("Expected variable name");
            }


            DocumentToken variableValue;

            if (reader.TryAdvance(out variableValue, skipWhitespace: true) == false)
            {
                throw new InvalidOperationException("Expected variable value expression after " + variableName.Position);
            }

            return new VarExpression(variableName, variableValue);
        }
    }

    public class ClearVarExpressionProvider : IDocumentExpressionProvider
    {
        public IDocumentExpression CreateExpression(List<DocumentToken> parameters, List<DocumentToken> body)
        {
            if (body.Count > 0)
            {
                throw new InvalidOperationException("clearvar tags can't have a body");
            }

            TokenReader<DocumentToken> reader = new TokenReader<DocumentToken>(parameters);

            DocumentToken variableName;

            if (reader.TryAdvance(out variableName, skipWhitespace: true) == false)
            {
                throw new InvalidOperationException("Expected variable name");
            }



            return new ClearVarExpression(variableName);
        }
    }
}
