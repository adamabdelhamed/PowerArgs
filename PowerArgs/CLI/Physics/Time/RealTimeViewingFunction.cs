using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
        public ConsoleString SleepSummary
        {
            get
            {
                var min = Geometry.Round(sleepTimeAverage.Min);
                var max = Geometry.Round(sleepTimeAverage.Max);
                var avg = Geometry.Round(sleepTimeAverage.Average);

                var color = min > 20 ? RGB.Green : min > 5 ? RGB.Yellow : RGB.Red;
                return $"Min:{min}, Max:{max}, Avg:{avg}".ToConsoleString(color);
            }
        }

        public List<DataPoint> SleepHistory { get; set; } 

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
        private Lifetime impl;

        private Queue<TaskCompletionSource<bool>> invokeSoonQueue = new Queue<TaskCompletionSource<bool>>();

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

            t.OnDisposed(() =>
            {
                foreach(var tcs in invokeSoonQueue)
                {
                    tcs.SetCanceled();
                }
            });
        }

        public Task WaitForFreeTime()
        {
            var tcs = new TaskCompletionSource<bool>();
            Time.CurrentTime.DelayThen((int)Time.CurrentTime.Increment.TotalMilliseconds, () => invokeSoonQueue.Enqueue(tcs));
            return tcs.Task;
        }

        public bool SignalPauseFrame { get; set; }

        private void Enable()
        {
            wallClockSample = DateTime.UtcNow;
            simulationTimeSample = t.Now;
            impl = new Lifetime();

            t.Invoke(async () =>
            {
                while(t.IsRunning && t.IsDrainingOrDrained == false && impl != null)
                {
                    Evaluate();
                    await t.YieldAsync();
                }
            });
        }

     
        private void Disable()
        {
            impl.Dispose();
            impl = null;
        }
    
        private void Evaluate()
        {
            var realTimeNow = DateTime.UtcNow;
            // while the simulation time is ahead of the wall clock, spin
            var wallClockTimeElapsed = TimeSpan.FromSeconds(1 * (realTimeNow - wallClockSample).TotalSeconds);
            var simulationTimeElapsed = TimeSpan.FromSeconds(SlowMoRatio * (t.Now - simulationTimeSample).TotalSeconds);
            var slept = false;

            var sleepTime = SignalPauseFrame ? Time.CurrentTime.Increment : simulationTimeElapsed - wallClockTimeElapsed;
            SignalPauseFrame = false;
            if (Enabled && simulationTimeElapsed > wallClockTimeElapsed)
            {
                while (sleepTime.TotalMilliseconds > Time.CurrentTime.Increment.TotalMilliseconds*.2 && invokeSoonQueue.Count > 0)
                {
                    var todo = invokeSoonQueue.Dequeue();
                    todo.SetResult(true);
                    realTimeNow = DateTime.UtcNow;
                    wallClockTimeElapsed = TimeSpan.FromSeconds(1 * (realTimeNow - wallClockSample).TotalSeconds);
                    simulationTimeElapsed = TimeSpan.FromSeconds(SlowMoRatio * (t.Now - simulationTimeSample).TotalSeconds);
                    sleepTime = simulationTimeElapsed - wallClockTimeElapsed;
                }

                if (sleepTime > TimeSpan.Zero)
                {
                    Thread.Sleep(sleepTime);
                    slept = true;
                }
            }

            if (Time.CurrentTime.Now > TimeSpan.FromSeconds(1))
            {
                SleepHistory?.Add(new DataPoint() { X = Time.CurrentTime.Now.Ticks, Y = Geometry.Round(sleepTime.TotalMilliseconds) });
            }

            wallClockTimeElapsed = DateTime.UtcNow - wallClockSample;

            if (slept == false)
            {
                ZeroSleepCycles++;
            }
            else
            {
                SleepCycles++;
            }

            var idleTime = Math.Min(t.Increment.TotalMilliseconds, (DateTime.UtcNow - realTimeNow).TotalMilliseconds);


            busyPercentageAverage.AddSample(1 - (idleTime / t.Increment.TotalMilliseconds));
            sleepTimeAverage.AddSample(idleTime);
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