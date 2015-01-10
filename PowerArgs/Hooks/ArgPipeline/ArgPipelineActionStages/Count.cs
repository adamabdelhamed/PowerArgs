namespace PowerArgs.Preview
{
    /// <summary>
    /// A pipeline stage that counts the objects that have been pushed through it
    /// </summary>
    [ArgPipelineActionStage("Count")]
    internal class Count : InProcessPipelineStage
    {
        object lockObj = new object();
        int count = 0;

        /// <summary>
        /// Creates a new Count stage
        /// </summary>
        /// <param name="commandLine">no command line arguments are supported.  You should always pass an empty array</param>
        public Count(string[] commandLine) : base(commandLine) 
        {
            if (commandLine.Length > 0) throw new ArgException("Filter takes no command line input");
        }

        /// <summary>
        /// Increments the count
        /// </summary>
        /// <param name="o">The object passed through the pipeline</param>
        protected override void OnObjectReceived(object o)
        {
            lock(lockObj)
            {
                count++;
            }
        }

        /// <summary>
        /// Pushes the count through the pipeline
        /// </summary>
        protected override void BeforeSetDrainedToTrue()
        {
            ArgPipeline.Push(count, this);
        }
    } 
}
