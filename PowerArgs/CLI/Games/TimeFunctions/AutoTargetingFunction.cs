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

        public IRectangularF SourceBounds => Source.Element is IHaveMassBounds ? (Source.Element as IHaveMassBounds).MassBounds : Source.Element;

    }

    public class AutoTargetingFunction : TimeFunction
    {
        public float AngularVisibility { get; set; } = 44;

        public Event<SpacialElement> TargetChanged { get; private set; } = new Event<SpacialElement>();
        public AutoTargetingOptions Options { get; private set; }
        private SpacialElement lastTarget;
        public AutoTargetingFunction(AutoTargetingOptions options)
        {
            this.Options = options;
            this.Governor = new RateGovernor(TimeSpan.FromSeconds(options.Source.Element is MainCharacter ? 0 : .5));
        }

        public override void Evaluate()
        {
            var obstacles = Options.Source.GetObstacles();

            var candidates = Options.TargetsEval();
            var target = candidates
                .Where(z =>
                {
                    var sb = Options.SourceBounds;
                    var angle = sb.Center().CalculateAngleTo(z.Center());
                    var delta = Options.Source.Angle.DiffAngle(angle);
                    if (delta >= AngularVisibility) return false;

                    var prediction = HitDetection.PredictHit(new HitDetectionOptions()
                    {
                        Angle = angle,
                        MovingObject = sb,
                        Obstacles = obstacles.Where(o => o is WeaponElement == false),
                        Visibility = 3*SpaceTime.CurrentSpaceTime.Bounds.Hypotenous(),
                        Precision = .5f,
                    });

                    var elementHit = prediction.ObstacleHit as SpacialElement;

                    if (elementHit == z)
                    {
                        return true;
                    }
                    else if(elementHit != null && z is IHaveMassBounds && (z as IHaveMassBounds).IsPartOfMass(elementHit))
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
