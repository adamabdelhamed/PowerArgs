using System;
using System.Collections.Generic;
using System.Linq;

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
            if (NameToken.Value == "ConsoleForegroundColor" || NameToken.Value == "ConsoleBackgroundColor")
            {
                ConsoleColor value;
                if(Enum.TryParse<ConsoleColor>(ValueToken.Value,out value) == false)
                {
                    throw new DocumentRenderException("Invalid ConsoleColor '" + ValueToken.Value + "'", ValueToken);
                }
                context.LocalVariables.Add(NameToken, value);
            }
            else
            {
                var value = context.EvaluateExpression(ValueToken.Value);
                context.LocalVariables.Add(NameToken, value);
            }
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
        public IDocumentExpression CreateExpression(DocumentToken replacementKeyToken, List<DocumentToken> parameters, List<DocumentToken> body)
        {
            if (body.Count > 0)
            {
                throw new DocumentRenderException("var tags can't have a body", body.First());
            }

            TokenReader<DocumentToken> reader = new TokenReader<DocumentToken>(parameters);

            DocumentToken variableName;

            if(reader.TryAdvance(out variableName,skipWhitespace:true) == false)
            {
                throw new DocumentRenderException("Expected variable name after var tag", replacementKeyToken);
            }

            DocumentToken variableValue;

            if (reader.TryAdvance(out variableValue, skipWhitespace: true) == false)
            {
                throw new DocumentRenderException("Expected variable value expression", variableName);
            }

            return new VarExpression(variableName, variableValue);
        }
    }

    public class ClearVarExpressionProvider : IDocumentExpressionProvider
    {
        public IDocumentExpression CreateExpression(DocumentToken replacementKeyToken, List<DocumentToken> parameters, List<DocumentToken> body)
        {
            if (body.Count > 0)
            {
                throw new DocumentRenderException("clearvar tags can't have a body", replacementKeyToken);
            }

            TokenReader<DocumentToken> reader = new TokenReader<DocumentToken>(parameters);

            DocumentToken variableName;

            if (reader.TryAdvance(out variableName, skipWhitespace: true) == false)
            {
                throw new DocumentRenderException("Expected variable name after clearvar tag", replacementKeyToken);
            }

            return new ClearVarExpression(variableName);
        }
    }
}
