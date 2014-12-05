using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs
{
    /// <summary>
    /// An expression that indicates the beginning of a local variable's scope
    /// </summary>
    public class VarExpression : IDocumentExpression
    {
        /// <summary>
        /// A token containing the name of the local variable to initialize
        /// </summary>
        public DocumentToken NameToken { get; private set; }

        /// <summary>
        /// A token containing an expression that should resolve to the initial value of the variable
        /// </summary>
        public DocumentToken ValueToken { get; private set; }

        /// <summary>
        /// Creates a new variable expression given a name and value expression
        /// </summary>
        /// <param name="name">A token containing the name of the local variable to initialize</param>
        /// <param name="value">A token containing an expression that should resolve to the initial value of the variable</param>
        public VarExpression(DocumentToken name, DocumentToken value)
        {
            this.NameToken = name;
            this.ValueToken = value;
        }

        /// <summary>
        /// Always results in an empty string, but initializes the local value in the data context
        /// </summary>
        /// <param name="context">The data context used to store the newly initialized variable</param>
        /// <returns>an empty string</returns>
        public ConsoleString Evaluate(DocumentRendererContext context)
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

    /// <summary>
    /// An expression that indicates the end of a local variable's scope
    /// </summary>
    public class ClearVarExpression : IDocumentExpression
    {
        /// <summary>
        /// A token containing the name of the variable whose scope is ending
        /// </summary>
        public DocumentToken NameToken { get; private set; }

        /// <summary>
        /// Creates a new clear variable expression given a variable name
        /// </summary>
        /// <param name="name">A token containing the name of the variable whose scope is ending</param>
        public ClearVarExpression(DocumentToken name)
        {
            this.NameToken = name;
        }

        /// <summary>
        /// Removes the named variable from the context's local variable set
        /// </summary>
        /// <param name="context">the context that should contain the local variable to remove</param>
        /// <returns>an empty string</returns>
        public ConsoleString Evaluate(DocumentRendererContext context)
        {
            context.LocalVariables.Remove(NameToken);
            return ConsoleString.Empty;
        }
    }

    /// <summary>
    /// A provider that can create a variable expression
    /// </summary>
    public class VarExpressionProvider : IDocumentExpressionProvider
    {
        /// <summary>
        /// Creates a variable expression given replacement info
        /// </summary>
        /// <param name="context">The context that contains information about the document being rendered</param>
        /// <returns>A variable expression</returns>
        public IDocumentExpression CreateExpression(DocumentExpressionContext context)
        {
            if (context.Body.Count > 0)
            {
                throw new DocumentRenderException("var tags can't have a body", context.Body.First());
            }

            TokenReader<DocumentToken> reader = new TokenReader<DocumentToken>(context.Parameters);

            DocumentToken variableName;

            if(reader.TryAdvance(out variableName,skipWhitespace:true) == false)
            {
                throw new DocumentRenderException("Expected variable name after var tag", context.ReplacementKeyToken);
            }

            DocumentToken variableValue;

            if (reader.TryAdvance(out variableValue, skipWhitespace: true) == false)
            {
                throw new DocumentRenderException("Expected variable value expression", variableName);
            }

            return new VarExpression(variableName, variableValue);
        }
    }

    /// <summary>
    /// A provider that can create an expression to clear a local variable
    /// </summary>
    public class ClearVarExpressionProvider : IDocumentExpressionProvider
    {
        /// <summary>
        /// Creates a clear variable expression given replacement info
        /// </summary>
        /// <param name="context">The context that contains information about the document being rendered</param>
        /// <returns>a clear variable expression</returns>
        public IDocumentExpression CreateExpression(DocumentExpressionContext context)
        {
            if (context.Body.Count > 0)
            {
                throw new DocumentRenderException("clearvar tags can't have a body", context.ReplacementKeyToken);
            }

            TokenReader<DocumentToken> reader = new TokenReader<DocumentToken>(context.Parameters);

            DocumentToken variableName;

            if (reader.TryAdvance(out variableName, skipWhitespace: true) == false)
            {
                throw new DocumentRenderException("Expected variable name after clearvar tag", context.ReplacementKeyToken);
            }

            return new ClearVarExpression(variableName);
        }
    }
}
