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
        Event<MultiPlayerMessage> MessageReceived { get; }
        void SendMessageToClient(string message, MultiPlayerClientConnection client);
    }

    public interface IClientNetworkProvider : IDisposable
    {
        string ClientId { get; }

        Promise Connect(string server);
        Event<MultiPlayerMessage> MessageReceived { get; }

        void SendMessage(MultiPlayerMessage message);
    }
}
