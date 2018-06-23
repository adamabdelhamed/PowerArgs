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
            this.MainCharacter = new MainCharacter();

            this.Scene.Add(MainCharacter);
            this.KeyMap = new ShooterKeyMap();
            this.KeyboardInput.KeyMap = KeyMap.GenerateKeyMap();
            this.KeyboardInput.UpdateKeyboardMappings();
        }


        private void ConfigureHeadsUpDisplay()
        {
            var hud = LayoutRoot.Add(new HeadsUpDisplay(this)).CenterHorizontally().DockToBottom();
        }
    }
}
