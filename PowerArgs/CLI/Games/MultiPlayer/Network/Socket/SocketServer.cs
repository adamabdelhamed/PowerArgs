using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using PowerArgs.Cli;

namespace PowerArgs.Games
{
    public class SocketServerNetworkProvider : Lifetime, IServerNetworkProvider
    {
        public string ServerId { get; private set; }

        public Event<MultiPlayerClientConnection> ClientConnected { get; private set; } = new Event<MultiPlayerClientConnection>();
        public Event<MultiPlayerClientConnection> ConnectionLost { get; private set; } = new Event<MultiPlayerClientConnection>();
        public Event<string> MessageReceived { get; private set; } = new Event<string>();

        private Dictionary<string, RemoteSocketConnection> connections = new Dictionary<string, RemoteSocketConnection>();
        private TcpListener listener;
        private bool isListening;
        private Deferred listeningDeferred;
        private int port;
        private IPHostEntry ipHostInfo;
        private IPEndPoint localEP;
        public SocketServerNetworkProvider(int port)
        {
            this.port = port;
            ipHostInfo = Dns.Resolve(Dns.GetHostName());
            localEP = new IPEndPoint(ipHostInfo.AddressList[0], port);
            this.ServerId = "http://" + localEP.Address + ":" + port;
        }

        public Promise OpenForNewConnections()
        {
            isListening = true;
            listeningDeferred = Deferred.Create();
            var startListeningDeferred = Deferred.Create();
            BackgroundThread t = null;
            t = new BackgroundThread(() =>
            {
                try
                {
                    listener = new TcpListener(localEP.Address, port);
                    listener.Start();
                }
                catch (Exception ex)
                {
                    startListeningDeferred.Reject(ex);
                    return;
                }

                // we have started listening
                startListeningDeferred.Resolve();

                try
                {
                    while (isListening && t.IsExpired == false)
                    {
                        Socket socket;
                        try
                        {
                            socket = listener.AcceptSocket(TimeSpan.FromSeconds(1));
                            if (t.IsExpired) return;
                        }
                        catch (TimeoutException)
                        {
                            continue;
                        }
                        var connection = new RemoteSocketConnection()
                        {
                            ClientId = (socket.RemoteEndPoint as IPEndPoint).Address.ToString() + ":" + (socket.RemoteEndPoint as IPEndPoint).Port,
                            RemoteSocket = socket,
                            MessageReceived = this.MessageReceived,
                        };
                        this.OnDisposed(connection.Dispose);
                        connection.Listen();
                        connections.Add(connection.ClientId, connection);
                        ClientConnected.Fire(connection);
                    }
                    listener.Stop();
                    listeningDeferred.Resolve();
                }
                catch(Exception ex)
                {
                    listeningDeferred.Reject(ex);
                }
                finally
                {
                    listeningDeferred = null;
                }
            });
            this.OnDisposed(t.Dispose);
            t.Start();
            return startListeningDeferred.Promise;
        }

        public Promise CloseForNewConnections()
        {
            isListening = false;
            return listeningDeferred.Promise;
        }

        public void SendMessageToClient(string message, MultiPlayerClientConnection client)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            var lengthBytes = BitConverter.GetBytes(bytes.Length);
            var sent = (client as RemoteSocketConnection).RemoteSocket.Send(lengthBytes);
            if (sent != lengthBytes.Length) throw new Exception("WTF");
            sent = (client as RemoteSocketConnection).RemoteSocket.Send(bytes);
            if (sent != bytes.Length) throw new Exception("WTF");
        }

        protected override void DisposeManagedResources() { }
    }

    public class RemoteSocketConnection : MultiPlayerClientConnection
    {
        public Socket RemoteSocket { get; set; }

        public Event<string> MessageReceived { get; set; }

        public Promise Listen() => new BackgroundThread(ListenThread).Start();
        
        private void ListenThread()
        {
            try
            {
                RemoteSocket.ReceiveTimeout = 1000;
                byte[] buffer = new byte[1024 * 1024];
                while (this.IsExpired == false)
                {
                    SocketHelpers.Read(this, RemoteSocket, buffer, 4);
                    if (this.IsExpired) break;
                    var messageLength = BitConverter.ToInt32(buffer, 0);
                    SocketHelpers.Read(this, RemoteSocket, buffer, messageLength);
                    if (this.IsExpired) break;
                    var messageText = Encoding.UTF8.GetString(buffer, 0, messageLength);
                    MessageReceived.Fire(messageText);
                }
            }
            finally
            {
                RemoteSocket.Close();
            }
        }
    }
}
