using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PowerArgs.Preview
{
    /*
     * TODOs before publishing
     * 
     * P0 - Interprocess piping - error cases, particularly around disconnected pipes
     * P1 - Implement $help to get usage info for built in commands.  Or $webhelp to get browser help.
     * P1 - Tab completion to support contextual pipeline info
     * 
     */

    public enum PipelineMode
    {
        SerializedStages,
        ParallelStages,
    }

    /// <summary>
    /// A hook that enables the ArgPipeline capabilities.
    /// </summary>
    public class ArgPipeline : ArgHook
    {
        /// <summary>
        /// Indicates that the commands surrounding the indicator should be connected in a pipeline
        /// </summary>
        public const string PowerArgsPipeIndicator = "=>";
        /// <summary>
        /// The standard prefix for a cross cutting, pipeline stage action (e.g. $filter)
        /// </summary>
        public const string PipelineStageActionIndicator = "$";

        /// <summary>
        /// This event is fired when the last stage in a pipeline pushes out an object.  The parameter is the object that was
        /// pushed through the pipeline.
        /// </summary>
        public static event Action<object> ObjectExitedPipeline;
        
        private Type _commandLineDefinitionFactoryType;

        /// <summary>
        /// Gets or sets a Type that must implement ICommandLineArgumentsDefinitionFactory and must provide a default constructor.
        /// The resulting factory will be used to create new definitions when dynamically invoking pipeline stages.  The default factory 
        /// can support and command line definition that was created from a .NET type.  You should only have to use this in very advanced scenarios.
        /// </summary>
        public Type CommandLineDefinitionFactoryType
        {
            get
            {
                return _commandLineDefinitionFactoryType;
            }
            set
            {
                if(value.GetInterfaces().Contains(typeof(ICommandLineArgumentsDefinitionFactory)) == false)
                {
                    throw new InvalidArgDefinitionException("The type: "+value.FullName+" does not implement " + typeof(ICommandLineArgumentsDefinitionFactory).FullName);
                }

                try
                {
                    this.commandLineDeinitionFactory = (ICommandLineArgumentsDefinitionFactory)Activator.CreateInstance(value);
                }
                catch(InvalidArgDefinitionException ex)
                {
                    throw new InvalidArgDefinitionException("Unable to initialize type: " + value.FullName, ex);
                }

                _commandLineDefinitionFactoryType = value;
            }
        }

        public PipelineMode Mode { get; set; }

        private ICommandLineArgumentsDefinitionFactory commandLineDeinitionFactory;

        static ArgPipeline()
        {
            
        }

        /// <summary>
        /// Creates a new ArgPipeline instance
        /// </summary>
        public ArgPipeline()
        {
            // We want to be the last thing that executes before the parser runs
            this.BeforeParsePriority = 0;
            CommandLineDefinitionFactoryType = typeof(ArgumentScaffoldTypeCommandLineDefinitionFactory);
            this.Mode = PipelineMode.SerializedStages;
        }

        /// <summary>
        /// This is the main hook point where the pipeline features get injected.  This method is responsible for creating the pipeline if needed.  It is also
        /// responsible for connecting to an external program if this program was launched by another program's pipeline manager.  It is also responsible for
        /// supporting any pipeline stage actions (e.g. $filter) that are not supported if the [ArgPipeline] metadata is omitted from the root definition.
        /// </summary>
        /// <param name="context">The processing context</param>
        public override void BeforeParse(ArgHook.HookContext context)
        {
            ExternalPipelineInputStage externalStage;
            if(ExternalPipelineProvider.TryLoadInputStage(context.Definition, context.CmdLineArgs, out externalStage) && externalStage.IsProgramLaunchedByExternalPipeline)
            {
                externalStage.CommandLineDefinitionFactory = this.commandLineDeinitionFactory;
                while (externalStage.IsDrained == false)
                {
                    Thread.Sleep(10);
                }
                PowerLogger.LogLine("Input stage drained");
                context.CancelAllProcessing();
                return;
            }
            else if(context.CmdLineArgs.Contains(PowerArgsPipeIndicator))
            {
                ValidatePipeline(context);
                List<List<string>> stageCommandLines = new List<List<string>>();
                stageCommandLines.Add(new List<string>());

                for(int i = 0; i < context.CmdLineArgs.Length; i++)
                {
                    var arg = context.CmdLineArgs[i];
                    if(arg == PowerArgsPipeIndicator)
                    {
                        stageCommandLines.Add(new List<string>());
                    }
                    else
                    {
                        stageCommandLines.Last().Add(arg);
                    }
                }

                context.CmdLineArgs = stageCommandLines.First().ToArray();

                var manager = GetPipelineManagerFromContext(context);

                for (int i = 0; i < stageCommandLines.Count; i++)
                {
                    var args = stageCommandLines[i];
                    if(args.Count == 0)
                    {
                        throw new ArgException("Missing action after pipeline indicator: "+PowerArgsPipeIndicator);
                    }
                    manager.CreateNextStage(context, args.ToArray(), this.commandLineDeinitionFactory);
                }

                context.CmdLineArgs = manager.Stages[0].CmdLineArgs.ToArray();
            }
            else
            {
                // do nothing
            }
        }

        private void ValidatePipeline(HookContext context)
        {
            foreach(var action in context.Definition.Actions)
            {
                bool hasMapper = false;
                bool hasDirectTarget = false;
                foreach(var argument in action.Arguments)
                {
                    hasMapper = hasMapper ? true : argument.Metadata.HasMeta<ArgPipelineExtractor>();

                    if (hasDirectTarget && argument.Metadata.HasMeta<ArgPipelineTarget>())
                    {
                        throw new InvalidArgDefinitionException("Action '" + action.DefaultAlias + "' has more than one argument with ArgPipelineTarget metadata.  Only one argument can be designated as a direct pipeline target.");
                    }
                    else
                    {
                        hasDirectTarget = argument.Metadata.HasMeta<ArgPipelineTarget>();
                    }

                    if(hasMapper && hasDirectTarget)
                    {
                        throw new InvalidArgDefinitionException("Action '"+action.DefaultAlias+"' cannot have both ArgPipelineMapper and ArgPipelineTarget metadata.  You must choose one or the other.");
                    }

                    if(hasDirectTarget && argument.Metadata.Meta<ArgPipelineTarget>().PipelineOnly == false && ArgRevivers.CanRevive(argument.ArgumentType) == false)
                    {
                        throw new InvalidArgDefinitionException("There is no reviver for type " + argument.ArgumentType.Name);
                    }
                }
            }
        }

        /// <summary>
        /// If the given context contains the pipeline manager then this method will make sure the 
        /// pipeline is drained before returning.
        /// </summary>
        /// <param name="context">The processing context</param>
        public override void AfterInvoke(ArgHook.HookContext context)
        {
            context.Definition.Clean();
            if (ContextHasPipelineManager(context) == false)
            {
                return;
            }
            var manager = GetPipelineManagerFromContext(context);
            manager.Drain();
        }

        /// <summary>
        /// Use this method to push an object to the next stage in the current pipeline.  If there is no
        /// pipeline currently setup or there is no next stage then the object exits the pipeline and can be
        /// processed via the ArgPipeline.ObjectExitedPipeline event.
        /// </summary>
        /// <param name="o">The object to push through the pipeline</param>
        public static void Push(object o)
        {
            Push(o, PipelineStage.Current);
        }

        /// <summary>
        /// Use this method to push an object to the given pipeline stage's next stage.  You should only use this
        /// for advanced scenarios where you're processing objects on a thread that was not created for you by
        /// PowerArgs.  If you're doing that and find the need to use this method, cool :).
        /// </summary>
        /// <param name="o">The object to push</param>
        /// <param name="current">The current pipeline stage.  The object is pushed to the next stage.</param>
        public static void Push(object o, PipelineStage current)
        {
            if (o == null)
            {
                if (ConsoleOutInterceptor.Instance.IsInitialized) return;
                ConsoleString.WriteLine("null object pushed through the pipeline", ConsoleColor.Yellow);
            }
            else if ( current != null && current.NextStage != null && current.Manager != null)
            {
                current.Manager.Push(o, current);
            }
            else
            {
                ArgPipeline.FireObjectExited(o);
                if (ConsoleOutInterceptor.Instance.IsInitialized) return;
                PipelineOutputFormatter.Format(o).WriteLine();
            }
        }

        private static void FireObjectExited(object o)
        {
            if (ObjectExitedPipeline != null)
            {
                ObjectExitedPipeline(o);
            }
        }

        private static bool ContextHasPipelineManager(ArgHook.HookContext context)
        {
            return context.HasProperty("ArgPipelineManager");
        }

        private ArgPipelineManager GetPipelineManagerFromContext(ArgHook.HookContext context)
        {
            if (ContextHasPipelineManager(context) == false)
            {
                context.SetProperty("ArgPipelineManager", new ArgPipelineManager(Mode));
            }

            return context.GetProperty<ArgPipelineManager>("ArgPipelineManager");
        }
    }
}
