using System;
using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System.Linq;
using System.Reflection;

namespace ConsoleZombies
{
    public class LevelBuilder
    {
        public LevelDefinition LevelDefinition { get; set; } = new LevelDefinition();

        public Cursor Cursor { get; private set; } = new Cursor();

        private RealmPanel RealmPanel { get; set; }

        private PowerArgs.Cli.Physics.Rectangle doorDropRectangle;

        public void Run()
        {
            var app = new ConsoleApp();

            var topPanel = app.LayoutRoot.Add(new ConsolePanel() { Background = System.ConsoleColor.Black }).Fill(padding: new Thickness(0, 0, 0, 6));
            var botPanel = app.LayoutRoot.Add(new ConsolePanel() { Height = 6, Background = System.ConsoleColor.DarkRed }).DockToBottom().FillHoriontally();

            var borderPanel = topPanel.Add(new ConsolePanel() { Background = ConsoleColor.DarkGray, Width = LevelDefinition.Width + 2, Height = LevelDefinition.Height + 2 }).CenterHorizontally().CenterVertically();
            RealmPanel = borderPanel.Add(new RealmPanel(LevelDefinition.Width, LevelDefinition.Height)).Fill(padding: new Thickness(1, 1, 1, 1));


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
                var paddedBounds = Cursor.Bounds.Clone();
                paddedBounds.Pad(.1f);

                LevelDefinition.Add(new ThingDefinition()
                {
                    ThingType = typeof(Zombie).FullName,
                    InitialBounds = paddedBounds.Clone(),
                });

                var zombie = new Zombie() { Bounds = paddedBounds.Clone() };
                RealmPanel.RenderLoop.Realm.Add(zombie);
            });

            BrokerKeyAction(ConsoleKey.D, () =>
            {
                if(doorDropRectangle == null)
                {
                    doorDropRectangle = Cursor.Bounds.Clone();
                    doorDropRectangle.Pad(.1f);
                }
                else
                {
                    var paddedBounds = Cursor.Bounds.Clone();
                    paddedBounds.Pad(.1f);

                    LevelDefinition.Add(new ThingDefinition()
                    {
                        ThingType = typeof(Door).FullName,
                        InitialBounds = doorDropRectangle,
                        InitialData =
                        {
                            { "ClosedX", doorDropRectangle.X+"" },
                            { "ClosedY", doorDropRectangle.Y + "" },
                            { "W", doorDropRectangle.W + "" },
                            { "H", doorDropRectangle.H + "" },
                            { "OpenX", paddedBounds.X+"" },
                            { "OpenY", paddedBounds.Y+"" },
                        }
                    });

                    var door = new Door(doorDropRectangle, Cursor.Bounds.Clone().Location);
                    RealmPanel.RenderLoop.Realm.Add(door);
                    doorDropRectangle = null;
                }

            });


            BrokerKeyAction(ConsoleKey.W, () =>
            {
                LevelDefinition.Add(new ThingDefinition()
                {
                    ThingType = typeof(Wall).FullName,
                    InitialBounds = new PowerArgs.Cli.Physics.Rectangle(Cursor.Left, Cursor.Top, Cursor.Bounds.W, Cursor.Bounds.H),
                });

                var wall = new Wall() { Bounds = Cursor.Bounds.Clone() };
                RealmPanel.RenderLoop.Realm.Add(wall);
            });

            BrokerKeyAction(ConsoleKey.M, () =>
            {
                var paddedBounds = Cursor.Bounds.Clone();
                paddedBounds.Pad(.1f);

                if (LevelDefinition.Where(item => item.ThingType == typeof(MainCharacter).FullName).Count() > 0)
                {
                    return;
                }
                LevelDefinition.Add(new ThingDefinition()
                {
                    ThingType = typeof(MainCharacter).FullName,
                    InitialBounds = paddedBounds.Clone(),
                });

                var character = new MainCharacter() { IsInLevelBuilder = true, Bounds = paddedBounds.Clone() };
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
