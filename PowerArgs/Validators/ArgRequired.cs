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
                var value = "";
                while (string.IsNullOrWhiteSpace(value))
                {
                    Console.Write("Enter value for " + argument.DefaultAlias + ": ");
                    value = Console.ReadLine();
                }

                arg = value;
            }

            if (arg == null && IsConditionallyRequired == false)
            {
                throw new MissingArgException("The argument '" + argument.DefaultAlias + "' is required", new ArgumentNullException(argument.DefaultAlias));
            }
        }
    }

    internal class ArgRequiredConditionalHook : ArgHook
    {
        internal ArgRequired parent;

        public ArgRequiredConditionalHook(ArgRequired parent)
        {
            this.parent = parent;
            this.AfterPopulatePropertiesPriority = 2;
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
                var value = "";
                while (string.IsNullOrWhiteSpace(value))
                {
                    Console.Write("Enter value for " + context.CurrentArgument.DefaultAlias + ": ");
                    value = Console.ReadLine();
                }

                context.ArgumentValue = value;
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
 