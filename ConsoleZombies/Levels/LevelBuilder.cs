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

        private ScenePanel ScenePanel { get; set; }

        private PowerArgs.Cli.Physics.Rectangle doorDropRectangle;

        public string LevelId { get; private set; }

        public LevelBuilder(string levelId = null)
        {
            this.LevelId = levelId;
        }

        public void Run()
        {
            var app = new ConsoleApp();

            var topPanel = app.LayoutRoot.Add(new ConsolePanel() { Background = System.ConsoleColor.Black }).Fill(padding: new Thickness(0, 0, 0, 6));
            var botPanel = app.LayoutRoot.Add(new ConsolePanel() { Height = 6, Background = System.ConsoleColor.DarkRed }).DockToBottom().FillHoriontally();

            var borderPanel = topPanel.Add(new ConsolePanel() { Background = ConsoleColor.DarkGray, Width = LevelDefinition.Width + 2, Height = LevelDefinition.Height + 2 }).CenterHorizontally().CenterVertically();
            ScenePanel = borderPanel.Add(new ScenePanel(LevelDefinition.Width, LevelDefinition.Height)).Fill(padding: new Thickness(1, 1, 1, 1));

            var sceneFPSLabel = app.LayoutRoot.Add(new Label() { Text = "".ToConsoleString() }).FillHoriontally();
            var renderFPSLabel = app.LayoutRoot.Add(new Label() { Y = 1, Text = "".ToConsoleString() }).FillHoriontally();
            var paintFPSLabel = app.LayoutRoot.Add(new Label() { Y = 2, Text = "".ToConsoleString() }).FillHoriontally();

            app.LifetimeManager.Manage(app.SetInterval(() =>
            {
                sceneFPSLabel.Text = $"{ScenePanel.Scene.FPS} scene frames per second".ToCyan();
                renderFPSLabel.Text = $"{app.FPS} render frames per second".ToCyan();
                paintFPSLabel.Text = $"{app.PPS} paint frames per second".ToCyan();
            }, TimeSpan.FromSeconds(1)));

            app.QueueAction(() =>
            {
                ScenePanel.Scene.Start();
                SetupCursorKeyInput();
                SetupDropKeyInput();
                SetupSaveKeyInput();
            });

            ScenePanel.Scene.QueueAction(() =>
            {
                Cursor.Bounds.Resize(ScenePanel.PixelSize);
                ScenePanel.Scene.Add(Cursor);
            });

            if(this.LevelId != null)
            {
                LoadLevel(this.LevelId);
            }

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
                 }, initialValue: LevelId?.ToConsoleString());

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

                        LoadLevel(levelFile);

                    }, buttons: options.ToArray(), maxHeight: ScenePanel.Application.LayoutRoot.Height);
                }
            }, ConsoleApp.Current.LifetimeManager);
        }

        private void LoadLevel(string levelFile)
        {
            var level = LevelDefinition.Load(levelFile);
            this.LevelDefinition = level;

            ScenePanel.Scene.QueueAction(() =>
            {
                foreach (var thing in ScenePanel.Scene.Things.ToArray())
                {
                    if (thing is Cursor)
                    {
                        continue;
                    }
                    ScenePanel.Scene.Remove(thing);
                }

                foreach (var interaction in ScenePanel.Scene.Interactions.ToArray())
                {
                    ScenePanel.Scene.Remove(interaction);
                }

                level.Populate(ScenePanel.Scene, true);
            });
        }

        private void SetupCursorKeyInput()
        {
            BrokerKeyAction(ConsoleKey.UpArrow, () => { SceneHelpers.MoveThingSafeBy(ScenePanel.Scene, Cursor, 0, -ScenePanel.PixelSize.H); });
            BrokerKeyAction(ConsoleKey.DownArrow, () => { SceneHelpers.MoveThingSafeBy(ScenePanel.Scene, Cursor, 0, ScenePanel.PixelSize.H); });
            BrokerKeyAction(ConsoleKey.LeftArrow, () => { SceneHelpers.MoveThingSafeBy(ScenePanel.Scene, Cursor, -ScenePanel.PixelSize.W, 0); });
            BrokerKeyAction(ConsoleKey.RightArrow, () => { SceneHelpers.MoveThingSafeBy(ScenePanel.Scene, Cursor, ScenePanel.PixelSize.W, 0); });
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
                ScenePanel.Scene.Add(zombie);
            });

            BrokerKeyAction(ConsoleKey.Delete, () =>
            {
                var paddedBounds = Cursor.Bounds.Clone();

                var levelDefThingsToDelete = LevelDefinition.Where(t => t.InitialBounds.Hits(paddedBounds)).ToList();
                foreach(var element in levelDefThingsToDelete)
                {
                    LevelDefinition.Remove(element);
                }

                var previewThingsToDelete = ScenePanel.Scene.Things.Where(t => t is Cursor == false && t.Bounds.Hits(paddedBounds)).ToList();

                foreach (var element in previewThingsToDelete)
                {
                    ScenePanel.Scene.Remove(element);
                }
            });

            ConsoleApp.Current.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.A, null, () =>
            {
                Dialog.ShowMessage("Choose Ammo Type".ToConsoleString(), (choice) =>
                 {
                     ScenePanel.Scene.QueueAction(() =>
                     {
                         var paddedBounds = Cursor.Bounds.Clone();
                         paddedBounds.Pad(.1f);
                         int amount = 10;
                         LevelDefinition.Add(new ThingDefinition()
                         {
                             ThingType = choice.Id,
                             InitialBounds = paddedBounds.Clone(),
                             InitialData = { { "Amount", "" + amount } }
                         });

                         var ammoType = Assembly.GetExecutingAssembly().GetType(choice.Id);
                         var ammo = Activator.CreateInstance(ammoType) as Ammo;
                         ammo.Amount = amount;
                         ammo.Bounds = paddedBounds.Clone();
                         ScenePanel.Scene.Add(ammo);
                     });
                 }, buttons: new DialogButton[] 
                 {
                     new DialogButton() { DisplayText = "Pistol".ToConsoleString(), Id =  typeof(PistolAmmo).FullName },
                     new DialogButton() { DisplayText = "RPG".ToConsoleString(), Id =  typeof(RPGAmmo).FullName }
                 });
            }, ConsoleApp.Current.LifetimeManager);

            BrokerKeyAction(ConsoleKey.D, () =>
            {
                DropDoor(false);
            });

            BrokerKeyAction(ConsoleKey.D, () =>
            {
                DropDoor(true);
            }, ConsoleModifiers.Shift);

            BrokerKeyAction(ConsoleKey.W, () =>
            {
                LevelDefinition.Add(new ThingDefinition()
                {
                    ThingType = typeof(Wall).FullName,
                    InitialBounds = new PowerArgs.Cli.Physics.Rectangle(Cursor.Left, Cursor.Top, Cursor.Bounds.W, Cursor.Bounds.H),
                });

                var wall = new Wall() { Bounds = Cursor.Bounds.Clone() };
                ScenePanel.Scene.Add(wall);
            });

            ConsoleApp.Current.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.P, null, (key) =>
            {
                Dialog.ShowTextInput("Provide portal Id".ToYellow(), (id) =>
                 {
                     ScenePanel.Scene.QueueAction(() =>
                     {
                         LevelDefinition.Add(new ThingDefinition()
                         {
                             ThingType = typeof(Portal).FullName,
                             InitialBounds = new PowerArgs.Cli.Physics.Rectangle(Cursor.Left, Cursor.Top, Cursor.Bounds.W, Cursor.Bounds.H),
                             InitialData = new System.Collections.Generic.Dictionary<string, string>()
                             {
                                 { "DestinationId", id.ToString() }
                             }
                         });

                         var portal = new Portal() { Bounds = Cursor.Bounds.Clone() };
                         ScenePanel.Scene.Add(portal);
                     });
                 });
            }, ConsoleApp.Current.LifetimeManager);

            BrokerKeyAction(ConsoleKey.C, () =>
            {
                LevelDefinition.Add(new ThingDefinition()
                {
                    ThingType = typeof(Cieling).FullName,
                    InitialBounds = new PowerArgs.Cli.Physics.Rectangle(Cursor.Left, Cursor.Top, Cursor.Bounds.W, Cursor.Bounds.H),
                });

                var cieling = new Cieling() { Bounds = Cursor.Bounds.Clone() };
                ScenePanel.Scene.Add(cieling);
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
                ScenePanel.Scene.Add(character);
            });
        }

        private void DropDoor(bool open)
        {
            if (doorDropRectangle == null)
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
                            { "IsOpen", open+""   }
                        }
                });

                var door = new Door(doorDropRectangle, Cursor.Bounds.Clone().Location);
                door.IsOpen = open;
                ScenePanel.Scene.Add(door);
                doorDropRectangle = null;
            }
        }

        private void BrokerKeyAction(ConsoleKey key, Action a, ConsoleModifiers? modifiers = null)
        {
            ConsoleApp.Current.FocusManager.GlobalKeyHandlers.PushForLifetime(key, modifiers, () =>
            {
                ScenePanel.Scene.QueueAction(() =>
                {
                    a();
                });
            }, ConsoleApp.Current.LifetimeManager);
        }
    }
}
