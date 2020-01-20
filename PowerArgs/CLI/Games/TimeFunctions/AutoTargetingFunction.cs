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
            var obstacles = Options.Source.GetObstacles();

            var target = Options.TargetsEval()
                .Where(z =>
                {
                    var angle = Options.Source.Element.CalculateAngleTo(z);
                    var delta = Options.Source.Angle.DiffAngle(angle);
                    if (delta >= 90) return false;

                    var prediction = HitDetection.PredictHit(new HitDetectionOptions()
                    {
                        Angle = angle,
                        MovingObject = z is IHaveMassBounds ? (z as IHaveMassBounds).MassBounds : z,
                        Obstacles = obstacles.Where(o => o is WeaponElement == false),
                        Visibility = SpaceTime.CurrentSpaceTime.Bounds.Hypotenous(),
                    });

                    var elementHit = prediction.ObstacleHit as SpacialElement;

                    if (prediction.ObstacleHit == z || (elementHit != null && z is IHaveMassBounds && (z as IHaveMassBounds).IsPartOfMass(elementHit)))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                })
                .OrderBy(z => Geometry.CalculateNormalizedDistanceTo( Options.Source.Element,z))
                .FirstOrDefault();

            if (target != lastTarget)
            {
                TargetChanged.Fire(target);
                lastTarget = target;
            }
        }
    }
}
