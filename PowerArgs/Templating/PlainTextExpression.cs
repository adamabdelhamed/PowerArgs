using System;
using System.Collections.Generic;

namespace PowerArgs
{
    public class PlainTextDocumentExpression : IDocumentExpression
    {
        private List<DocumentToken> tokens;

        public PlainTextDocumentExpression(List<DocumentToken> tokens)
        {
            this.tokens = tokens;
        }
        public PlainTextDocumentExpression(DocumentToken singleToken) : this(new List<DocumentToken> { singleToken }) { }
        public ConsoleString Evaluate(DataContext context)
        {
            var ret = new ConsoleString();

            ConsoleColor fg = new ConsoleCharacter('a').ForegroundColor;
            ConsoleColor bg = new ConsoleCharacter('a').BackgroundColor;

            if(context.LocalVariables.IsDefined("ConsoleForegroundColor"))
            {
                fg = (ConsoleColor)context.LocalVariables["ConsoleForegroundColor"];
            }

            if (context.LocalVariables.IsDefined("ConsoleBackgroundColor"))
            {
                bg = (ConsoleColor)context.LocalVariables["ConsoleBackgroundColor"];
            }

            foreach (var token in this.tokens)
            {
                ret += new ConsoleString(token.Value, fg, bg);
            }

            return ret;
        }
    }
}
