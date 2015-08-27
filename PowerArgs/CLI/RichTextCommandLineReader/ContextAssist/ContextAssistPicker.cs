using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli
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
        public List<ContextAssistSearchResult> Options { get; private set; }

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
            Options = new List<ContextAssistSearchResult>();
        }

        /// <summary>
        /// returns all options that contain the given search string, ignoring case
        /// </summary>
        /// <param name="searchString">the search string</param>
        /// <returns>all options that contain the given search string, ignoring case</returns>
        protected override List<ContextAssistSearchResult> GetResults(string searchString)
        {
            return Options
                    .Where(o => o.DisplayText.IndexOf(searchString, StringComparison.InvariantCultureIgnoreCase) >= 0)
                    .OrderByDescending(o => o.DisplayText.StartsWith(searchString, StringComparison.InvariantCultureIgnoreCase) ? 1 : 0)
                    .ToList();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="searchString">Not implemented</param>
        /// <returns>Not implemented</returns>
        protected override System.Threading.Tasks.Task<List<ContextAssistSearchResult>> GetResultsAsync(string searchString)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Lets the user pick from the set of options.
        /// </summary>
        /// <param name="console">optionally provide a custom console implementation</param>
        /// <param name="allowCancel">if true, users can cancel picking by pressing the escape key.  If false, the escape key does nothing.</param>
        /// <returns>A valid selection or null if the search was cancelled.</returns>
        public ContextAssistSearchResult Pick(IConsoleProvider console = null, bool allowCancel = true)
        {
             return this.Search(console, allowCancel);
        }
    }
}
