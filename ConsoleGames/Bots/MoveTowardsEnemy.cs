using PowerArgs.Cli.Physics;
using System;

namespace ConsoleGames
{
    public class MoveTowardsEnemy : IBotStrategy
    {
        public Character Me { get; set; }
        public RateGovernor EvalGovernor { get; private set; } = new RateGovernor(TimeSpan.FromSeconds(.25f));

        public DecisionSpace DecisionSpace => DecisionSpace.Movement;

        public MoveTowardsEnemy()
        {
            
        }

        public StrategyEval EvaluateApplicability()
        {
            var ret = new StrategyEval()
            {
                Applicability = Me.Target != null && Me.CalculateDistanceTo(Me.Target) > 4 ? 1 : .25f,
                Strategy = this
            };

            return ret;
        }

        public void Work()
        {
            if (Me.Target != null && Me.Target.Width > 0 && Me.Target.Height > 0)
            {
                Waypoint.MoveTowards(Me, Me.Target, 1);
            }
        }
    }
}
