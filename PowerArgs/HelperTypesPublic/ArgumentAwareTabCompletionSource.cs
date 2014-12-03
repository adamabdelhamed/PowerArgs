using System;
using System.Linq;

namespace PowerArgs
{
    /// <summary>
    /// An abstract class that can be used to implement tab completion logic that is specific to a given argument.
    /// </summary>
    public abstract class ArgumentAwareTabCompletionSource : ITabCompletionSourceWithContext
    {
        CommandLineArgumentsDefinition definition;
        CommandLineArgument argument;

        internal ArgumentAwareTabCompletionSource(CommandLineArgumentsDefinition definition, CommandLineArgument target)
        {
            this.definition = definition;
            this.argument = target;
        }

        /// <summary>
        /// Internal implementation that determines whether or not the context matches the target command line argument
        /// </summary>
        /// <param name="shift">true if the shift key was pressed along with the tab key</param>
        /// <param name="context">the completed token that appeared on the command line before the current, incomplete token that's being tabbed through</param>
        /// <param name="soFar">the incomplete token</param>
        /// <param name="completion">the completion to populate</param>
        /// <returns>true if completion was successful, false otherwise</returns>
        public bool TryComplete(bool shift, string context, string soFar, out string completion)
        {
            if (context.StartsWith("-"))
            {
                context = context.Substring(1);
            }
            else if (context.StartsWith("/"))
            {
                context = context.Substring(1);
            }
            else
            {
                completion = null;
                return false;
            }

            var match = definition.Arguments.Where(arg => arg.IsMatch(context)).SingleOrDefault();

            if(match == null)
            {
                foreach(var action in definition.Actions)
                {
                    match = action.Arguments.Where(arg => arg.IsMatch(context)).SingleOrDefault();
                    if (match != null) break;
                }
            }

            if (match == null)
            {
                completion = null;
                return false;
            }

            if(match != argument)
            {
                completion = null;
                return false;
            }

            return TryComplete(shift, match, soFar, out completion);
        }

        /// <summary>
        /// The abstract method that should be implemented to perform some tab completion logic
        /// </summary>
        /// <param name="shift">true if the shift key was pressed along with the tab key</param>
        /// <param name="target">the command line argument that the soFar token is going to apply to</param>
        /// <param name="soFar">the incomplete token</param>
        /// <param name="completion">the completion to populate</param>
        /// <returns>true if completion was successful, false otherwise</returns>
        public abstract bool TryComplete(bool shift, CommandLineArgument target, string soFar, out string completion);

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="shift">Not implemented</param>
        /// <param name="soFar">Not implemented</param>
        /// <param name="completion">Not implemented</param>
        /// <returns>throws NotImplementedException</returns>
        public bool TryComplete(bool shift, string soFar, out string completion)
        {
            throw new NotImplementedException("Since this class implements ITabCompletionSourceWithContext it is expected that the other TryComplete will be called.");
        }
    }

    internal class ArgumentAwareWrapperTabCompletionSource : ArgumentAwareTabCompletionSource
    {
        public ITabCompletionSource WrappedSource { get; private set; }
        public ArgumentAwareWrapperTabCompletionSource(CommandLineArgumentsDefinition definition, CommandLineArgument target, ITabCompletionSource toWrap) : base(definition, target)
        {
            this.WrappedSource = toWrap;
        }

        public override bool TryComplete(bool shift, CommandLineArgument context, string soFar, out string completion)
        {
            return WrappedSource.TryComplete(shift, soFar, out completion);
        }
    }

    internal class ArgumentAwareWrapperSmartTabCompletionSource : ISmartTabCompletionSource
    {
        public CommandLineArgumentsDefinition Definition { get; set; }
        public CommandLineArgument Target { get; set; }
        public ISmartTabCompletionSource WrappedSource { get; private set; }
        public ArgumentAwareWrapperSmartTabCompletionSource(CommandLineArgumentsDefinition definition, CommandLineArgument target, ISmartTabCompletionSource toWrap)
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
