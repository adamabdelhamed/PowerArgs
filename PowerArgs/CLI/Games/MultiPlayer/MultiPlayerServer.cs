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
        public string ServerId => serverNetworkProvider.ServerId;
        public Promise OpenForNewConnections() => serverNetworkProvider.OpenForNewConnections();
        public Promise CloseForNewConnections() => serverNetworkProvider.CloseForNewConnections();

        public IMultiPlayerContest Contest { get; set; }

        private IServerNetworkProvider serverNetworkProvider;

        private List<MultiPlayerClientConnection> clients = new List<MultiPlayerClientConnection>();

        public EventRouter<MultiPlayerMessage> MessageRouter { get; private set; } = new EventRouter<MultiPlayerMessage>();

        public Event<UndeliverableEvent> Undeliverable { get; private set; } = new Event<UndeliverableEvent>();

        public MultiPlayerServer(IServerNetworkProvider networkProvider)
        {
            this.serverNetworkProvider = networkProvider;
            this.serverNetworkProvider.ClientConnected.SubscribeForLifetime(OnClientConnected, this);
            this.serverNetworkProvider.ConnectionLost.SubscribeForLifetime(OnConnectionLost, this);
            this.serverNetworkProvider.MessageReceived.SubscribeForLifetime((m) => MessageRouter.Fire(m.Path, m), this);
            this.OnDisposed(this.serverNetworkProvider.Dispose);

            this.MessageRouter.SubscribeForLifetime($"{nameof(Ping)}/{P("sender")}/{this.ServerId.Replace("/", "-")}", Ping, this);
            this.MessageRouter.SubscribeForLifetime($"{P("event")}/{P("sender")}/{this.ServerId.Replace("/", "-")}", OnMessageSentToServer, this);
            this.MessageRouter.SubscribeForLifetime($"{P("event")}/{P("sender")}/{this.ServerId.Replace("/", "-")}/{P("*")}", OnMessageSentToServer, this);
            this.MessageRouter.SubscribeForLifetime($"{P("event")}/{P("sender")}/{P("recipient")}", OnForwardMessageReceived, this);
            this.MessageRouter.SubscribeForLifetime($"{P("event")}/{P("sender")}/{P("recipient")}/{P("*")}", OnForwardMessageReceived, this);
        }

        private string P(string name) => "{" + name.Replace("/","-") + "}";

        private void Ping(RoutedEvent<MultiPlayerMessage> ev)
        {
            var message = ev.Data;
            if (message.Data.TryGetValue("delay", out string delay))
            {
                var delayMs = int.Parse(delay);
                Thread.Sleep(delayMs);
            }
            var requester = clients.Where(c => c.ClientId == message.SenderId).SingleOrDefault();
            var response = MultiPlayerMessage.Create(this.ServerId, message.SenderId, "Response", new Dictionary<string, string>()
                    {
                        { "RequestId", message.Data["RequestId"] },
                    });

            SendMessageInternal(response, requester);
        }

        private void OnMessageSentToServer(RoutedEvent<MultiPlayerMessage> ev)
        {
            var message = ev.Data;

            if (Contest == null)
            {
                var requester = clients.Where(c => c.ClientId == message.SenderId).SingleOrDefault();
                if (requester != null)
                {
                    var response = MultiPlayerMessage.Create(this.ServerId, message.SenderId, "Response", new Dictionary<string, string>()
                        {
                            { "error", "NoContest" },
                            { "RequestId", message.Data["RequestId"] },
                        });

                    SendMessageInternal(response, requester);
                }
            }
            else
            {
                Contest.GetResponse(message).Then((response) =>
                {
                    if (message.Data.ContainsKey("RequestId"))
                    {
                        var requester = clients.Where(c => c.ClientId == message.SenderId).SingleOrDefault();
                        if (requester != null)
                        {
                            response.AddProperty("RequestId", message.Data["RequestId"]);
                            SendMessageInternal(response, requester);
                        }
                    }
                });
            }
        }

        private void OnForwardMessageReceived(RoutedEvent<MultiPlayerMessage> ev)
        {
            var message = ev.Data;
            var recipients = message.RecipientId == null ?
                      clients.Where(c => c.ClientId != message.SenderId) :
                      clients.Where(c => c.ClientId == message.RecipientId);

            foreach (var recipient in recipients)
            {
                SendMessageInternal(message, recipient);
            }
        }

        private void OnClientConnected(MultiPlayerClientConnection newClient)
        {
            lock (clients)
            {
                foreach (var existingClient in clients)
                {
                    SendMessageInternal(MultiPlayerMessage.Create(this.ServerId, existingClient.ClientId, "NewUser", new Dictionary<string, string>()
                    {
                        { "ClientId", newClient.ClientId  }
                    }), existingClient);

                    SendMessageInternal(MultiPlayerMessage.Create(this.ServerId, newClient.ClientId, "NewUser", new Dictionary<string, string>()
                    {
                        { "ClientId", existingClient.ClientId  }
                    }), newClient);
                }
                clients.Add(newClient);
            }
        }

        private void OnConnectionLost(MultiPlayerClientConnection client)
        {
            clients.Remove(client);
        }

        private void SendMessageInternal(MultiPlayerMessage message, MultiPlayerClientConnection client)
        {
            try
            {
                serverNetworkProvider.SendMessageToClient(message.RawContents, client);
            }
            catch (Exception ex)
            {
                Undeliverable.Fire(new UndeliverableEvent()
                {
                    Message = message,
                    Exception = ex
                });
            }
        }
    }
}
