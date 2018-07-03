using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;

namespace ConsoleGames
{
    public interface IGameAppAware
    {
        GameApp GameApp { get; set; }
    }

    public abstract class GameApp : ConsoleApp
    {
        public static GameApp CurrentGameApp => ConsoleApp.Current as GameApp;
        private SpacetimePanel ScenePanel { get; set; }
        public SpaceTime Scene => ScenePanel.SpaceTime;
        public KeyboardInputManager KeyboardInput { get; private set; } 
        public GameStateManager GameState { get; private set; }

        public Theme Theme { get => Get<Theme>(); set => Set(value); }

        public MainCharacter MainCharacter { get { return Get<MainCharacter>(); } private set { Set(value); } }

        private ConsolePanel disposableRoot;

        public GameApp()
        {
            GameState = new GameStateManager();
            this.SubscribeForLifetime(nameof(Theme), () => Theme.Bind(this), this);
            Theme = new DefaultTheme();
            this.QueueActionInFront(InitializeGame);
        }

        protected abstract SceneFactory SceneFactory { get; }
        protected abstract void OnSceneInitialize();


        protected virtual void OnAppInitialize() { }
        protected virtual void OnAddedToScene(SpacialElement element) { }
        protected virtual void OnLevelLoaded(Level l) { }
        protected virtual void BeforeLevelUnloaded() { }

        private void InitializeGame()
        {
            ConfigureQuitOnEscapeKey();
            OnAppInitialize();
        }

        public void Load(Level level)
        {
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

            cleanupPromise.Then(()=> QueueAction(() => ConfigureGamingArea(level.Width, level.Height)).Then(() =>
            {
                Scene.QueueAction(() =>
                {
                    foreach (var item in SceneFactory.InitializeScene(level))
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
                        OnAddedToScene(item);
                    }
                    OnLevelLoaded(level);
                });
            }));
            
        }

        private void ConfigureGamingArea(int w, int h)
        {
            LayoutRoot.Controls.Remove(disposableRoot);
            disposableRoot = LayoutRoot.Add(new ConsolePanel() { Background = ConsoleColor.DarkGray, Width = w + 2, Height = h + 2 }).CenterHorizontally().CenterVertically();
            ScenePanel = disposableRoot.Add(new SpacetimePanel(w, h)).Fill(padding: new Thickness(1, 1, 1, 1));
            ScenePanel.Background = ConsoleColor.Black;
            Scene.QueueAction(OnSceneInitialize);
            KeyboardInput = new KeyboardInputManager(Scene, this);
            Scene.Start();
            LayoutRoot.Add(new FramerateControl(ScenePanel));

        }

        private void ConfigureQuitOnEscapeKey()
        {
            this.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.Escape, null, () =>
            {
                Scene.Stop();
                Dialog.ConfirmYesOrNo("Are you sure you want to quit?", Stop, () =>
                {
                    ScenePanel.RealTimeViewing.ReSync();
                    Scene.Start();
                });
            }, this);
        }
    }
}
