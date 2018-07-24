using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using PowerArgs.Games;
using System;
using System.Collections.Generic;
using PowerArgs;
namespace DemoGame
{
    public class DemoMultiPlayerGameApp : GameApp
    {
        protected override SceneFactory SceneFactory => new SceneFactory(new List<ItemReviver>()
        {
            new MainCharacterReviver(),
        });

        public override Dictionary<string, Level> Levels => new Dictionary<string, Level>()
        {
            {
                "default",
                new Level()
                {
                    Items = new List<LevelItem>()
                    {
                        new LevelItem(){ Width=1, Height=1, Tags = new List<string>(){ "main-character" }  }
                    }
                }
            }
        };

        private string remoteServerId;
        private MultiPlayerClient client;
        private RemoteCharacter opponent;
        public DemoMultiPlayerGameApp(string remoteServerId = null)
        {
            this.remoteServerId = remoteServerId;
            this.RequiredWidth = 102;
            this.RequiredHeight = 45;
            QueueAction(Initialize);
        }

        protected void Initialize()
        {
            this.RequiredSizeMet.SubscribeOnce(() =>
            {
                var shooterKeys = new ShooterKeys(() => this.Scene);
                this.KeyboardInput.KeyMap = shooterKeys.ToKeyMap();
 
                this.Load("default");

                var requiredSizeCausedPause = false;
                this.RequiredSizeNotMet.SubscribeForLifetime(() =>
                {
                    if (Scene.IsRunning)
                    {
                        this.Pause(false);
                        requiredSizeCausedPause = true;
                    }
                    else
                    {
                        requiredSizeCausedPause = false;
                    }
                }, this);

                this.RequiredSizeMet.SubscribeForLifetime(() =>
                {
                    if (requiredSizeCausedPause)
                    {
                        this.Resume();
                    }

                }, this);
            });
        }

        protected override void AfterLevelLoaded(Level l)
        {
            if (remoteServerId == null)
            {
                StartLocalServer();
            }
            else
            {
                ConnectToRemoteServer();
            }
        }

        private void StartLocalServer()
        {
            var server = new MultiPlayerServer(new SocketServerNetworkProvider(8080));
            this.OnDisposed(server.Dispose);
            server.OpenForNewConnections().Then(() =>
            {
                client = new MultiPlayerClient(new SocketClientNetworkProvider());
                client.NewRemoteUser.SubscribeOnce((u) =>
                {
                    this.Scene.QueueAction(() =>
                    {
                        opponent = new RemoteCharacter(u);
                        opponent.ResizeTo(1, 1);
                        opponent.MoveTo(this.LayoutRoot.Width - 2, 0);
                        this.Scene.Add(opponent);
                    });
                });

                client.MessageReceived.SubscribeForLifetime((m) => TryUpdateOpponentBounds(m), this);

                client.Connect(server.ServerId).Then(() =>
                {
                    SetInterval(() => client.SendMessage(CreateLocationMessage(client.ClientId, MainCharacter)), TimeSpan.FromMilliseconds(5));
                });
            });
        }

        private void ConnectToRemoteServer()
        {
            MainCharacter.ResizeTo(0, 0);

            client = new MultiPlayerClient(new SocketClientNetworkProvider());
            client.Connect(remoteServerId).Then(() =>
            {
                client.NewRemoteUser.SubscribeOnce((u) =>
                {
                    this.Scene.QueueAction(() =>
                    {
                        MainCharacter.ResizeTo(1, 1);
                        MainCharacter.MoveTo(this.LayoutRoot.Width - 2, 0);

                        opponent = new RemoteCharacter(u);
                        opponent.ResizeTo(1, 1);
                   
                        this.Scene.Add(opponent);  
                    });
                });

                client.MessageReceived.SubscribeForLifetime((m) => TryUpdateOpponentBounds(m), this);
                SetInterval(() => client.SendMessage(CreateLocationMessage(client.ClientId, MainCharacter)), TimeSpan.FromMilliseconds(5));
            });
        }

        private MultiPlayerMessage CreateLocationMessage(string clientId, SpacialElement element) =>  MultiPlayerMessage.Create(clientId, null, "Bounds", new Dictionary<string, string>()
        {
            { "X", element.Left+"" },
            { "Y", element.Top+"" },
            { "W", element.Width+"" },
            { "H", element.Height+"" },
        });
     

        private bool TryUpdateOpponentBounds(MultiPlayerMessage m)
        {
            if(m.EventId != "Bounds")
            {
                return false;
            }

            Scene.QueueAction(() => SyncBounds(m, opponent));

            return true;
        }

        private void SyncBounds(MultiPlayerMessage m, SpacialElement localElement)
        {
            var x = float.Parse(m.Data["X"]);
            var y = float.Parse(m.Data["Y"]);
            var w = float.Parse(m.Data["W"]);
            var h = float.Parse(m.Data["H"]);
            
            localElement?.MoveTo(x, y);
            localElement?.ResizeTo(w, h);
        }
    }

    public class RemoteCharacter : SpacialElement
    {
        public RemoteClient Remote { get; private set; }
        public RemoteCharacter(RemoteClient remote)
        {
            Remote = remote;
        }
    }
}
