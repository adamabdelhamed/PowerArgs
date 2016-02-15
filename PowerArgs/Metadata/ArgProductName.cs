using System;

namespace PowerArgs
{
    /// <summary>
    /// Use this attribute to describe your proper program name (which might be longer than exeName). This string that will appear in the usage documentation
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ArgProductName : Attribute, IGlobalArgMetadata
    {
        /// <summary>
        /// The product name value
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Creates a new ArgProductName attribute.
        /// </summary>
        /// <param name="value">The product name value</param>
        public ArgProductName(string value)
        {
            this.Value = value;
        }
    }
}
