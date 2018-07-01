using System;
using PowerArgs.Cli;
namespace ConsoleGames.Shooter
{
    public class ShooterGameApp : GameApp
    {
        public MainCharacter MainCharacter { get { return Get<MainCharacter>(); } private set { Set(value); } }

        public ShooterKeyMap KeyMap { get; private set; }
 

        protected override void OnAppInitialize()
        {
            ConfigureHeadsUpDisplay();   
        }


        protected override void OnSceneInitialize()
        {
            this.KeyMap = new ShooterKeyMap();
            this.KeyboardInput.KeyMap = KeyMap.GenerateKeyMap();
            this.KeyboardInput.UpdateKeyboardMappings();
        }

        public void Load(Level level, SceneFactory factory)
        {
            foreach(var item in factory.InitializeScene(level))
            {
                if(item is MainCharacter)
                {
                    this.MainCharacter = item as MainCharacter;
                }
                this.Scene.Add(item);
            }
        }

        private void ConfigureHeadsUpDisplay()
        {
            var hud = LayoutRoot.Add(new HeadsUpDisplay(this)).CenterHorizontally().DockToBottom();
        }
    }
}
