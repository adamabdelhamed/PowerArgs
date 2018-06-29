using PowerArgs.Cli.Physics;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleGames.Shooter
{
    public class Bot : SpacialElementFunction
    {
        private List<IBotStrategy> strategies;
        private StrategyEval currentStrategy;
        private ShooterCharacter me;
  

        public Bot(ShooterCharacter toAnimate, IEnumerable<IBotStrategy> strategies) : base(toAnimate)
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
            var newStrategyCandidate = strategies
                 .Where(s => s.EvalGovernor.ShouldFire(Time.CurrentTime.Now))
                 .Select(s => s.EvaluateApplicability())
                 .Where(s => s.Applicability > 0)
                 .OrderByDescending(r => r.Applicability)
                 .SingleOrDefault();

            if (newStrategyCandidate != null)
            {
                newStrategyCandidate.Strategy.Work();
                currentStrategy = newStrategyCandidate;
            }
            else if(currentStrategy != null && currentStrategy.Strategy.EvalGovernor.ShouldFire(Time.CurrentTime.Now))
            {
                currentStrategy.Strategy.Work();
            }
        }
    }
}
