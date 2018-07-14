using System;

namespace PowerArgs
{
    /// <summary>
    /// Enum used to specify how user errors (ArgExceptions) should be handled by the parser.
    /// </summary>
    public enum ArgExceptionPolicy
    {
        /// <summary>
        /// The default, PowerArgs will throw these exceptions for your program to handle.
        /// </summary>
        DontHandleExceptions,
        /// <summary>
        /// PowerArgs will print the user friendly error message as well as the auto-generated usage documentation
        /// for the program.
        /// </summary>
        StandardExceptionHandling,
    }

    /// <summary>
    /// Use this attrbiute to opt into standard error handling of user input errors.  
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ArgExceptionBehavior : Attribute, ICommandLineArgumentsDefinitionMetadata
    {
        /// <summary>
        /// The policy to use, defaults to DontHandleExceptions.
        /// </summary>
        public ArgExceptionPolicy Policy { get; private set; }


        /// <summary>
        /// A type that should implement IUsageTemplateProvider.  When specified the help hook will use the GenerateUsageFromTemplate function rather than the obsolete GenerateStyledUsage function.
        /// </summary>
        public Type UsageTemplateProviderType { get; set; }

        /// <summary>
        /// Creates a new ArgExceptionBehavior attributes with the given policy.
        /// </summary>
        /// <param name="policy">The policy to use, defaults to DontHandleExceptions.</param>
        public ArgExceptionBehavior(ArgExceptionPolicy policy = ArgExceptionPolicy.DontHandleExceptions)
        {
            this.Policy = policy;
            this.UsageTemplateProviderType = typeof(DefaultConsoleUsageTemplateProvider);
        }
    }
}
