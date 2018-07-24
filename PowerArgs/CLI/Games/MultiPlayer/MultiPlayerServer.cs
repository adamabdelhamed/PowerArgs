using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Linq;

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
    
        private List<MultiPlayerClientConnection> clients = new List<MultiPlayerClientConnection>();

        public Event<UndeliverableEvent> Undeliverable { get; private set; } = new Event<UndeliverableEvent>(); 

        public MultiPlayerServer(IServerNetworkProvider networkProvider)
        {
            this.serverNetworkProvider = networkProvider;
            this.serverNetworkProvider.ClientConnected.SubscribeForLifetime(OnClientConnected, this);
            this.serverNetworkProvider.ConnectionLost.SubscribeForLifetime(OnConnectionLost, this);
            this.serverNetworkProvider.MessageReceived.SubscribeForLifetime(OnMessageReceived, this);
            this.OnDisposed(this.serverNetworkProvider.Dispose);
        }

        private void OnMessageReceived(MultiPlayerMessage message)
        {
            var recipients = message.RecipientId == null ?
                clients.Where(c => c.ClientId != message.SenderId) :
                clients.Where(c => c.ClientId == message.RecipientId);

            foreach(var recipient in recipients)
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
