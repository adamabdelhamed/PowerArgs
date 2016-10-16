using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Linq;

namespace ConsoleZombies
{
    public class Targeting : Interaction
    {
        private Func<PowerArgs.Cli.Physics.Rectangle> sourceEval;
        private Func<Thing, bool> targetFilter;

        public Event<Thing> TargetChanged { get; private set; } = new Event<Thing>();

        public Targeting(Func<PowerArgs.Cli.Physics.Rectangle> sourceEval, Func<Thing,bool> targetFilter) 
        {
            this.sourceEval = sourceEval;
            this.targetFilter = targetFilter;
        }

        public override void Behave(Scene scene)
        {
            var targets = Scene.Things.Where(t => targetFilter(t))
                .OrderBy(z => sourceEval().Location.CalculateDistanceTo(z.Bounds.Location));

            foreach(var target in targets)
            {
                var route = SceneHelpers.CalculateLineOfSight(scene, sourceEval(), target.Bounds.Location, 1);

                if(route.Obstacles.Where(o => o is Wall).Count() == 0)
                {
                    TargetChanged.Fire(target);
                    return;
                }
            }

            TargetChanged.Fire(null);
        }
    }
}
