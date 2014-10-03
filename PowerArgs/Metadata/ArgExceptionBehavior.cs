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
        /// Optionally show the TYPE column in the auto generated usage.  Defaults to true.
        /// </summary>
        [Obsolete("You can now use GenerateUsageFromTemplate to generate usage output.  There are a few built in templates, and you can write your own.  Go to TODO-URL to learn more.")] // TODO - Add a URL to a page that talks about how to create usage templates.
        public bool ShowTypeColumn { get; set; }

        /// <summary>
        /// Optionally show the POSITION column in the auto generated usage.  Defaults to true.
        /// </summary>
        [Obsolete("You can now use GenerateUsageFromTemplate to generate usage output.  There are a few built in templates, and you can write your own.  Go to TODO-URL to learn more.")] // TODO - Add a URL to a page that talks about how to create usage templates.
        public bool ShowPositionColumn { get; set; }

        /// <summary>
        /// Set to true to list possible values (usually for enums).  Defaults to true.
        /// </summary>
        [Obsolete("You can now use GenerateUsageFromTemplate to generate usage output.  There are a few built in templates, and you can write your own.  Go to TODO-URL to learn more.")] // TODO - Add a URL to a page that talks about how to create usage templates.
        public bool ShowPossibleValues { get; set; }

        /// <summary>
        /// Optionally override the ExeName.  You need to do this in unit tests.  In a real console app the
        /// value will be detected automatically if you leave this as null.
        /// </summary>
        [Obsolete("You can now use GenerateUsageFromTemplate to generate usage output.  There are a few built in templates, and you can write your own.  Go to TODO-URL to learn more.")] // TODO - Add a URL to a page that talks about how to create usage templates.
        public string ExeName { get; set; }

        /// <summary>
        /// The usage template to use to display usage information.  You can leave this null if you want to use the default template.
        /// </summary>
        [Obsolete("Use UsageTemplateProviderType")]
        public string UsageTemplateFile { get; set; }

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
            this.ShowTypeColumn = true;
            this.ShowPositionColumn = true;
            this.ShowPossibleValues = true;
            this.ExeName = null;
            this.UsageTemplateFile = null;
            this.UsageTemplateProviderType = typeof(DefaultConsoleUsageTemplateProvider);
        }
    }
}
