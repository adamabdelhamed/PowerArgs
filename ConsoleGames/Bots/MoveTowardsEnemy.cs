using PowerArgs.Cli.Physics;
using System;

namespace ConsoleGames
{
    public class MoveTowardsEnemy : IBotStrategy
    {
        public Character Me { get; set; }
        public RateGovernor EvalGovernor { get; private set; } 

        public DecisionSpace DecisionSpace => DecisionSpace.Movement;

        public MoveTowardsEnemy()
        {
            EvalGovernor = new RateGovernor(TimeSpan.FromSeconds(.1));
        }

        public StrategyEval EvaluateApplicability()
        {
            return new StrategyEval()
            {
                Applicability = Me.Target != null && Me.CalculateDistanceTo(Me.Target) > 4 ? 1 : 0,
                Strategy = this
            };
        }

        public void Work()
        {
            Waypoint.MoveTowards(Me, Me.Target, 1);
        }
    }
}
