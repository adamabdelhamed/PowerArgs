using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Games
{
    public class AutoTargetingOptions
    {
        public SpeedTracker Source { get; set; }
        public Func<IEnumerable<SpacialElement>> TargetsEval { get; set; }

    }

    public class AutoTargetingFunction : TimeFunction
    {
        public Event<SpacialElement> TargetChanged { get; private set; } = new Event<SpacialElement>();
        private AutoTargetingOptions options;
        private SpacialElement lastTarget;
        public AutoTargetingFunction(AutoTargetingOptions options)
        {
            this.options = options;
            this.Governor = new RateGovernor(TimeSpan.FromSeconds(.1));
        }

        public override void Evaluate()
        {
            var targets = options.TargetsEval()
                .Where(z=>
                {
                    var angle = options.Source.Element.CalculateAngleTo(z);
                    var delta = Math.Abs(options.Source.Angle - angle);
                    if(delta > 180)
                    {
                  //      delta -= 180;
                    }

                    return delta < 90;
                })
                .OrderBy(z => options.Source.Element.CalculateDistanceTo(z));
            var obstacles = new HashSet<IRectangular>();

            foreach(var target in options.TargetsEval())
            {
                obstacles.Add(target);
            }

            foreach(var element in SpaceTime.CurrentSpaceTime.Elements.Where(e => e.HasSimpleTag(Weapon.WeaponTag) == false && e.ZIndex == options.Source.Element.ZIndex))
            {
                obstacles.Add(element);
            }
            

            foreach (var target in targets)
            {
                var hasLineOfSight = SpaceExtensions.HasLineOfSight(options.Source.Element, target, obstacles.ToList(), 1);

                if (hasLineOfSight)
                {
                    if (target != lastTarget)
                    {
                        TargetChanged.Fire(target);
                        lastTarget = target;
                    }
                    return;
                }
            }

            if (lastTarget != null)
            {
                lastTarget = null;
                TargetChanged.Fire(null);
            }
        }
    }
}
