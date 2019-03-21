using PowerArgs.Cli.Physics;
using System;

namespace PowerArgs.Games
{
    public class NoOpStrategyOptions
    {
        public TimeSpan Duration { get; set; }
        public Action Success { get; set; }
    }
    public class NoOpStrategy : IBotStrategy
    {
        public Character Me { get; set; }
        public RateGovernor EvalGovernor { get; private set; } 
        private NoOpStrategyOptions options;

        public NoOpStrategy(NoOpStrategyOptions options)
        {
            this.options = options;
            EvalGovernor = new RateGovernor(options.Duration);
        }

        public void Work() => options.Success();
    }
}
