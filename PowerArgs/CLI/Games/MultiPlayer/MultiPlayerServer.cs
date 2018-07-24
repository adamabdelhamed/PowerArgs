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

        private IServerNetworkProvider serverNetworkProvider;

        public ObservableCollection<MultiPlayerClientConnection> clients = new ObservableCollection<MultiPlayerClientConnection>();

        public EventRouter<MultiPlayerMessage> MessageRouter { get; private set; } = new EventRouter<MultiPlayerMessage>();

        public Event<UndeliverableEvent> Undeliverable { get; private set; } = new Event<UndeliverableEvent>();

        public MultiPlayerServer(IServerNetworkProvider networkProvider)
        {
            this.serverNetworkProvider = networkProvider;
            this.serverNetworkProvider.ClientConnected.SubscribeForLifetime(OnClientConnected, this);
            this.serverNetworkProvider.ConnectionLost.SubscribeForLifetime(OnConnectionLost, this);
            this.serverNetworkProvider.MessageReceived.SubscribeForLifetime((m) =>
            {
                MessageRouter.Route(m.Path, m);
            }, this);
            this.OnDisposed(this.serverNetworkProvider.Dispose);

            this.MessageRouter.RegisterRouteForLifetime($"ping/{P("sender")}/{MultiPlayerMessage.Encode(ServerId)}", Ping, this);
            this.MessageRouter.NotFound.SubscribeForLifetime(NotFound, this);
        }

        private void NotFound(RoutedEvent<MultiPlayerMessage> args)
        {
            if (args.Data.Data.ContainsKey("RequestId"))
            {
                Respond(args.Data, new Dictionary<string, string>()
                {
                    {"error", "NotFound" }
                });
            }
        }

        public void Respond(MultiPlayerMessage request, Dictionary<string,string> data)
        {
            data.Add("RequestId", request.Data["RequestId"]);
            var requester = clients.Where(c => c.ClientId == request.SenderId).SingleOrDefault();
            SendMessageInternal(MultiPlayerMessage.Create(this.ServerId, request.SenderId, "Response", data), requester);
        }

        private string P(string name) => "{" + MultiPlayerMessage.Encode(name) + "}";

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

        private MultiPlayerClientConnection GetClient(string id) => clients.Where(c => c.ClientId == id).SingleOrDefault();
         
        public void Send(MultiPlayerMessage message)
        {
            var client = GetClient(message.RecipientId);
            SendMessageInternal(message, client);
        }

        public void Broadcast(MultiPlayerMessage message)
        {
            foreach (var recipient in clients.Where(c => c.ClientId != message.SenderId))
            {
                SendMessageInternal(message, recipient);
            }
        }

        private void OnClientConnected(MultiPlayerClientConnection newClient)
        {
            this.MessageRouter.RegisterRouteForLifetime($"{P("event")}/{P("sender")}/{ MultiPlayerMessage.Encode(newClient.ClientId)}/{P("*")}", (toForward)=>
            {
                SendMessageInternal(toForward.Data, newClient);

            }, newClient);

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
