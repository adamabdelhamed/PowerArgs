using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs
{
    /// <summary>
    /// Validates that the user actually provided a value for the given property on the command line.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public class ArgRequired : ArgValidator
    {
        /// <summary>
        /// Determines whether or not the validator should run even if the user doesn't specify a value on the command line.
        /// This value is always true for this validator.
        /// </summary>
        public override bool ImplementsValidateAlways
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// A valid command line alias or boolean expression of aliases (and/or/not supported as and '&amp;', or '|', and not '!').
        /// When specified the target argument is only required if the referenced argument(s) were specified on the command line.
        /// </summary>
        public string If { get; set; }

        /// <summary>
        /// A valid command line alias or boolean expression of aliases (and/or/not supported as and '&amp;', or '|', and not '!').
        /// When specified the target argument is only required if the referenced argument(s) were not specified on the command line.
        /// </summary>
        public string IfNot { get; set; }

        /// <summary>
        /// Determines if this metadata represents an argument conditionionally required.  This will be true if you've set the If or the IfNot property.
        /// </summary>
        public bool IsConditionallyRequired
        {
            get
            {
                return If != null || IfNot != null;
            }
        }

        /// <summary>
        /// Gets or sets the type to be used for tab completion when prompting for a missing, required argument.  The type must implement ITabCompletionHandler
        /// </summary>
        public Type TabCompletionHandlerType { get; set; }

        /// <summary>
        /// Gets or sets the type to use inject custom syntax highlighting when prompting for a missing, required argument.  The type must implement IHighlighterConfigurator
        /// </summary>
        public Type HighlighterConfiguratorType { get; set; }

        /// <summary>
        /// Creates a new ArgRequired attribute.
        /// </summary>
        public ArgRequired()
        {
            Priority = 100;
        }

        /// <summary>
        /// If you set this to true and the user didn't specify a value then the command line will prompt the user for the value.
        /// </summary>
        public bool PromptIfMissing { get; set; }

        /// <summary>
        /// Validates that the user actually specified a value and optionally prompts them when it is missing.
        /// </summary>
        /// <param name="argument">The argument being populated.  This validator doesn't do anything with it.</param>
        /// <param name="arg">The value specified on the command line or null if it wasn't specified</param>
        public override void ValidateAlways(CommandLineArgument argument, ref string arg)
        {
            if (IsConditionallyRequired)
            {
                var matchingHook = (from h in argument.Metadata.Metas<ArgRequiredConditionalHook>() where h.parent == this select h).SingleOrDefault();
                if(matchingHook == null)
                {
                    argument.Metadata.Add(new ArgRequiredConditionalHook(this));
                }
            }

            if (IsConditionallyRequired == false && arg == null && PromptIfMissing && ArgHook.HookContext.Current.Definition.IsNonInteractive == false)
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

                cli.Reader.UnregisterHandler(ConsoleKey.Escape);
                cli.Reader.RegisterHandler(KeyHandler.FromAction((searchReaderContext) =>
                {
                    TabCompletion tabCompletionInfo;
                    if (ArgHook.HookContext.Current.Definition.IsNonInteractive == false &&
                        ArgHook.HookContext.Current.Definition.Metadata.TryGetMeta<TabCompletion>(out tabCompletionInfo) 
                        && tabCompletionInfo.REPL == true)
                    {
                        // if this is an interactive REPL then continue the REPL in this case as the user may have changed their mind about taking
                        // this action - Note there are two places in this file that have this logic
                        throw new REPLContinueException();
                    }
                    else
                    {
                        throw new MissingArgException("The argument '" + argument.DefaultAlias + "' is required", new ArgumentNullException(argument.DefaultAlias));
                    }
                }, ConsoleKey.Escape));

                arg = cli.PromptForLine("Enter value for " + argument.DefaultAlias);
            }

            if (arg == null && IsConditionallyRequired == false)
            {
                throw new MissingArgException("The argument '" + argument.DefaultAlias + "' is required", new ArgumentNullException(argument.DefaultAlias));
            }
        }
    }

    internal class ArgRequiredConditionalHook : ArgHook
    {
        /// <summary>
        /// Gets or sets the type to be used for tab completion when prompting for a missing, required argument.  The type must implement ITabCompletionHandler
        /// </summary>
        public Type TabCompletionHandlerType { get; set; }

        /// <summary>
        /// Gets or sets the type to use inject custom syntax highlighting when prompting for a missing, required argument.  The type must implement IHighlighterConfigurator
        /// </summary>
        public Type HighlighterConfiguratorType { get; set; }


        internal ArgRequired parent;

        public ArgRequiredConditionalHook(ArgRequired parent)
        {
            this.parent = parent;
            this.AfterPopulatePropertiesPriority = 2;
            this.TabCompletionHandlerType = parent.TabCompletionHandlerType;
            this.HighlighterConfiguratorType = parent.HighlighterConfiguratorType;
        }

        public override void AfterPopulateProperties(ArgHook.HookContext context)
        {
            if(parent.If != null && parent.IfNot != null)
            {
                throw new InvalidArgDefinitionException("You cannot specify both the 'If' and the 'IfNot' properties on the ArgRequired metadata");
            }
            else if(parent.If != null)
            {
                Evaluate(context, parent.If, false);
            }
            else if (parent.IfNot != null)
            {
                Evaluate(context, parent.IfNot, true);
            }
            else
            {
                throw new InvalidOperationException("ArgRequired could not determine if the given argument was required.  This is likely a bug in PowerArgs.");
            }
        }

        private void Evaluate(ArgHook.HookContext context, string expressionText, bool not)
        {
            try
            {
                var newExpressionText = expressionText;
                if(not)
                {
                    newExpressionText = "!(" + expressionText + ")";
                }

                var expression = BooleanExpressionParser.Parse(newExpressionText);
                var eval = expression.Evaluate(context.Definition.CreateVariableResolver());

                if(not)
                {
                    if (eval == true && context.CurrentArgument.RevivedValue == null)
                    {
                        if (TryPreventExceptionWithPrompt(context) == false)
                        {
                            throw new MissingArgException("The argument '" + context.CurrentArgument.DefaultAlias + "' is required if the following argument(s) are not specified: " + expressionText);
                        }
                    }
                }
                else
                {
                    if (eval == true && context.CurrentArgument.RevivedValue == null)
                    {
                        if (TryPreventExceptionWithPrompt(context) == false)
                        {
                            throw new MissingArgException("The argument '" + context.CurrentArgument.DefaultAlias + "' is required if the following argument(s) are specified: " + expressionText);
                        }
                    }
                }
            }
            catch(MissingArgException)
            {
                throw;
            }
            catch(REPLContinueException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var targetText = context.CurrentArgument.DefaultAlias + " (" + expressionText + ")";
                throw new InvalidArgDefinitionException("Failed to evaluate conditional ArgRequired clause on target '" + targetText + "'" + ex.Message);
            }
        }

        private bool TryPreventExceptionWithPrompt(ArgHook.HookContext context)
        {
            if (parent.PromptIfMissing && ArgHook.HookContext.Current.Definition.IsNonInteractive == false)
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

                cli.Reader.UnregisterHandler(ConsoleKey.Escape);
                cli.Reader.RegisterHandler(KeyHandler.FromAction((searchReaderContext) =>
                {
                    TabCompletion tabCompletionInfo;
                    if (context.Definition.IsNonInteractive == false && 
                        context.Definition.Metadata.TryGetMeta<TabCompletion>(out tabCompletionInfo) && 
                        tabCompletionInfo.REPL == true)
                    {
                        // if this is an interactive REPL then continue the REPL in this case as the user may have changed their mind about taking
                        // this action - Note there are two places in this file that have this logic
                        throw new REPLContinueException();
                    }
                    else
                    {
                        throw new MissingArgException("The argument '" + context.CurrentArgument.DefaultAlias + "' is required", new ArgumentNullException(context.CurrentArgument.DefaultAlias));
                    }
                }, ConsoleKey.Escape));

                context.ArgumentValue = cli.PromptForLine("Enter value for " + context.CurrentArgument.DefaultAlias);
                context.CurrentArgument.Populate(context);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
 