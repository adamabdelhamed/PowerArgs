using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace PowerArgs.Preview
{
    public class HttpPipelineMessageListener
    {
        HttpListener listener;

        public Func<HttpPipelineMessage, HttpPipelineControlResponse> MessageReceivedHandler { get; set; }
        public event Action Stopped;
        public event Action Timeout;
        public event Action<Exception> ListenException;

        private Thread idleTimeoutCheckerThread;

        private TimeSpan idleTimeout;
        private DateTime lastRequestReceivedTime;

        public bool IsListening
        {
            get
            {
                return listener.IsListening;
            }
        }



        public HttpPipelineMessageListener(int port, TimeSpan idleTimeout)
        {
            this.idleTimeout = idleTimeout;
            listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:" + port + "/");
            idleTimeoutCheckerThread = new Thread(IdleTimeoutCheckerImpl);
        }

        private void IdleTimeoutCheckerImpl()
        {
            while(listener.IsListening)
            {
                if(DateTime.Now - lastRequestReceivedTime > idleTimeout)
                {
                    Stop();
                    if (Timeout != null) Timeout();
                    break;
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }

        public void Start()
        {
            lastRequestReceivedTime = DateTime.Now;
            listener.Start();
            idleTimeoutCheckerThread.Start();
            new Task(ProcessLoopImpl).Start();
        }

        public void Stop()
        {
            if (listener.IsListening == false) return;

            listener.Stop();
            PowerLogger.LogLine("Listener closed");
            if (Stopped != null)
            {
                Stopped();
            }
        }

        private void ProcessLoopImpl()
        {
            while (listener.IsListening)
            {
                try
                {
                    var task = GetContextAsync();
                    try
                    {
                        task.Wait();
                    }
                    catch(Exception)
                    {
                        if (listener.IsListening) throw;
                        continue;
                    }

                    lastRequestReceivedTime = DateTime.Now;
                    PowerLogger.LogLine("Listener accepted a request");
                    new Task(() => Dispatch(task.Result)).Start();
                }
                catch (Exception ex)
                {
                    if (ListenException != null)
                    {
                        ListenException(ex);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        private void Dispatch(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                using (var reader = new StreamReader(request.InputStream))
                {
                    var body = reader.ReadToEnd();
                    var message = JsonConvert.DeserializeObject<HttpPipelineMessage>(body);

                    HttpPipelineControlResponse responseMessage = new HttpPipelineControlResponse();

                    if (MessageReceivedHandler != null)
                    {
                        try
                        {
                            responseMessage = MessageReceivedHandler(message);
                        }
                        catch(Exception ex)
                        {
                            PowerLogger.LogLine("Receive handler threw an exception");
                            responseMessage.ExceptionInfo = ex.ToString();
                            responseMessage.StatusCode = HttpStatusCode.BadRequest;
                        }
                    }

                    var resposneMessageContents = JsonConvert.SerializeObject(responseMessage, HttpPipelineMessage.CommonSettings);
                    using (var writer = new StreamWriter(context.Response.OutputStream))
                    {
                        writer.Write(resposneMessageContents);
                    }

                    if (responseMessage.Close)
                    {
                        PowerLogger.LogLine("Closing listener because a response message told us to");
                        Stop();
                    }
                }
            }
            catch (Exception ex)
            {
                if (ListenException != null)
                {
                    ListenException(ex);
                }
                else
                {
                    throw;
                }
            }
        }

        private Task<HttpListenerContext> GetContextAsync()
        {
            return Task.Factory.FromAsync<HttpListenerContext>(listener.BeginGetContext, listener.EndGetContext, null);
        }
    }
}
