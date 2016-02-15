using System;

namespace PowerArgs
{
    /// <summary>
    /// Use this attribute to describe a copyright string that can appear in the usage documentation
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ArgCopyright : Attribute, IGlobalArgMetadata
    {
        /// <summary>
        /// The copyright value
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Creates a new ArgCopyright attribute.
        /// </summary>
        /// <param name="value">The copyright value</param>
        public ArgCopyright(string value)
        {
            this.Value = value;
        }
    }
}
