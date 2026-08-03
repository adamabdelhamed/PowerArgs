using System;
using System.Text.RegularExpressions;

namespace PowerArgs
{
    /// <summary>
    /// Performs regular expression validation on a property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public class ArgRegex : ArgValidator
    {
        /// <summary>
        /// The regular expression to match
        /// </summary>
        protected string regex;

        /// <summary>
        /// A prefix for the error message to show in the case of a match.
        /// </summary>
        protected string errorMessage;

        /// <summary>
        /// The exact match that was found.
        /// </summary>
        protected Match exactMatch;

        /// <summary>
        /// Creates a new ArgRegex validator.
        /// </summary>
        /// <param name="regex">The regular expression that requires an exact match to be valid</param>
        /// <param name="errorMessage">A prefix for the error message to show in the case of a match.</param>
        public ArgRegex(string regex, string errorMessage = "Invalid argument")
        {
            this.regex = regex;
            this.errorMessage = errorMessage;
        }

        /// <summary>
        /// Validates that the given arg exactly matches the regular expression provided.
        /// </summary>
        /// <param name="name">the name of the property being populated.  This validator doesn't do anything with it.</param>
        /// <param name="arg">The value specified on the command line.</param>
        public override void Validate(string name, ref string arg)
        {
            exactMatch = Regex.Match(arg, regex);
            if (exactMatch.Success && exactMatch.Index == 0 && exactMatch.Length == arg.Length) return;
            exactMatch = null;
            throw new ValidationArgException(errorMessage + ": " + arg);
        }
    }
}
