using System;
using System.Collections.Generic;
using System.Text;

namespace PowerArgs
{
    /// <summary>
    /// Use this attribute to customize your property argument's name to display.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Class, AllowMultiple = false)]
    public class ArgDisplayName : Attribute, IGlobalArgMetadata
    {
        /// <summary>
        /// A customized argument name to display.
        /// </summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// Creates a new ArgDisplayName attribute.
        /// </summary>
        /// <param name="displayName">A customized argument name to display.</param>
        public ArgDisplayName(string displayName)
        {
            this.DisplayName = displayName;
        }
    }
}
