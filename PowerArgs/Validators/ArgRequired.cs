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
        /// A valid command line alias or a boolean expression of aliases (and/or both supported as and '&amp;' and or '|').  
        /// When specified, the target argument is required unless the alternative argument(s) are specified.
        /// </summary>
        public string Unless { get; set; }

        /// <summary>
        /// True by default.  When set to true the user must specify either the target argument, the alternative arguments(s), but not both.
        /// When set to false the user can specify both the target and alternate arguments.
        /// </summary>
        public bool UnlessIsExclusive { get; set; }

        /// <summary>
        /// Your expression may be complex.  If so, set this optional property to customize the message shown if the user specifies both the
        /// target and alternate arguments and if UnlessIsExclusive is set to true.
        /// </summary>
        public string ExclusiveUnlessViolationErrorMessage { get; set;  }

        /// <summary>
        /// Your expression may be complex.  If so, set this optional property to customize the message shown if the user did not set target or
        /// alternate arguments.
        /// </summary>
        public string UnlessDescription { get; set; }

        /// <summary>
        /// A valid command line alias or boolean expression of aliases (and/or both supported as and '&amp;' and or '|').
        /// When specified the target argument is only required if the referenced argument(s) were specified on the command line.
        /// </summary>
        public string If { get; set; }

        /// <summary>
        /// Creates a new ArgRequired attribute.
        /// </summary>
        public ArgRequired()
        {
            Priority = 100;
            UnlessIsExclusive = true;
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
            if (Unless != null || If != null)
            {
                argument.Metadata.Add(new ArgRequiredConditionalHook(this));
            }

            if (arg == null && PromptIfMissing && ArgHook.HookContext.Current.Definition.IsNonInteractive == false)
            {
                var value = "";
                while (string.IsNullOrWhiteSpace(value))
                {
                    Console.Write("Enter value for " + argument.DefaultAlias + ": ");
                    value = Console.ReadLine();
                }

                arg = value;
            }
            if (arg == null)
            {
                if (Unless == null && If == null)
                {
                    throw new MissingArgException("The argument '" + argument.DefaultAlias + "' is required", new ArgumentNullException(argument.DefaultAlias));
                }
            }
        }
    }

    internal class ArgRequiredConditionalHook : ArgHook
    {
        private ArgRequired parent;

        public ArgRequiredConditionalHook(ArgRequired parent)
        {
            this.parent = parent;
        }

        public override void AfterPopulateProperties(ArgHook.HookContext context)
        {
            BooleanExpression expression;

            var variableResolver = new FuncVariableResolver((variableIdentifier) =>
            {
                foreach (var argument in context.Definition.Arguments)
                {
                    if (argument.IsMatch(variableIdentifier))
                    {
                        return argument.RevivedValue != null;
                    }
                }

                if (context.SpecifiedAction != null)
                {
                    foreach (var argument in context.SpecifiedAction.Arguments)
                    {
                        if (argument.IsMatch(variableIdentifier))
                        {
                            return argument.RevivedValue != null;
                        }
                    }
                }

                throw new InvalidArgDefinitionException(string.Format("'{0}' is not a valid argument alias", variableIdentifier));
            });

            if (parent.Unless != null)
            {
                try
                {
                    expression = BooleanExpressionParser.Parse(parent.Unless);
                }
                catch (Exception ex)
                {
                    var targetText =context.CurrentArgument.DefaultAlias + " (" + parent.Unless + ")";
                    throw new InvalidArgDefinitionException("Failed to parse the Unless clause on target '" + targetText + "'" + ex.Message);
                }

                bool unlessIsTrue;

                try
                {
                    unlessIsTrue = expression.Evaluate(variableResolver);
                }
                catch(Exception ex)
                {
                    var targetText = context.CurrentArgument.DefaultAlias + " (" + parent.Unless + ")";
                    throw new InvalidArgDefinitionException("Failed to parse the Unless clause on target '" + targetText + "'" + ex.Message);
                }

                if (unlessIsTrue == false && context.CurrentArgument.RevivedValue == null)
                {
                    throw new MissingArgException("The argument '" + context.CurrentArgument.DefaultAlias + "' is required unless " + (parent.UnlessDescription ?? "the following is true: " + parent.Unless), new ArgumentNullException(context.CurrentArgument.DefaultAlias));
                }

                if (unlessIsTrue == true && context.CurrentArgument.RevivedValue != null && parent.UnlessIsExclusive)
                {
                    throw new UnexpectedArgException("The argument '" + context.CurrentArgument.DefaultAlias + "' can't be specified if  " + (parent.UnlessDescription ?? "the following is true: " + parent.Unless + ". "), new ArgumentNullException(context.CurrentArgument.DefaultAlias));
                }
            }

            if(parent.If != null)
            {
                try
                {
                    expression = BooleanExpressionParser.Parse(parent.If);
                }
                catch (Exception ex)
                {
                    var targetText = context.CurrentArgument.DefaultAlias + " (" + parent.Unless + ")";
                    throw new InvalidArgDefinitionException("Failed to parse the If clause on target '" + targetText + "'" + ex.Message);
                }

                bool ifIsTrue;

                try
                {
                    ifIsTrue = expression.Evaluate(variableResolver);
                }
                catch(Exception ex)
                {
                    var targetText = context.CurrentArgument.DefaultAlias + " (" + parent.Unless + ")";
                    throw new InvalidArgDefinitionException("Failed to parse the If clause on target '" + targetText + "'" + ex.Message);
                }

                if(ifIsTrue == true && context.CurrentArgument.RevivedValue == null)
                {
                    throw new MissingArgException("The argument '" + context.CurrentArgument.DefaultAlias + "' is required if the following argument(s) are specified: " + parent.If);
                }
            }
        }
    }
}
 