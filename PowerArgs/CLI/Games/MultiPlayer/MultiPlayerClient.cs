using PowerArgs.Cli;
using System;

namespace PowerArgs.Games
{
    public class RemoteClient
    {
        public string ClientId { get; set; }
    }

    public class MultiPlayerClient : Lifetime
    {
        public Event<RemoteClient> NewRemoteUser { get; private set; } = new Event<RemoteClient>();
        public Event<RemoteClient> RemoteUserLeft { get; private set; } = new Event<RemoteClient>();

        public Event<MultiPlayerMessage> MessageReceived { get; private set; } = new Event<MultiPlayerMessage>();
        public string ClientId => clientNetworkProvider.ClientId;
        public Promise Connect(string server)
        {
            var ret = clientNetworkProvider.Connect(server);
            ret.Then(() => isConnected = true);
            return ret;
        }
        public void SendMessage(MultiPlayerMessage message) => clientNetworkProvider.SendMessage(message);

        private IClientNetworkProvider clientNetworkProvider;
        private bool isConnected;
        public MultiPlayerClient(IClientNetworkProvider networkProvider)
        {
            this.clientNetworkProvider = networkProvider;
            networkProvider.MessageReceived.SubscribeForLifetime(OnMessageReceived, this);
            this.OnDisposed(() =>
            {
                if (isConnected)
                {
                    SendMessage(MultiPlayerMessage.Create(ClientId, null, "Left"));
                }
                this.clientNetworkProvider.Dispose();
            });
        }

        private void OnMessageReceived(MultiPlayerMessage message)
        {
            if (message.EventId == "NewUser")
            {
                var id = message.Properties["ClientId"];
                NewRemoteUser.Fire(new RemoteClient() { ClientId = id });
            }
            else if (message.EventId == "Left")
            {
                RemoteUserLeft.Fire(new RemoteClient() { ClientId = message.SenderId });
            }
            else
            {
                MessageReceived.Fire(message);
            }
        }
    }
}
