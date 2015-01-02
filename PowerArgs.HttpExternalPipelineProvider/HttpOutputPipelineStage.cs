using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using PowerArgs;
namespace PowerArgs.Preview
{
    [ExternalOutputPipelineStageProviderAttribute]
    public class HttpOutputPipelineStage : PipelineStage
    {
        private bool isDrained;
        public HttpPipelineMessageSender Sender { get; private set; }

        private string exe;
        private List<string> commandLineParameters;

        private object processLock;
        private Process externalProcess;

        static int nextPort = 5000;
        public HttpOutputPipelineStage(string[] commandLine)
            : base(commandLine)
        {
            Init.InitIfNotAlreadyDone();
            int port = nextPort++;
            this.processLock = new object();
            this.exe = commandLine[0];
            this.commandLineParameters = new List<string>(commandLine.Skip(1));
            this.commandLineParameters.Add("$PowerArgs.ArgPipelineInputPort:" + port);
            Sender = new HttpPipelineMessageSender(port);

            lock (processLock)
            {
                if (externalProcess == null)
                {
                    externalProcess = new Process();
                    externalProcess.StartInfo = new ProcessStartInfo(exe, string.Join(" ", commandLineParameters.Select(p => '"' + p + '"')));
                    externalProcess.StartInfo.UseShellExecute = false;
                    externalProcess.StartInfo.CreateNoWindow = true;

                    try
                    {
                        externalProcess.Start();
                    }
                    catch(Exception ex)
                    {
                        throw new ArgException("Failed to start external process: " + exe, ex);
                    }
                }
            }
        }

        public override void Accept(object o)
        {
            PowerLogger.LogLine("Sending object over the wire: " + o);
            HttpPipelineControlResponse response = null;
            try
            {
                response = Sender.SendObject(o);
            }
            catch(WebException ex)
            {
                if(ex.Status == WebExceptionStatus.ConnectFailure)
                {
                    try
                    {
                        throw new ArgException("Could not connect to external program '" + exe + "', possibly because it was not built with PowerArgs, or it does not have remote pipelining enabled.  TODO - Help URL here.", ex);
                    }
                    catch (Exception toBubble)
                    {
                        this.Manager.BubbleAsyncException(toBubble);
                    }
                }
                else
                {
                    this.Manager.BubbleAsyncException(ex);
                }
            }
            catch(Exception ex)
            {
                this.Manager.BubbleAsyncException(ex);
            }

            if(response == null)
            {
                return;
            }

            if(response.StatusCode != HttpStatusCode.Accepted)
            {
                this.Manager.BubbleAsyncException(new ArgException("The external program '"+exe+"' could not accept the object of type: " + o.GetType().FullName + "\n\n" + response.ExceptionInfo));
            }
        }

        public override bool IsDrained
        {
            get
            {
                if (isDrained == false)
                {
                    try
                    {
                        var response = Sender.SendControlAction("Poll");

                        if (response.ExceptionInfo != null)
                        {
                            var msg = "Remote stage provided exception info: \n\n" + response.ExceptionInfo;
                            Console.WriteLine(msg);
                            this.Manager.BubbleAsyncException(new IOException(msg));
                        }

                        if (response.ConsoleOutput != null && response.ConsoleOutput.Count > 0)
                        {
                            new ConsoleString(response.ConsoleOutput).WriteLine();
                        }

                        if (response.PipedObjectArrayJson != null)
                        {
                            PowerLogger.LogLine("Received piped response from remote stage: " + response.PipedObjectArrayJson);
                            var objects = JsonConvert.DeserializeObject<List<object>>(response.PipedObjectArrayJson);
                            foreach (var obj in objects)
                            {
                                ArgPipeline.Push(obj, this);
                            }
                        }

                        isDrained = response != null && response.Value.ToLower() == "true";
                        if (isDrained)
                        {
                            FireDrained();
                            Sender.SendControlAction("Close");
                            externalProcess.WaitForExit(1000 * 10); // wait for at most 10 seconds
                            try { externalProcess.Dispose(); }
                            catch (Exception) { }
                        }
                    }
                    catch(WebException ex)
                    {
                        if (ex.Status == WebExceptionStatus.ConnectFailure)
                        {
                            try
                            {
                                throw new ArgException("Could not connect to external program '" + exe + "', possibly because it was not built with PowerArgs, or it does not have remote pipelining enabled.  TODO - Help URL here.", ex);
                            }
                            catch (Exception toBubble)
                            {
                                this.Manager.BubbleAsyncException(toBubble);
                            }
                        }
                        else
                        {
                            this.Manager.BubbleAsyncException(ex);
                        }

                        isDrained = true;
                        FireDrained();
                    }
                }
                return isDrained;
            }
            protected set
            {
                throw new NotImplementedException();
            }
        }

        public override void Drain()
        {
            try
            {
                var drainResponse = Sender.SendControlAction("Drain");
                if (drainResponse.StatusCode != HttpStatusCode.Accepted)
                {
                    throw new IOException("Failed to send Drain request over port " + Sender.Port + ".  Expected '202', got '" + (int)drainResponse.StatusCode);
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ConnectFailure)
                {
                    try
                    {
                        throw new ArgException("Could not connect to external program '" + exe + "', possibly because it was not built with PowerArgs, or it does not have remote pipelining enabled.  TODO - Help URL here.", ex);
                    }
                    catch (Exception toBubble)
                    {
                        this.Manager.BubbleAsyncException(toBubble);
                    }
                }
                else
                {
                    this.Manager.BubbleAsyncException(ex);
                }
            }
            catch (Exception ex)
            {
                this.Manager.BubbleAsyncException(ex);
            }
        }
    }
}
