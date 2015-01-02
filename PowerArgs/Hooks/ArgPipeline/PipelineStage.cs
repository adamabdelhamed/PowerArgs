using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace PowerArgs.Preview
{
    /// <summary>
    /// An abstract class that represents a pipeline stage that can be launched from another PowerArgs enabled application.
    /// </summary>
    public abstract class ExternalPipelineInputStage : PipelineStage
    {
        /// <summary>
        /// Gets whether or not this stage has verified that this process has been launched by another application and that the pipeline should
        /// be connected
        /// </summary>
        public abstract bool IsProgramLaunchedByExternalPipeline { get; protected set; }

        /// <summary>
        /// Creates an instance of the stage given a base definition and a command line
        /// </summary>
        /// <param name="baseDefinition">The base definition that declares which actions are supported by this program</param>
        /// <param name="commandLine">The command line arguments</param>
        public ExternalPipelineInputStage(CommandLineArgumentsDefinition baseDefinition, string[] commandLine) : base(commandLine) { }
    }

    /// <summary>
    /// An abstract class that represents a stage in a processing pipeline
    /// </summary>
    public abstract class PipelineStage
    {
        /// <summary>
        /// An even that fires when this stage is drained.
        /// </summary>
        public event Action Drained;

        private List<string> _cmdLineArgs;

        [ThreadStatic]
        private static PipelineStage _current;

        /// <summary>
        /// Gets a reference to the Pipeline Stage running on this thread or null if there isn't one
        /// </summary>
        public static PipelineStage Current
        {
            get
            {
                return _current;
            }
            internal set
            {
                _current = value;
            }
        }

        /// <summary>
        /// Gets a reference to the pipeline manager that is orchestrating the pipeline or null if there isn't one
        /// </summary>
        public ArgPipelineManager Manager { get; set; }

        /// <summary>
        /// Gets a reference to the next stage in the pipeline or null if there isn't one
        /// </summary>
        public PipelineStage NextStage { get; internal set; }

        /// <summary>
        /// Gets a read only collection of the command line args that were used to initialize this stage
        /// </summary>
        public ReadOnlyCollection<string> CmdLineArgs
        {
            get
            {
                return _cmdLineArgs.AsReadOnly();
            }
        }

        /// <summary>
        /// Gets the index of this stage in the pipeline
        /// </summary>
        public int StageIndex { get; internal set; }

        /// <summary>
        /// Gets a reference to the factory to use if the stage needs to create more instances of the base definition
        /// </summary>
        public ICommandLineArgumentsDefinitionFactory CommandLineDefinitionFactory { get; set; }

        /// <summary>
        /// Creates a new stage given a set of command line arguments
        /// </summary>
        /// <param name="args"></param>
        public PipelineStage(string[] args)
        {
            _cmdLineArgs = new List<string>(args);
            CommandLineDefinitionFactory = new ArgumentScaffoldTypeCommandLineDefinitionFactory();
        }

        /// <summary>
        /// Implementers should choose how to process objects that enter the stage.
        /// </summary>
        /// <param name="o"></param>
        public abstract void Accept(object o);

        /// <summary>
        /// Implementers should return true only after Drain() has been called and the stage has
        /// finished processing all objects that have been passed to Accept().  
        /// </summary>
        public abstract bool IsDrained { get; protected set; }

        /// <summary>
        /// You should not accept any more objects after this is called and PowerArgs should not try to pass you any objects after
        /// Drain is called.
        /// </summary>
        public abstract void Drain();

        /// <summary>
        /// Gets a string representation of the stage
        /// </summary>
        /// <returns>A string representation of the stage</returns>
        public override string ToString()
        {
            return StageIndex + " - " + string.Join(" ", this.CmdLineArgs.ToArray());
        }

        /// <summary>
        /// Call this to fire the Drained event
        /// </summary>
        protected void FireDrained()
        {
            if(Drained != null)
            {
                Drained();
            }
        }
    }
}
