using PowerArgs.Cli.Physics;
using System;

namespace PowerArgs.Games
{
    public interface IApplicableStrategy : IBotStrategy
    {
        StrategyEval EvaluateApplicability();
        bool CanInterrupt { get; }
        void OnInterrupted();
    }

    public interface IBotStrategy
    {
        Character Me { get; set; }
        RateGovernor EvalGovernor { get; }
        void Work();
    }

    public class StrategyEval
    {
        public IApplicableStrategy Strategy { get; set; }
        public float Applicability { get; set; }
    }
}
