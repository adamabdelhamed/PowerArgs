using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using PowerArgs.Cli;

namespace PowerArgs.Games
{
    public class InProcServerNetworkProvider : Lifetime, IServerNetworkProvider
    {
        internal static Dictionary<string, InProcServerNetworkProvider> servers = new Dictionary<string, InProcServerNetworkProvider>();

        public string ServerId { get; set; }
        public Event<MultiPlayerClientConnection> ClientConnected { get; private set; } = new Event<MultiPlayerClientConnection>();
        public Event<string> MessageReceived { get; private set; } = new Event<string>();
 
        private Dictionary<string, InProcClientNetworkProvider> inProcClients = new Dictionary<string, InProcClientNetworkProvider>();
        private bool allowNewConnections;

        public InProcServerNetworkProvider(ServerInfo info)
        {
            this.ServerId = info.Server+":"+info.Port;
            lock(servers)
            {
                servers.Add(this.ServerId, this);
            }

            this.OnDisposed(() =>
            {
                lock (servers)
                {
                    servers.Remove(this.ServerId);
                }
            });
        }

        public Task OpenForNewConnections()
        {
            var d = new TaskCompletionSource<bool>();
            allowNewConnections = true;
            d.SetResult(true);
            return d.Task;
        }

        public Task CloseForNewConnections()
        {
            var d = new TaskCompletionSource<bool>();
            allowNewConnections = false;
            d.SetResult(true);
            return d.Task;
        }

        public void SendMessageToClient(string message, MultiPlayerClientConnection client)
        {
            if (inProcClients.ContainsKey(client.ClientId) == false)
            {
                throw new IOException("Client not found: " + client.ClientId);
            }

            var inProcClient = inProcClients[client.ClientId];
            inProcClient.MessageReceived.Fire(message);
        }


        internal static void AcceptMessage(string clientId, string message)
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

        internal static Task AcceptConnection(InProcClientNetworkProvider inProcClient, ServerInfo serverInfo)
        {
            var d = new TaskCompletionSource<bool>();
            try
            {
                var server = servers[serverInfo.Server+":"+serverInfo.Port];
                if (server.allowNewConnections)
                {
                    server.inProcClients.Add(inProcClient.ClientId, inProcClient);
                    server.ClientConnected.Fire(new MultiPlayerClientConnection() { ClientId = inProcClient.ClientId });
                }
                else
                {
                    throw new IOException("new connections not allowed");
                }
                d.SetResult(true);
            }
            catch (Exception ex)
            {
                d.SetException(ex);
            }
            return d.Task;
        }
    }

    public class InProcClientNetworkProvider : Lifetime, IClientNetworkProvider
    {
        public Event<Exception> Disconnected { get; private set; } = new Event<Exception>();
        public string ClientId { get; private set; }

        public InProcClientNetworkProvider(string id)
        {
            this.ClientId = id;
        }

        public Event<string> MessageReceived { get; private set; } = new Event<string>();
        public Task Connect(ServerInfo server) => InProcServerNetworkProvider.AcceptConnection(this, server);
        public void SendMessage(string message) => InProcServerNetworkProvider.AcceptMessage(this.ClientId, message);
        protected override void DisposeManagedResources() { }
    }
}
