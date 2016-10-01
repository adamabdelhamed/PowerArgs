using System;
using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System.Linq;
using System.Reflection;

namespace ConsoleZombies
{
    public class PathBuilderSession : Lifetime
    {
        public string PathId { get; set; }
        public int NextIndex { get; set; }
    }

    public class LevelBuilder
    {
        public LevelDefinition LevelDefinition { get; set; } = new LevelDefinition() { Width = 80, Height = 20 };

        public Cursor Cursor { get; private set; } = new Cursor();

        private RealmPanel RealmPanel { get; set; }

        private PathBuilderSession PathBuilderLifetime { get; set; }

        public void Run()
        {
            var app = new ConsoleApp();

            var topPanel = app.LayoutRoot.Add(new ConsolePanel() { Background = System.ConsoleColor.Black }).Fill(padding: new Thickness(0, 0, 0, 6));
            var botPanel = app.LayoutRoot.Add(new ConsolePanel() { Height = 6, Background = System.ConsoleColor.DarkRed }).DockToBottom().FillHoriontally();

            RealmPanel = topPanel.Add(new RealmPanel((int)LevelDefinition.Width, (int)LevelDefinition.Height)).FillAndPreserveAspectRatio();
            app.QueueAction(() =>
            {
                RealmPanel.RenderLoop.Start();
                SetupCursorKeyInput();
                SetupDropKeyInput();
                SetupSaveKeyInput();
            });

            RealmPanel.RenderLoop.QueueAction(() =>
            {
                Cursor.Bounds.Resize(RealmPanel.PixelSize);
                RealmPanel.RenderLoop.Realm.Add(Cursor);
            });

            var appTask = app.Start();
            appTask.Wait();
            return;
        }

        private void SetupSaveKeyInput()
        {
            ConsoleApp.Current.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.S, null, () =>
            {
                Dialog.ShowRichTextInput("Name this level".ToYellow(), (result) =>
                 {
                     LevelDefinition.Save(result.ToString());
                 });

            }, ConsoleApp.Current.LifetimeManager);

            ConsoleApp.Current.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.L, null, () =>
            {
                var options = LevelDefinition.GetLevelDefinitionFiles().Select(f => new DialogButton() { DisplayText = System.IO.Path.GetFileNameWithoutExtension(f).ToYellow(), Id = f });

                if (options.Count() == 0)
                {
                    Dialog.ShowMessage("No levels to load");
                }
                else
                {
                    Dialog.ShowMessage("Choose a level to load".ToYellow(), (button) =>
                    {
                        var levelFile = button.Id;

                        var level = LevelDefinition.Load(levelFile);
                        this.LevelDefinition = level;

                        RealmPanel.RenderLoop.QueueAction(() =>
                        {
                            foreach (var thing in RealmPanel.RenderLoop.Realm.Things.ToArray())
                            {
                                if(thing is Cursor)
                                {
                                    continue;
                                }
                                RealmPanel.RenderLoop.Realm.Remove(thing);
                            }

                            foreach (var interaction in RealmPanel.RenderLoop.Realm.Interactions.ToArray())
                            {
                                RealmPanel.RenderLoop.Realm.Remove(interaction);
                            }

                            level.Populate(RealmPanel.RenderLoop.Realm, true);
                        });


                    }, buttons: options.ToArray(), maxHeight: RealmPanel.Application.LayoutRoot.Height);
                }
            }, ConsoleApp.Current.LifetimeManager);
        }

        private void SetupCursorKeyInput()
        {
            BrokerKeyAction(ConsoleKey.UpArrow, () => { RealmHelpers.MoveThingSafeBy(RealmPanel.RenderLoop.Realm, Cursor, 0, -RealmPanel.PixelSize.H); });
            BrokerKeyAction(ConsoleKey.DownArrow, () => { RealmHelpers.MoveThingSafeBy(RealmPanel.RenderLoop.Realm, Cursor, 0, RealmPanel.PixelSize.H); });
            BrokerKeyAction(ConsoleKey.LeftArrow, () => { RealmHelpers.MoveThingSafeBy(RealmPanel.RenderLoop.Realm, Cursor, -RealmPanel.PixelSize.W, 0); });
            BrokerKeyAction(ConsoleKey.RightArrow, () => { RealmHelpers.MoveThingSafeBy(RealmPanel.RenderLoop.Realm, Cursor, RealmPanel.PixelSize.W, 0); });
        }

        private void SetupDropKeyInput()
        {
            BrokerKeyAction(ConsoleKey.Z, () => 
            {
                LevelDefinition.Add(new ThingDefinition()
                {
                    ThingType = typeof(Zombie).FullName,
                    InitialBounds = new PowerArgs.Cli.Physics.Rectangle(Cursor.Left,Cursor.Top,Cursor.Bounds.W,Cursor.Bounds.H),
                });

                var zombie = new Zombie() { Bounds = new PowerArgs.Cli.Physics.Rectangle(Cursor.Left, Cursor.Top, Cursor.Bounds.W, Cursor.Bounds.H) };
                RealmPanel.RenderLoop.Realm.Add(zombie);
            });

            BrokerKeyAction(ConsoleKey.N, () =>
            {
                if (PathBuilderLifetime != null)
                {
                    PathBuilderLifetime.Dispose();
                }

                PathBuilderLifetime = new PathBuilderSession() { PathId = Guid.NewGuid().ToString(), NextIndex = 0 };

                LevelDefinition.Add(new ThingDefinition()
                {
                    ThingType = typeof(PathElement).FullName,
                    InitialBounds = new PowerArgs.Cli.Physics.Rectangle(Cursor.Left, Cursor.Top, Cursor.Bounds.W, Cursor.Bounds.H),
                    InitialData = { { "PathId", PathBuilderLifetime.PathId }, { "Index", (PathBuilderLifetime.NextIndex++) + "" } }
                });

                var pathElement = new PathElement() { Bounds = new PowerArgs.Cli.Physics.Rectangle(Cursor.Left, Cursor.Top, Cursor.Bounds.W, Cursor.Bounds.H) };
                RealmPanel.RenderLoop.Realm.Add(pathElement);
                pathElement.IsHighlighted = true;
            });

            BrokerKeyAction(ConsoleKey.P, () =>
            {
                if (PathBuilderLifetime == null)
                {
                    PathBuilderLifetime = new PathBuilderSession() { PathId = Guid.NewGuid().ToString(),NextIndex = 0 };
                }

                LevelDefinition.Add(new ThingDefinition()
                {
                    ThingType = typeof(PathElement).FullName,
                    InitialBounds = new PowerArgs.Cli.Physics.Rectangle(Cursor.Left, Cursor.Top, Cursor.Bounds.W, Cursor.Bounds.H),
                    InitialData = { { "PathId", PathBuilderLifetime.PathId }, { "Index", (PathBuilderLifetime.NextIndex++)+"" } }
                });

                var pathElement = new PathElement() { Bounds = new PowerArgs.Cli.Physics.Rectangle(Cursor.Left, Cursor.Top, Cursor.Bounds.W, Cursor.Bounds.H) };
                RealmPanel.RenderLoop.Realm.Add(pathElement);
                pathElement.IsHighlighted = true;
            });

            BrokerKeyAction(ConsoleKey.W, () =>
            {
                LevelDefinition.Add(new ThingDefinition()
                {
                    ThingType = typeof(Wall).FullName,
                    InitialBounds = new PowerArgs.Cli.Physics.Rectangle(Cursor.Left, Cursor.Top, Cursor.Bounds.W, Cursor.Bounds.H),
                });

                var wall = new Wall() { Bounds = new PowerArgs.Cli.Physics.Rectangle(Cursor.Left, Cursor.Top, Cursor.Bounds.W, Cursor.Bounds.H) };
                RealmPanel.RenderLoop.Realm.Add(wall);
            });

            BrokerKeyAction(ConsoleKey.M, () =>
            {
                if(LevelDefinition.Where(item => item.ThingType == typeof(MainCharacter).FullName).Count() > 0)
                {
                    return;
                }
                LevelDefinition.Add(new ThingDefinition()
                {
                    ThingType = typeof(MainCharacter).FullName,
                    InitialBounds = new PowerArgs.Cli.Physics.Rectangle(Cursor.Left, Cursor.Top, Cursor.Bounds.W, Cursor.Bounds.H),
                });

                var character = new MainCharacter() { IsInLevelBuilder = true, Bounds = new PowerArgs.Cli.Physics.Rectangle(Cursor.Left, Cursor.Top, Cursor.Bounds.W, Cursor.Bounds.H) };
                RealmPanel.RenderLoop.Realm.Add(character);
            });
        }

        private void BrokerKeyAction(ConsoleKey key, Action a)
        {
            ConsoleApp.Current.FocusManager.GlobalKeyHandlers.PushForLifetime(key, null, () =>
            {
                RealmPanel.RenderLoop.QueueAction(() =>
                {
                    a();
                });
            }, ConsoleApp.Current.LifetimeManager);
        }
    }
}
