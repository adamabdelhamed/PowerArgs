﻿
using System;
using System.Linq;
namespace PowerArgs
{
    /// <summary>
    /// A hook that can be put on an argument so that if a user specifies the argument with no value they will get prompted for that value with a rich prompt
    /// that supports tab completion and syntax highlighting.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public class PromptIfEmpty : ArgHook, ICommandLineArgumentMetadata
    {
        /// <summary>
        /// Gets or sets the type to be used for global tab completion.  The type must implement ITabCompletionHandler
        /// </summary>
        public Type TabCompletionHandlerType { get; set; }

        /// <summary>
        /// Gets or sets the type to use inject custom syntax highlighting to the command prompt.  The type must implement IHighlighterConfigurator
        /// </summary>
        public Type HighlighterConfiguratorType { get; set; }

        /// <summary>
        /// If set to true the prompt will only run once on empty.  Program execution will continue if the value given to the prompt is empty
        /// </summary>
        public bool KeepAsking { get; set; }

        /// <summary>
        /// Prompts the user to enter a value for the given property in the case that the option was specified with no value
        /// </summary>
        /// <param name="context">the parser context</param>
        public override void BeforePopulateProperty(ArgHook.HookContext context)
        {
            if (string.IsNullOrEmpty(context.ArgumentValue))
            {
                do
                {
                    var cli = new CliHelper();

                    ITabCompletionHandler tabHandler;
                    IHighlighterConfigurator highlighterConfigurator;

                    if (TabCompletionHandlerType.TryCreate<ITabCompletionHandler>(out tabHandler))
                    {
                        cli.Reader.TabHandler.TabCompletionHandlers.Add(tabHandler);
                    }

                    if (HighlighterConfiguratorType.TryCreate<IHighlighterConfigurator>(out highlighterConfigurator))
                    {
                        cli.Reader.Highlighter = new SimpleSyntaxHighlighter();
                        highlighterConfigurator.Configure(cli.Reader.Highlighter);
                    }

                    var result = cli.PromptForLine("Enter value for " + context.CurrentArgument.DefaultAlias);
                    result = result == "" ? null : result;
                    context.ArgumentValue = result;
                } while (KeepAsking && context.ArgumentValue == null);
            }
        }
    }
}
