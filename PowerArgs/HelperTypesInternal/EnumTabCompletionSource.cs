using System;
using System.Globalization;
using System.Linq;

namespace PowerArgs
{
    internal class EnumTabCompletionSource : ArgumentAwareTabCompletionSource
    {
        SimpleTabCompletionSource wrappedSource;
        public EnumTabCompletionSource(CommandLineArgumentsDefinition definition, CommandLineArgument argument)
            : base(definition, argument)
        {
            var options = Enum.GetNames(argument.ArgumentType).Union(argument.ArgumentType.GetEnumShortcuts());
            wrappedSource = new SimpleTabCompletionSource(options);
        }

        public override bool TryComplete(bool shift, CommandLineArgument context, string soFar, out string completion)
        {
            return wrappedSource.TryComplete(shift, soFar, out completion);
        }
    }
}
