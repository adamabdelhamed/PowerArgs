using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
namespace PowerArgs.Games
{
    public interface IGameAppAware
    {
        GameApp GameApp { get; set; }
    }

    public abstract class GameApp : ConsoleApp
    {
        public static GameApp CurrentGameApp => Current as GameApp;
        public Event Paused { get; private set; } = new Event();
        public Event Resumed { get; private set; } = new Event();
        public SpaceTime Scene => ScenePanel.SpaceTime;
        public KeyboardInputManager KeyboardInput { get; private set; } 
        public Theme Theme { get => Get<Theme>(); set => Set(value); }
        public MainCharacter MainCharacter { get { return Get<MainCharacter>(); } private set { Set(value); } }

        private ConsolePanel disposableRoot;
        private SpacetimePanel ScenePanel { get; set; }

        public abstract Dictionary<string,Level> Levels { get; }

        /// <summary>
        /// Creates the app
        /// </summary>
        public GameApp()
        {
            this.SubscribeForLifetime(nameof(Theme), () => Theme.Bind(this), this);
            Theme = new DefaultTheme();
            this.QueueActionInFront(()=>
            {
                KeyboardInput = new KeyboardInputManager(this);
                ConfigureQuitOnEscapeKey();
            });
            Levels.ForEach(l => l.Value.Name = l.Key);
        }

        /// <summary>
        /// Set the factory that will be used to hydrate levels
        /// </summary>
        protected abstract SceneFactory SceneFactory { get; }


        /// <summary>
        /// This is called at the end of every level load. It is a good time to apply game state. This is called from the SpaceTime
        /// thread so you can safely interact with the game elements.
        /// </summary>
        /// <param name="l"></param>
        protected virtual void AfterLevelLoaded(Level l) { }

        /// <summary>
        /// This is called just before levels are unloaded. It is a good time to store
        /// your game state. This is called from the SpaceTime thread so you can safely 
        /// interact with the game elements.
        /// </summary>
        protected virtual void BeforeLevelUnloaded() { }

        public void Load(string levelName)
        {
            var level = Levels[levelName];
            Promise cleanupPromise = null;
            if (ScenePanel != null)
            {
                cleanupPromise = Scene.QueueAction(() =>
                {
                    BeforeLevelUnloaded();
                    foreach (var element in Scene.Elements)
                    {
                        element.Lifetime.Dispose();
                    }
                });
            }
            else
            {
                var d = Deferred.Create();
                cleanupPromise = d.Promise;
                d.Resolve();
            }

            cleanupPromise
                .Then(() => QueueAction(() => InitializeScene(level.Width, level.Height))
                .Then(() => { Scene.QueueAction(() =>
                {
                    foreach (var item in SceneFactory.InitializeScene(level).OrderBy(i => i is MainCharacter ? 0 : i is Ceiling == false ? 1 : 2))
                    {
                        if (item is IGameAppAware)
                        {
                            (item as IGameAppAware).GameApp = this;
                        }

                        if (item is MainCharacter)
                        {
                            this.MainCharacter = item as MainCharacter;
                        }

                        this.Scene.Add(item);
                    }
                    AfterLevelLoaded(level);
                });
            }));
        }
 

        public void Pause(bool showPauseDialog)
        {
            if(Scene.IsRunning)
            {
                Scene.Stop();
                Paused.Fire();
                if (showPauseDialog)
                {
                    QueueAction(() => Dialog.ShowMessage("Game paused", Resume));
                }
            }
        }

        public void Resume()
        {
            if(Scene.IsRunning == false)
            {
                ScenePanel.RealTimeViewing.ReSync();
                Scene.Start();
                Resumed.Fire();
            }
        }

        private void InitializeScene(int w, int h)
        {
            LayoutRoot.Controls.Remove(disposableRoot);
            disposableRoot = LayoutRoot.Add(new ConsolePanel() { Background = ConsoleColor.DarkGray, Width = w + 2, Height = h + 2 }).CenterHorizontally().CenterVertically();
            ScenePanel = disposableRoot.Add(new SpacetimePanel(w, h)).Fill(padding: new Thickness(1, 1, 1, 1));
            ScenePanel.Background = ConsoleColor.Black;
            Scene.Start();
            LayoutRoot.Add(new FramerateControl(ScenePanel));
        }

        private void ConfigureQuitOnEscapeKey()
        {
            this.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.Escape, null, () =>
            {
                Pause(false);
                Dialog.ConfirmYesOrNo("Are you sure you want to quit?", Stop, () =>
                {
                    Resume();
                });
            }, this);
        }
    }
}
