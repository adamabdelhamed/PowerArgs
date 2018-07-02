using ConsoleGames;
using ConsoleGames.Shooter;
using System;
using System.Collections.Generic;

namespace Playground
{
    public class PlaygroundGame : ShooterGameApp
    {
        protected override void OnSceneInitialize()
        {
            base.OnSceneInitialize();

            var level = LevelEditor.LoadBySimpleName("DefaultLevel");

            var factory = new SceneFactory(new List<ItemReviver>()
            {
                new MainCharacterReviver(),
                new AmmoReviver(),
                new EnemyReviver(),
                new WallReviver()
            });

            this.Load(level, factory);
        }
    }
}
