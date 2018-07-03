using ConsoleGames;
using System;
using System.Collections.Generic;
using PowerArgs.Cli;
namespace Playground
{
    public class PlaygroundGame : GameApp
    {
        protected override SceneFactory SceneFactory => new SceneFactory(new List<ItemReviver>()
        {
            new MainCharacterReviver(),
            new LooseWeaponReviver(),
            new EnemyReviver(),
            new PortalReviver(),
            new WaypointReviver(),
            new CeilingReviver(),
            new DoorReviver(),
            new WallReviver()
        });

        private ShooterKeys shooterKeys = new ShooterKeys();
        private GameState currentState;

        public PlaygroundGame()
        {
            currentState = this.GameState.LoadSavedGameOrDefault(GameState.DefaultSavedGameName);

            if (currentState.Data.TryGetValue("CurrentLevel", out object levelName) == false)
            {
                levelName = "DefaultLevel";
            }

            var level = LevelEditor.LoadBySimpleName(levelName.ToString());
            this.Load(level);
        }

        protected override void OnSceneInitialize()
        {
            this.KeyboardInput.KeyMap = this.shooterKeys.ToKeyMap();
        }

        protected override void OnLevelLoaded(Level l)
        {
            currentState.SetValue("CurrentLevel", l.Name);
            if(currentState.Data.TryGetValue(nameof(Character.Inventory), out object data))
            {
                var inventory = data as Inventory;
                inventory.Owner = MainCharacter.Current;
                MainCharacter.Current.Inventory = inventory;
            }
            this.GameState.SaveGame(currentState, GameState.DefaultSavedGameName);
        }

        protected override void BeforeLevelUnloaded()
        {
            if(MainCharacter.Current != null)
            {
                currentState.SetValue(nameof(Character.Inventory), MainCharacter.Current.Inventory);
                this.GameState.SaveGame(currentState, GameState.DefaultSavedGameName);
            }
        }

        protected override void OnAppInitialize()
        {
            var hud = LayoutRoot.Add(new HeadsUpDisplay(this, shooterKeys)).CenterHorizontally().DockToBottom();
        }
    }
}
