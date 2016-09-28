using System;

namespace PowerArgs.Cli.Physics
{
    public class Interaction : Lifetime
    {
        public Event Added { get; private set; } = new Event();
        public Event Removed { get; private set; } = new Event();
        public Event Updated { get; private set; } = new Event();

        public Realm Realm{ get; internal set; }
        public long Id { get; internal set; }
        public RateGovernor Governor { get; private set; }
        public TimeSpan LastBehavior { get; internal set; }
        public Interaction()
        {
            Governor = new RateGovernor();
        }

        public virtual void Initialize(Realm realm) { }
        public virtual void Behave(Realm realm) { }
    }
}
