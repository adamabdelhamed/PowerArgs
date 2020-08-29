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
        public string TargetTag { get; set; }
        public float AngularVisibility { get; set; } = 60;

        public IRectangularF SourceBounds => Source.Element is IHaveMassBounds ? (Source.Element as IHaveMassBounds).MassBounds : Source.Element;

    }

    public class AutoTargetingFunction : TimeFunction
    {

        public Event<SpacialElement> TargetChanged { get; private set; } = new Event<SpacialElement>();
        public AutoTargetingOptions Options { get; private set; }
        private SpacialElement lastTarget;
        private List<SpacialElement> targets = new List<SpacialElement>();

        public IEnumerable<SpacialElement> PotentialTargets => targets;

        public float Delay { get; set; }

        public AutoTargetingFunction(AutoTargetingOptions options)
        {
            this.Options = options;

            Delay = options.Source.Element is MainCharacter ? (int)Time.CurrentTime.Increment.TotalMilliseconds : 100;
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

            SpacialElement target = null;
            float winningCandidateProximity = float.MaxValue;
            targets.Clear();
            foreach (var element in SpaceTime.CurrentSpaceTime.Elements)
            {
                if (element.ZIndex != Options.Source.Element.ZIndex) continue;
                if (element.HasSimpleTag(Options.TargetTag) == false) continue;

                if (element is Character && (element as Character).IsVisible == false) continue;

                var sb = Options.SourceBounds;
                var angle = sb.Center().CalculateAngleTo(element.Center());
                var delta = Options.Source.Angle.DiffAngle(angle);
                if (delta >= Options.AngularVisibility) continue;

                var prediction = HitDetection.PredictHit(new HitDetectionOptions()
                {
                    Angle = angle,
                    MovingObject = sb,
                    Obstacles = obstacles,
                    Visibility = 3 * SpaceTime.CurrentSpaceTime.Bounds.Hypotenous(),
                    Mode = CastingMode.Rough,
                });

                var elementHit = prediction.ObstacleHit as SpacialElement;

                if (elementHit == element)
                {
                    targets.Add(elementHit);
                    var d = Geometry.CalculateNormalizedDistanceTo(Options.Source.Element, element);
                    if(d < winningCandidateProximity)
                    {
                        target = elementHit;
                        winningCandidateProximity = d;
                    }
                }
                else if (elementHit != null && element is IHaveMassBounds && (element as IHaveMassBounds).IsPartOfMass(elementHit))
                {
                    targets.Add(elementHit);
                    var d = Geometry.CalculateNormalizedDistanceTo(Options.Source.Element, element);
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
