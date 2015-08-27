using PowerArgs.Cli;
using System;

namespace PowerArgs
{
    /// <summary>
    /// An attribute that you can add to argument properties or parameters that lets you inject custom contextual assistant logic into the PowerArgs enhanced command line.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public class ArgContextualAssistant : Attribute, ICommandLineArgumentMetadata
    {
        /// <summary>
        /// Gets the type to be used for contexual assistance.  It must implement IContextAssistProvider.
        /// </summary>
        public Type ContextAssistProviderType { get; private set; }

        private IContextAssistProvider _cachedProvider;

        internal IContextAssistProvider GetContextAssistProvider(CommandLineArgumentsDefinition definition)
        {
            if (_cachedProvider == null)
            {
                try
                {
                    ContextAssistProviderType.TryCreate<IContextAssistProvider>(new object[] { definition }, out _cachedProvider);
                }
                catch (InvalidArgDefinitionException)
                {
                    ContextAssistProviderType.TryCreate<IContextAssistProvider>(out _cachedProvider);
                }
            }
            return _cachedProvider;
        }

        /// <summary>
        /// Initializes the metadata given the type that implements IContextAssistProvider.
        /// </summary>
        /// <param name="contextAssistProviderType">a type that implements IContextAssistProvider</param>
        public ArgContextualAssistant(Type contextAssistProviderType)
        {
            this.ContextAssistProviderType = contextAssistProviderType;
        }
    }
}
