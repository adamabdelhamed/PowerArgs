using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    public class EvalExpression : IDocumentExpression
    {
        public DocumentToken EvalToken { get; private set; }

        public DocumentToken ForegroundColorToken { get; set; }
        public DocumentToken BackgroundColorToken { get; set; }

        public ConsoleColor? FG
        {
            get
            {
                if (ForegroundColorToken == null) return null;
                else return (ConsoleColor)Enum.Parse(typeof(ConsoleColor), ForegroundColorToken.Value, true);
            }
        }

        public ConsoleColor? BG
        {
            get
            {
                if (BackgroundColorToken == null) return null;
                else return (ConsoleColor)Enum.Parse(typeof(ConsoleColor), BackgroundColorToken.Value, true);
            }
        }

        public EvalExpression(DocumentToken evalToken)
        {
            this.EvalToken = evalToken;
        }

        public ConsoleString Evaluate(DataContext context)
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
                    else
                    {
                        var result = eval.ToString();
                        var ret = DocumentRenderer.Render(result, context);
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

    public class EvalExpressionProvider : IDocumentExpressionProvider
    {
        public IDocumentExpression CreateExpression(List<DocumentToken> parameters, List<DocumentToken> body)
        {
            if (body.Count > 0)
            {
                throw new InvalidOperationException("eval tags can't have a body");
            }

            TokenReader<DocumentToken> reader = new TokenReader<DocumentToken>(parameters);

            DocumentToken variableExpressionToken, fgToken, bgToken;

            if (reader.TryAdvance(out variableExpressionToken, skipWhitespace: true) == false)
            {
                throw new InvalidOperationException("missing variable expression");
            }

            var ret = new EvalExpression(variableExpressionToken);

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
