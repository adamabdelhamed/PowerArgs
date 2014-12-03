using System;
using System.Globalization;
using System.Linq;

namespace PowerArgs
{
    internal class EnumTabCompletionSource : ISmartTabCompletionSource
    {
        SimpleTabCompletionSource wrappedSource;
        CommandLineArgument target;

        public EnumTabCompletionSource(CommandLineArgument target)
        {
            this.target = target;
            var options = Enum.GetNames(target.ArgumentType).Union(target.ArgumentType.GetEnumShortcuts());
            wrappedSource = new SimpleTabCompletionSource(options) { MinCharsBeforeCyclingBegins = 0};
        }

        public bool TryComplete(TabCompletionContext context, out string completion)
        {
            if(context.TargetArgument != target)
            {
                completion = null;
                return false;
            }

            return wrappedSource.TryComplete(context.Shift, context.CompletionCandidate, out completion);
        }
    }
}
