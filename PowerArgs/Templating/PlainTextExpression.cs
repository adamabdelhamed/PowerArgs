using System;
using System.Collections.Generic;

namespace PowerArgs
{
    /// <summary>
    /// A document expression that represents plain text with no replacements or custom logic
    /// </summary>
    public class PlainTextDocumentExpression : IDocumentExpression
    {
        /// <summary>
        /// The plain text tokens
        /// </summary>
        private List<DocumentToken> tokens;

        /// <summary>
        /// Creates a plain text document expression given a list of tokens
        /// </summary>
        /// <param name="tokens">A list of plain text tokens to render without any special handling</param>
        public PlainTextDocumentExpression(List<DocumentToken> tokens)
        {
            this.tokens = tokens;
        }

        /// <summary>
        /// Creates a plain text document expression given a single plain text token
        /// </summary>
        /// <param name="singleToken">A single plain text token to render without any special handling</param>
        public PlainTextDocumentExpression(DocumentToken singleToken) : this(new List<DocumentToken> { singleToken }) { }
        
        /// <summary>
        /// Renders the tokens in the expression, using the ambient foreground and background colors if they are set.
        /// </summary>
        /// <param name="context">The data context to use for evaluation</param>
        /// <returns>The rendered plain text</returns>
        public ConsoleString Evaluate(DocumentRendererContext context)
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
