using System;
using System.Collections.Generic;

namespace PowerArgs
{
    /// <summary>
    /// The core expression that knows how to evaluate C# object expressions like property navigation, including indexed properties.
    /// </summary>
    public class EvalExpression : IDocumentExpression
    {
        /// <summary>
        /// Gets the evaluation token that will be evaluated against a data context
        /// </summary>
        public DocumentToken EvalToken { get; private set; }

        /// <summary>
        /// The optional foreground color token that can be used to customize the color of the resulting value
        /// </summary>
        public DocumentToken ForegroundColorToken { get; set; }

        /// <summary>
        /// The optional background color token that can be used to customize the color of the resulting value
        /// </summary>
        public DocumentToken BackgroundColorToken { get; set; }

        /// <summary>
        /// Gets the ConsoleColor that matches the provided foreground color token, if it was provided.
        /// </summary>
        public ConsoleColor? FG
        {
            get
            {
                if (ForegroundColorToken == null) return null;
                else return (ConsoleColor)Enum.Parse(typeof(ConsoleColor), ForegroundColorToken.Value, true);
            }
        }

        /// <summary>
        /// Gets the ConsoleColor that matches the provided background color token, if it was provided.
        /// </summary>
        public ConsoleColor? BG
        {
            get
            {
                if (BackgroundColorToken == null) return null;
                else return (ConsoleColor)Enum.Parse(typeof(ConsoleColor), BackgroundColorToken.Value, true);
            }
        }

        /// <summary>
        /// Creates an eval expression given a token that represents the expression to evaluate
        /// </summary>
        /// <param name="evalToken">The token containing the expression to evaluate</param>
        public EvalExpression(DocumentToken evalToken)
        {
            this.EvalToken = evalToken;
        }

        /// <summary>
        /// Evaluates the evaluation expression against a data context, optionally setting the console color if the expression contains those parameters
        /// </summary>
        /// <param name="context">The datta context to evaluate against</param>
        /// <returns>The result of the evaluation as a ConsoleString</returns>
        public ConsoleString Evaluate(DocumentRendererContext context)
        {
            context.LocalVariables.PushConsoleColors(FG, BG);
            try
            {
                var eval = context.EvaluateExpression(this.EvalToken.Value);
                if (eval == null)
                {
                    return ConsoleString.Empty;
                }
                else
                {
                    if (eval is ConsoleString)
                    {
                        return (ConsoleString)eval;
                    }
                    else if(eval is string)
                    {
                        return new ConsoleString(eval as string, context.LocalVariables.CurrentForegroundColor, context.LocalVariables.CurrentBackgroundColor);
                    }
                    else
                    {
                        var result = eval.ToString();
                        var ret = context.RenderDynamicContent(result, this.EvalToken);
                        return ret;
                    }
                }
            }
            finally
            {
                context.LocalVariables.PopConsoleColors();
            }
        }
    }

    /// <summary>
    /// The provider that can create an eval expression from template replacement info
    /// </summary>
    public class EvalExpressionProvider : IDocumentExpressionProvider
    {
        /// <summary>
        /// Creates an eval expression given template replacement info
        /// </summary>
        /// <param name="context">The context that contains information about the document being rendered</param>
        /// <returns></returns>
        public IDocumentExpression CreateExpression(DocumentExpressionContext context)
        {
            if (context.Body.Count > 0)
            {
                throw new DocumentRenderException("eval tags can't have a body", context.ReplacementKeyToken);
            }

            TokenReader<DocumentToken> reader = new TokenReader<DocumentToken>(context.Parameters);

            DocumentToken fgToken, bgToken;

            var ret = new EvalExpression(context.ReplacementKeyToken);

            if(reader.TryAdvance(out fgToken, skipWhitespace: true) == false)
            {
                return ret;
            }
            else
            {
                ret.ForegroundColorToken = fgToken;
            }

            if (reader.TryAdvance(out bgToken, skipWhitespace: true) == false)
            {
                return ret;
            }
            else
            {
                ret.BackgroundColorToken = bgToken;
            }

            return ret;
        }
    }
}
