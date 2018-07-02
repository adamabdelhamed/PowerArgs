using PowerArgs.Cli.Physics;
using System;

namespace ConsoleGames
{
    public class FireAtWill : IBotStrategy
    {
        public Character Me { get; set; }
        public RateGovernor EvalGovernor { get; } = new RateGovernor(TimeSpan.FromSeconds(1));

        public DecisionSpace DecisionSpace => DecisionSpace.PrimaryWeapon;

        public StrategyEval EvaluateApplicability()
        {
            var canFire = (Me.Inventory).PrimaryWeapon != null &&
                (Me.Inventory).PrimaryWeapon.AmmoAmount > 0;

            if (canFire == false) return new StrategyEval() { Applicability = 0, Strategy = this };

            // todo - Fix line of sight and then uncomment
            /*
            var hasLineOfSight = SpaceExtensions
                .CalculateLineOfSight(Me, Location.Create(Target.CenterX, Target.CenterY), .5f)
                .Obstacles.Count == 0;
                */

            var hasLineOfSight = true;
            if(hasLineOfSight == false)
            {
                return new StrategyEval() { Applicability = 0, Strategy = this };
            }

            var d = Me.CalculateDistanceTo(Me.Target);
            if (d > 20) return new StrategyEval() { Applicability = 0, Strategy = this };
            else if(d > 15) return new StrategyEval() { Applicability = .2f, Strategy = this };
            else if (d > 10) return new StrategyEval() { Applicability = .5f, Strategy = this };
            else return new StrategyEval() { Applicability = 1f, Strategy = this };
        }

        public void Work()
        {
            Me.Inventory?.PrimaryWeapon?.TryFire();
        }
    }
}
