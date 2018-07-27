using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace PowerArgs.Games
{
    public class MultiPlayerClientConnection : Lifetime
    {
        public string ClientId { get; set; }
        public string DisplayName { get; set; }
    }

    public class UndeliverableEvent
    {
        public MultiPlayerMessage Message { get; set; }
        public Exception Exception { get; set; }
    }

    /// <summary>
    /// Manages communication between clients
    /// </summary>
    public class MultiPlayerServer : Lifetime
    {
        public Event<string> Info { get; private set; } = new Event<string>();
        public Event<string> Warning { get; private set; } = new Event<string>();
        public Event<string> Error { get; private set; } = new Event<string>();
        public Event<UndeliverableEvent> Undeliverable { get; private set; } = new Event<UndeliverableEvent>();
        public string ServerId => serverNetworkProvider.ServerId;
        public ObservableCollection<MultiPlayerClientConnection> Connections { get; private set; } = new ObservableCollection<MultiPlayerClientConnection>();
        public MultiPlayerMessageRouter MessageRouter { get; private set; } = new MultiPlayerMessageRouter();

        private object connectionsLock = new object();
        private IServerNetworkProvider serverNetworkProvider;

        public MultiPlayerServer(IServerNetworkProvider networkProvider)
        {
            this.serverNetworkProvider = networkProvider;
            this.OnDisposed(this.serverNetworkProvider.Dispose);
            this.serverNetworkProvider.ClientConnected.SubscribeForLifetime(OnClientConnected, this);
            this.serverNetworkProvider.MessageReceived.SubscribeForLifetime(OnRawMessageReceived, this);
            this.MessageRouter.Register<PingMessage>(OnPing, this);
            this.MessageRouter.Register<LeftMessage>(OnUserLeftGracefully, this);
            this.MessageRouter.Register<UserInfoMessage>(OnReceivedUserInfo, this);
            this.MessageRouter.NotFound.SubscribeForLifetime(OnNotFound, this);
        }

        public Promise OpenForNewConnections()
        {
            Info.Fire("Opening...");
            var ret = serverNetworkProvider.OpenForNewConnections();
            Info.Fire("Open for connections");
            return ret;
        }

        public Promise CloseForNewConnections()
        {
            Info.Fire("Closing...");
            var ret = serverNetworkProvider.CloseForNewConnections();
            Info.Fire("Closed for connections");
            return ret;
        }

        public bool TrySendMessage(MultiPlayerMessage message)
        {
            try
            {
                SendMessage(message);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool TryBroadcast(Func<MultiPlayerClientConnection, MultiPlayerMessage> messageEval)
        {
            var ret = true;
            foreach (var recipient in Connections)
            {
                var message = messageEval(recipient);
                if (message != null)
                {
                    message.Recipient = recipient.ClientId;
                    if (TrySendMessage(message) == false) ret = false;
                }
            }
            return ret;
        }

        public bool TryRespond(MultiPlayerMessage response)
        {
            if (response.RequestId == null)
            {
                throw new ArgumentNullException("RequestId cannot be null");
            }

            return TrySendMessage(response);
        }

        public void SendMessage(MultiPlayerMessage message)
        {
            message.Sender = message.Sender ?? ServerId;
            lock (connectionsLock)
            {
                var client = GetClient(message.Recipient);

                try
                {
                    if (client == null)
                    {
                        throw new Exception($"The client {message.Recipient} is not connected at the moment");
                    }
                    else
                    {
                        serverNetworkProvider.SendMessageToClient(message.Serialize(), client);
                    }
                }
                catch (Exception ex)
                {
                    Error.Fire(ex.ToString());
                    Undeliverable.Fire(new UndeliverableEvent()
                    {
                        Message = message,
                        Exception = ex
                    });
                    throw;
                }
            }
        }

        public void Broadcast(Func<MultiPlayerClientConnection, MultiPlayerMessage> messageEval)
        {
            var exceptions = new List<Exception>();
            lock (connectionsLock)
            {
                foreach (var recipient in Connections)
                {
                    var message = messageEval(recipient);
                    if (message == null)
                    {
                        continue;
                    }
                    message.Recipient = recipient.ClientId;
                    try
                    {
                        SendMessage(message);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }
            }
            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }

        public void Respond(MultiPlayerMessage response)
        {
            if (response.RequestId == null)
            {
                throw new ArgumentNullException("RequestId cannot be null");
            }

            SendMessage(response);
        }

        private void OnClientConnected(MultiPlayerClientConnection newClient)
        {
            lock (connectionsLock)
            {
                var existingConnections = Connections.ToArray();
                Connections.Add(newClient);
                Info.Fire($"Client {newClient.ClientId} arrived");
                foreach (var existingClient in existingConnections)
                {
                    SendMessage(new NewUserMessage() { NewUserId = newClient.ClientId, Recipient = existingClient.ClientId });
                    SendMessage(new NewUserMessage() { NewUserId = existingClient.ClientId, Recipient = newClient.ClientId });
                }
            }

            newClient.OnDisposed(() =>
            {
                lock (connectionsLock)
                {
                    if (Connections.Remove(newClient))
                    {
                        Warning.Fire($"Client {newClient.ClientId} was disconnected");
                        TryBroadcast((conn) => new LeftMessage() { ClientWhoLeft = newClient.ClientId });
                    }
                }
            });
        }

        private void OnRawMessageReceived(string messageText)
        {
            var hydratedMessage = MultiPlayerMessage.Deserialize(messageText);
            MessageRouter.Route(hydratedMessage.GetType().Name, hydratedMessage);
        }

        private void OnPing(PingMessage pingMessage)
        {
            Info.Fire("Received ping from " + pingMessage.Sender);
            if (pingMessage.Delay > 0) Thread.Sleep(pingMessage.Delay);
            Respond(new Ack() { Recipient = pingMessage.Sender, RequestId = pingMessage.RequestId });
        }
        private void OnReceivedUserInfo(UserInfoMessage message)
        {
            Info.Fire("Received user info from " + message.DisplayName);
            lock (connectionsLock)
            {
                var requester = GetClient(message.Sender);
                if (requester != null)
                {
                    requester.DisplayName = message.DisplayName;
                }
            }
            Respond(new Ack() { Recipient = message.Sender, RequestId = message.RequestId });
        }

        private void OnNotFound(MultiPlayerMessage message)
        {
            Warning.Fire($"Message not handled from {message.Sender}: {message.GetType().Name}");
            if (message.RequestId != null)
            {
                Respond(new NotFoundMessage() { Recipient = message.Sender, RequestId = message.RequestId });
            }
        }

        private void OnUserLeftGracefully(LeftMessage message)
        {
            lock (connectionsLock)
            {
                var client = GetClient(message.Sender);
                if (client != null && Connections.Remove(client))
                {
                    Warning.Fire($"Client {client.ClientId} left gracefully");
                    TryBroadcast((conn) => new LeftMessage() { ClientWhoLeft = message.Sender });
                }
            }
        }

        private MultiPlayerClientConnection GetClient(string id) => Connections.Where(c => c.ClientId == id).SingleOrDefault();
    }

    public class NewUserMessage : MultiPlayerMessage { public string NewUserId { get; set; } }

    public class PingMessage : MultiPlayerMessage { public int Delay { get; set; } }

    public class UserInfoMessage : MultiPlayerMessage { public string DisplayName { get; set; } }

    public class NotFoundMessage : MultiPlayerMessage { }

    public class LeftMessage : MultiPlayerMessage
    {
        public string ClientWhoLeft { get; set; }
    }
}
