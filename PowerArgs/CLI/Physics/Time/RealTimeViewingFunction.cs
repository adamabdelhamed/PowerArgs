using System;

namespace PowerArgs.Cli.Physics
{
    public class RealTimeViewingFunction
    {
        public Event<bool> Behind => behindSignal.ActiveChanged;



        public bool Enabled
        {
            get
            {
                return impl != null;
            }
            set
            {
                if (Enabled == false && value)
                {
                    Enable();
                }
                else if (Enabled == true && !value)
                {
                    Disable();
                }
            }
        }

        private DebounceableSignal behindSignal = new DebounceableSignal()
        {
            Threshold = 100, // we've fallen behind if we're 100ms off of wall clock time
            CoolDownAmount = 30, // we're not back on track until we are within 70 ms of wall clock time
        };

        private DateTime wallClockTimeAdded;
        private TimeSpan timeAdded;
        private Time t;
        private ITimeFunction impl;
        public RealTimeViewingFunction(Time t)
        {
            this.t = t;
        }

        private void Enable()
        {
            wallClockTimeAdded = DateTime.UtcNow;
            timeAdded = t.Now;
            impl = TimeFunction.Create(Evaluate);
            impl.Lifetime.LifetimeManager.Manage(() => { impl = null; });
            t.Add(impl);
        }

        private void Disable()
        {
            impl.Lifetime.Dispose();
        }

        public void Evaluate()
        {
            var realTimeNow = DateTime.UtcNow;
            // while the simulation time is ahead of the wall clock, spin
            var wallClockTimeElapsed = realTimeNow - wallClockTimeAdded;
            var age = t.Now - timeAdded;
            while (age > wallClockTimeElapsed)
            {
                wallClockTimeElapsed = DateTime.UtcNow - wallClockTimeAdded;
            }

            var idleTime = DateTime.UtcNow - realTimeNow;
            var idlePercentage = idleTime.TotalSeconds / t.Increment.TotalSeconds;

            age = t.Now - timeAdded;

            // At this point, we're sure that the wall clock is equal to or ahead of the simulation time.

            // If the wall clock is ahead by too much then the simulation is falling behind. Calculate the amount. 
            var behindAmount = (wallClockTimeElapsed - age).TotalMilliseconds;

            // Send the latest behind amount to the behind signal debouncer.
            behindSignal.Update(behindAmount);
        }
    }
}