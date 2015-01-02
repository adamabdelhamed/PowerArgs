using System.Collections;

namespace PowerArgs.Preview
{
    /// <summary>
    /// A pipeline stage that expands the inumerable objects into individual objects and pushes them thorough the pipeline
    /// </summary>
    [ArgPipelineActionStage("Expand")]
    internal class Expand : InProcessPipelineStage
    {
        /// <summary>
        /// Creates an expand stage
        /// </summary>
        /// <param name="commandLine">no command line arguments are supported.  You should always pass an empty array</param>
        public Expand(string[] commandLine) : base(commandLine)
        {
            if (commandLine.Length > 0) throw new ArgException("Expand takes no command line input");
        }

        /// <summary>
        /// Non enumerable objects pass through.  Enumerable objects are enumerated and each item is passed through.
        /// </summary>
        /// <param name="o">The object to process</param>
        protected override void OnObjectReceived(object o)
        {
            if (o is IEnumerable == false)
            {
                ArgPipeline.Push(o);
            }
            else
            {
                foreach (var item in (IEnumerable)o)
                {
                    ArgPipeline.Push(item);
                }
            }
        }
    }
}
