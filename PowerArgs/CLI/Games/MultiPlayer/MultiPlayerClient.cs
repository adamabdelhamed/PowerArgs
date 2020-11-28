using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PowerArgs.Games
{

    public class ServerInfo
    {
        public string Server { get; set; }
        public int Port { get; set; }
    }

    public class MultiPlayerClient : Lifetime
    {
        private class PendingRequest
        {
            public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(2);
            public string Id { get; set; }
            private Stopwatch timer;
            public TaskCompletionSource<MultiPlayerMessage> ResponseDeferred { get; set; }

            public PendingRequest()
            {
                timer = new Stopwatch();
                timer.Start();
            }

            public void Complete(MultiPlayerMessage response)
            {
                timer.Stop();
                ResponseDeferred.SetResult(response);
            }

            public void Fail(Exception error)
            {
                timer.Stop();
                ResponseDeferred.SetException(error);
            }

            public bool IsTimedOut()
            {
                if(timer.Elapsed >= Timeout)
                {
                    timer.Stop();
                    ResponseDeferred.SetException(new TimeoutException());
                    return true;
                }
                return false;
            }
        }

        public MultiPlayerMessageRouter EventRouter { get; private set; } = new MultiPlayerMessageRouter();
        public string ClientId => clientNetworkProvider.ClientId;

        public Event<Exception> Disconnected => clientNetworkProvider.Disconnected;

        private Dictionary<string, PendingRequest> pendingRequests = new Dictionary<string, PendingRequest>();

        private Timer timeoutChecker;

      
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
                    TrySendMessage(new LeftMessage() { ClientWhoLeft = this.ClientId });
                }
                this.clientNetworkProvider.Dispose();
            });

            EventRouter.Register<Ack>(OnAck, this);
        }

        public async Task Connect(ServerInfo server)
        {
            await clientNetworkProvider.Connect(server);

            isConnected = true;
            timeoutChecker?.Dispose();
            timeoutChecker = new Timer((o) => EvaluateTimeouts(), null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));
            this.OnDisposed(timeoutChecker.Dispose);
        }

        public void SendMessage(MultiPlayerMessage message)
        {
            message.Sender = ClientId;
            clientNetworkProvider.SendMessage(message.Serialize());
        }

        public bool TrySendMessage(MultiPlayerMessage message)
        {
            try
            {
                message.Sender = ClientId;
                clientNetworkProvider.SendMessage(message.Serialize());
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public Task<MultiPlayerMessage> SendRequest(MultiPlayerMessage message, TimeSpan? timeout = null)
        {
            try
            {
                message.Sender = ClientId;
                message.RequestId = Guid.NewGuid().ToString();
                var pendingRequest = new PendingRequest()
                {
                    Id = message.RequestId,
                    ResponseDeferred = new TaskCompletionSource<MultiPlayerMessage>(),
                };

                if (timeout.HasValue)
                {
                    pendingRequest.Timeout = timeout.Value;
                }
                lock (pendingRequests)
                {
                    pendingRequests.Add(message.RequestId, pendingRequest);
                }
                SendMessage(message);
                return pendingRequest.ResponseDeferred.Task;
            }
            catch (Exception ex)
            {
                var d = new TaskCompletionSource<MultiPlayerMessage>();
                d.SetException(ex);
                return d.Task;
            }
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
