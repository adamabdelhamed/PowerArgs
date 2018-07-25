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

        private MultiPlayerClient client;
        private RemoteCharacter opponent;
        public DemoMultiPlayerGameApp()
        {
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

        private Lifetime gameLifetime;

        private async void OrchestrateGame()
        {
            gameLifetime = new Lifetime();
            var username = (await Dialog.ShowRichTextInput("What is your name?".ToConsoleString(), allowEscapeToCancel: false, initialValue: Environment.MachineName.ToConsoleString() ).AsAwaitable()).ToString();

            var choice = await Dialog.ShowMessage("Choose your adventure".ToConsoleString(), allowEscapeToCancel: false, buttons: new DialogButton[]
            {
                new DialogButton(){ DisplayText = "Start a server".ToConsoleString() },
                new DialogButton(){ DisplayText = "Connect to server".ToConsoleString() },
            }).AsAwaitable();

            if(choice.DisplayText.ToString() == "Start a server")
            {
                StartLocalServer(username);
            }
            else
            {
                var serverInfo = new ServerInfo { Server = "adamab2018", Port = 8080  };

                var panel = new ConsolePanel() { Height = 10 };
                var form = panel.Add(new Form(FormOptions.FromObject(serverInfo))).Fill(padding: new Thickness(1,1,1,2));
                var okButton = panel.Add(new Button() { X = 1, Text = "OK".ToConsoleString(), Shortcut = new KeyboardShortcut(ConsoleKey.Enter) }).DockToBottom(padding: 1);
                var dialog = new Dialog(panel);
                dialog.MaxHeight = 8;
                okButton.Pressed.SubscribeOnce(() => LayoutRoot.Controls.Remove(dialog));
                await dialog.Show().AsAwaitable();
                Scene.QueueAction(()=>ConnectToRemoteServer(serverInfo, username));
            }
        }

        protected override void AfterLevelLoaded(Level l)
        {
            MainCharacter.Inventory.Items.Add(new RPGLauncher() { AmmoAmount = 100 });
            QueueAction(OrchestrateGame);
        }

        private async void StartLocalServer(string userDisplayName)
        {
            var socketServer = new SocketServerNetworkProvider(8080);
            var server = new MultiPlayerServer(socketServer);
            this.OnDisposed(server.Dispose);

            var deathmatch = new Deathmatch(new MultiPlayerContestOptions() { MaxPlayers = 2, Server = server });
            deathmatch.Start();
            await Task.Delay(1000);

            client = new MultiPlayerClient(new SocketClientNetworkProvider());
            client.EventRouter.RegisterOnce<GameOverMessage>(OnGameOver);
            MainCharacter.MultiPlayerClient = client;
            await client.Connect(socketServer.ServerInfo).AsAwaitable();
            var opponentArrivedEvent = await client.EventRouter.Await<NewUserMessage>();
            this.Scene.QueueAction(() =>
            {
                opponent = new RemoteCharacter(client, opponentArrivedEvent.NewUserId);
                opponent.Tags.Add("enemy");
                opponent.ResizeTo(1, 1);
                opponent.MoveTo(this.LayoutRoot.Width - 2, 0);
                this.Scene.Add(opponent);
            });

            await client.SendRequest(new UserInfoMessage() { DisplayName = userDisplayName }).AsAwaitable();
            client.EventRouter.Register<BoundsMessage>(OnBoundsReceived, this);
            var intervalHandle = SetInterval(() => client.SendMessage(CreateLocationMessage(client.ClientId, MainCharacter)), TimeSpan.FromMilliseconds(5));
            gameLifetime.OnDisposed(intervalHandle.Dispose);
        }

        private async void ConnectToRemoteServer(ServerInfo info, string userDisplayName)
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
                var intervalHandle = SetInterval(() => client.SendMessage(CreateLocationMessage(client.ClientId, MainCharacter)), TimeSpan.FromMilliseconds(5));
                gameLifetime.OnDisposed(intervalHandle.Dispose);
            });

            Dialog dialog = null;
            QueueAction(() =>
            {
                dialog = new Dialog(new Label() { Text = $"Connecting to {info.Server}:{info.Port}".ToCyan() });
                dialog.MaxHeight = 4;
                dialog.Show();
            });

            try
            {
                await client.Connect(info).AsAwaitable();
                QueueAction(() => LayoutRoot.Controls.Remove(dialog));
            }
            catch(Exception ex)
            {
                QueueAction(() => LayoutRoot.Controls.Remove(dialog));
                QueueAction(() => Dialog.ShowMessage("Failed to connect".ToRed(), ()=>
                {
                    Scene.Stop();
                    Stop();
                }));
                return;
            }

            await client.SendRequest(new UserInfoMessage() { DisplayName = userDisplayName }).AsAwaitable();
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
                gameLifetime.Dispose();
                Dialog.ShowMessage("And the winner is: ".ToConsoleString() + message.WinnerDisplayName.ToGreen(), () =>
                {
                    Scene.Stop();
                    ConsoleApp.Current.Stop();
                });
            });
        }
    }
}
