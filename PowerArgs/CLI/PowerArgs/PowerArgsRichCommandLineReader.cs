using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A customized version of the RichTextCommandLineReader that configures tab completion, history, and syntax highlighting for a given
    /// command line arguments definition.
    /// </summary>
    public class PowerArgsRichCommandLineReader : RichTextCommandLineReader, ITabCompletionHandler
    {
        /// <summary>
        /// Gets or sets the foreground color to use when a valid argument name appears on the command line
        /// </summary>
        public ConsoleColor ArgumentNameForeground { get; set; }

        /// <summary>
        /// Gets or sets the foreground color to use when a numeric value appears on the command line
        /// </summary>
        public ConsoleColor NumericForeground { get; set; }

        /// <summary>
        /// Gets or sets the foreground color to use when a double quoted string literal appears on the command line
        /// </summary>
        public ConsoleColor StringLiteralForeground { get; set; }

        /// <summary>
        /// Gets or sets the foregrund color to use when a valid action alias appears as the first token on the command line
        /// </summary>
        public ConsoleColor ActionForeground { get; set; }

        /// <summary>
        /// Gets the definition that is used to configure the reader
        /// </summary>
        public CommandLineArgumentsDefinition Definition { get; private set; }


        private IEnumerable<ITabCompletionSource> oldHooks;
        private IEnumerable<ISmartTabCompletionSource> newHooks;
        
        /// <summary>
        /// Configures the reader for the given definition and history information.
        /// </summary>
        /// <param name="definition">The definition to use to configure the reader</param>
        /// <param name="history">previous command line values that the end user will be able to cycle through using the up and down arrows</param>
        public PowerArgsRichCommandLineReader(CommandLineArgumentsDefinition definition, List<ConsoleString> history)
        {
            this.Console = ConsoleProvider.Current;
            this.HistoryManager.Values.AddRange(history);
            this.TabHandler.TabCompletionHandlers.Add(this);
            this.ContextAssistProvider = new PowerArgsMultiContextAssistProvider(definition);
            this.Definition = definition;

            this.ArgumentNameForeground = ConsoleColor.Cyan;
            this.NumericForeground = ConsoleColor.Green;
            this.StringLiteralForeground = ConsoleColor.Yellow;
            this.ActionForeground = ConsoleColor.Magenta;

            this.oldHooks = FindOldTabCompletionHooks(this.Definition);
            this.newHooks = FindNewTabCompletionHooks(this.Definition);
            InitHighlighters();
        }

        private void InitHighlighters()
        {
            this.Highlighter = new SimpleSyntaxHighlighter();
            this.Highlighter.AddTokenHighlighter(new ValidationEnforcementTokenHighlighter(this.Definition));
            foreach(var argument in Definition.Arguments)
            {
                foreach(var alias in argument.Aliases)
                {
                    this.Highlighter.AddKeyword("-" + alias, ArgumentNameForeground, comparison: argument.IgnoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture);
                }
            }

            foreach(var action in Definition.Actions)
            {
                foreach(var alias in action.Aliases)
                {
                    this.Highlighter.AddConditionalKeyword(alias, (readerContext, highlighterContext) => { return highlighterContext.CurrentTokenIndex == 0; }, ActionForeground, comparison: action.IgnoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture);
                }

                foreach(var argument in action.Arguments)
                {
                    foreach(var alias in argument.Aliases)
                    {
                        this.Highlighter.AddConditionalKeyword("-" + alias, (context, highlighterContext) => 
                            {
                                return action.IsMatch(context.Tokens.First().Value);
                            }
                        , ArgumentNameForeground, comparison: argument.IgnoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture);
                    }
                }
            }

            this.Highlighter.SetNumericHighlight(NumericForeground);
            this.Highlighter.SetQuotedStringLiteralHighlight(StringLiteralForeground);
        }

        /// <summary>
        /// Implementation of tab completion that leverages tab completion sources that are registered with the target definition.
        /// </summary>
        /// <param name="cliContext">cintext used internally</param>
        /// <returns>true if a tab completion was successfully made, false otherwise</returns>
        public bool TryTabComplete(RichCommandLineContext cliContext)
        {
            var powerArgsContext = ConvertContext(this.Definition, cliContext);

            bool oldHookWon = false;
            string completion = null;
            foreach (var completionSource in oldHooks)
            {
                if (completionSource is ITabCompletionSourceWithContext)
                {
                    if (((ITabCompletionSourceWithContext)completionSource).TryComplete(powerArgsContext.Shift, powerArgsContext.PreviousToken, powerArgsContext.CompletionCandidate, out completion))
                    {
                        oldHookWon = true;
                        break;
                    }
                }
                else
                {
                    if (completionSource.TryComplete(powerArgsContext.Shift, powerArgsContext.CompletionCandidate, out completion))
                    {
                        oldHookWon = true;
                        break;
                    }
                }
            }

            if (oldHookWon == false)
            {
                foreach (var completionSource in newHooks)
                {
                    if (completionSource.TryComplete(powerArgsContext, out completion))
                    {
                        break;
                    }
                }
            }

            if (completion != null)
            {
                cliContext.CompleteCurrentToken(cliContext.CurrentToken, new ConsoleString(completion));
                return true;
            }
            else
            {
                return false;
            }
        }

        internal static TabCompletionContext ConvertContext(CommandLineArgumentsDefinition definition, RichCommandLineContext innerContext)
        {
            TabCompletionContext context = new TabCompletionContext();
            context.Definition = definition;
            context.Shift = innerContext.KeyPressed.Modifiers.HasFlag(ConsoleModifiers.Shift);
            context.PreviousToken = innerContext.CurrentTokenIndex > 0 ? innerContext.PreviousNonWhitespaceToken.Value : string.Empty;
            context.CompletionCandidate = innerContext.CurrentToken.Value;

            if (context.CompletionCandidate == " ")
            {
                context.CompletionCandidate = "";
            }

            context.CommandLineText = new ConsoleString(innerContext.Buffer).ToString();
            context.TargetAction = FindContextualAction(innerContext.Tokens.FirstOrDefault().Value, definition);
            context.TargetArgument = FindContextualArgument(context.PreviousToken, context.TargetAction, definition);
            return context;
        }

        private static IEnumerable<ITabCompletionSource> FindOldTabCompletionHooks(CommandLineArgumentsDefinition definition)
        {
            List<ITabCompletionSource> completionSources = new List<ITabCompletionSource>();

            if (definition.Metadata.HasMeta<TabCompletion>() && definition.Metadata.Meta<TabCompletion>().CompletionSourceType != null && definition.Metadata.Meta<TabCompletion>().CompletionSourceType.GetInterfaces().Contains(typeof(ITabCompletionSource)))
            {
                completionSources.Add((ITabCompletionSource)Activator.CreateInstance(definition.Metadata.Meta<TabCompletion>().CompletionSourceType));
            }

            foreach (var argument in definition.AllGlobalAndActionArguments)
            {
                foreach (var argSource in argument.Metadata.Metas<ArgumentAwareTabCompletionAttribute>())
                {
                    var source = argSource.CreateTabCompletionSource(definition, argument);
                    if (source is ITabCompletionSource)
                    {
                        completionSources.Insert(0, (ITabCompletionSource)source);
                    }
                }
            }

            return completionSources;
        }

        private static IEnumerable<ISmartTabCompletionSource> FindNewTabCompletionHooks(CommandLineArgumentsDefinition definition)
        {
            List<ISmartTabCompletionSource> completionSources = new List<ISmartTabCompletionSource>();

            if (definition.Metadata.HasMeta<TabCompletion>() && definition.Metadata.Meta<TabCompletion>().CompletionSourceType != null && definition.Metadata.Meta<TabCompletion>().CompletionSourceType.GetInterfaces().Contains(typeof(ISmartTabCompletionSource)))
            {
                completionSources.Add((ISmartTabCompletionSource)Activator.CreateInstance(definition.Metadata.Meta<TabCompletion>().CompletionSourceType));
            }

            foreach (var argument in definition.AllGlobalAndActionArguments)
            {
                foreach (var argSource in argument.Metadata.Metas<ArgumentAwareTabCompletionAttribute>())
                {
                    var source = argSource.CreateTabCompletionSource(definition, argument);
                    if (source is ISmartTabCompletionSource)
                    {
                        completionSources.Insert(0, (ISmartTabCompletionSource)source);
                    }
                }

                if (argument.ArgumentType.IsEnum)
                {
                    completionSources.Insert(0, new EnumTabCompletionSource(argument));
                }
            }
            completionSources.Add(new ActionAndArgumentSmartTabCompletionSource());
            completionSources.Add(new FileSystemTabCompletionSource());

            return completionSources;
        }

        /// <summary>
        /// A helper that detects the argument represented by the current token given a definition.  
        /// </summary>
        /// <param name="contextualAction">An action to inspect for a match if the current token does not match a global argument.  Pass null to only check global arguments.</param>
        /// <param name="currentToken">The token to inspect.  If you pass null you will get null back.</param>
        /// <param name="expectMatchingArg">This will be set to true if the current token starts with a '-' or a '/' meaning that the token was an argument indicator, even if it didn't match an argument in the definition.</param>
        /// <param name="def">The definition to inspect.  If null, the ambient definition will be used.  If there is no ambient definition and null is passed then this method throws a NullReferenceException.</param>
        /// <returns>An argument that is matched by the given token or null if there was no match</returns>
        public static CommandLineArgument FindCurrentTokenArgument(CommandLineAction contextualAction, string currentToken, out bool expectMatchingArg, CommandLineArgumentsDefinition def = null)
        {            
            def = PassThroughOrTryGetAmbientDefinition(def);

            if (currentToken == null)
            {
                expectMatchingArg = false;
                return null;
            }

            string currentTokenArgumentNameValue = null;
            expectMatchingArg = false;
            if (currentToken.StartsWith("-"))
            {
                currentTokenArgumentNameValue = currentToken.Substring(1);
                expectMatchingArg = true;
            }
            else if (currentToken.StartsWith("/"))
            {
                currentTokenArgumentNameValue = currentToken.Substring(1);
                expectMatchingArg = true;
            }

            CommandLineArgument currentTokenArgument = null;
            if (currentTokenArgumentNameValue != null)
            {
                currentTokenArgument = def.Arguments.Where(arg => arg.IsMatch(currentTokenArgumentNameValue)).SingleOrDefault();

                if (currentTokenArgument == null && contextualAction != null)
                {
                    currentTokenArgument = contextualAction.Arguments.Where(arg => arg.IsMatch(currentTokenArgumentNameValue)).SingleOrDefault();
                }
            }
            return currentTokenArgument;
        }

        /// <summary>
        /// A helper that detects the argument represented by the current token given a definition.  
        /// </summary>
        /// <param name="previousToken">The token to inspect.  If you pass null you will get null back.</param>
        /// <param name="contextualAction">An action to inspect for a match if the current token does not match a global argument.  Pass null to only check global arguments.</param>
        /// <param name="def">The definition to inspect.  If null, the ambient definition will be used.  If there is no ambient definition and null is passed then this method throws a NullReferenceException.</param>
        /// <returns>An argument that is matched by the given token or null if there was no match</returns>
        public static CommandLineArgument FindContextualArgument(string previousToken, CommandLineAction contextualAction, CommandLineArgumentsDefinition def = null)
        {
            def = PassThroughOrTryGetAmbientDefinition(def);

            if (previousToken == null)
            {
                return null;
            }

            string currentTokenArgumentNameValue = null;
            if (previousToken.StartsWith("-"))
            {
                currentTokenArgumentNameValue = previousToken.Substring(1);
            }
            else if (previousToken.StartsWith("/"))
            {
                currentTokenArgumentNameValue = previousToken.Substring(1);
            }

            CommandLineArgument currentTokenArgument = null;
            if (currentTokenArgumentNameValue != null)
            {
                currentTokenArgument = def.Arguments.Where(arg => arg.IsMatch(currentTokenArgumentNameValue) && arg.ArgumentType != typeof(bool)).SingleOrDefault();

                if (currentTokenArgument == null && contextualAction != null)
                {
                    currentTokenArgument = contextualAction.Arguments.Where(arg => arg.IsMatch(currentTokenArgumentNameValue) && arg.ArgumentType != typeof(bool)).SingleOrDefault();
                }
            }
            return currentTokenArgument;
        }

        /// <summary>
        /// Searches the reader's tokens for a non whitespace token that preceeds the current token
        /// </summary>
        /// <param name="readerContext">the reader context to inspect</param>
        /// <param name="highlighterContext">the highlighter context to inspect</param>
        /// <returns>a non whitespace token that preceeds the current token or null if no such token is found</returns>
        public static string FindPreviousNonWhitespaceToken(RichCommandLineContext readerContext, HighlighterContext highlighterContext)
        {
            string previousToken = null;

            for (int i = highlighterContext.CurrentTokenIndex - 1; i >= 0; i--)
            {
                if (string.IsNullOrWhiteSpace(readerContext.Tokens[i].Value) == false)
                {
                    previousToken = readerContext.Tokens[i].Value;
                    break;
                }
            }
            return previousToken;
        }

        /// <summary>
        /// Finds the action that matches the given token in the given definition
        /// </summary>
        /// <param name="firstToken">the token to test.  If you pass null you will get null back.</param>
        /// <param name="def">The definition to inspect.  If null, the ambient definition will be used.  If there is no ambient definition and null is passed then this method throws a NullReferenceException.</param>
        /// <returns>the action that matches the given token in the given definition or null if no such action is found</returns>
        public static CommandLineAction FindContextualAction(string firstToken, CommandLineArgumentsDefinition def = null)
        {
            def = PassThroughOrTryGetAmbientDefinition(def);
            if(firstToken == null)
            {
                return null;
            }
            return def.FindMatchingAction(firstToken);
        }

        private static CommandLineArgumentsDefinition PassThroughOrTryGetAmbientDefinition(CommandLineArgumentsDefinition def)
        {
            if(def != null)
            {
                return def;
            }
            else if(ArgHook.HookContext.Current != null && ArgHook.HookContext.Current.Definition != null)
            {
                return ArgHook.HookContext.Current.Definition;
            }
            else
            {
                throw new NullReferenceException("There is no ambient CommandLineArgumentsDefinition argument and you did not pass one in explicitly");
            }
        }
    }

    internal class ValidationEnforcementTokenHighlighter : ITokenHighlighter
    {
        CommandLineArgumentsDefinition definition;
        public ValidationEnforcementTokenHighlighter(CommandLineArgumentsDefinition definition)
        {
            this.definition = definition;
        }

        public bool ShouldBeHighlighted(RichCommandLineContext readerContext, HighlighterContext highlighterContext)
        {
            // don't even try mark tokens as invalid unless the cursor is on it
            if (readerContext.BufferPosition >= highlighterContext.CurrentToken.StartIndex && readerContext.BufferPosition < highlighterContext.CurrentToken.EndIndex)
            {
                return false;
            }

            var currentToken = highlighterContext.CurrentToken.Value;
            var previousToken = PowerArgsRichCommandLineReader.FindPreviousNonWhitespaceToken(readerContext, highlighterContext);
            var firstToken = readerContext.Tokens[0].Value;

            CommandLineAction contextualAction = PowerArgsRichCommandLineReader.FindContextualAction(firstToken, definition);
            CommandLineArgument contextualArgument = PowerArgsRichCommandLineReader.FindContextualArgument(previousToken, contextualAction, definition);

            if (contextualArgument != null)
            {
                if(contextualArgument.TestIsValidAndRevivable(currentToken) == false)
                {
                    // the current token either failed validation or could not be revived
                    return true;
                }
            }

            bool expectMatchingArg;
            CommandLineArgument currentTokenArgument = PowerArgsRichCommandLineReader.FindCurrentTokenArgument(contextualAction, currentToken, out expectMatchingArg, definition);

            if(currentTokenArgument == null && expectMatchingArg)
            {
                // The current token starts with a - or /, but does not match a global or action specific argument, so we'll highlight the token red
                return true;
            }

            return false;
        }

        public ConsoleColor? HighlightForegroundColor
        {
            get { return ConsoleColor.Red; }
        }

        public ConsoleColor? HighlightBackgroundColor
        {
            get { return null; }
        }
    }
}
