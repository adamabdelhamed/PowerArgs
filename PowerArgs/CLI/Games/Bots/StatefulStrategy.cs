using PowerArgs.Cli.Physics;
using System;

namespace PowerArgs.Games
{
    public class StatefulStrategyOptions
    {
        public bool CanInterrupt { get; set; }
        public Action Cant { get; set; }
    }

    public abstract class StatefulStrategy : IBotStrategy
    {
        private IBotStrategy current;
        private Character me;
        private StatefulStrategyOptions options;

        public IBotStrategy CurrentState
        {
            get
            {
                return current;
            }
            set
            {
                current = value;
                if (current != null)
                {
                    current.Me = Me;
                }
            }
        }


        public Character Me
        {
            get => me;
            set
            {
                me = value;
                if (current != null && current.Me != me)
                {
                    current.Me = me;
                }
            }
        }

        public RateGovernor EvalGovernor { get; } = new RateGovernor(TimeSpan.FromSeconds(.05));

        public bool CanInterrupt => options.CanInterrupt;

        public StatefulStrategy(StatefulStrategyOptions options)
        {
            this.options = options;
        }

        public virtual void OnInterrupted() { }

        public void Work() => CurrentState?.Work();
    }
}
