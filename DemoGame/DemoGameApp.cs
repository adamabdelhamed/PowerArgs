using PowerArgs.Games;
using System;
using System.Collections.Generic;
using PowerArgs.Cli;
using PowerArgs;

namespace DemoGame
{
    public class DemoGameApp : GameApp
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
            new WaypointReviver(),
            new CeilingReviver(),
            new DoorReviver(),
            new WallReviver()
        });

        private ShooterKeys shooterKeys;
        private IDisposable bgMusicHandle;
        private Inventory lastLevelInventory;

        private Dictionary<string, Level> levels = new Dictionary<string, Level>()
        {
            { "IntroCutScene", GameIntro.Level },
          };

        public override Dictionary<string, Level> Levels => levels;
       
        public DemoGameApp()
        {
            this.RequiredWidth = 102;
            this.RequiredHeight = 45;
            QueueAction(Initialize);
        }

        protected void Initialize()
        {
            this.RequiredSizeMet.SubscribeOnce(() =>
            {
                var introPanel = new PowerArgsGamesIntro();
                var frameRateControl = LayoutRoot.Add(new FramerateControl(introPanel) { ZIndex = 100 });
                LayoutRoot.Add(introPanel).Play().Then(() => {
                    QueueAction(() =>
                    {
                        LayoutRoot.Controls.Remove(frameRateControl);
                        this.shooterKeys = new ShooterKeys(() => this.Scene);
                        this.KeyboardInput.KeyMap = this.shooterKeys.ToKeyMap();
                        EnableThemeToggling();
                        LayoutRoot.Add(new HeadsUpDisplay(this, shooterKeys)).CenterHorizontally().DockToBottom();

                        this.Load("IntroCutScene");

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
                            if(requiredSizeCausedPause)
                            {
                                this.Resume();
                            }

                        }, this);

                        Sound.Loop("bgmusicmain").Then(d => bgMusicHandle = d);

                        this.Paused.SubscribeForLifetime(() =>
                        {
                            bgMusicHandle?.Dispose();
                            bgMusicHandle = null;
                        }, this);

                        this.Resumed.SubscribeForLifetime(() =>
                        {
                            Sound.Loop("bgmusicmain").Then(d => bgMusicHandle = d);
                        }, this);
                    });
                });
            });
        }


        protected override void AfterLevelLoaded(Level l)
        {
            if(l.Name == null)
            {
                throw new ArgumentNullException("level.Name cannot be null");
            }

            if(lastLevelInventory != null)
            {
                lastLevelInventory.Owner = MainCharacter.Current;
                MainCharacter.Current.Inventory = lastLevelInventory;
            }
             
            MainCharacter.Current.Destroyed.SubscribeOnce(() =>
            {
                QueueAction(() =>
                {
                    Sound.Play("gameover");
                    Dialog.ShowMessage("Game over".ToRed()).Then(()=>  Load("DefaultLevel"));
                });
            });
        }

        protected override void BeforeLevelUnloaded()
        {
            lastLevelInventory = MainCharacter.Current?.Inventory;
            
        }

        private void EnableThemeToggling()
        {
            this.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.T, null, () =>
            {
                if (Theme is DefaultTheme)
                {
                    Theme = new DarkTheme();
                }
                else
                {
                    Theme = new DefaultTheme();
                }
            }, this);
        }
    }
}
