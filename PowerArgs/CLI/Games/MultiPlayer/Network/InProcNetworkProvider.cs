using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PowerArgs.Cli;

namespace PowerArgs.Games
{
    public class InProcServerNetworkProvider : Disposable, IServerNetworkProvider
    {
        internal static Dictionary<string, InProcServerNetworkProvider> servers = new Dictionary<string, InProcServerNetworkProvider>();

        public string ServerId { get; set; }
        public Event<MultiPlayerClientConnection> ClientConnected { get; private set; } = new Event<MultiPlayerClientConnection>();
        public Event<MultiPlayerClientConnection> ConnectionLost { get; private set; } = new Event<MultiPlayerClientConnection>();
        public Event<MultiPlayerMessage> MessageReceived { get; private set; } = new Event<MultiPlayerMessage>();
 
        private Dictionary<string, InProcClientNetworkProvider> inProcClients = new Dictionary<string, InProcClientNetworkProvider>();
        private bool allowNewConnections;

        public InProcServerNetworkProvider(string serverId)
        {
            this.ServerId = serverId;
            lock(servers)
            {
                servers.Add(this.ServerId, this);
            }
        }

        public Promise OpenForNewConnections()
        {
            var d = Deferred.Create();
            allowNewConnections = true;
            d.Resolve();
            return d.Promise;
        }

        public Promise CloseForNewConnections()
        {
            var d = Deferred.Create();
            allowNewConnections = false;
            d.Resolve();
            return d.Promise;
        }

        public void SendMessageToClient(string message, MultiPlayerClientConnection client)
        {
            if (inProcClients.ContainsKey(client.ClientId) == false)
            {
                throw new IOException("Client not found: " + client.ClientId);
            }

            var inProcClient = inProcClients[client.ClientId];
            var parsedMessage = MultiPlayerMessage.Parse(message);
            inProcClient.MessageReceived.Fire(parsedMessage);
        }


        internal static void AcceptMessage(string clientId, MultiPlayerMessage message)
        {
            foreach(var server in servers.Values)
            {
                if(server.inProcClients.ContainsKey(clientId))
                {
                    server.MessageReceived.Fire(message);
                    return;
                }
            }
        }

        internal static Promise AcceptConnection(InProcClientNetworkProvider inProcClient, string serverId)
        {
            var d = Deferred.Create();
            try
            {
                var server = servers[serverId];
                if (server.allowNewConnections)
                {
                    server.inProcClients.Add(inProcClient.ClientId, inProcClient);
                    server.ClientConnected.Fire(new MultiPlayerClientConnection() { ClientId = inProcClient.ClientId });
                }
                else
                {
                    throw new IOException("new connections not allowed");
                }
                d.Resolve();
            }
            catch (Exception ex)
            {
                d.Reject(ex);
            }
            return d.Promise;
        }

        protected override void DisposeManagedResources() { }
    }

    public class InProcClientNetworkProvider : Disposable, IClientNetworkProvider
    {
        public string ClientId { get; private set; }

        public InProcClientNetworkProvider(string id)
        {
            this.ClientId = id;
        }

        public Event<MultiPlayerMessage> MessageReceived { get; private set; } = new Event<MultiPlayerMessage>();
        public Promise Connect(string server) => InProcServerNetworkProvider.AcceptConnection(this, server);
        public void SendMessage(MultiPlayerMessage message) => InProcServerNetworkProvider.AcceptMessage(this.ClientId, message);
        protected override void DisposeManagedResources() { }
    }
}
