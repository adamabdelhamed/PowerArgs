using PowerArgs.Cli.Physics;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Games
{
    public class Bot : SpacialElementFunction
    {
        private List<IBotStrategy> strategies;
        private Character me;

        public IEnumerable<IBotStrategy> Strategies => strategies.ToArray();

        public Bot(Character toAnimate, IEnumerable<IBotStrategy> strategies) : base(toAnimate)
        {
            this.strategies = strategies.ToList();
            this.me = toAnimate;
        }
        
        public override void Initialize()
        {
            strategies.ForEach(s => s.Me = me);
        }

        public override void Evaluate()
        {
            var newStrategyCandidateGroups = strategies
                 .Where(s => s.EvalGovernor.ShouldFire(Time.CurrentTime.Now))
                 .Select(s => s.EvaluateApplicability())
                 .Where(s => s.Applicability > 0)
                 .GroupBy(r => r.Strategy.DecisionSpace);

            foreach (var category in newStrategyCandidateGroups)
            {
                var bestStrategyInCategory = category.OrderByDescending(s => s.Applicability).First().Strategy;
                bestStrategyInCategory.Work();
            }
        }
    }
}
