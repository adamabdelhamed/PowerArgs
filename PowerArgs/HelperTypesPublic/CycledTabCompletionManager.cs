using System;
using System.Collections.Generic;

namespace PowerArgs
{
    /// <summary>
    /// This helper class can be leveraged when implementing custom tab completion logic.  It knows how to cycle through multple
    /// candidates and support tabbing forward and shift/tabbing backwards.  You just pass values from the tab completion methods
    /// and then provide an evaluation function that knows how to get the list of possible matches.
    /// </summary>
    public class CycledTabCompletionManager
    {
        /// <summary>
        /// If the value of soFar is a string that's less than this value then no completion will be returned.
        /// </summary>
        public int MinCharsBeforeCyclingBegins { get; set; }

        int lastIndex;
        string lastCompletion;
        string lastSoFar;
 
        public bool Cycle(TabCompletionContext context, Func<List<string>> evaluation, out string completion)
        {
            if (context.CompletionCandidate == lastCompletion && lastCompletion != null)
            {
                context.CompletionCandidate = lastSoFar;
            }

            var candidates = evaluation();

            if (context.CompletionCandidate == lastSoFar) lastIndex = context.Shift ? lastIndex - 1 : lastIndex + 1;
            if (lastIndex >= candidates.Count) lastIndex = 0;
            if (lastIndex < 0) lastIndex = candidates.Count - 1;
            lastSoFar = context.CompletionCandidate;

            if (candidates.Count == 0 || (candidates.Count > 1 && context.CompletionCandidate.Length < MinCharsBeforeCyclingBegins))
            {
                completion = null;
                return false;
            }
            else
            {
                completion = candidates[lastIndex];
                lastCompletion = completion;
                return true;
            }
        }
    }
}
