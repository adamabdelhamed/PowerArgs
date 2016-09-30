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
        public static void Run()
        {
            var app = new ConsoleApp();
            var realmPanel = app.LayoutRoot.Add(new RealmPanel(80, 30)).Fill();
            app.QueueAction(() => 
            {
                realmPanel.RenderLoop.Start();
            });

            realmPanel.RenderLoop.QueueAction(()=>
            {
                AddWalls(realmPanel);
                AddPaths(realmPanel);
                AddZombies(realmPanel, app);

                realmPanel.RenderLoop.Realm.Add(new MainCharacter() { Bounds = new PowerArgs.Cli.Physics.Rectangle(5, 20, 1, 1) });
            });
            
            var appTask = app.Start();
            appTask.Wait();
            return;
        }

        static IDisposable zombieAdder;
        private static void AddZombies(RealmPanel realmPanel, ConsoleApp app)
        {
           zombieAdder = app.SetInterval(() =>
           {
               realmPanel.RenderLoop.QueueAction(() =>
               {
                   var zombie = new Zombie();
                   realmPanel.RenderLoop.Realm.Add(zombie);
                   RealmHelpers.PlaceInEmptyLocation(realmPanel.RenderLoop.Realm, zombie);
                  
               });

           }, TimeSpan.FromSeconds(5));

        }

        private static void AddWalls(RealmPanel realmPanel)
        {
            realmPanel.RenderLoop.Realm.Add(new Wall() { Bounds = new PowerArgs.Cli.Physics.Rectangle(10,2,30,1) });
            realmPanel.RenderLoop.Realm.Add(new Wall() { Bounds = new PowerArgs.Cli.Physics.Rectangle(20, 8, 30, 1) });
        }

        private static void AddPaths(RealmPanel realmPanel)
        {
            realmPanel.RenderLoop.Realm.Add(
                new Path(new Location(1,1), 
                new Location(20, 12),
                new Location(50, 14),
                new Location(70, 1)
            ).ToArray());
        }
    }
}
