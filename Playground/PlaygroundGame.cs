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

            var level = new Level()
            {
                Items = new System.Collections.Generic.List<LevelItem>()
                {
                    new LevelItem(){ X = 4, Y = 4, Width = 1, Height = 1, Tags = new List<string>(){ "main-character" } },
                    new LevelItem(){ X = 6, Y = 6, Symbol = 'W', FG = ConsoleColor.Red, BG = ConsoleColor.Yellow, Width = 1, Height = 1,  }
                }
            };

            var factory = new SceneFactory(new List<ItemReviver>()
            {
                new MainCharacterReviver(),
                new WallReviver()
            });

            this.Load(level, factory);
        }
    }
}
