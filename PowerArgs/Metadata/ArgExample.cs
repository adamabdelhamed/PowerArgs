using System;

namespace PowerArgs
{
    /// <summary>
    /// Use this attribute to provide an example of how to use your program.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class ArgExample : Attribute, IGlobalArgMetadata
    {
        /// <summary>
        /// Returns true if this example has a title, false otherwwise
        /// </summary>
        public bool HasTitle
        {
            get
            {
                return Title != null;
            }
        }

        /// <summary>
        /// An optional title for this example
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The example command line.
        /// </summary>
        public string Example { get; private set; }

        /// <summary>
        /// A brief description of what this example does.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Creates a new ArgExample class
        /// </summary>
        /// <param name="example">The example command line.</param>
        /// <param name="description">A brief description of what this example does.</param>
        public ArgExample(string example, string description)
        {
            this.Example = example;
            this.Description = description;
        }
    } 
}
