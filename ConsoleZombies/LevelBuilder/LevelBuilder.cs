using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleZombies
{
    public class LevelBuilder : ConsoleApp
    {
        public Cursor Cursor { get; private set; }
        public Scene PreviewScene
        {
            get
            {
                return ScenePanel.Scene;
            }
        }
        public LevelDefinition CurrentLevelDefinition { get; set; }

        public string LevelId { get; set; }

        public ScenePanel ScenePanel { get; private set; }

        public UndoRedoStack UndoStack { get; private set; }

        public ConsoleCharacter WallPen
        {
            get; private set;
        } =  new Wall().Texture;

        public float WallPenHP = 10;

        public LevelBuilder()
        {
            Cursor = new Cursor();
            UndoStack = new UndoRedoStack();
            var topPanel = LayoutRoot.Add(new ConsolePanel() { Background = System.ConsoleColor.Black }).Fill(padding: new Thickness(0, 0, 0, 6));
            var botPanel = LayoutRoot.Add(new ConsolePanel() { Height = 6, Background = System.ConsoleColor.DarkRed }).DockToBottom().FillHoriontally();

            var borderPanel = topPanel.Add(new ConsolePanel() { Background = ConsoleColor.DarkGray, Width = LevelDefinition.Width + 2, Height = LevelDefinition.Height + 2 }).CenterHorizontally().CenterVertically();
            ScenePanel = borderPanel.Add(new ScenePanel(LevelDefinition.Width, LevelDefinition.Height)).Fill(padding: new Thickness(1, 1, 1, 1));

            var sceneFPSLabel = LayoutRoot.Add(new Label() { Text = "".ToConsoleString() }).FillHoriontally();
            var renderFPSLabel = LayoutRoot.Add(new Label() { Y = 1, Text = "".ToConsoleString() }).FillHoriontally();
            var paintFPSLabel = LayoutRoot.Add(new Label() { Y = 2, Text = "".ToConsoleString() }).FillHoriontally();

            LifetimeManager.Manage(SetInterval(() =>
            {
                sceneFPSLabel.Text = $"{ScenePanel.Scene.FPS} scene frames per second".ToCyan();
                renderFPSLabel.Text = $"{FPS} render frames per second".ToCyan();
                paintFPSLabel.Text = $"{PPS} paint frames per second".ToCyan();
            }, TimeSpan.FromSeconds(1)));


           
            QueueAction(() =>
            {
                if (this.LevelId != null)
                {
                    LoadLevel(this.LevelId);
                }
                else
                {
                    CurrentLevelDefinition = new LevelDefinition();
                }

                PreviewScene.Start();
                SetupCursorKeyInput();
                SetupDropKeyInput();
                SetupSaveKeyInput();
            });

            PreviewScene.QueueAction(() =>
            {
                Cursor.Bounds.Resize(ScenePanel.PixelSize);
                PreviewScene.Add(Cursor);
            });
        }

        private void SetupSaveKeyInput()
        {
            this.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.S, null, () =>
            {
                Dialog.ShowRichTextInput("Name this level".ToYellow(), (result) =>
                {
                    CurrentLevelDefinition.Save(result.ToString());
                }, initialValue: LevelId?.ToConsoleString());

            }, LifetimeManager);

            this.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.L, null, () =>
            {
                var options = LevelDefinition.GetLevelDefinitionFiles().Select(f => new DialogOption() { DisplayText = System.IO.Path.GetFileNameWithoutExtension(f).ToYellow(), Id = f });

                if (options.Count() == 0)
                {
                    Dialog.ShowMessage("No levels to load");
                }
                else
                {
                    Dialog.Pick("Choose a level to load".ToYellow(), options).Then((button) =>
                    {
                        var levelFile = button.Id;
                        LoadLevel(levelFile);
                    });
                }
            }, LifetimeManager);
        }

        private void LoadLevel(string levelFile)
        {
            var level = LevelDefinition.Load(levelFile);
            this.CurrentLevelDefinition = level;

            PreviewScene.QueueAction(() =>
            {
                foreach (var thing in PreviewScene.Things.ToArray())
                {
                    if (thing is Cursor)
                    {
                        continue;
                    }
                    PreviewScene.Remove(thing);
                }

                foreach (var interaction in PreviewScene.Interactions.ToArray())
                {
                    PreviewScene.Remove(interaction);
                }

                level.Hydrate(PreviewScene, true);
            });
        }

        private void SetupCursorKeyInput()
        {
            BrokerToScene(ConsoleKey.UpArrow, () => { SceneHelpers.MoveThingSafeBy(PreviewScene, Cursor, 0, -ScenePanel.PixelSize.H); });
            BrokerToScene(ConsoleKey.DownArrow, () => { SceneHelpers.MoveThingSafeBy(PreviewScene, Cursor, 0, ScenePanel.PixelSize.H); });
            BrokerToScene(ConsoleKey.LeftArrow, () => { SceneHelpers.MoveThingSafeBy(PreviewScene, Cursor, -ScenePanel.PixelSize.W, 0); });
            BrokerToScene(ConsoleKey.RightArrow, () => { SceneHelpers.MoveThingSafeBy(PreviewScene, Cursor, ScenePanel.PixelSize.W, 0); });
        }

        private void SetupDropKeyInput()
        {
            BrokerToScene(ConsoleKey.W, () => { UndoStack.Do(new DropWallAction() { Context = this }); });
            BrokerToScene(ConsoleKey.C, () => { UndoStack.Do(new DropAutoCeilingAction() { Context = this }); });
            BrokerToScene(ConsoleKey.T, () => { UndoStack.Do(new DropTurretAction() { Context = this }); });

            BrokerToScene(ConsoleKey.M, () => { UndoStack.Do(new DropMainCharacterAction() { Context = this }); });

            BrokerToScene(ConsoleKey.Z, () => { UndoStack.Do(new DropZombieAction() { Context = this }); });
            PushAppHandler(ConsoleKey.A, () => { UndoStack.Do(new DropAmmoAction() { Context = this }); });
            PushAppHandler(ConsoleKey.P, () => { UndoStack.Do(new DropPortalAction() { Context = this }); });
            BrokerToScene(ConsoleKey.D, () =>
        {
            if (PositionDoorAction.IsReadyForDrop(this) == false)
            {
                UndoStack.Do(new PositionDoorAction(false) { Context = this });
            }
            else
            {
                UndoStack.Do(new DropDoorAction() { Context = this });
            }
        });

            BrokerToScene(ConsoleKey.D, () =>
            {
                if (PositionDoorAction.IsReadyForDrop(this) == false)
                {
                    UndoStack.Do(new PositionDoorAction(true) { Context = this });
                }
                else
                {
                    UndoStack.Do(new DropDoorAction() { Context = this });
                }
            }, ConsoleModifiers.Shift);


            BrokerToScene(ConsoleKey.Delete, () => { UndoStack.Do(new DeleteAction() { Context = this }); });

            BrokerToScene(ConsoleKey.U, () => { UndoStack.Undo(); });
            BrokerToScene(ConsoleKey.R, () => { UndoStack.Redo(); });

            PushAppHandler(ConsoleKey.T, () =>
            {
                Dialog.PickFromEnum<ConsoleColor>("Choose a background color".ToConsoleString())
                .Then((val) =>
                {
                    if (val.HasValue == false) return;

                    this.WallPen = new ConsoleCharacter(this.WallPen.Value, this.WallPen.ForegroundColor, val.Value);
                }).Then((val) =>
                {
                    Dialog.ShowTextInput("Pick a character".ToConsoleString(), (result) =>
                    {
                        this.WallPen = new ConsoleCharacter(result.Length == 0 ? ' ' : result[0].Value, WallPen.ForegroundColor, WallPen.BackgroundColor);

                        Dialog.ShowTextInput("Pick HP".ToConsoleString(), (hpResult) =>
                        {
                            float hpVal;
                            if (float.TryParse(hpResult.ToString(), out hpVal) == false)
                            {
                                hpVal = 10;
                            }
                            this.WallPenHP = hpVal;
                        });
                    });
                });
            });
        }

        private void PushAppHandler(ConsoleKey key, Action a, ConsoleModifiers? modifiers = null)
        {
            FocusManager.GlobalKeyHandlers.PushForLifetime(key, modifiers, () =>
            {
                a();
            }, LifetimeManager);
        }

        private void BrokerToScene(ConsoleKey key, Action a, ConsoleModifiers? modifiers = null)
        {
            FocusManager.GlobalKeyHandlers.PushForLifetime(key, modifiers, () =>
            {
                PreviewScene.QueueAction(a);
            }, LifetimeManager);
        }
    }
}
