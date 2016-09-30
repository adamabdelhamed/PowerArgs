using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleZombies
{
    public class PathTraveler : ThingInteraction
    {
        PathElement target;
        bool reverse;

        public PathTraveler(Thing thing, PathElement initialTarget, bool reverse = false) : base(thing)
        {
            this.target = initialTarget;
            this.reverse = reverse;
        }

        public override void Initialize(Realm r)
        {
            this.Governor.Rate = TimeSpan.FromSeconds(.05);
        }

        public override void Behave(Realm r)
        {
            if (target == null)
            {
                r.Remove(this);
                return;
            }

            var oldLocation = MyThing.Bounds.Location;
            var newLocation = RealmHelpers.MoveTowards(oldLocation, target.Bounds.Location, 1.5f);

            MyThing.Bounds.MoveTo(newLocation);
            r.Update(MyThing);
            if (RealmHelpers.GetThingsITouch(r, MyThing, new List<Type>() { typeof(PathElement) }).Contains(target))
            {
                target.IsHighlighted = false;
                target = reverse ? target.Last : target.Next;
            }
        }
    }
}
