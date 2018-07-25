using PowerArgs.Cli;
using System;

namespace PowerArgs.Games
{
    public interface IServerNetworkProvider : IDisposable
    {
        string ServerId { get; }

        // connnect / disconnect
        Event<MultiPlayerClientConnection> ClientConnected { get; }
        Event<MultiPlayerClientConnection> ConnectionLost { get; }

        // listen / stop
        Promise OpenForNewConnections();
        Promise CloseForNewConnections();

        // send / receive
        Event<string> MessageReceived { get; }
        void SendMessageToClient(string message, MultiPlayerClientConnection client);
    }

    public interface IClientNetworkProvider : IDisposable
    {
        string ClientId { get; }

        Promise Connect(ServerInfo server);
        Event<string> MessageReceived { get; }

        void SendMessage(string message);
    }
}
