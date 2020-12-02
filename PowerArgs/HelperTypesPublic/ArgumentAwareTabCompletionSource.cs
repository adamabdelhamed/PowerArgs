using System;
using System.Linq;

namespace PowerArgs
{
    public class ArgumentAwareTabCompletionContext 
    {
        public CommandLineArgument Argument { get; set; }
        public TabCompletionContext InnerContext { get; set; }
    }

    /// <summary>
    /// An abstract class that can be used to implement tab completion logic that is specific to a given argument.
    /// </summary>
    public abstract class ArgumentAwareTabCompletionSource : ITabCompletionSource
    {
        CommandLineArgumentsDefinition definition;
        CommandLineArgument argument;

        internal ArgumentAwareTabCompletionSource(CommandLineArgumentsDefinition definition, CommandLineArgument target)
        {
            this.definition = definition;
            this.argument = target;
        }
 
        /// <summary>
        /// Derived clases will implement this method which can perform completions
        /// using the provided context
        /// </summary>
        /// <param name="context">the tab completion context</param>
        /// <param name="completion">the completion value</param>
        /// <returns>true if the completion was successful, false otherwise</returns>
        public abstract bool TryComplete(ArgumentAwareTabCompletionContext context, out string completion);

        public bool TryComplete(TabCompletionContext context, out string completion)
        {
            var fixedUpCandidate = context.CompletionCandidate;
            if (ArgParser.IsDashSpecifiedArgumentIdentifier(fixedUpCandidate))
            {
                fixedUpCandidate = fixedUpCandidate.Substring(1);
            }
            else if (fixedUpCandidate.StartsWith("/"))
            {
                fixedUpCandidate = fixedUpCandidate.Substring(1);
            }
            else
            {
                completion = null;
                return false;
            }

            var match = definition.Arguments.Where(arg => arg.IsMatch(fixedUpCandidate)).SingleOrDefault();

            if (match == null)
            {
                foreach (var action in definition.Actions)
                {
                    match = action.Arguments.Where(arg => arg.IsMatch(fixedUpCandidate)).SingleOrDefault();
                    if (match != null) break;
                }
            }

            if (match == null)
            {
                completion = null;
                return false;
            }

            if (match != argument)
            {
                completion = null;
                return false;
            }

            return TryComplete(new ArgumentAwareTabCompletionContext()
            {
                InnerContext = context,
                Argument = argument
            }
            , out completion);
        }
    }


    internal class ArgumentAwareWrapperSmartTabCompletionSource : ITabCompletionSource
    {
        public CommandLineArgumentsDefinition Definition { get; set; }
        public CommandLineArgument Target { get; set; }
        public ITabCompletionSource WrappedSource { get; private set; }
        public ArgumentAwareWrapperSmartTabCompletionSource(CommandLineArgumentsDefinition definition, CommandLineArgument target, ITabCompletionSource toWrap)
        {
            this.Target = target;
            this.Definition = definition;
            this.WrappedSource = toWrap;
        }

        public bool TryComplete(TabCompletionContext context, out string completion)
        {
            if (context.TargetArgument == Target)
            {
                return WrappedSource.TryComplete(context, out completion);
            }
            else
            {
                completion = null;
                return false;
            }
        }
    }
}
