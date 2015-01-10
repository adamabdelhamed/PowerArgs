using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;

namespace PowerArgs.Preview
{

    /// <summary>
    /// A class that manages a pipeline that lets pipeline stages comminicate with each other
    /// </summary>
    public class ArgPipelineManager
    {
        private List<PipelineStage> _stages;

        private Dictionary<PipelineStage, Queue<object>> serializedStageInput;

        /// <summary>
        /// Gets a read only collection of stages in this pipeline
        /// </summary>
        public ReadOnlyCollection<PipelineStage> Stages
        {
            get
            {
                return _stages.AsReadOnly();
            }
        }

        /// <summary>
        /// Returns true if the entire pipeline is drained, false otherwise
        /// </summary>
        public bool IsDrained
        {
            get
            {
                foreach (var stage in _stages)
                {
                    if (stage.IsDrained == false) return false;
                }
                return true;
            }
        }

        object asyncExceptionLock;
        List<Exception> asyncExceptions;

        public PipelineMode Mode { get; private set; }

        internal ArgPipelineManager(PipelineMode mode)
        {
            this.Mode = mode;
            _stages = new List<PipelineStage>();
            asyncExceptions = new List<Exception>();
            asyncExceptionLock = new object();

            if(Mode == PipelineMode.SerializedStages)
            {
                serializedStageInput = new Dictionary<PipelineStage, Queue<object>>();
            }
        }

        internal PipelineStage CreateNextStage(ArgHook.HookContext context, string[] commandLine, ICommandLineArgumentsDefinitionFactory factory)
        {
            PipelineStage next;
            CommandLineAction inProcAction;

            if (Stages.Count == 0)
            {
                next = new RootPipelineStage(commandLine);
            }
            else if (commandLine[0].StartsWith(ArgPipeline.PipelineStageActionIndicator) && ArgPipelineActionStage.TryCreateActionStage(commandLine, out next))
            {
                // do nothing, next is populated
            }
            else if (TryParseStageAction(context.Definition, commandLine[0], out inProcAction))
            {
                next = new InProcessPipelineStage(context.Definition, commandLine);
            }
            else if (ExternalPipelineProvider.TryLoadOutputStage(commandLine, out next) == false)
            {
                throw new UnexpectedArgException("The pipeline action '"+string.Join(" ", commandLine)+"' is not valid.  If you want to support piping between processes, learn how to here (TODO URL)");
            }

            next.CommandLineDefinitionFactory = factory;

            this.AddStage(next);
            return next;
        }

        internal void Push(object o, PipelineStage current)
        {
            if (Mode == PipelineMode.ParallelStages)
            {
                current.NextStage.Accept(o);
            }
            else if (Mode == PipelineMode.SerializedStages)
            {
                if (serializedStageInput.ContainsKey(current.NextStage) == false)
                {
                    serializedStageInput.Add(current.NextStage, new Queue<object>());
                }

                serializedStageInput[current.NextStage].Enqueue(o);
            }
            else
            {
                throw new NotSupportedException("Unknown mode: " + Mode);
            }
        }

        internal void Drain(bool block = true)
        {
            if(_stages.Count > 0)
            {
                _stages.First().Drain();
            }

            while (block && IsDrained == false)
            {
                Thread.Sleep(10);
            }
            if(block)
            {
                PowerLogger.LogLine("The pipeline is drained");
                lock (asyncExceptionLock)
                {
                    try
                    {
                        if (asyncExceptions.Count == 1)
                        {
                            throw asyncExceptions.First();
                        }
                        else if (asyncExceptions.Count > 1)
                        {
                            throw new AggregateException(asyncExceptions.ToArray());
                        }
                    }
                    finally
                    {
                        asyncExceptions.Clear();
                    }
                }
            }
        }

        /// <summary>
        /// When pipeline stages encounter an exception they should report them to this method so that they can be propagated to the original caller.  This is
        /// because you can't be sure your stage is running on the same thread as the original caller.
        /// </summary>
        /// <param name="ex">The exception to propagate</param>
        public void BubbleAsyncException(Exception ex)
        {
            lock(asyncExceptionLock)
            {
                asyncExceptions.Add(ex);
            }
        }

        internal void AddStage(PipelineStage toAdd)
        {
            if(_stages.Count > 0)
            {
                _stages.Last().NextStage = toAdd;
            }
            toAdd.Manager = this;
            toAdd.StageIndex = _stages.Count;

            toAdd.Drained += () =>
            {
                if(toAdd.NextStage != null)
                {
                    Queue<object> nextStageSerializedInput;
                    if(Mode == PipelineMode.SerializedStages && serializedStageInput.TryGetValue(toAdd.NextStage, out nextStageSerializedInput))
                    {
                        while(nextStageSerializedInput.Count > 0)
                        {
                            toAdd.NextStage.Accept(nextStageSerializedInput.Dequeue());
                        }
                    }

                    toAdd.NextStage.Drain();
                }
            };

            _stages.Add(toAdd);
        }

        private static bool TryParseStageAction(CommandLineArgumentsDefinition effectiveDefinition, string actionKey, out CommandLineAction action)
        {
            if (actionKey == null) throw new ArgException("Unexpected '|' at end of pipeline");
            action = effectiveDefinition.FindMatchingAction(actionKey);
            return action != null;
        }
    }
}
