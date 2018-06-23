using PowerArgs.Cli.Physics;
using System;

namespace ConsoleGames.Shooter
{
    public class FireAtWill : IBotStrategy
    {
        public Character Me { get; set; }
        public Character Target { get; set; }
        public RateGovernor EvalGovernor { get; } = new RateGovernor(TimeSpan.FromSeconds(1));
        public StrategyEval EvaluateApplicability()
        {
            var canFire = (Me.Inventory as ShooterInventory).PrimaryWeapon != null &&
                (Me.Inventory as ShooterInventory).PrimaryWeapon.AmmoAmount > 0;

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

            var d = Me.CalculateDistanceTo(Target);
            if (d > 20) return new StrategyEval() { Applicability = 0, Strategy = this };
            else if(d > 15) return new StrategyEval() { Applicability = .2f, Strategy = this };
            else if (d > 10) return new StrategyEval() { Applicability = .5f, Strategy = this };
            else return new StrategyEval() { Applicability = 1f, Strategy = this };
        }

        public void Work()
        {
            (Me.Inventory as ShooterInventory).PrimaryWeapon.TryFire();
        }
    }
}
