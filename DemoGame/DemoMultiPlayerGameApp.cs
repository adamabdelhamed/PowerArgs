using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using PowerArgs.Games;
using System;
using System.Collections.Generic;
using PowerArgs;
using System.Threading.Tasks;

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

        private async void StartLocalServer()
        {
            var server = new MultiPlayerServer(new SocketServerNetworkProvider(8080));
            this.OnDisposed(server.Dispose);

            var deathmatch = new Deathmatch(new MultiPlayerContestOptions()
            {
                MaxPlayers = 2,
                Server = server
            });

            deathmatch.Start();
            await Task.Delay(1000);
            client = new MultiPlayerClient(new SocketClientNetworkProvider());
            await client.Connect(server.ServerId).AsAwaitable();
            var userResult = await client.EventRouter.Await("newuser/{*}"); 
            this.Scene.QueueAction(() =>
            {
                opponent = new RemoteCharacter(userResult.Data.Data["ClientId"]);
                opponent.ResizeTo(1, 1);
                opponent.MoveTo(this.LayoutRoot.Width - 2, 0);
                this.Scene.Add(opponent);
            });
              
            client.EventRouter.RegisterRouteForLifetime("bounds/{*}", OnBoundsReceived, this);
            SetInterval(() => client.SendMessage(CreateLocationMessage(client.ClientId, MainCharacter)), TimeSpan.FromMilliseconds(5));
        }

        private async void ConnectToRemoteServer()
        {
             MainCharacter.ResizeTo(0, 0);
             client = new MultiPlayerClient(new SocketClientNetworkProvider());
       
             client.EventRouter.RegisterRouteOnce("newuser/{*}",(userInfoResult)=>
             {
                 this.Scene.QueueAction(() =>
                 {
                     MainCharacter.ResizeTo(1, 1);
                     MainCharacter.MoveTo(this.LayoutRoot.Width - 2, 0);
                     opponent = new RemoteCharacter(userInfoResult.Data.Data["ClientId"]);
                     opponent.ResizeTo(1, 1);
                     this.Scene.Add(opponent);
                 });
             });

            client.EventRouter.RegisterRouteOnce("start/{*}",(args)=>
            {
                client.EventRouter.RegisterRouteForLifetime("bounds/{*}", OnBoundsReceived, this);
                SetInterval(() => client.SendMessage(CreateLocationMessage(client.ClientId, MainCharacter)), TimeSpan.FromMilliseconds(5));
            });

            await client.Connect(remoteServerId).AsAwaitable();
        }

        private MultiPlayerMessage CreateLocationMessage(string clientId, SpacialElement element) =>  MultiPlayerMessage.Create(clientId, null, "Bounds", new Dictionary<string, string>()
        {
            { "X", element.Left+"" },
            { "Y", element.Top+"" },
            { "W", element.Width+"" },
            { "H", element.Height+"" },
        });
     

        private void OnBoundsReceived(RoutedEvent<MultiPlayerMessage> ev)
        {
            Scene.QueueAction(() => SyncBounds(ev.Data, opponent));
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
        public string ClientId{ get; private set; }
        public RemoteCharacter(string clientId)
        {
            this.ClientId = clientId;
        }
    }
}
