using System;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;

namespace ConsoleGames.Shooter
{
    public abstract class ShooterGameApp : GameApp
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

        protected override void OnAddedToScene(SpacialElement element)
        {
            if(element is MainCharacter)
            {
                this.MainCharacter = element as MainCharacter;
            }
        }



        private void ConfigureHeadsUpDisplay()
        {
            var hud = LayoutRoot.Add(new HeadsUpDisplay(this)).CenterHorizontally().DockToBottom();
        }
    }
}
