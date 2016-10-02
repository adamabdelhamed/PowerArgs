using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleZombies
{
    public class PrototypeLevel
    {
        public static void Run(LevelDefinition def)
        {
            Exception ex = null;
            var app = new ConsoleApp();
            var borderPanel = app.LayoutRoot.Add(new ConsolePanel() { Background= ConsoleColor.DarkGray, Width = LevelDefinition.Width+2, Height = LevelDefinition.Height+2 }).CenterHorizontally().CenterVertically();
            var realmPanel = borderPanel.Add(new RealmPanel(LevelDefinition.Width, LevelDefinition.Height)).Fill(padding:new Thickness(1, 1, 1, 1));

            app.PumpException.SubscribeForLifetime((args) =>
            {
                ex = args.Exception;
            }, app.LifetimeManager);

            app.QueueAction(() =>
            {
                realmPanel.RenderLoop.Start();
            });


            realmPanel.RenderLoop.QueueAction(() =>
            {
                def.Populate(realmPanel.RenderLoop.Realm, false);
                SoundEffects.Instance.SoundThread.Start();
                SoundEffects.Instance.PlaySound("music");
                foreach(var zombie in realmPanel.RenderLoop.Realm.Things.Where(t => t is Zombie).Select(z => z as Zombie))
                {
                    zombie.IsActive = true;
                }

                if(MainCharacter.Current != null)
                {
                    MainCharacter.Current.EatenByZombie.SubscribeForLifetime(() =>
                    {
                        realmPanel.RenderLoop.Stop();
                        app.QueueAction(() =>
                        {
                            SoundEffects.Instance.PlaySound("playerdead");
                            
                            Dialog.ShowMessage("Game over :(",()=> { app.Stop(); SoundEffects.Instance.SoundThread.Stop(); });
                        });
                    },realmPanel.LifetimeManager);
                }
            });

            var appTask = app.Start();
            appTask.Wait();

            if(ex != null)
            {
                ConsoleString.WriteLine(ex.ToString(), ConsoleColor.Red);
                Console.ReadLine();
            }
            return;
        }
    }
}
