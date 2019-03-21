using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using PowerArgs.Games;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace DemoGame
{
    public class DemoMultiPlayerGameApp : GameApp
    {
        protected override SceneFactory SceneFactory => new SceneFactory(new List<ItemReviver>()
        {
            new CutSceneReviver(),
            new TriggerReviver(),
            new TextEffectReviver(nameof(BurnIn)),
            new MainCharacterReviver(),
            new LooseWeaponReviver(),
            new FriendlyReviver(),
            new EnemyReviver(),
            new PortalReviver(),
            new CeilingReviver(),
            new DoorReviver(),
            new WallReviver()
        });
        public override Dictionary<string, Level> Levels => new Dictionary<string, Level>()
        {
            {
                "default",
                new GeneratedLevels.MultiPlayerLevel()
            }
        };

        private MultiPlayerClient client;
        private List<RemoteCharacter> opponents = new List<RemoteCharacter>();
        private Lifetime gameLifetime;
 
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

        private static bool isStartup = true;
        protected override void AfterLevelLoaded(Level l)
        {
            QueueAction(() => this.ScenePanel.IsVisible = false);
            MainCharacter.ResizeTo(0, 0);
            MainCharacter.Inventory.Items.Add(new RPGLauncher() { AmmoAmount = 100 });
            OrchestrateGame();
        }

        private async void OrchestrateGame()
        {
            try
            {
                if (isStartup)
                {
                    await ShowLogo();
                    isStartup = false;
                }

                using (gameLifetime = new Lifetime())
                {
                    gameLifetime.OnDisposed(() => Sound.Play("GameOver"));

                    if (await IsServerPrompt())
                    {
                        await OrchestrateServerMode();
                    }
                    else
                    {
                        var serverInfo = await CollectServerInfoFromUser();
                        await ConnectToRemoteServer(serverInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                QueueAction(() =>
                {
                    Dialog.ShowMessage("Fatal error: " + ex.ToString().ToRed()).Then(Cleanup);
                });
            }
        }

        private async Task ShowLogo()
        {
            Promise introPromise = null;
            await QueueAction(() =>
            {
                var logo = LayoutRoot.Add(new PowerArgsGamesIntro()).CenterBoth();
                introPromise = logo.Play();
                introPromise.Then(() =>
                {
                    QueueAction(()=>LayoutRoot.Controls.Remove(logo));
                });
            }).AsAwaitable();

            await introPromise.AsAwaitable();
        }

        private async Task OrchestrateServerMode()
        {
            var numPlayers = await PromptForNumberOfPlayers();

            // first initialize the server
            var socketServer = new SocketServerNetworkProvider(8080);
            var server = new MultiPlayerServer(socketServer);
            await QueueAction(() => LayoutRoot.Add(new MultiPlayerServerInfoControl(server) { Width = 50 }).DockToRight().FillVertically()).AsAwaitable();
            this.OnDisposed(server.Dispose);
            var deathmatch = new Deathmatch(new MultiPlayerContestOptions() { MaxPlayers = numPlayers, Server = server });


            deathmatch.OrchestrationFailed.SubscribeOnce((ex) =>
            {
                QueueAction(() =>
                {
                    Dialog.ShowMessage("Fatal error: " + ex.ToString().ToRed()).Then(Cleanup);
                });
            });

            await deathmatch.Start();

            // then connect to it as a client
            await ConnectToRemoteServer(socketServer.ServerInfo);
        }

        private async Task<int> PromptForNumberOfPlayers()
        {
            Promise<ConsoleString> p = null;
            await QueueAction(() =>
            {
                p = Dialog.ShowRichTextInput(new RichTextDialogOptions()
                {
                    Message = "How many players?".ToConsoleString(),
                    MaxHeight = 8,
                    TextBox = new TextBox() { Value = "2".ToConsoleString() },
                });
            }).AsAwaitable();

            var ret = (await p.AsAwaitable()).ToString();

            if(int.TryParse(ret, out int numPlayers) == false)
            {
                numPlayers = await PromptForNumberOfPlayers();
            }

            return numPlayers;
        }

        private async Task ConnectToRemoteServer(ServerInfo serverInfo)
        {
            var delayedConnectingDialog = new DelayedWaitingPrompt();
            var connectingDialogTask = ShowConnectingDialogDelayed(serverInfo, delayedConnectingDialog);
            Task<StartGameMessage> gameStartSignal;

            try
            {
                client = InitializeClient();
                gameStartSignal = client.EventRouter.GetAwaitable<StartGameMessage>();
                await client.Connect(serverInfo).AsAwaitable();
                delayedConnectingDialog.PromptIsStillNeeded = false;
                await connectingDialogTask;
                await client.SendRequest(new UserInfoMessage() { DisplayName = Environment.UserName }).AsAwaitable();
             
            }
            catch (Exception ex)
            {
                QueueAction(() => Dialog.ShowMessage("Failed to connect".ToRed()).Then(Cleanup));
                return;
            }
            finally
            {
                await delayedConnectingDialog.Cleanup();
            }

            var delayedWaitingForOtherPlayersDialogSignal = new DelayedWaitingPrompt();
            var waitingForOtherPlayersDialogTask = ShowWaitingMessage("Waiting for other players...".ToConsoleString(), delayedWaitingForOtherPlayersDialogSignal);

            // If we got here then we are connected. Now we wait for the game to start
            var gameStartInfo = await gameStartSignal;
            delayedWaitingForOtherPlayersDialogSignal.PromptIsStillNeeded = false;
            await waitingForOtherPlayersDialogTask;
            await delayedWaitingForOtherPlayersDialogSignal.Cleanup();
            await RevealPlayers(gameStartInfo);
            QueueAction(()=>ScenePanel.IsVisible = true);
            Sound.Play("Reload");
            var bgMusicHandle = await Sound.Play("BackgroundMusic").AsAwaitable();
            gameLifetime.OnDisposed(bgMusicHandle.Dispose);
            // wait for the game to end
            var gameOverMessage = await client.EventRouter.GetAwaitable<GameOverMessage>();
            ShowWinnerAndCleanup(gameOverMessage);
        }
         

        private void OnMyClientDisconnected(Exception ex)
        {
            QueueAction(() =>
            {
                ScenePanel.IsVisible = false;
                Dialog.ShowMessage("Disconnected from server").Then(Cleanup);
            });
        }

        private void ShowWinnerAndCleanup(GameOverMessage message)
        {
            QueueAction(() =>
            {
                ScenePanel.IsVisible = false;
                Dialog.ShowMessage("And the winner is: ".ToConsoleString() + message.WinnerDisplayName.ToGreen()).Then(Cleanup);
            });
        }

        private async Task<ServerInfo> CollectServerInfoFromUser()
        {
            var serverInfo = new ServerInfo { Server = "192.168.1.16", Port = 8080 };
            Task dialogTask = null;
            await QueueAction(() =>
            {
                var panel = new ConsolePanel() { Height = 10 };
                var form = panel.Add(new Form(FormOptions.FromObject(serverInfo))).Fill(padding: new Thickness(1, 1, 1, 2));
                var okButton = panel.Add(new Button() { X = 1, Text = "OK".ToConsoleString(), Shortcut = new KeyboardShortcut(ConsoleKey.Enter) }).DockToBottom(padding: 1);
                
                okButton.Pressed.SubscribeOnce(Dialog.Dismiss);
                dialogTask = Dialog.Show(new ControlDialogOptions() { Content = panel, MaxHeight = 8 }).AsAwaitable();
            }).AsAwaitable();
            await dialogTask;
            return serverInfo;
        }

        private Task RevealPlayers(StartGameMessage message)
        {
            MainCharacter.TrySendBounds();
            return Scene.QueueAction(() =>
            {
                MainCharacter.ResizeTo(1, 1);
                foreach(var opponent in opponents)
                {
                    opponent.ResizeTo(1, 1);
                }

            }).AsAwaitable();
        }

        private MultiPlayerClient InitializeClient()
        {
            var client = new MultiPlayerClient(new SocketClientNetworkProvider());
            client.Disconnected.SubscribeOnce(OnMyClientDisconnected);
            client.EventRouter.Register<NewUserMessage>(OnOpponentArrived, gameLifetime);
            client.EventRouter.Register<LeftMessage>(OnOpponentLeft, gameLifetime);
            client.EventRouter.Register<BoundsMessage>(OnBoundsReceived, this);
            MainCharacter.MultiPlayerClient = client;
            return client;
        }

        private void OnOpponentArrived(NewUserMessage opponentInfo)
        {
            this.Scene.QueueAction(() =>
            {
                var opponent = new RemoteCharacter(client, opponentInfo.NewUserId);
                opponent.ResizeTo(0, 0);
                opponent.Tags.Add("enemy");
                this.Scene.Add(opponent);
                opponents.Add(opponent);
            });
        }

        private void OnOpponentLeft(LeftMessage opponentInfo)
        {
            this.Scene.QueueAction(() =>
            {
                var opponent = opponents.Where(o => o.RemoteClientId == opponentInfo.ClientWhoLeft).Single();
                opponent.IsConnected = false;
            });
        }

        private async Task ShowConnectingDialogDelayed(ServerInfo info, DelayedWaitingPrompt signal)
        {
            await Task.Delay(signal.Delay);

            if(signal.PromptIsStillNeeded == false)
            {
                return;
            }
            
            await QueueAction(() =>
            {
                Dialog.Show(new ControlDialogOptions()
                {
                    Content = new Label() { Text = $"Connecting to {info.Server}:{info.Port}".ToCyan() },
                    MaxHeight = 4
                });
                signal.Dialog = Dialog.Current;
            }).AsAwaitable();
        }

        private async Task ShowWaitingMessage(ConsoleString message, DelayedWaitingPrompt signal)
        {
            await Task.Delay(signal.Delay);

            if (signal.PromptIsStillNeeded == false)
            {
                return;
            }

            await QueueAction(() =>
            {
                Dialog.Show(new ControlDialogOptions()
                {
                    Content = new Label() { Text = message },
                    MaxHeight = 4
                });
                signal.Dialog = Dialog.Current;
            }).AsAwaitable();
        }

        private void OnBoundsReceived(BoundsMessage m)
        {
            Scene.QueueAction(() =>
            {
                if (m.ClientToUpdate == client.ClientId)
                {
                    MainCharacter.MoveTo(m.X, m.Y);
                    MainCharacter.Speed.SpeedX = m.SpeedX;
                    MainCharacter.Speed.SpeedY = m.SpeedY;
                }
                else
                {
                    var opponent = opponents.WhereAs<RemoteCharacter>().Where(o => o.RemoteClientId == m.ClientToUpdate).Single();
                    if (opponent != null)
                    {
                        Scene.QueueAction(() =>
                        {
                            opponent.MoveTo(m.X, m.Y);
                            opponent.Speed.SpeedX = m.SpeedX;
                            opponent.Speed.SpeedY = m.SpeedY;
                        });
                    }
                }
            });
        }

        private async Task<bool> IsServerPrompt()
        {
            Promise<DialogOption> dialogPromise = null;
            await QueueAction(() =>
            {
                dialogPromise = Dialog.ShowMessage(new DialogButtonOptions()
                {
                    Message = "Choose your adventure".ToConsoleString(),
                    AllowEscapeToCancel = false,
                    MaxHeight = 8,
                    Options = new List<DialogOption>()
                    {
                        new DialogOption(){ DisplayText = "Start a server".ToConsoleString() },
                        new DialogOption(){ DisplayText = "Connect to server".ToConsoleString() },
                    }
                });
            }).AsAwaitable();

            return (await dialogPromise.AsAwaitable()).DisplayText.ToString() == "Start a server";
        }

        private BoundsMessage CreateLocationMessage(string clientId, SpacialElement element) => new BoundsMessage()
        {
            X = element.Left,
            Y = element.Top,
            W = element.Width,
            H = element.Height,
            ClientToUpdate = clientId
        };

        private void Cleanup()
        {
            Scene.Stop();
            ConsoleApp.Current.Stop();
            Program.Main(new string[0]);
        }
    }

    public class DelayedWaitingPrompt
    {
        public Dialog Dialog { get; set; }
        public TimeSpan Delay { get; set; } = TimeSpan.FromMilliseconds(1000);
        public bool PromptIsStillNeeded { get; set; } = true;

        public async Task Cleanup()
        {
            if (Dialog != null && Dialog.Application != null)
            {
                await Dialog.Application.QueueAction(() =>
                {
                    Dialog.Application.LayoutRoot.Controls.Remove(Dialog);
                }).AsAwaitable();
            }
        }
    }
}
