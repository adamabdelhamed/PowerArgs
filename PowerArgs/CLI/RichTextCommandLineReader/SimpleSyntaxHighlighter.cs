using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PowerArgs
{
    /// <summary>
    /// An implementation of ISyntaxHighlighter that makes it easy to perform common types of syntax highlighting based on keywords, regular expressions, etc.
    /// </summary>
    public class SimpleSyntaxHighlighter : ISyntaxHighlighter
    {
        private List<ITokenHighlighter> TokenHighlighters { get; set; }

        /// <summary>
        /// Creates a new SimpleSyntaxHighlighter
        /// </summary>
        public SimpleSyntaxHighlighter()
        {
            TokenHighlighters = new List<ITokenHighlighter>();
        }

        /// <summary>
        /// Registers a keyword with the highlighter.
        /// </summary>
        /// <param name="keyword">The keyword that will be highlighted when found on the command line</param>
        /// <param name="fg">The foreground highlight color</param>
        /// <param name="bg">The background highlight color</param>
        /// <param name="comparison">Determines how strings are compared. </param>
        /// <param name="onlyIfFirst">If true, only highlights the keyword if it is the first value on the command line</param>
        public void AddKeyword(string keyword, ConsoleColor? fg = null, ConsoleColor? bg = null, StringComparison comparison = StringComparison.InvariantCulture, bool onlyIfFirst = false)
        {
            TokenHighlighters.Add(new KeywordHighlighter(keyword, fg, bg, comparison, onlyIfFirst));
        }

        /// <summary>
        /// Registers a keyword with the highlighter that should only be highlighted if some condition is true
        /// </summary>
        /// <param name="keyword">The keyword that will conditionally be highlighted when found on the command line</param>
        /// <param name="conditionEval">the conditional evaluation function</param>
        /// <param name="fg">The foreground highlight color</param>
        /// <param name="bg">The background highlight color</param>
        /// <param name="comparison">Determines how strings are compared. </param>
        public void AddConditionalKeyword(string keyword, Func<RichCommandLineContext, bool> conditionEval, ConsoleColor? fg = null, ConsoleColor? bg = null, StringComparison comparison = StringComparison.InvariantCulture)
        {
            TokenHighlighters.Add(new ConditionalKeywordHighlighter(keyword, conditionEval,  fg, bg, comparison));
        }

        /// <summary>
        /// Registers a regular expression with the highlighter. Tokens that exactly match the given regex will be highlighted.
        /// </summary>
        /// <param name="regex">the regular expression pattern to search for</param>
        /// <param name="fg">The foreground highlight color</param>
        /// <param name="bg">The background highlight color</param>
        public void AddRegex(string regex, ConsoleColor? fg = null, ConsoleColor? bg = null)
        {
            TokenHighlighters.Add(new RegexHighlighter(regex, fg, bg));
        }

        /// <summary>
        /// Lets you control how quoted string literals should be highlighted
        /// </summary>
        /// <param name="fg">The foreground highlight color</param>
        /// <param name="bg">The background highlight color</param>
        public void SetQuotedStringLiteralHighlight(ConsoleColor? fg = null, ConsoleColor? bg = null)
        {
            var existing = (from h in TokenHighlighters where h is StringLiteralHighlighter select h).FirstOrDefault();
            if(existing != null)
            {
                TokenHighlighters.Remove(existing);
            }

            TokenHighlighters.Add(new StringLiteralHighlighter(fg, bg));
        }

        /// <summary>
        /// Lets you control how numeric values should be highlighted
        /// </summary>
        /// <param name="fg">The foreground highlight color</param>
        /// <param name="bg">The background highlight color</param>
        public void SetNumericHighlight(ConsoleColor? fg = null, ConsoleColor? bg = null)
        {
            var existing = (from h in TokenHighlighters where h is NumericHighlighter select h).FirstOrDefault();
            if (existing != null)
            {
                TokenHighlighters.Remove(existing);
            }

            TokenHighlighters.Add(new NumericHighlighter(fg, bg));
        }

        /// <summary>
        /// The implementation of ISyntaxHighlighter that uses the configuration you've created to perform syntax highlighting.
        /// </summary>
        /// <param name="context">Context that is used internally</param>
        /// <returns>true if any highlighting changes were made, false otherwise</returns>
        public bool TryHighlight(RichCommandLineContext context)
        {
            context.RefreshTokenInfo();
            bool didWork = false;
            for (int i = 0; i < context.Tokens.Count; i++)
            {
                var token = context.Tokens[i];
                bool hasMoreTokens = i < context.Tokens.Count - 1;
                bool didWorkOnThisToken = false;

                bool shouldBeHighlightedByAtLeastOneHighlighter = false;
                foreach(var tokenHighlighter in TokenHighlighters)
                {
                    bool shouldBeHighlightedByThisHighlighter = tokenHighlighter.ShouldBeHighlighted(context,token, i, hasMoreTokens == false);
                    shouldBeHighlightedByAtLeastOneHighlighter = shouldBeHighlightedByAtLeastOneHighlighter || shouldBeHighlightedByThisHighlighter;
                    if (shouldBeHighlightedByThisHighlighter)
                    {
                        didWorkOnThisToken = EnsureHighlighted(token, context, tokenHighlighter.HighlightForegroundColor, tokenHighlighter.HighlightBackgroundColor);
                        if (didWorkOnThisToken) break;
                    }
                }

                if(shouldBeHighlightedByAtLeastOneHighlighter == false)
                {
                    didWorkOnThisToken = EnsureHighlighted(token, context, null, null);
                }

                didWork = didWork || didWorkOnThisToken;
            }

            return didWork;
        }

        private bool EnsureHighlighted(Token token, RichCommandLineContext context, ConsoleColor? fg, ConsoleColor? bg)
        {
            if (fg.HasValue == false) fg = ConsoleString.DefaultForegroundColor;
            if (bg.HasValue == false) bg = ConsoleString.DefaultBackgroundColor;
            bool didWork = false;
            for (int i = token.StartIndex; i < token.StartIndex + token.Value.Length; i++)
            {
                if (context.Buffer[i].ForegroundColor != fg.Value || context.Buffer[i].BackgroundColor != bg.Value)
                {
                    didWork = true;
                    context.Buffer[i] = new ConsoleCharacter(context.Buffer[i].Value, fg, bg);
                }
            }
            return didWork;
        }
    }

    internal interface ITokenHighlighter
    {
        bool ShouldBeHighlighted(RichCommandLineContext context, Token currentToken, int currentTokenIndex, bool isLastToken);
        ConsoleColor? HighlightForegroundColor { get; }
        ConsoleColor? HighlightBackgroundColor { get; }
    }

    internal abstract class FixedHighlightTokenHighlighter : ITokenHighlighter
    {
        public ConsoleColor? HighlightForegroundColor { get; private set; }
        public ConsoleColor? HighlightBackgroundColor { get; private set; }

        public FixedHighlightTokenHighlighter(ConsoleColor? fg = null, ConsoleColor? bg = null)
        {
            this.HighlightForegroundColor = fg;
            this.HighlightBackgroundColor = bg;
        }

        public abstract bool ShouldBeHighlighted(RichCommandLineContext context, Token currentToken, int currentTokenIndex, bool isLastToken);
    }

    internal class KeywordHighlighter : FixedHighlightTokenHighlighter
    {
        protected string keyword;
        private StringComparison comparison;
        private bool onlyIfFirst;

        public KeywordHighlighter(string keyword, ConsoleColor? fg = null, ConsoleColor? bg = null, StringComparison comparison = StringComparison.InvariantCulture, bool onlyIfFirst = false) : base(fg, bg)
        {
            this.keyword = keyword;
            this.comparison = comparison;
            this.onlyIfFirst = onlyIfFirst;
        }

        public override bool ShouldBeHighlighted(RichCommandLineContext context, Token currentToken, int currentTokenIndex, bool isLastToken)
        {
            if (isLastToken) return false;
            if (onlyIfFirst && currentTokenIndex != 0) return false;
            return currentToken.Value.Equals(keyword, comparison);
        }
    }

    internal class ConditionalKeywordHighlighter : KeywordHighlighter
    {
        Func<RichCommandLineContext, bool> conditionEval;
        public ConditionalKeywordHighlighter(string keyword, Func<RichCommandLineContext, bool> conditionEval, ConsoleColor? fg = null, ConsoleColor? bg = null, StringComparison comparison = StringComparison.InvariantCulture)
            : base(keyword, fg, bg,comparison, false)
        {
            this.conditionEval = conditionEval;
        }
        public override bool ShouldBeHighlighted(RichCommandLineContext context, Token currentToken, int currentTokenIndex, bool isLastToken)
        {
            if (conditionEval(context))
            {
                return base.ShouldBeHighlighted(context, currentToken, currentTokenIndex, isLastToken);
            }
            else
            {
                return false;
            }
        }
    }

    internal class RegexHighlighter : FixedHighlightTokenHighlighter
    {

        private Regex regex;

        public RegexHighlighter(string pattern, ConsoleColor? fg = null, ConsoleColor? bg = null) : base(fg,bg)
        {
            this.regex = new Regex(pattern);
        }

        public override bool ShouldBeHighlighted(RichCommandLineContext context, Token currentToken, int currentTokenIndex, bool isLastToken)
        {
            var matches = regex.Matches(currentToken.Value);
            foreach(Match match in matches)
            {
                if (match.Value == currentToken.Value) return true;
            }
            return false;
        }
    }

    internal class StringLiteralHighlighter : RegexHighlighter
    {
        public StringLiteralHighlighter(ConsoleColor? fg = null, ConsoleColor? bg = null) : base("\".*\"", fg, bg) { }
    }

    internal class NumericHighlighter : FixedHighlightTokenHighlighter
    {
        public NumericHighlighter(ConsoleColor? fg = null, ConsoleColor? bg = null) : base(fg, bg) { }

        public override bool ShouldBeHighlighted(RichCommandLineContext context, Token currentToken, int currentTokenIndex, bool isLastToken)
        {
            double numericValue;
            return double.TryParse(currentToken.Value, out numericValue);
        }
    }
}
