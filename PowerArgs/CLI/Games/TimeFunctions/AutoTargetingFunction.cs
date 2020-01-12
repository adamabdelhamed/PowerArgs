using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Games
{
    public class AutoTargetingOptions
    {
        public Velocity Source { get; set; }
        public Func<IEnumerable<SpacialElement>> TargetsEval { get; set; }

    }

    public class AutoTargetingFunction : TimeFunction
    {
        public Event<SpacialElement> TargetChanged { get; private set; } = new Event<SpacialElement>();
        public AutoTargetingOptions Options { get; private set; }
        private SpacialElement lastTarget;
        public AutoTargetingFunction(AutoTargetingOptions options)
        {
            this.Options = options;
            this.Governor = new RateGovernor(TimeSpan.FromSeconds(0));
        }

        protected virtual SpacialElement FilterTarget(SpacialElement t) => t;

        public override void Evaluate()
        {
            var targets = Options.TargetsEval()
                .Where(z =>
                {
                    var angle = Options.Source.Element.CalculateAngleTo(z);
                    var delta = Options.Source.Angle.DiffAngle(angle);
                    return delta < 90;
                })
                .OrderBy(z => Geometry.CalculateNormalizedDistanceTo( Options.Source.Element,z));
            var obstacles = new HashSet<IRectangularF>();

            foreach(var target in Options.TargetsEval())
            {
                if (Options.Source.HitDetectionExclusions.Contains(target) == false && Options.Source.HitDetectionExclusionTypes.Contains(target.GetType()) == false)
                {
                    obstacles.Add(target);
                }
            }

            foreach(var element in SpaceTime.CurrentSpaceTime.Elements.Where(e => e.HasSimpleTag(Weapon.WeaponTag) == false && e.ZIndex == Options.Source.Element.ZIndex))
            {
                if (Options.Source.HitDetectionExclusions.Contains(element) == false && Options.Source.HitDetectionExclusionTypes.Contains(element.GetType()) == false)
                {
                    obstacles.Add(element);
                }
            }

            foreach (var target in targets)
            {
                var hasLineOfSight = SpacialAwareness.HasLineOfSight(Options.Source.Element, target, obstacles.ToList());

                if (hasLineOfSight)
                {
                    var finalTarget = FilterTarget(target);
                    if (target != lastTarget)
                    {
                        TargetChanged.Fire(finalTarget);
                        lastTarget = finalTarget;
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
