using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Linq;

namespace PowerArgs.Games
{
    public class AutoTargetingFunction : TimeFunction
    {
        private Func<IRectangular> sourceEval;
        private Func<SpacialElement, bool> targetFilter;
        private Func<SpacialElement, bool> obstacleFilter;

        public Event<SpacialElement> TargetChanged { get; private set; } = new Event<SpacialElement>();

        public AutoTargetingFunction(Func<IRectangular> sourceEval, Func<SpacialElement, bool> targetFilter, Func<SpacialElement, bool> obstacleFilter)
        {
            this.sourceEval = sourceEval;
            this.obstacleFilter = obstacleFilter;
            this.targetFilter = targetFilter;
        }

        public override void Initialize() { }

        public override void Evaluate()
        {
            var targets = SpaceTime.CurrentSpaceTime.Elements.Where(t => targetFilter(t))
                .OrderBy(z => sourceEval().CalculateDistanceTo(z));

            foreach (var target in targets)
            {
                var hasLineOfSight = SpaceExtensions.HasLineOfSightRounded(sourceEval(), target,SpaceTime.CurrentSpaceTime.Elements.Where(e => obstacleFilter(e) || targetFilter(e)).Select(e => e as IRectangular).ToList(), 1);

                if (hasLineOfSight)
                {
                    TargetChanged.Fire(target);
                    return;
                }
            }

            TargetChanged.Fire(null);
        }
    }
}
