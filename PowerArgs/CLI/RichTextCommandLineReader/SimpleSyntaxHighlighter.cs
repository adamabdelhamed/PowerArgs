using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PowerArgs.Cli
{
    /// <summary>
    /// Context about a token that helps determine if it should be highlighted
    /// </summary>
    public class HighlighterContext
    {
        /// <summary>
        /// The index of this token within the set of tokens in the base context
        /// </summary>
        public int CurrentTokenIndex { get; internal set; }

        /// <summary>
        /// The token that may or may not need to be highlighted
        /// </summary>
        public Token CurrentToken { get; internal set; }

        /// <summary>
        /// True if this is the last token, false otherwise
        /// </summary>
        public bool IsLastToken { get; internal set; }
    }

    /// <summary>
    /// A utility that makes it easy to perform common types of syntax highlighting based on keywords, regular expressions, etc.
    /// </summary>
    public class SimpleSyntaxHighlighter
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
        public void AddKeyword(string keyword, ConsoleColor? fg = null, ConsoleColor? bg = null, StringComparison comparison = StringComparison.InvariantCulture)
        {
            TokenHighlighters.Add(new KeywordHighlighter(keyword, fg, bg, comparison));
        }

        /// <summary>
        /// Registers a keyword with the highlighter that should only be highlighted if some condition is true
        /// </summary>
        /// <param name="keyword">The keyword that will conditionally be highlighted when found on the command line</param>
        /// <param name="conditionEval">the conditional evaluation function</param>
        /// <param name="fg">The foreground highlight color</param>
        /// <param name="bg">The background highlight color</param>
        /// <param name="comparison">Determines how strings are compared. </param>
        public void AddConditionalKeyword(string keyword, Func<RichCommandLineContext,HighlighterContext, bool> conditionEval, ConsoleColor? fg = null, ConsoleColor? bg = null, StringComparison comparison = StringComparison.InvariantCulture)
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
        /// Registers a custom token highlighter
        /// </summary>
        /// <param name="highlighter">the custom highlighter</param>
        public void AddTokenHighlighter(ITokenHighlighter highlighter)
        {
            TokenHighlighters.Add(highlighter);
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
        /// <param name="readerContext">Context that is used internally</param>
        /// <returns>true if any highlighting changes were made, false otherwise</returns>
        public bool TryHighlight(RichCommandLineContext readerContext)
        {
            readerContext.RefreshTokenInfo();
            bool didWork = false;
            for (int i = 0; i < readerContext.Tokens.Count; i++)
            {
                if(string.IsNullOrWhiteSpace(readerContext.Tokens[i].Value))
                {
                    continue;
                }

                var highlighterContext = new HighlighterContext()
                {
                    CurrentToken = readerContext.Tokens[i],
                    CurrentTokenIndex = i,
                    IsLastToken = i == readerContext.Tokens.Count-1,

                };

                bool didWorkOnThisToken = false;

                bool shouldBeHighlightedByAtLeastOneHighlighter = false;
                foreach (var tokenHighlighter in TokenHighlighters)
                {
                    bool shouldBeHighlightedByThisHighlighter = tokenHighlighter.ShouldBeHighlighted(readerContext, highlighterContext);
                    shouldBeHighlightedByAtLeastOneHighlighter = shouldBeHighlightedByAtLeastOneHighlighter || shouldBeHighlightedByThisHighlighter;
                    if (shouldBeHighlightedByThisHighlighter)
                    {
                        didWorkOnThisToken = EnsureHighlighted(highlighterContext.CurrentToken, readerContext, tokenHighlighter.HighlightForegroundColor, tokenHighlighter.HighlightBackgroundColor);
                        break;
                    }
                }

                if(shouldBeHighlightedByAtLeastOneHighlighter == false)
                {
                    didWorkOnThisToken = EnsureHighlighted(highlighterContext.CurrentToken, readerContext, null, null);
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

    /// <summary>
    /// An interface that defines how to dynamically configure a highlighter
    /// </summary>
    public interface IHighlighterConfigurator
    {
        /// <summary>
        /// Lets implementors configure a highlighter in a dynamic way
        /// </summary>
        /// <param name="highlighter">The highlighter to configure</param>
        void Configure(SimpleSyntaxHighlighter highlighter);
    }

    /// <summary>
    /// An interface the defines the contract for how individual tokens get syntax highlighting on the command line
    /// </summary>
    public interface ITokenHighlighter
    {
        /// <summary>
        /// Determines if this highlighter should highlight the current token with this highlighter's foreground and background
        /// colors.  
        /// </summary>
        /// <param name="readerContext">context from the reader</param>
        /// <param name="highlighterContext">context about the current token</param>
        /// <returns>true if this highlighter should highlight the current token, false otherwise</returns>
        bool ShouldBeHighlighted(RichCommandLineContext readerContext, HighlighterContext highlighterContext);
        
        /// <summary>
        /// The foreground color of this highlighter.  If null, the console default is used.
        /// </summary>
        ConsoleColor? HighlightForegroundColor { get; }
        /// <summary>
        /// The background color of this highlighter.  If null, the console default is used.
        /// </summary>
        ConsoleColor? HighlightBackgroundColor { get; }
    }

    /// <summary>
    /// A highlighter that has a fixed foreground and background color.  Most highlighters derive from this base.
    /// </summary>
    public abstract class FixedHighlightTokenHighlighter : ITokenHighlighter
    {
        /// <summary>
        /// The foreground color of this highlighter.  If null, the console default is used.
        /// </summary>
        public ConsoleColor? HighlightForegroundColor { get; private set; }
        /// <summary>
        /// The background color of this highlighter.  If null, the console default is used.
        /// </summary>
        public ConsoleColor? HighlightBackgroundColor { get; private set; }

        /// <summary>
        /// Creates a new highlighter using the given colors
        /// </summary>
        /// <param name="fg">The foreground color of this highlighter.  If null, the console default is used.</param>
        /// <param name="bg">The background color of this highlighter.  If null, the console default is used.</param>
        public FixedHighlightTokenHighlighter(ConsoleColor? fg = null, ConsoleColor? bg = null)
        {
            this.HighlightForegroundColor = fg;
            this.HighlightBackgroundColor = bg;
        }

        /// <summary>
        /// Determines if this highlighter should highlight the current token with this highlighter's foreground and background
        /// colors.  
        /// </summary>
        /// <param name="readerContext">context from the reader</param>
        /// <param name="highlighterContext">context about the current token</param>
        /// <returns>true if this highlighter should highlight the current token, false otherwise</returns>
        public abstract bool ShouldBeHighlighted(RichCommandLineContext readerContext, HighlighterContext highlighterContext);
    }

    /// <summary>
    /// A highlighter that is used to highlight a specific keyword
    /// </summary>
    public class KeywordHighlighter : FixedHighlightTokenHighlighter
    {
        /// <summary>
        /// The keyword to highlight whenever it is found
        /// </summary>
        protected string keyword;
        private StringComparison comparison;

        /// <summary>
        /// Creates the highlighter.
        /// </summary>
        /// <param name="keyword">The keyword to highlight whenever it is found</param>
        /// <param name="fg">The foreground color of this highlighter.  If null, the console default is used.</param>
        /// <param name="bg">The background color of this highlighter.  If null, the console default is used.</param>
        /// <param name="comparison">determines how strings are compared.  By default the comparison is case sensitive</param>
        public KeywordHighlighter(string keyword, ConsoleColor? fg = null, ConsoleColor? bg = null, StringComparison comparison = StringComparison.InvariantCulture) : base(fg, bg)
        {
            this.keyword = keyword;
            this.comparison = comparison;
        }

        /// <summary>
        /// Returns true if the keyword is matched, false otherwise
        /// </summary>
        /// <param name="readerContext">context from the reader</param>
        /// <param name="highlighterContext">context about the current token</param>
        /// <returns>true if the keyword matched, false otherwise</returns>
        public override bool ShouldBeHighlighted(RichCommandLineContext readerContext, HighlighterContext highlighterContext)
        {
            if (highlighterContext.IsLastToken) return false;
            return highlighterContext.CurrentToken.Value.Equals(keyword, comparison);
        }
    }

    /// <summary>
    /// A keyword highlighter that highlights based on a condition
    /// </summary>
    public class ConditionalKeywordHighlighter : KeywordHighlighter
    {
        private Func<RichCommandLineContext, HighlighterContext, bool> conditionEval;

        /// <summary>
        /// Creates the highlighter.
        /// </summary>
        /// <param name="keyword">The keyword to match</param>
        /// <param name="conditionEval">The conditional match evaluation function</param>
        /// <param name="fg">The foreground color of this highlighter.  If null, the console default is used.</param>
        /// <param name="bg">The background color of this highlighter.  If null, the console default is used.</param>
        /// <param name="comparison">determines how characters are compared</param>
        public ConditionalKeywordHighlighter(string keyword, Func<RichCommandLineContext, HighlighterContext, bool> conditionEval, ConsoleColor? fg = null, ConsoleColor? bg = null, StringComparison comparison = StringComparison.InvariantCulture)
            : base(keyword, fg, bg,comparison)
        {
            this.conditionEval = conditionEval;
        }

        /// <summary>
        /// Returns true if the token matches the keyword and the given conditional evaluation returns true
        /// </summary>
        /// <param name="readerContext">context from the reader</param>
        /// <param name="highlighterContext">context about the current token</param>
        /// <returns>true if the token matches the keyword and the given conditional evaluation returns true</returns>
        public override bool ShouldBeHighlighted(RichCommandLineContext readerContext, HighlighterContext highlighterContext)
        {
            if (conditionEval(readerContext, highlighterContext))
            {
                return base.ShouldBeHighlighted(readerContext, highlighterContext);
            }
            else
            {
                return false;
            }
        }
    }

    /// <summary>
    /// A highlighter that highlights based on a regular expression match
    /// </summary>
    public class RegexHighlighter : FixedHighlightTokenHighlighter
    {

        private Regex regex;

        /// <summary>
        /// Creates the highlighter
        /// </summary>
        /// <param name="pattern">The regular expression pattern</param>
        /// <param name="fg">The foreground color of this highlighter.  If null, the console default is used.</param>
        /// <param name="bg">The background color of this highlighter.  If null, the console default is used.</param>
        public RegexHighlighter(string pattern, ConsoleColor? fg = null, ConsoleColor? bg = null) : base(fg,bg)
        {
            this.regex = new Regex(pattern);
        }

        /// <summary>
        /// Returns true if the regular expression is matched, false otherwise
        /// </summary>
        /// <param name="readerContext">context from the reader</param>
        /// <param name="highlighterContext">context about the current token</param>
        /// <returns>true if the regular expression is matched, false otherwise</returns>
        public override bool ShouldBeHighlighted(RichCommandLineContext readerContext, HighlighterContext highlighterContext)
        {
            var matches = regex.Matches(highlighterContext.CurrentToken.Value);
            foreach(Match match in matches)
            {
                if (match.Value == highlighterContext.CurrentToken.Value) return true;
            }
            return false;
        }
    }

    /// <summary>
    /// A highlighter that highlights double quoted string literals
    /// </summary>
    public class StringLiteralHighlighter : RegexHighlighter
    {
        /// <summary>
        /// Creates the highlighter
        /// </summary>
        /// <param name="fg">The foreground color of this highlighter.  If null, the console default is used.</param>
        /// <param name="bg">The background color of this highlighter.  If null, the console default is used.</param>
        public StringLiteralHighlighter(ConsoleColor? fg = null, ConsoleColor? bg = null) : base("\".*\"", fg, bg) { }
    }

    /// <summary>
    /// A highlighter that highlights numeric values
    /// </summary>
    public class NumericHighlighter : FixedHighlightTokenHighlighter
    {
        /// <summary>
        /// Creates the highlighter
        /// </summary>
        /// <param name="fg">The foreground color of this highlighter.  If null, the console default is used.</param>
        /// <param name="bg">The background color of this highlighter.  If null, the console default is used.</param>
        public NumericHighlighter(ConsoleColor? fg = null, ConsoleColor? bg = null) : base(fg, bg) { }

        /// <summary>
        /// Returns true if the current token is a numeric value, false otherwise
        /// </summary>
        /// <param name="readerContext">context from the reader</param>
        /// <param name="highlighterContext">context about the current token</param>
        /// <returns>true if the current token is a numeric value, false otherwise</returns>
        public override bool ShouldBeHighlighted(RichCommandLineContext readerContext, HighlighterContext highlighterContext)
        {
            double numericValue;
            return double.TryParse(highlighterContext.CurrentToken.Value, out numericValue);
        }
    }
}
