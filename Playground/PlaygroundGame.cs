using ConsoleGames;
using System;
using System.Collections.Generic;
using PowerArgs.Cli;
namespace Playground
{
    public class PlaygroundGame : GameApp
    {
        private SceneFactory factory = new SceneFactory(new List<ItemReviver>()
        {
            new MainCharacterReviver(),
            new LooseWeaponReviver(),
            new EnemyReviver(),
            new PortalReviver(),
            new WallReviver()
        });

        protected override SceneFactory SceneFactory => factory;

        private ShooterKeys shooterKeys = new ShooterKeys();

        protected override void OnSceneInitialize()
        {
            this.KeyboardInput.KeyMap = this.shooterKeys.ToKeyMap();
            var level = LevelEditor.LoadBySimpleName("DefaultLevel");
            this.Load(level);
        }

        protected override void OnAppInitialize()
        {
            var hud = LayoutRoot.Add(new HeadsUpDisplay(this, shooterKeys)).CenterHorizontally().DockToBottom();
        }
    }
}
