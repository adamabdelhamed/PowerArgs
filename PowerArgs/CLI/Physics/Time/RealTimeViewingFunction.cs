using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private RealTimeStateMachine rtStateMachine = new RealTimeStateMachine();

        public Event<RealTimeState> RealTimeStateChanged => rtStateMachine.StateChanged;

        public RollingAverage SleepTimeWindow { get; private set; } = new RollingAverage(20);
        public ConsoleString SleepSummary
        {
            get
            {
                var min = Geometry.Round(SleepTimeWindow.Min);
                var max = Geometry.Round(SleepTimeWindow.Max);
                var avg = Geometry.Round(SleepTimeWindow.Average);
                var p95 = Geometry.Round(SleepTimeWindow.Percentile(.95));

                var color = p95 > 20 ? RGB.Green : p95 > 5 ? RGB.Yellow : RGB.Red;
                return $"Min:{min}, Max:{max}, Avg:{avg}, P95: {p95}".ToConsoleString(color);
            }
        }

        public int ZeroSleepCycles { get; private set; }
        public int SleepCycles { get; private set; }

        /// <summary>
        /// 1 is normal speed. Make bigger to slow down the simulation. Make smaller fractions to speed it up.
        /// </summary>
        public float SlowMoRatio { get; set; } = 1;


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
            this.t = t;

            t.OnDisposed(() =>
            {
                foreach(var tcs in invokeSoonQueue)
                {
                    tcs.SetCanceled();
                }
            });
        }

        public async Task WaitForFreeTime()
        {
            var tcs = new TaskCompletionSource<bool>();
            await Time.CurrentTime.DelayAsync((int)Time.CurrentTime.Increment.TotalMilliseconds);
            invokeSoonQueue.Enqueue(tcs);
            await tcs.Task;
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
                    await Task.Yield();
                }
            });
        }

     
        private void Disable()
        {
            impl.Dispose();
            impl = null;
        }

        private Stopwatch rtsw = new Stopwatch();
        private void Evaluate()
        {
            var realTimeNow = DateTime.UtcNow;
            // while the simulation time is ahead of the wall clock, spin
            var wallClockTimeElapsed = TimeSpan.FromSeconds(1 * (realTimeNow - wallClockSample).TotalSeconds);
            var simulationTimeElapsed = TimeSpan.FromSeconds(SlowMoRatio * (t.Now - simulationTimeSample).TotalSeconds);
            var slept = false;

            var sleepTime = SignalPauseFrame ? Time.CurrentTime.Increment : (simulationTimeElapsed - wallClockTimeElapsed);
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
                    rtsw.Restart();
                    var togo = sleepTime.TotalMilliseconds - rtsw.ElapsedMilliseconds;
                    while(togo > 0)
                    {
                        if(togo > 25)
                        {
                            Thread.Sleep(1);
                        }
                        togo = sleepTime.TotalMilliseconds - rtsw.ElapsedMilliseconds;
                    }
                    rtsw.Stop();
                    slept = true;
                }
            }

            if (slept == false)
            {
                ZeroSleepCycles++;
            }
            else
            {
                SleepCycles++;
            }

            SleepTimeWindow.AddSample(sleepTime.TotalMilliseconds);
            if (Time.CurrentTime.Now >= TimeSpan.FromSeconds(1.5f))
            {
                rtStateMachine.Evaluate(SleepTimeWindow);
            }

            // At this point, we're sure that the wall clock is equal to or ahead of the simulation time.
            wallClockSample = DateTime.UtcNow;
            simulationTimeSample = t.Now;
        }
    }

    public enum RealTimeState
    {
        Cold,
        Warm,
        Hot
    }

    public class RealTimeStateMachine
    {
        public Event<RealTimeState> StateChanged { get; private set; } = new Event<RealTimeState>();
        public RealTimeState State { get; set; } = RealTimeState.Cold;
        private TimeSpan stateTime;

        private bool StateHasSettled => Time.CurrentTime.Now - stateTime >= TimeSpan.FromSeconds(3);

        private void ChangeState(RealTimeState newState)
        {
            State = newState;
            stateTime = Time.CurrentTime.Now;
            StateChanged.Fire(newState);
        }

        public void Evaluate(RollingAverage signal)
        {
            if (signal.IsWindowFull == false) return;

            var negTotal = 0.0;
            for(var i = 0; i < signal.Samples.Length; i++)
            {
                if(signal.Samples[i] < 0)
                {
                    negTotal += -signal.Samples[i];
                }
            }

            var threshold = signal.Average;
            var gettingWarmThreshold = 25f;   
            var coolingDownThreshold = 40f;


            var gettingWarmNegThreshold = 100f;
            var gettingHotNegThreshold = 200f;

            if (State == RealTimeState.Cold)
            {
                if(negTotal >= gettingHotNegThreshold)
                {
                    ChangeState(RealTimeState.Hot);
                }
                else if(threshold <= gettingWarmThreshold || negTotal >= gettingWarmNegThreshold)
                {
                    ChangeState(RealTimeState.Warm);
                }
            }
            else if(State == RealTimeState.Warm)
            {
                if (negTotal >= gettingHotNegThreshold)
                {
                    ChangeState(RealTimeState.Hot);
                }
                else if (threshold > coolingDownThreshold && StateHasSettled)
                {
                    ChangeState(RealTimeState.Cold);
                }
            }
            else if(State == RealTimeState.Hot)
            {
                if (negTotal == 0 && StateHasSettled)
                {
                    ChangeState(threshold > coolingDownThreshold ? RealTimeState.Cold : RealTimeState.Warm);
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}