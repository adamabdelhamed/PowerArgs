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
                .Where(z =>
                {
                    var angle = options.Source.Element.CalculateAngleTo(z);
                    var delta = options.Source.Angle.DiffAngle(angle);
                    return delta < 90;
                })
                .OrderBy(z => Geometry.CalculateNormalizedDistanceTo( options.Source.Element,z));
            var obstacles = new HashSet<IRectangularF>();

            foreach(var target in options.TargetsEval())
            {
                if (options.Source.HitDetectionExclusions.Contains(target) == false && options.Source.HitDetectionExclusionTypes.Contains(target.GetType()) == false)
                {
                    obstacles.Add(target);
                }
            }

            foreach(var element in SpaceTime.CurrentSpaceTime.Elements.Where(e => e.HasSimpleTag(Weapon.WeaponTag) == false && e.ZIndex == options.Source.Element.ZIndex))
            {
                if (options.Source.HitDetectionExclusions.Contains(element) == false && options.Source.HitDetectionExclusionTypes.Contains(element.GetType()) == false)
                {
                    obstacles.Add(element);
                }
            }

            foreach (var target in targets)
            {
                var hasLineOfSight = SpacialAwareness.HasLineOfSight(options.Source.Element, target, obstacles.ToList());

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
