using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs
{
    /// <summary>
    /// A context assist provider that lets the user select from a fixed set of options
    /// </summary>
    public class ContextAssistPicker : ContextAssistSearch
    {
        /// <summary>
        /// returns false, always
        /// </summary>
        public override bool SupportsAsync { get { return false; } }

        /// <summary>
        /// The options the user can choose from
        /// </summary>
        public List<string> Options { get; private set; }

        /// <summary>
        /// Returns true if there is at least one option, false otherwise.
        /// </summary>
        /// <param name="context">context about the parent reader</param>
        /// <returns>true if there is at least one option, false otherwise</returns>
        public override bool CanAssist(RichCommandLineContext context)
        {
            return Options.Count > 0;
        }

        /// <summary>
        /// initialized the picker
        /// </summary>
        public ContextAssistPicker()
        {
            Options = new List<string>();
        }

        /// <summary>
        /// returns all options that contain the given search string, ignoring case
        /// </summary>
        /// <param name="searchString">the search string</param>
        /// <returns>all options that contain the given search string, ignoring case</returns>
        protected override List<string> GetResults(string searchString)
        {
            return Options
                    .Where(o => o.IndexOf(searchString, StringComparison.InvariantCultureIgnoreCase) >= 0)
                    .OrderByDescending(o => o.StartsWith(searchString, StringComparison.InvariantCultureIgnoreCase) ? 1 : 0)
                    .ToList();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="searchString">Not implemented</param>
        /// <returns>Not implemented</returns>
        protected override System.Threading.Tasks.Task<List<string>> GetResultsAsync(string searchString)
        {
            throw new NotImplementedException();
        }
    }
}
