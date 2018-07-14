using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs
{
    /// <summary>
    /// An aggregate tab completion source that cycles through it's inner sources looking for matches.
    /// </summary>
    public class MultiTabCompletionSource : ITabCompletionSource
    {
        ITabCompletionSource[] sources;

        /// <summary>
        /// Create a new MultiTabCompletionSource given an array of sources.
        /// </summary>
        /// <param name="sources">The sources to wrap</param>
        public MultiTabCompletionSource(params ITabCompletionSource[] sources)
        {
            this.sources = sources;
        }

        /// <summary>
        /// Create a new MultiTabCompletionSource given an IEnumerable of sources.
        /// </summary>
        /// <param name="sources">The sources to wrap</param>
        public MultiTabCompletionSource(IEnumerable<ITabCompletionSource> sources)
        {
            this.sources = sources.ToArray();
        }

        /// <summary>
        /// Iterates over the wrapped sources looking for a match
        /// </summary>
        /// <param name="shift">Indicates if shift was being pressed</param>
        /// <param name="soFar">The text token that the user has typed before pressing tab.</param>
        /// <param name="context"></param>
        /// <param name="completion">The variable that you should assign the completed string to if you find a match.</param>
        /// <returns></returns>
        public bool TryComplete(TabCompletionContext context, out string completion)
        {
            foreach (var source in sources)
            {
                if(source.TryComplete(context, out completion))
                {
                    return true;
                }
            }
            completion = null;
            return false;
        }
    }
}
