using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using PowerArgs.Games;
using System;
using System.Collections.Generic;
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
            MainCharacter.Inventory.Items.Add(new RPGLauncher() { AmmoAmount = 100 });
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

            var deathmatch = new Deathmatch(new MultiPlayerContestOptions() { MaxPlayers = 2, Server = server });
            deathmatch.Start();
            await Task.Delay(1000);

            client = new MultiPlayerClient(new SocketClientNetworkProvider());
            client.EventRouter.RegisterOnce<GameOverMessage>(OnGameOver);
            MainCharacter.MultiPlayerClient = client;
            await client.Connect(server.ServerId).AsAwaitable();
            var opponentArrivedEvent = await client.EventRouter.Await<NewUserMessage>();
            this.Scene.QueueAction(() =>
            {
                opponent = new RemoteCharacter(client, opponentArrivedEvent.NewUserId);
                opponent.Tags.Add("enemy");
                opponent.ResizeTo(1, 1);
                opponent.MoveTo(this.LayoutRoot.Width - 2, 0);
                this.Scene.Add(opponent);
            });
              
            client.EventRouter.Register<BoundsMessage>(OnBoundsReceived, this);
            SetInterval(() => client.SendMessage(CreateLocationMessage(client.ClientId, MainCharacter)), TimeSpan.FromMilliseconds(5));
        }

        private async void ConnectToRemoteServer()
        {
            MainCharacter.ResizeTo(0, 0);
            client = new MultiPlayerClient(new SocketClientNetworkProvider());
            client.EventRouter.RegisterOnce<GameOverMessage>(OnGameOver);
            MainCharacter.MultiPlayerClient = client;
            client.EventRouter.RegisterOnce<NewUserMessage>((userInfoResult) =>
            {
                this.Scene.QueueAction(() =>
                {
                    MainCharacter.ResizeTo(1, 1);
                    MainCharacter.MoveTo(this.LayoutRoot.Width - 2, 0);
                    opponent = new RemoteCharacter(client, userInfoResult.NewUserId);
                    opponent.Tags.Add("enemy");
                    opponent.ResizeTo(1, 1);
                    this.Scene.Add(opponent);
                });
            });

            client.EventRouter.RegisterOnce<StartGameMessage>((args)=>
            {
                client.EventRouter.Register<BoundsMessage>(OnBoundsReceived, this);
                SetInterval(() => client.SendMessage(CreateLocationMessage(client.ClientId, MainCharacter)), TimeSpan.FromMilliseconds(5));
            });

            await client.Connect(remoteServerId).AsAwaitable();
        }

      
        private BoundsMessage CreateLocationMessage(string clientId, SpacialElement element) =>  new BoundsMessage()
        {
            X = element.Left,
            Y = element.Top,
            W = element.Width,
            H = element.Height,
            ClientToUpdate = clientId
        };
     

        private void OnBoundsReceived(BoundsMessage m)
        {
            Scene.QueueAction(() =>
            {
                opponent?.MoveTo(m.X, m.Y);
                opponent?.ResizeTo(m.W, m.H);
            });
        }

        private void OnGameOver(GameOverMessage message)
        {
            QueueAction(() =>
            {
                Dialog.ShowMessage("And the winner is: ".ToConsoleString() + message.Winner.ToGreen(), () =>
                {
                    Scene.Stop();
                    ConsoleApp.Current.Stop();
                });
            });
        }
    }
}
