using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public override void Behave(Realm realm)
        {
            var zombies = Realm.Things.Where(t => t is Zombie).Select(t => t as Zombie)
                .OrderBy(z => MainCharacter.Bounds.Location.CalculateDistanceTo(z.Bounds.Location));

            foreach(var zombie in zombies)
            {
                var route = RealmHelpers.CalculateLineOfSight(realm, MainCharacter, zombie.Bounds.Location, 1);

                if(route.Obstacles.Where(o => o is Wall).Count() == 0)
                {
                    if(MainCharacter.Target != null && MainCharacter.Target != zombie && MainCharacter.Target.IsExpired == false)
                    {
                        Realm.Update(MainCharacter.Target);
                    }
                    MainCharacter.Target = zombie;
                    Realm.Update(zombie);
                    return;
                }
            }

            MainCharacter.Target = null;
        }
    }
}
