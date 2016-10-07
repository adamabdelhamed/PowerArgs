using PowerArgs.Cli.Physics;
using System;
using System.Linq;

namespace ConsoleZombies
{
    public class Targeting : ThingInteraction
    {
        public MainCharacter MainCharacter
        {
            get
            {
                return MyThing as MainCharacter;
            }
        }

        public Targeting(MainCharacter character) : base(character)
        {
            Governor.Rate = TimeSpan.FromSeconds(.25);
        }

        public override void Behave(Scene scene)
        {
            var zombies = Scene.Things.Where(t => t is Zombie).Select(t => t as Zombie)
                .OrderBy(z => MainCharacter.Bounds.Location.CalculateDistanceTo(z.Bounds.Location));

            foreach(var zombie in zombies)
            {
                var route = SceneHelpers.CalculateLineOfSight(scene, MainCharacter, zombie.Bounds.Location, 1);

                if(route.Obstacles.Where(o => o is Wall).Count() == 0)
                {
                    if(MainCharacter.Target != null && MainCharacter.Target != zombie && MainCharacter.Target.IsExpired == false)
                    {
                        Scene.Update(MainCharacter.Target);
                    }
                    MainCharacter.Target = zombie;
                    Scene.Update(zombie);
                    return;
                }
            }

            MainCharacter.Target = null;
        }
    }
}
