using PowerArgs.Cli.Physics;

namespace ConsoleGames.Shooter
{
    public interface IBotStrategy
    {
        Character Me { get; set; }
        Character Target { get; set; }
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
