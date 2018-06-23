using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;

namespace ConsoleGames
{
    public abstract class GameApp : ConsoleApp
    {
        public static GameApp CurrentGameApp => ConsoleApp.Current as GameApp;
        private SpacetimePanel ScenePanel { get; set; }
        public SpaceTime Scene => ScenePanel.SpaceTime;
        public KeyboardInputManager KeyboardInput { get; private set; } 

        public int SceneWidth { get; set; } = 78;
        public int SceneHeight { get; set; } = 20;

        public GameApp()
        {
            this.QueueActionInFront(InitializeGame);
        }

        protected virtual void OnAppInitialize()
        {
            
        }
        protected abstract void OnSceneInitialize();

        private void InitializeGame()
        {
            ConfigureQuitOnEscapeKey();
            ConfigureGamingArea();
            OnAppInitialize();
        }

 
        private void ConfigureGamingArea()
        {
            var borderPanel = LayoutRoot.Add(new ConsolePanel() { Background = ConsoleColor.DarkGray, Width = SceneWidth + 2, Height = SceneHeight + 2 }).CenterHorizontally().CenterVertically();
            ScenePanel = borderPanel.Add(new SpacetimePanel(SceneWidth, SceneHeight)).Fill(padding: new Thickness(1, 1, 1, 1));
            ScenePanel.Background = ConsoleColor.Black;
            Scene.QueueAction(OnSceneInitialize);

            Scene.Start();
            KeyboardInput = new KeyboardInputManager(Scene, this);
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
