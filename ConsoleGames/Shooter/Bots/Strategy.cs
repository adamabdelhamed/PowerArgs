using PowerArgs.Cli.Physics;
using System;

namespace ConsoleGames.Shooter
{
    [Flags]
    public enum DecisionSpace
    {
        Exclusive,
        PrimaryWeapon,
        ExplosiveWeapon,
        Movement,
        None,
    }

    public interface IBotStrategy
    {
        DecisionSpace DecisionSpace { get;  }
        ShooterCharacter Me { get; set; }
        RateGovernor EvalGovernor { get; }
        StrategyEval EvaluateApplicability();
        void Work();
    }

    public class StrategyEval
    {
        public IBotStrategy Strategy { get; set; }
        public float Applicability { get; set; }
    }
}
