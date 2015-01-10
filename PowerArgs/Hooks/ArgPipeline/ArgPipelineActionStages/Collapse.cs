using System.Collections.Generic;

namespace PowerArgs.Preview
{
    /// <summary>
    /// An implementation of a pipeline stage that collapses all objects that have been accepted into a list and then pushes the list through the pipeline
    /// </summary>
    [ArgPipelineActionStage("Collapse")]
    internal class Collapse : InProcessPipelineStage
    {
        List<object> objects = new List<object>();

        /// <summary>
        /// Creates a Collapse stage
        /// </summary>
        /// <param name="commandLine"></param>
        public Collapse(string[] commandLine) : base(commandLine)
        {
            if (commandLine.Length > 0) throw new ArgException("Collapse takes no command line input");
        }

        /// <summary>
        /// Stores the object
        /// </summary>
        /// <param name="o">The object to store</param>
        protected override void OnObjectReceived(object o)
        {
            lock (objects)
            {
                objects.Add(o);
            }
        }

        /// <summary>
        /// Pushes the list of stored objects through the pipeline
        /// </summary>
        protected override void BeforeSetDrainedToTrue()
        {
            ArgPipeline.Push(objects, this);
        }
    }
}
