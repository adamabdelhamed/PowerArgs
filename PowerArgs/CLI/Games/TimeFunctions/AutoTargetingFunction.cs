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
        public float AngularVisibility { get; set; } = 60;

        public Event<SpacialElement> TargetChanged { get; private set; } = new Event<SpacialElement>();
        public AutoTargetingOptions Options { get; private set; }
        private SpacialElement lastTarget;
        private List<SpacialElement> targets = new List<SpacialElement>();

        public IEnumerable<SpacialElement> PotentialTargets => targets;

        public float Delay { get; set; }

        public AutoTargetingFunction(AutoTargetingOptions options)
        {
            this.Options = options;

            Delay = options.Source.Element is MainCharacter ? (int)Time.CurrentTime.Increment.TotalMilliseconds : 500;
            this.Added.SubscribeOnce(async () =>
            {
                while(this.Lifetime.IsExpired == false)
                {
                    Evaluate();
                    await Time.CurrentTime.DelayOrYield(Delay);
                }
            });
        }

        private void Evaluate()
        {
            var obstacles = Options.Source.GetObstacles().Where(o => o is WeaponElement == false).ToArray();

            var candidates = Options.TargetsEval().ToArray();

            SpacialElement target = null;
            float winningCandidateProximity = float.MaxValue;
            targets.Clear();
            for(var i = 0; i < candidates.Length; i++)
            {
                var z = candidates[i];
                var sb = Options.SourceBounds;
                var angle = sb.Center().CalculateAngleTo(z.Center());
                var delta = Options.Source.Angle.DiffAngle(angle);
                if (delta >= AngularVisibility) continue;

                var prediction = HitDetection.PredictHit(new HitDetectionOptions()
                {
                    Angle = angle,
                    MovingObject = sb,
                    Obstacles = obstacles,
                    Visibility = 3 * SpaceTime.CurrentSpaceTime.Bounds.Hypotenous(),
                    Precision = 1f,
                });

                var elementHit = prediction.ObstacleHit as SpacialElement;

                if (elementHit == z)
                {
                    targets.Add(elementHit);
                    var d = Geometry.CalculateNormalizedDistanceTo(Options.Source.Element, z);
                    if(d < winningCandidateProximity)
                    {
                        target = elementHit;
                        winningCandidateProximity = d;
                    }
                }
                else if (elementHit != null && z is IHaveMassBounds && (z as IHaveMassBounds).IsPartOfMass(elementHit))
                {
                    targets.Add(elementHit);
                    var d = Geometry.CalculateNormalizedDistanceTo(Options.Source.Element, z);
                    if (d < winningCandidateProximity)
                    {
                        target = elementHit;
                        winningCandidateProximity = d;
                    }
                }
            }

            if (Options.Source.Element is Character && (Options.Source.Element as Character).IsVisible == false)
            {
                target = null;
            }

            if (target != lastTarget)
            {
                TargetChanged.Fire(target);
                lastTarget = target;
            }
        }
    }
}
