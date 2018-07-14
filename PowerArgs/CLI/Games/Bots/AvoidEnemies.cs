using PowerArgs.Cli.Physics;
using System;
using PowerArgs;
using System.Linq;

namespace PowerArgs.Games
{
    public class AvoidEnemies : IBotStrategy
    {
        public Character Me { get; set; }
        public RateGovernor EvalGovernor { get; private set; } 
        public DecisionSpace DecisionSpace => DecisionSpace.Movement;

        public AvoidEnemies()
        {
            EvalGovernor = new RateGovernor(TimeSpan.FromSeconds(.5));
        }

        public StrategyEval EvaluateApplicability()
        {
            var enemies = SpaceTime.CurrentSpaceTime.Elements.WhereAs<Enemy>().OrderBy(e => e.CalculateDistanceTo(Me)).ToList();

            if (enemies.Count == 0)
            {
                return new StrategyEval()
                {
                    Applicability = 0,
                    Strategy = this
                };
            }
            else
            {
                var closestDistance = Me.CalculateDistanceTo(enemies.First());
                Me.Target = enemies.First();
                var minThreatDistance = 15;
                if (closestDistance > minThreatDistance)
                {
                    return new StrategyEval() { Applicability = 0, Strategy = this };
                }
                else
                {
                    var delta = 15f - closestDistance;
                    var threat = delta / 15f;
                    return new StrategyEval() { Applicability = threat, Strategy = this };
                } }
        }

        public void Work()
        {
            var angleToTarget = Me.CalculateAngleTo(Me.Target);
            var oppositeAngle = angleToTarget;// SpaceExtensions.GetOppositeAngle(angleToTarget);
            var newLocation = SpaceExtensions.MoveTowards(Me.TopLeft(), oppositeAngle, 1);
            var overlapCount = SpaceTime.CurrentSpaceTime.Elements.Where(e => Rectangular.Create(newLocation.Left, newLocation.Top, 1, 1).OverlapPercentage(e) > 0).Count();
            if (overlapCount == 0)
            {
                Me.MoveTo(newLocation.Left, newLocation.Top);
            }
        }
    }
}
