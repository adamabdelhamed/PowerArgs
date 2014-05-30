using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    /// <summary>
    /// Argument metadata that lets you declare that a particular argument is not allowed if one or more other arguments are specified by the user.
    /// </summary>
    public class ArgCantBeCombinedWith : ArgHook
    {
        private IBooleanExpression Expression { get;  set; }

        /// <summary>
        /// The expression text that was passed into the constructor.  This can either be an alias argument or a boolean expression of arguments (e.g. Argument1 | Argument2). Valid operators are
        /// and '&amp;', or '|', and not '!'.  Grouping with parentheses is also supported.  Example: "(Argument1 &amp; Argumrnt2) | Argument3".
        /// </summary>
        public string ExpressionText { get; private set; }

        /// <summary>
        /// Creates a new ArgCantBeCombinedWith hook given an expression. This can either be an alias argument or a boolean expression of arguments (e.g. Argument1 | Argument2). Valid operators are
        /// and '&amp;', or '|', and not '!'.  Grouping with parentheses is also supported.  Example: "(Argument1 &amp; Argumrnt2) | Argument3".
        /// If the expression evaluates to true after all arguments have been populated then an UnexpectedArgumentException is thrown.
        /// </summary>
        /// <param name="expression">This can either be an alias argument or a boolean expression of arguments (e.g. Argument1 | Argument2). Valid operators are
        /// and '&amp;', or '|', and not '!'.  Grouping with parentheses is also supported.  Example: "(Argument1 &amp; Argumrnt2) | Argument3".</param>
        public ArgCantBeCombinedWith(string expression)
        {
            this.AfterPopulatePropertiesPriority = 1;
            try
            {
                this.Expression = BooleanExpressionParser.Parse(expression);
                this.ExpressionText = expression;
            }
            catch(Exception ex)
            {
                throw new InvalidArgDefinitionException("Exception parsing conditional ArgCantBeCombinedWith expression '" + expression + "' - " + ex.Message);
            }
        }

        /// <summary>
        /// Determines if the current argument is allowed to be populated based on which other arguments are present and based on the expression passed to the constructor.
        /// </summary>
        /// <param name="context">The current PowerArgs processing context</param>
        /// <returns>True if this argument can be specified, false otherwise</returns>
        public bool IsCurrentArgumentAllowed(ArgHook.HookContext context)
        {
            if (context.CurrentArgument == null)
            {
                throw new InvalidArgDefinitionException("The " + GetType().Name + " metadata must be applied to a particular argument");
            }

            try
            {
                bool eval = this.Expression.Evaluate(context.Definition.CreateVariableResolver());
                if (eval == true)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                var targetText = context.CurrentArgument.DefaultAlias;
                throw new InvalidArgDefinitionException("Failed to evaluate conditional " + GetType().Name + " clause on target '" + targetText + "' - " + ex.Message);
            }

            return true;
        }

        /// <summary>
        /// Checks to see if the current argument is allowed to have a value based on which other arguments are present and based on the expression
        /// passed to the constructor.  If it's not allowed and has been specified then an UnexpectedArgException is thrown.
        /// </summary>
        /// <param name="context">The current PowerArgs processing context</param>
        public override void AfterPopulateProperties(ArgHook.HookContext context)
        {
            if(IsCurrentArgumentAllowed(context) == false && context.CurrentArgument.RevivedValue != null)
            {
                throw new UnexpectedArgException("The argument '" + context.CurrentArgument.DefaultAlias + "' cannot be used with one or more arguments: " + ExpressionText);
            }
        }
    }
}
