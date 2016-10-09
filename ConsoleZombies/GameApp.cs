using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Linq;

namespace ConsoleZombies
{
    public class GameApp : ConsoleApp
    {
        public GameInputManager inputManager;
        private HeadsUpDisplay headsUpDisplay;
        private ScenePanel scenePanel;
        public GameApp()
        {
            this.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.Escape, null, () =>
            {
                Dialog.ConfirmYesOrNo("Are you sure you want to quit?", () =>
                {
                    Stop();
                });
            }, this.LifetimeManager);

            var borderPanel = LayoutRoot.Add(new ConsolePanel() { Background = ConsoleColor.DarkGray, Width = LevelDefinition.Width + 2, Height = LevelDefinition.Height + 2 }).CenterHorizontally().CenterVertically();
            scenePanel = borderPanel.Add(new ScenePanel(LevelDefinition.Width, LevelDefinition.Height)).Fill(padding: new Thickness(1, 1, 1, 1));
            headsUpDisplay = LayoutRoot.Add(new HeadsUpDisplay(this, scenePanel.Scene) { Width = LevelDefinition.Width }).DockToBottom().CenterHorizontally();
            LayoutRoot.Add(new FramerateControl(scenePanel.Scene));
            QueueAction(() => { scenePanel.Scene.Start(); });
            inputManager = new GameInputManager(scenePanel.Scene, this);
        }

        public void Load(LevelDefinition def)
        {
            scenePanel.Scene.QueueAction(() =>
            {
                Inventory toKeep = null;
                if(MainCharacter.Current != null)
                {
                    toKeep = MainCharacter.Current.Inventory;
                }

                scenePanel.Scene.Clear();
                def.Populate(scenePanel.Scene, false);
                SoundEffects.Instance.SoundThread.Start();
                SoundEffects.Instance.PlaySound("music");
                foreach(var zombie in scenePanel.Scene.Things.Where(t => t is Zombie).Select(z => z as Zombie))
                {
                    zombie.IsActive = true;
                }

                foreach(var portal in scenePanel.Scene.Things.Where(p => p is Portal).Select(p => p as Portal))
                {
                    var localPortal = portal;
                    localPortal.PortalEntered.SubscribeForLifetime(()=>
                    {
                        Load(LevelDefinition.Load(localPortal.DestinationId));
                    }, portal.LifetimeManager);
                }

                if(MainCharacter.Current != null)
                {
                    headsUpDisplay.ViewModel.MainCharacter = MainCharacter.Current;
                    if (toKeep != null)
                    {
                        MainCharacter.Current.Inventory = toKeep;
                    }
                    inputManager.InitializeDefaultControls();
                    MainCharacter.Current.EatenByZombie.SubscribeForLifetime(() =>
                    {
                        scenePanel.Scene.Stop();
                        QueueAction(() =>
                        {
                            SoundEffects.Instance.PlaySound("playerdead");
                            
                            Dialog.ShowMessage("Game over :(",()=> { Stop(); SoundEffects.Instance.SoundThread.Stop(); });
                        });
                    },scenePanel.LifetimeManager);
                }
            });
        }
    }
}
