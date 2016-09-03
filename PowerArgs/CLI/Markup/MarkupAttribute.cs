using System;

namespace PowerArgs.Cli
{
    /// <summary>
    /// An attribute that a can be added to a ConsoleControl property of a complex type
    /// so that text values in markup can be converted to those types.  If the complex type implements
    /// a static Parse method then you may not need this attribute.  However, if your complex type would benefit
    /// from an experience where the user provides multiple attributes for your type (e.g. Fill and Fill-Padding)
    /// then the MarkupPropertyAttribute will work because you will have access to the parser context. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class MarkupPropertyAttribute : Attribute
    {
        internal IMarkupProcessor Processor { get; private set; }

        /// <summary>
        /// Initiates the processor
        /// </summary>
        /// <param name="processorType">The processor type that must implement IMarkupProcessor</param>
        public MarkupPropertyAttribute(Type processorType)
        {
            this.Processor = (IMarkupProcessor)Activator.CreateInstance(processorType);
        }
    }

    /// <summary>
    /// An attribute you can add to a ConsoleControl class to indicate that the given markup
    /// attribute name can be safely ignored.  This is likely because another markup processor
    /// will handle it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class MarkupIgnoreAttribute : MarkupExtensionAttribute
    {
        private class NoOpProcessor : IMarkupProcessor
        {
            public void Process(ParserContext context)
            {

            }
        }

        /// <summary>
        /// Initiates the ignore attribute given the markup xml attribute name
        /// </summary>
        /// <param name="attributeName">the xml markup attribute name to ignore</param>
        public MarkupIgnoreAttribute(string attributeName) : base(attributeName, typeof(NoOpProcessor)) { }
    }

    /// <summary>
    /// An attribute that can be added to a ConsoleControl class that lets you process markup attributes that do not map to properties on your control
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class MarkupExtensionAttribute : Attribute
    {
        internal IMarkupProcessor Processor { get; private set; }

        /// <summary>
        /// The markup attribute name that you're adding support for
        /// </summary>
        public string AttributeName { get; private set; }

        /// <summary>
        /// Creates a markup extension given an attribute name and a processor type
        /// </summary>
        /// <param name="attributeName">The markup attribute name that you're adding support for</param>
        /// <param name="processorType">The processor type that must implement IMarkupProcessor</param>
        public MarkupExtensionAttribute(string attributeName, Type processorType)
        {
            this.AttributeName = attributeName;
            this.Processor = (IMarkupProcessor)Activator.CreateInstance(processorType);
        }
    }

    /// <summary>
    /// An interface that defines the protocol for processing ConsoleApp markup
    /// </summary>
    public interface IMarkupProcessor
    {
        /// <summary>
        /// A method that will be called by the markup parser at the appropriate time
        /// </summary>
        /// <param name="context">Context about the markup element, control, and view model being processed</param>
        void Process(ParserContext context);
    }
}
