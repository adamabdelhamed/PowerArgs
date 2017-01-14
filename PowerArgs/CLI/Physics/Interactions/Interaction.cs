using System;

namespace PowerArgs.Cli.Physics
{
    public class Interaction : Lifetime
    {
        public Event Added { get; private set; } = new Event();
        public Event Removed { get; private set; } = new Event();
        public Event Updated { get; private set; } = new Event();

        public Scene Scene{ get; internal set; }
        public long Id { get; internal set; }
        public RateGovernor Governor { get; private set; }
        public TimeSpan LastBehavior { get; internal set; }
        public Interaction()
        {
            Governor = new RateGovernor();
        }

        public virtual void Initialize(Scene scene) { }
        public virtual void Behave(Scene scene) { }
    }
}
