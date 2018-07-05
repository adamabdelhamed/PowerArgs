using ConsoleGames;
using System;
using System.Collections.Generic;
using PowerArgs.Cli;
namespace DemoGame
{
    public class DemoGameApp : GameApp
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

        private ShooterKeys shooterKeys;
        private GameState currentState;
        private IDisposable bgMusicHandle;

        protected override void OnAppInitialize()
        {
            var introPanel = new PowerArgsGamesIntro();
            var frameRateControl = LayoutRoot.Add(new FramerateControl(introPanel) { ZIndex = 100 });
            LayoutRoot.Add(introPanel).Play().Then(() => { QueueAction(() =>
            {
                LayoutRoot.Controls.Remove(frameRateControl);
                this.shooterKeys = new ShooterKeys(()=> this.Scene);
                this.KeyboardInput.KeyMap = this.shooterKeys.ToKeyMap();
                EnableThemeToggling();
                LayoutRoot.Add(new HeadsUpDisplay(this, shooterKeys)).CenterHorizontally().DockToBottom();
                currentState = this.GameState.LoadSavedGameOrDefault(GameState.DefaultSavedGameName);

                if (currentState.Data.TryGetValue("CurrentLevel", out object levelName) == false)
                {
                    levelName = "DefaultLevel";
                }

                var level = LevelEditor.LoadBySimpleName(levelName.ToString());
                this.Load(level);


                Sound.Loop("bgmusicmain").Then(d => bgMusicHandle = d);

                this.Paused.SubscribeForLifetime(() =>
                {
                    bgMusicHandle?.Dispose();
                    bgMusicHandle = null;
                }, this);

                this.Resumed.SubscribeForLifetime(() =>
                {
                    Sound.Loop("bgmusicmain").Then(d => bgMusicHandle = d);
                }, this);
            });});
        }

        protected override void AfterLevelLoaded(Level l)
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

        private void EnableThemeToggling()
        {
            this.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.T, null, () =>
            {
                if (Theme is DefaultTheme)
                {
                    Theme = new DarkTheme();
                }
                else
                {
                    Theme = new DefaultTheme();
                }
            }, this);
        }
    }
}
