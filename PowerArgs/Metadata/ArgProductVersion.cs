using System;

namespace PowerArgs
{
    /// <summary>
    /// Use this attribute to describe a version string that can appear in the usage documentation
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ArgProductVersion : Attribute, IGlobalArgMetadata
    {
        /// <summary>
        /// The copyright value
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Creates a new ArgProductVersion attribute.
        /// </summary>
        /// <param name="value">The version value</param>
        public ArgProductVersion(string value)
        {
            this.Value = value;
        }
    }
}
