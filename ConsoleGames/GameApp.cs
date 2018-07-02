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

        public int SceneWidth { get; set; } = 78;
        public int SceneHeight { get; set; } = 40;

        public MainCharacter MainCharacter { get { return Get<MainCharacter>(); } private set { Set(value); } }

        public GameApp()
        {
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
            ConfigureGamingArea();
            OnAppInitialize();
        }

        public void Load(Level level)
        {
            BeforeLevelUnloaded();
            foreach(var element in Scene.Elements)
            {
                element.Lifetime.Dispose();
            }

            foreach (var item in SceneFactory.InitializeScene(level))
            {
                if(item is IGameAppAware)
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
        }


        private void ConfigureGamingArea()
        {
            var borderPanel = LayoutRoot.Add(new ConsolePanel() { Background = ConsoleColor.DarkGray, Width = SceneWidth + 2, Height = SceneHeight + 2 }).CenterHorizontally().CenterVertically();
            ScenePanel = borderPanel.Add(new SpacetimePanel(SceneWidth, SceneHeight)).Fill(padding: new Thickness(1, 1, 1, 1));
            ScenePanel.Background = ConsoleColor.Black;
            Scene.QueueAction(OnSceneInitialize);
            Scene.Start();
            KeyboardInput = new KeyboardInputManager(Scene, this);
            GameState = new GameStateManager();
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
            }, this.LifetimeManager);
        }
    }
}
