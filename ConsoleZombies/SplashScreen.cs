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
    public class SplashScreen
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
                
                Random r = new Random();
                foreach (var thing in realmPanel.RenderLoop.Realm.Things.ToArray())
                {
                    var zombie = thing as Zombie;
                    if (zombie == null) continue;

                    var target = new Thing() { Bounds = thing.Bounds.Clone() };
                    realmPanel.RenderLoop.Realm.Add(target);
                    thing.Bounds.MoveTo(new Location() { X = r.Next() < .5 ? zombie.Bounds.X : r.Next(0, realmPanel.Width), Y = r.Next(2,8) });
                    new Seeker(zombie, target, zombie.SpeedTracker, 1f) { RemoveWhenReached=true, IsSeeking=true};
                }
            });

            var timer = app.SetTimeout(() => 
            {
                realmPanel.RenderLoop.QueueAction(()=>
                {
                    foreach (var thing in realmPanel.RenderLoop.Realm.Things.ToArray())
                    {
                        var zombie = thing as Zombie;
                        if (zombie == null) continue;

                        var target = new Thing() { Bounds = new PowerArgs.Cli.Physics.Rectangle(realmPanel.Width/2,realmPanel.Height/2,1,1)};
                        realmPanel.RenderLoop.Realm.Add(target);
               
                        new Seeker(zombie, target, zombie.SpeedTracker, 1f) { RemoveWhenReached = true, IsSeeking = true };
                    }
                });
            }, TimeSpan.FromSeconds(4));

            realmPanel.RenderLoop.SpeedFactor = 20;

            var timer2 = app.SetTimeout(() =>
            {
                realmPanel.RenderLoop.Stop();
                app.Stop();
            }, TimeSpan.FromSeconds(7));

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
