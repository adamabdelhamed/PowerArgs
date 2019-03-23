using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Games
{
    public class AutoTargetingOptions
    {
        public SpacialElement Source { get; set; }
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
        }

        public override void Initialize() { }

        public override void Evaluate()
        {
            var targets = options.TargetsEval().OrderBy(z => options.Source.CalculateDistanceTo(z));
            var obstacles = new HashSet<IRectangular>();

            foreach(var target in options.TargetsEval())
            {
                obstacles.Add(target);
            }

            foreach(var element in SpaceTime.CurrentSpaceTime.Elements.Where(e => e.ZIndex == options.Source.ZIndex))
            {
                obstacles.Add(element);
            }
            

            foreach (var target in targets)
            {
                var hasLineOfSight = SpaceExtensions.HasLineOfSightRounded(options.Source, target, obstacles.ToList(), 1);

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
