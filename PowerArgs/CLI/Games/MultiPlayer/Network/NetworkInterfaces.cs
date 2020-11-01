using PowerArgs.Cli;
using System;
using System.Threading.Tasks;

namespace PowerArgs.Games
{
    public interface IServerNetworkProvider : IDisposable
    {
        string ServerId { get; }

        // connnect / disconnect
        Event<MultiPlayerClientConnection> ClientConnected { get; }

        // listen / stop
        Task OpenForNewConnections();
        Task CloseForNewConnections();

        // send / receive
        Event<string> MessageReceived { get; }
        void SendMessageToClient(string message, MultiPlayerClientConnection client);
    }

    public interface IClientNetworkProvider : ILifetimeManager, IDisposable
    {
        Event<Exception> Disconnected { get;  }
        string ClientId { get; }

        Task Connect(ServerInfo server);
        Event<string> MessageReceived { get; }

        void SendMessage(string message);
    }
}
