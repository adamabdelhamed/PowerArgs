using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs
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
            var powerArgsContext = ConvertContext(cliContext);

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

        private TabCompletionContext ConvertContext(RichCommandLineContext innerContext)
        {
            TabCompletionContext context = new TabCompletionContext();
            context.Definition = this.Definition;
            context.Shift = innerContext.KeyPressed.Modifiers.HasFlag(ConsoleModifiers.Shift);
            context.PreviousToken = innerContext.CurrentTokenIndex > 0 ? innerContext.PreviousNonWhitespaceToken.Value : string.Empty;
            context.CompletionCandidate = innerContext.CurrentToken.Value;

            if (context.CompletionCandidate == " ")
            {
                context.CompletionCandidate = "";
            }

            context.CommandLineText = new ConsoleString(innerContext.Buffer).ToString();

            var firstToken = innerContext.Tokens.FirstOrDefault();
            if (firstToken != null)
            {
                var match = (from a in this.Definition.Actions where a.IsMatch(firstToken.Value) select a).SingleOrDefault();
                if (match != null)
                {
                    context.TargetAction = match;
                }
            }

            string argumentMatchId = null;


            if (context.PreviousToken.StartsWith("-"))
            {
                argumentMatchId = context.PreviousToken.Substring(1);
            }
            else if (context.PreviousToken.StartsWith("/"))
            {
                argumentMatchId = context.PreviousToken.Substring(1);
            }


            if (argumentMatchId != null)
            {
                var match = this.Definition.Arguments.Where(arg => arg.IsMatch(argumentMatchId) && arg.ArgumentType != typeof(bool)).SingleOrDefault();

                if (match == null && context.TargetAction != null)
                {
                    match = context.TargetAction.Arguments.Where(arg => arg.IsMatch(argumentMatchId) && arg.ArgumentType != typeof(bool)).SingleOrDefault();
                }

                if (match != null)
                {
                    context.TargetArgument = match;
                }
            }

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
    }
}
