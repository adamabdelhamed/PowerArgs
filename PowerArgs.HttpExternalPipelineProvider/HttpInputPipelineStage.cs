using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Dynamic;

namespace PowerArgs.Preview
{
    [ExternalInputPipelineStageProviderAttribute]
    public class HttpInputPipelineStage : ExternalPipelineInputStage
    {
        public override bool IsProgramLaunchedByExternalPipeline{get;protected set;}

        HttpPipelineMessageListener listener;
        InProcessPipelineStage wrappedStage;

        private object outputQueueLock = new object();
        private Queue<object> outputQueue = new Queue<object>();
        private Queue<Exception> exceptionQueue = new Queue<Exception>();
        private Queue<ConsoleCharacter> textOutputQueue = new Queue<ConsoleCharacter>();
        private Action<object> outputHandler;
        private ConsoleOutInterceptor interceptor;
        public HttpInputPipelineStage(CommandLineArgumentsDefinition baseDefinition, string[] rawCommandLine)
            : base(baseDefinition, CleanCommandLineOfInputPortInfo(rawCommandLine))
        {
            int port;

            if (TryFindPort(rawCommandLine, out port) == false)
            {
                IsProgramLaunchedByExternalPipeline = false;
                return;
            }
            else
            {
                IsProgramLaunchedByExternalPipeline = true;
            }

            Init.InitIfNotAlreadyDone();

            interceptor = ConsoleOutInterceptor.Instance;
            interceptor.Attach();
            PowerLogger.LogLine("Initializing input pipe for command line on port "+port+": "+string.Join(" ", this.CmdLineArgs));
            wrappedStage = new InProcessPipelineStage(baseDefinition, this.CmdLineArgs.ToArray());
            listener = new HttpPipelineMessageListener(port, TimeSpan.FromSeconds(10));

            listener.Timeout += () =>
            {
                PowerLogger.LogLine("HttpInputPipelineStage listener timed out.");
                Process.GetCurrentProcess().Kill();
            };

            wrappedStage.UnhandledException += (ex) =>
            {
                QueueException(ex);
            };

            listener.ListenException += (ex) =>
            {
                QueueException(ex);
            };

            listener.MessageReceivedHandler = (message) =>
            {
                if (message.ControlAction == null)
                {
                    LogLine("Pipe input received");
                    var pipedObject = DeserializePipedObject(message);
                    wrappedStage.CommandLineDefinitionFactory = this.CommandLineDefinitionFactory;
                    wrappedStage.Accept(pipedObject);
                    LogLine("Pipe input processed");
                    return new HttpPipelineControlResponse() { StatusCode = HttpStatusCode.Accepted  };

                }
                else if (message.ControlAction == "Drain")
                {
                    Drain();
                    return new HttpPipelineControlResponse() { StatusCode = HttpStatusCode.Accepted };
                }
                else if (message.ControlAction == "Poll")
                {
                    LogLine("Poll requested");
                    string pipedObjectArrayJson = null;
                    string exceptionInfo = null;
                    List<ConsoleCharacter> textOutput = null;
                    lock (outputQueueLock)
                    {
                        List<object> batch = new List<object>();
                        while (outputQueue.Count > 0)
                        {
                            batch.Add(outputQueue.Dequeue());
                        }

                        if (batch.Count > 0)
                        {
                            LogLine("Pipe output sent");
                            pipedObjectArrayJson = JsonConvert.SerializeObject(batch, HttpPipelineMessage.CommonSettings);
                        }

                        while(exceptionQueue.Count > 0)
                        {
                            LogLine("Exception output sent");
                            exceptionInfo = exceptionInfo ?? "";
                            exceptionInfo += exceptionQueue.Dequeue().ToString() + Environment.NewLine + Environment.NewLine;
                        }

                        var interceptedText = interceptor.ReadAndClear();

                        while(interceptedText.Count > 0)
                        {
                            textOutput = textOutput ?? new List<ConsoleCharacter>();
                            textOutput.Add(interceptedText.Dequeue());
                        }

                        while(textOutputQueue.Count > 0)
                        {
                            textOutput = textOutput ?? new List<ConsoleCharacter>();
                            textOutput.Add(textOutputQueue.Dequeue());
                        }
                    }
                    return new HttpPipelineControlResponse() { StatusCode = HttpStatusCode.OK, ExceptionInfo = exceptionInfo, ConsoleOutput = textOutput, PipedObjectArrayJson = pipedObjectArrayJson, Value = wrappedStage.IsDrained + "" };
                }
                else if (message.ControlAction == "Close")
                {
                    LogLine("Close requested");
                    ArgPipeline.ObjectExitedPipeline -= outputHandler;
                    return new HttpPipelineControlResponse() { StatusCode = HttpStatusCode.OK, Close = true };
                }
                else
                {
                    LogLine("Unrecognized action: " + message.ControlAction);
                    return new HttpPipelineControlResponse() { StatusCode = HttpStatusCode.BadRequest, ConsoleOutput = new ConsoleString("Unrecognized action: "+message.ControlAction).ToList() };
                }
            };

            outputHandler = (obj) =>
            {
                LogLine("Pipe output queued");
                lock(outputQueueLock)
                {
                    outputQueue.Enqueue(obj);
                }
            };

            ArgPipeline.ObjectExitedPipeline += outputHandler;

            listener.Start();
        }

        private bool TryFindPort(string[] args, out int port)
        {
            var nonRootPort = (from arg in args
                               where arg.StartsWith("$PowerArgs.ArgPipelineInputPort:")
                               select int.Parse(arg.Split(':')[1])).SingleOrDefault();

            port = nonRootPort;
            return port != default(int);
        }

        private static string[] CleanCommandLineOfInputPortInfo(string[] args)
        {
            return (from arg in args
                    where arg.StartsWith("$PowerArgs.ArgPipelineInputPort:") == false
                    select arg).ToArray();
        }

        private static object DeserializePipedObject(HttpPipelineMessage message)
        {
            object pipedObject;

            try
            {
                pipedObject = JsonConvert.DeserializeObject(message.PipedObjectJson, HttpPipelineMessage.CommonSettings);
            }
            catch(Exception ex)
            {
                pipedObject = JObject.Parse(message.PipedObjectJson);
            }
            return pipedObject;
        }

        private void QueueException(Exception ex)
        {
            lock (outputQueueLock)
            {
                LogLine("Queueing exception: " + ex.ToString());
                exceptionQueue.Enqueue(ex);
            }
        }

        private void LogLine(string s)
        {
            PowerLogger.LogLine(s);
        }

        public override void Accept(object o)
        {
            throw new NotSupportedException(typeof(HttpInputPipelineStage).Name+" only accepts objects over Http");
        }

        public override bool IsDrained
        {
            get
            {
                if(wrappedStage.IsDrained == false)return false;
                
                lock(outputQueueLock)
                {
                    if (outputQueue.Count > 0 || exceptionQueue.Count > 0 || textOutputQueue.Count > 0) return false;
                }
                interceptor.Detatch();
                if (listener.IsListening) return false;

                return true;
            }
            protected set
            {
                throw new NotImplementedException();
            }
        }

        public override void Drain()
        {
            LogLine("Drain requested");
            wrappedStage.Drain();
        }
    }
}
