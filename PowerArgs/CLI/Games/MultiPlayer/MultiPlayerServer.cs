using PowerArgs.Cli;
using System;
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

        public string ServerId => serverNetworkProvider.ServerId;
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
        public ObservableCollection<MultiPlayerClientConnection> Clients { get; private set; } = new ObservableCollection<MultiPlayerClientConnection>();
        public MultiPlayerMessageRouter MessageRouter { get; private set; } = new MultiPlayerMessageRouter();
        public Event<UndeliverableEvent> Undeliverable { get; private set; } = new Event<UndeliverableEvent>();

        private IServerNetworkProvider serverNetworkProvider;

        public MultiPlayerServer(IServerNetworkProvider networkProvider)
        {
            this.serverNetworkProvider = networkProvider;
            this.serverNetworkProvider.ClientConnected.SubscribeForLifetime(OnClientConnected, this);
            this.serverNetworkProvider.ConnectionLost.SubscribeForLifetime(OnConnectionLost, this);
            this.serverNetworkProvider.MessageReceived.SubscribeForLifetime((messageText) =>
            {
                var hydratedMessage = MultiPlayerMessage.Deserialize(messageText);
                MessageRouter.Route(hydratedMessage.GetType().Name, hydratedMessage);
            }, this);
            this.OnDisposed(this.serverNetworkProvider.Dispose);

            this.MessageRouter.Register<PingMessage>(Ping, this);
            this.MessageRouter.Register<UserInfoMessage>(SetUserInfo, this);
            this.MessageRouter.NotFound.SubscribeForLifetime(NotFound, this);
        }

        public void SendMessage(MultiPlayerMessage message)
        {
            SendMessageInternal(message, GetClient(message.Recipient));
        }

        public void Broadcast(MultiPlayerMessage message)
        {
            foreach (var recipient in Clients.Where(c => c.ClientId != message.Sender))
            {
                SendMessageInternal(message, recipient);
            }
        }

        public void Respond(MultiPlayerMessage response)
        {
            if(response.RequestId == null)
            {
                throw new ArgumentNullException("RequestId cannot be null");
            }

            var requester = Clients.Where(c => c.ClientId == response.Recipient).SingleOrDefault();
            SendMessageInternal(response, requester);
        }
 
        private void Ping(PingMessage pingMessage)
        {
            Info.Fire("Received ping from "+pingMessage.Sender);
            if (pingMessage.Delay > 0)
            {
                Thread.Sleep(pingMessage.Delay);
            }
            var requester = GetClient(pingMessage.Sender);
            Respond(new Ack() { Recipient = pingMessage.Sender, RequestId = pingMessage.RequestId });
        }
        private void SetUserInfo(UserInfoMessage message)
        {
            Info.Fire("Received user info from " + message.DisplayName);
            var requester = GetClient(message.Sender);
            requester.DisplayName = message.DisplayName;
            Respond(new Ack() { Recipient = message.Sender, RequestId = message.RequestId });
        }

        private void NotFound(MultiPlayerMessage message)
        {
            Warning.Fire($"Message not handled from {message.Sender}: {message.GetType().Name}");
            if (message.RequestId != null)
            {
                Respond(new NotFoundMessage() { Recipient = message.Sender, RequestId = message.RequestId });
            }
        }


        private MultiPlayerClientConnection GetClient(string id) => Clients.Where(c => c.ClientId == id).Single();

        private void OnClientConnected(MultiPlayerClientConnection newClient)
        {
            lock (Clients)
            {
                foreach (var existingClient in Clients)
                {
                    SendMessageInternal(new NewUserMessage() { NewUserId = newClient.ClientId }, existingClient);
                    SendMessageInternal(new NewUserMessage() { NewUserId = existingClient.ClientId }, newClient);
                }
                Clients.Add(newClient);
                Info.Fire($"Client {newClient.ClientId} arrived");
            }
        }

        private void OnConnectionLost(MultiPlayerClientConnection client)
        {
            Clients.Remove(client);
            Info.Fire($"Client {client.ClientId} disconnected");
        }

        private void SendMessageInternal(MultiPlayerMessage message, MultiPlayerClientConnection client)
        {
            message.Sender = message.Sender ?? ServerId;
            message.Recipient = client.ClientId;
            try
            {
                serverNetworkProvider.SendMessageToClient(message.Serialize(), client);
            }
            catch (Exception ex)
            {
                Error.Fire(ex.ToString());
                Undeliverable.Fire(new UndeliverableEvent()
                {
                    Message = message,
                    Exception = ex
                });
            }
        }
    }

    public class NewUserMessage : MultiPlayerMessage { public string NewUserId { get; set; } }

    public class PingMessage : MultiPlayerMessage { public int Delay { get; set; } }

    public class UserInfoMessage : MultiPlayerMessage { public string DisplayName { get; set; } }

    public class NotFoundMessage : MultiPlayerMessage { }

    public class LeftMessage : MultiPlayerMessage { }
}
