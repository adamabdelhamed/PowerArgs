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
            var app = new ConsoleApp();
            var realmPanel = app.LayoutRoot.Add(new RealmPanel(80, 20)).FillAndPreserveAspectRatio();
            app.QueueAction(() =>
            {
                realmPanel.RenderLoop.Start();
            });

            realmPanel.RenderLoop.QueueAction(() =>
            {
                def.Populate(realmPanel.RenderLoop.Realm, false);

                foreach(var zombie in realmPanel.RenderLoop.Realm.Things.Where(t => t is Zombie).Select(z => z as Zombie))
                {
                    zombie.IsActive = true;
                }
            });

            var appTask = app.Start();
            appTask.Wait();
            return;
        }
    }
}
