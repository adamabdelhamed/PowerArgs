using ConsoleGames;
using ConsoleGames.Shooter;
using System;
using System.Collections.Generic;

namespace Playground
{
    public class PlaygroundGame : ShooterGameApp
    {
        private SceneFactory factory = new SceneFactory(new List<ItemReviver>()
        {
            new MainCharacterReviver(),
            new AmmoReviver(),
            new EnemyReviver(),
            new ShooterPortalReviver(),
            new WallReviver()
        });

        protected override SceneFactory SceneFactory => factory;

        protected override void OnSceneInitialize()
        {
            base.OnSceneInitialize();

            var level = LevelEditor.LoadBySimpleName("DefaultLevel");
            this.Load(level);
 
        }
    }
}
