using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PowerArgs.Preview
{
    /// <summary>
    /// A pipeline stage that can run in the current process.  This is the stage used if you are piping beween actions in the same program.
    /// </summary>
    public class InProcessPipelineStage : PipelineStage
    {
        /// <summary>
        /// Returns true if the stage is drained, false otherwise.
        /// </summary>
        public override bool IsDrained { get; protected set; }

        private Thread executionThread;
        private object queueLock;
        private Queue<object> inputQueue;
        private bool drainRequested;
        private CommandLineArgumentsDefinition baseDefinition;
        
        /// <summary>
        /// An event that gets fired if there's an unhandled exception and there is no pipeline manager attached to this stage.
        /// If there are no handlers attached to this event and there is no pipeline manager attached then exceptions will bubble to the top of the
        /// stage's processing therad, and bad things will happen.
        /// </summary>
        public event Action<Exception> UnhandledException;

        /// <summary>
        /// Creates a new in process stage given a base definition and a command line
        /// </summary>
        /// <param name="baseDefinition">The definition that describes actions that can be invoked using the given command line</param>
        /// <param name="commandLine">The command line that represents the structure of this stage</param>
        public InProcessPipelineStage(CommandLineArgumentsDefinition baseDefinition, string [] commandLine) : base(commandLine)
        {
            this.baseDefinition = baseDefinition;
            queueLock = new object();
            inputQueue = new Queue<object>();
        }

        /// <summary>
        /// Creates a new in process stage given a command line.
        /// </summary>
        /// <param name="commandLine">The command line that represents the structure of this stage</param>
        public InProcessPipelineStage(string[] commandLine) : this(null, commandLine) { }

        /// <summary>
        /// The object is queued up and will be processed by the stage's execution thread.  If this is the first object
        /// being passed to the stage then the execution thread will be started.
        /// </summary>
        /// <param name="o">The object to accept</param>
        public override void Accept(object o)
        {
            lock (queueLock)
            {
                if (drainRequested)
                {
                    throw new InvalidOperationException("STAGE "+StageIndex+" - This stage cannot accept objects since a drain request has been submitted.");
                }

                inputQueue.Enqueue(o);

                if (executionThread == null)
                {
                    executionThread = new Thread(InProcessStageImpl);
                    executionThread.Start();
                }
            }
        }

        /// <summary>
        /// asyncronously starts draining this stage.  It returns immediately, but you should not assume that
        /// IsDrained will return true immediately.  Once this method is called this stage will no longer accept
        /// objects (it will throw).  Once all queued objects are processed IsDrained will return true.
        /// </summary>
        public override void Drain()
        {
            lock (queueLock)
            {
                if (drainRequested)
                {
                    throw new InvalidOperationException("This stage has already had a drain requested.");
                }
                drainRequested = true;

                if ((executionThread == null || executionThread.IsAlive == false) && inputQueue.Count == 0)
                {
                    SetDrainedToTrue();
                }
            }
        }

        private void SetDrainedToTrue()
        {
            try
            {
                BeforeSetDrainedToTrue();
            }
            catch(Exception ex)
            {
                if(Manager != null)
                {
                    Manager.BubbleAsyncException(ex);
                }
                else
                {
                    throw;
                }
            }
            IsDrained = true;
            FireDrained();
        }

        /// <summary>
        /// When overridden by a derived class this method will be called just after we've processed the last queued object (drain has been requested) and just before
        /// IsDrained is set to true.  This is useful if you're implementing a stage action that needs to have seen all the inputs before outputting it's results (e.g. $count).  This is
        /// your last opportunity for your stage to write output to the next stage.
        /// </summary>
        protected virtual void BeforeSetDrainedToTrue()
        {

        }

        /// <summary>
        /// gets a string representation fo this stage
        /// </summary>
        /// <returns>a string representation fo this stage</returns>
        public override string ToString()
        {
            return StageIndex + " - " + string.Join(" ", this.CmdLineArgs.ToArray());
        }

        private void InProcessStageImpl()
        {
            try
            {
                PipelineStage.Current = this;
                object lastObjectProcessed;
                bool wasDrainRequestedAtTimeOfDequeue = false;
                do
                {
                    lastObjectProcessed = null;
                    lock (queueLock)
                    {
                        wasDrainRequestedAtTimeOfDequeue = drainRequested;
                        if (inputQueue.Count > 0)
                        {
                            lastObjectProcessed = inputQueue.Dequeue();
                        }
                    }

                    if (lastObjectProcessed != null)
                    {
                        ProcessPipedObject(lastObjectProcessed);
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }
                while (wasDrainRequestedAtTimeOfDequeue == false || lastObjectProcessed != null);

                SetDrainedToTrue();
            }
            catch (Exception ex)
            {
                if (Manager != null)
                {
                    Manager.BubbleAsyncException(ex);
                }
                else if (UnhandledException != null)
                {
                    UnhandledException(ex);
                }
                else
                {
                    throw;
                }
            }
        }

        private void ProcessPipedObject(object o)
        {
            try
            {
                OnObjectReceived(o);
            }
            catch (Exception ex)
            {
                if(Manager != null)
                {
                    Manager.BubbleAsyncException(ex);
                }
                else if(UnhandledException != null)
                {
                    UnhandledException(ex);
                }
                else 
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// The default implementation is to use the given definition factory to new up a definition and invoke an action using the given command line.  You can
        /// override this if you want to process the given pipeline object in a different way.  You will be called on this stage's processing thread and you should let exceptions
        /// flow through since they'll be bubbled up to the caller directly.
        /// </summary>
        /// <param name="o">An object recceived on the pipeline</param>
        protected virtual void OnObjectReceived(object o)
        {
            var definitionToUse = baseDefinition != null ? CommandLineDefinitionFactory.MakeDefinition(baseDefinition) : CommandLineDefinitionFactory.MakeDefinition();
            var childCommandLine = MapObject(o, definitionToUse);
            PowerLogger.LogLine("STAGE - " + StageIndex + " - Executing piped command: " + string.Join(" ", childCommandLine));

            if (definitionToUse.Actions.Count > 0)
            {
                Args.InvokeAction(definitionToUse, childCommandLine);
            }
            else
            {
                Args.InvokeMain(definitionToUse, childCommandLine);
            }
        }

        private string[] MapObject(object o, CommandLineArgumentsDefinition effectiveDefinition)
        {
            List<string> newCommandLine = new List<string>();
            newCommandLine.AddRange(CmdLineArgs);

            var preParseResult = ArgParser.Parse(effectiveDefinition, newCommandLine.ToArray());

            var predictedAction = effectiveDefinition.FindMatchingAction(this.CmdLineArgs[0]);

            if (predictedAction == null)
            {
                PowerLogger.LogLine("Could not determine action: "+this.CmdLineArgs[0]+" - Here are the supported action:");
                foreach(var action in effectiveDefinition.Actions)
                {
                    PowerLogger.LogLine("  "+action.DefaultAlias);
                }
                throw new ArgException("TODO - Could not determine action: "+this.CmdLineArgs[0]);
            }

            PowerLogger.LogLine("Predicted action is " + predictedAction.DefaultAlias);


            var argsToInspectForDirectMappingTarget = predictedAction.Arguments.Union(effectiveDefinition.Arguments).ToList();
            var directMappingTarget = (from argument in argsToInspectForDirectMappingTarget
                                       where argument.Metadata.HasMeta<ArgPipelineTarget>()
                                       select argument).SingleOrDefault();

            if (directMappingTarget != null)
            {
                var revivedValue = o;
                if (IsCompatible(o, directMappingTarget) == false)
                {
                    PowerLogger.LogLine("Need to map "+o.GetType().FullName+" to "+directMappingTarget.ArgumentType.FullName);
                    
                    if(TrySimpleConvert(o, directMappingTarget.ArgumentType, out revivedValue))
                    {
                        // we're good
                    }
                    else if (ArgPipelineObjectMapper.CurrentMapper == null)
                    {
                        throw new InvalidArgDefinitionException("Unable to attempt tp map type " + o.GetType().FullName + " to " + directMappingTarget.ArgumentType.FullName + " because no mapper is registered at ArgPipelineObjectMapperProvider.CurrentMapper");
                    }
                    else
                    {
                        revivedValue = ArgPipelineObjectMapper.CurrentMapper.MapIncompatibleDirectTargets(directMappingTarget.ArgumentType, o);
                    }
                }

                directMappingTarget.RevivedValueOverride = revivedValue;
            }
            else
            {
                PowerLogger.LogLine("Attempting to shred object: " + o.ToString());
                foreach (var argument in predictedAction.Arguments.Union(effectiveDefinition.Arguments))
                {
                    bool manualOverride = false;

                    foreach (var explicitKey in preParseResult.ExplicitParameters.Keys)
                    {
                        if (argument.IsMatch(explicitKey))
                        {
                            manualOverride = true;
                            break;
                        }
                    }

                    if (preParseResult.ImplicitParameters.ContainsKey(argument.Position))
                    {
                        manualOverride = true;
                    }

                    if (manualOverride) continue;

                    var mapper = argument.Metadata.Meta<ArgPipelineExtractor>() ?? new ArgPipelineExtractor();
                    string mappedKey, mappedValue;
                    if (mapper.TryExtractObjectPropertyIntoCommandLineArgument(o, argument, out mappedKey, out mappedValue))
                    {
                        newCommandLine.Add(mappedKey);
                        newCommandLine.Add(mappedValue);
                    }
                }
            }

            return newCommandLine.ToArray();
        }

        private static bool IsCompatible(object o, CommandLineArgument directMappingTarget)
        {
            var oType = o.GetType();

            if (oType == directMappingTarget.ArgumentType) return true;
            if (oType.GetInterfaces().Contains(directMappingTarget.ArgumentType)) return true;
            if (oType.IsSubclassOf(directMappingTarget.ArgumentType)) return true;

            return false;
        }

        private bool TrySimpleConvert(object o, Type target, out object result)
        {
            if (o is string && ArgRevivers.CanRevive(target))
            {
                result = ArgRevivers.Revive(target, null, (string)o);
                return true;
            }
            else
            {
                try
                {
                    result = Convert.ChangeType(o, target);
                    return true;
                }
                catch (Exception ex)
                {
                    result = null;
                    return false;
                }
            }
        }
    }
}
