using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Linq;

namespace ConsoleGames
{
    public class AutoTargetingFunction : TimeFunction
    {
        private Func<IRectangular> sourceEval;
        private Func<SpacialElement, bool> targetFilter;

        public Event<SpacialElement> TargetChanged { get; private set; } = new Event<SpacialElement>();

        public AutoTargetingFunction(Func<IRectangular> sourceEval, Func<SpacialElement, bool> targetFilter)
        {
            this.sourceEval = sourceEval;
            this.targetFilter = targetFilter;
        }

        public override void Initialize() { }

        public override void Evaluate()
        {
            var targets = SpaceTime.CurrentSpaceTime.Elements.Where(t => targetFilter(t))
                .OrderBy(z => sourceEval().CalculateDistanceTo(z));

            foreach (var target in targets)
            {
                var route = SpaceExtensions.CalculateLineOfSight(sourceEval(), target.Bounds.Center(), 1);

                if (route.Obstacles.Where(o => o is Wall).Count() == 0)
                {
                    TargetChanged.Fire(target);
                    return;
                }
            }

            TargetChanged.Fire(null);
        }
    }
}
