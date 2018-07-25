using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace PowerArgs.Games
{
 

    public class MultiPlayerClient : Lifetime
    {
        private class PendingRequest
        {
            public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(2);
            public string Id { get; set; }
            private Stopwatch timer;
            public Deferred<MultiPlayerMessage> ResponseDeferred { get; set; }

            public PendingRequest()
            {
                timer = new Stopwatch();
                timer.Start();
            }

            public void Complete(MultiPlayerMessage response)
            {
                timer.Stop();
                ResponseDeferred.Resolve(response);
            }

            public void Fail(Exception error)
            {
                timer.Stop();
                ResponseDeferred.Reject(error);
            }

            public bool IsTimedOut()
            {
                if(timer.Elapsed >= Timeout)
                {
                    timer.Stop();
                    ResponseDeferred.Reject(new TimeoutException());
                    return true;
                }
                return false;
            }
        }

        public MultiPlayerMessageRouter EventRouter { get; private set; } = new MultiPlayerMessageRouter();
        public string ClientId => clientNetworkProvider.ClientId;

        private Dictionary<string, PendingRequest> pendingRequests = new Dictionary<string, PendingRequest>();

        private Timer timeoutChecker;

        public Promise Connect(string server)
        {
            var ret = clientNetworkProvider.Connect(server);
            ret.Then(() =>
            {
                isConnected = true;
                timeoutChecker?.Dispose();
                timeoutChecker = new Timer((o)=> EvaluateTimeouts(), null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));
                this.OnDisposed(timeoutChecker.Dispose);
            });
            return ret;
        }

        public void SendMessage(MultiPlayerMessage message)
        {
            message.Sender = ClientId;
            clientNetworkProvider.SendMessage(message.Serialize());
        }

      
        public Promise<MultiPlayerMessage> SendRequest(MultiPlayerMessage message, TimeSpan? timeout = null)  
        {
            message.Sender = ClientId;
            message.RequestId = Guid.NewGuid().ToString();
            var pendingRequest = new PendingRequest()
            {
                Id = message.RequestId,
                ResponseDeferred = Deferred<MultiPlayerMessage>.Create(),
            };

            if(timeout.HasValue)
            {
                pendingRequest.Timeout = timeout.Value;
            }
            lock (pendingRequests)
            {
                pendingRequests.Add(message.RequestId, pendingRequest);
            }
            SendMessage(message);
            return pendingRequest.ResponseDeferred.Promise;
        }

        private IClientNetworkProvider clientNetworkProvider;
        private bool isConnected;
        public MultiPlayerClient(IClientNetworkProvider networkProvider)
        {
            this.clientNetworkProvider = networkProvider;
            networkProvider.MessageReceived.SubscribeForLifetime((m) =>
            {
                var hydratedEvent = MultiPlayerMessage.Deserialize(m);
                EventRouter.Route(hydratedEvent.GetType().Name, hydratedEvent);
            }, this);
            this.OnDisposed(() =>
            {
                if (isConnected)
                {
                    SendMessage(new LeftMessage());
                }
                this.clientNetworkProvider.Dispose();
            });

            EventRouter.Register<Ack>(OnAck, this);
        }

        private void EvaluateTimeouts()
        {
            lock(pendingRequests)
            {
                foreach(var key in pendingRequests.Keys.ToList())
                {
                    if(pendingRequests[key].IsTimedOut())
                    {
                        pendingRequests.Remove(key);
                    }
                }
            }
        }

        private void OnAck(Ack message)
        {
            var requestId = message.RequestId;
            lock (pendingRequests)
            {
                if (pendingRequests.TryGetValue(requestId, out PendingRequest pendingRequest))
                {
                    if (message.Error != null)
                    {
                        pendingRequest.Fail(new IOException(message.Error));
                        pendingRequests.Remove(requestId);
                    }
                    else
                    {
                        pendingRequest.Complete(message);
                        pendingRequests.Remove(requestId);
                    }
                }
                else
                {
                    // it probably timed out so we don't have it anymore
                }
            }
        }
    }
}
