using System;
using System.Threading;

namespace PowerArgs.Cli.Physics
{
    /// <summary>
    /// A time function that ensures that its target time simulation does not proceed
    /// faster than the system's wall clock
    /// </summary>
    public class RealTimeViewingFunction
    {
        private RollingAverage busyPercentageAverage = new RollingAverage(30);
        public double BusyPercentage => busyPercentageAverage.Average;

        private RollingAverage sleepTimeAverage = new RollingAverage(30);
        public double SleepTime => sleepTimeAverage.Average;

        public int ZeroSleepCycles { get; private set; }
        public int SleepCycles { get; private set; }

        /// <summary>
        /// 1 is normal speed. Make bigger to slow down the simulation. Make smaller fractions to speed it up.
        /// </summary>
        public float SlowMoRatio { get; set; } = 1;

        /// <summary>
        /// An event that fires when the target time simulation falls behind or catches up to
        /// the wall clock
        /// </summary>
        public Event<bool> Behind => behindSignal.ActiveChanged;

        /// <summary>
        /// Enables or disables the real time viewing function
        /// </summary>
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

        private DebounceableSignal behindSignal;
        private DateTime wallClockSample;
        private TimeSpan simulationTimeSample;
        private Time t;
        private ITimeFunction impl;

        /// <summary>
        /// Creates a realtime viewing function
        /// </summary>
        /// <param name="t">the time simulation model to target</param>
        /// <param name="fallBehindThreshold">The time model will be determined to have fallen behind if the simulation falls
        /// behind the system wall clock by more than this amound (defaults to 100 ms)</param>
        /// <param name="fallBehindCooldownPeriod">When in the behind state the time simulation must surpass the FallBehindThreshold
        /// by this amount before moving out of the behind state. This is a debouncing mechanism.</param>
        public RealTimeViewingFunction(Time t, TimeSpan? fallBehindThreshold = null, TimeSpan? fallBehindCooldownPeriod = null)
        {
            behindSignal = new DebounceableSignal()
            {
                Threshold = fallBehindThreshold.HasValue ? fallBehindThreshold.Value.TotalMilliseconds : 100, // we've fallen behind if we're 100ms off of wall clock time
                CoolDownAmount = fallBehindCooldownPeriod.HasValue ? fallBehindCooldownPeriod.Value.TotalMilliseconds : 30, // we're not back on track until we are within 70 ms of wall clock time
            };
            this.t = t;
        }

        private void Enable()
        {
            wallClockSample = DateTime.UtcNow;
            simulationTimeSample = t.Now;
            impl = TimeFunction.Create(Evaluate);
            impl.Lifetime.OnDisposed(() => { impl = null; });
            t.Add(impl);
        }
 

        private void Disable()
        {
            impl.Lifetime.Dispose();
            impl = null;
        }
    
        internal void Evaluate()
        {
            var realTimeNow = DateTime.UtcNow;
            // while the simulation time is ahead of the wall clock, spin
            var wallClockTimeElapsed = TimeSpan.FromSeconds(1 * (realTimeNow - wallClockSample).TotalSeconds);
            var simulationTimeElapsed = TimeSpan.FromSeconds(SlowMoRatio * (t.Now - simulationTimeSample).TotalSeconds);
            var slept = false;

            if (Enabled && simulationTimeElapsed > wallClockTimeElapsed)
            {
                var sleepTime = simulationTimeElapsed - wallClockTimeElapsed;
                Thread.Sleep(sleepTime);
                slept = true;
            }

            wallClockTimeElapsed = DateTime.UtcNow - wallClockSample;

            if (slept == false)
            {
                ZeroSleepCycles++;
                Time.CurrentTime.QueueAction("Clogged", () => { });
            }
            else
            {
                SleepCycles++;
            }

            var idleTime = DateTime.UtcNow - realTimeNow;
            busyPercentageAverage.AddSample(1 - (idleTime.TotalSeconds / t.Increment.TotalSeconds));
            sleepTimeAverage.AddSample(idleTime.TotalMilliseconds);
            simulationTimeElapsed = t.Now - simulationTimeSample;

            // At this point, we're sure that the wall clock is equal to or ahead of the simulation time.

            // If the wall clock is ahead by too much then the simulation is falling behind. Calculate the amount. 
            var behindAmount = wallClockTimeElapsed - simulationTimeElapsed;

            // Send the latest behind amount to the behind signal debouncer.
            behindSignal.Update(behindAmount.TotalMilliseconds);
            wallClockSample = DateTime.UtcNow;
            simulationTimeSample = t.Now;
        }
    }
}