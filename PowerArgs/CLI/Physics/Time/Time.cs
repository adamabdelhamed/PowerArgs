using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PowerArgs.Cli.Physics
{
 
    /// <summary>
    /// A model of time that lets you plug time functions and play them out on a thread. Each iteration of the time loop processes queued actions,
    /// executes time functions in order, and then increments the Now value.
    /// </summary>
    public class Time : EventLoop, IDelayProvider
    {
        [ThreadStatic]
        private static Time current;

        /// <summary>
        /// Gets the time model running on the current thread. 
        /// </summary>
        public static Time CurrentTime => current;

        /// <summary>
        /// An event that fires when a time function is added to the model
        /// </summary>
        public Event<ITimeFunction> TimeFunctionAdded { get; private set; } = new Event<ITimeFunction>();

        /// <summary>
        /// An event that fires when a time function is removed from the model
        /// </summary>
        public Event<ITimeFunction> TimeFunctionRemoved { get; private set; } = new Event<ITimeFunction>();


        /// <summary>
        /// The current time
        /// </summary>
        public TimeSpan Now { get; private set; }

        /// <summary>
        /// The amount to add to the value of 'Now' after each tick.
        /// </summary>
        public TimeSpan Increment { get; set; }

        /// <summary>
        /// Enumerates all of the time functions that are a part of the model as of now.
        /// </summary>
        public IEnumerable<ITimeFunction> Functions => EnumerateFunctions();

        private List<ITimeFunction> timeFunctions = new List<ITimeFunction>();
        private Random rand = new Random();
        private Dictionary<string, ITimeFunction> idMap = new Dictionary<string, ITimeFunction>();
        private Lifetime myLifetime;

        /// <summary>
        /// Creates a new time model, optionally providing a starting time and increment
        /// </summary>
        /// <param name="increment">The amount of time to increment on each iteration, defaults to one 100 nanosecond tick</param>
        /// <param name="now">The starting time, defaults to zero</param>
        public Time(TimeSpan? increment = null, TimeSpan? now = null)
        {
            Increment = increment.HasValue ? increment.Value : TimeSpan.FromTicks(1);
            Now = now.HasValue ? now.Value : TimeSpan.Zero;
            InvokeNextCycle(() => current = this);
            myLifetime = new Lifetime();
            EndOfCycle.SubscribeForLifetime(() => Now = Now.Add(Increment), myLifetime);
        }

        public async Task DelayFuzzyAsync(float ms, double maxDeltaPercentage = .1)
        {
            var maxDelta = maxDeltaPercentage * ms;
            var min = ms - maxDelta;
            var max = ms + maxDelta;
            var delay = rand.Next((int)min, (int)max);
            await DelayAsync(delay);
        }

        public async Task DelayAsync(double ms) => await DelayAsync(TimeSpan.FromMilliseconds(ms));

        public async Task DelayAsync(TimeSpan timeout)
        {
            if (timeout == TimeSpan.Zero) throw new ArgumentException("Delay for a time span of zero is not supported because there's a good chance you're putting it in a loop on the time thread, which will block the thread. You may want to call DelayOrYield (extension method).");
            var startTime = Now;
            await DelayAsync(() => Now - startTime >= timeout);
        }

        public async Task DelayAsync(Event ev, TimeSpan? timeout = null, TimeSpan? evalFrequency = null)
        {
            var fired = false;

            ev.SubscribeOnce(() =>
            {
                fired = true;
            });

            await DelayAsync(() => fired, timeout, evalFrequency);
        }

        public async Task YieldAsync() => await Task.Yield();

        public async Task DelayAsync(Func<bool> condition, TimeSpan? timeout = null, TimeSpan? evalFrequency = null)
        {
            if (await TryDelayAsync(condition, timeout, evalFrequency) == false)
            {
                throw new TimeoutException("Timed out awaiting delay condition");
            }
        }

        public async Task<bool> TryDelayAsync(Func<bool> condition, TimeSpan? timeout = null, TimeSpan? evalFrequency = null)
        {
            var startTime = Now;
            var governor = evalFrequency.HasValue ? new RateGovernor(evalFrequency.Value, lastFireTime: startTime) : null;
            while (true)
            {
                if (governor != null && governor.ShouldFire(Now) == false)
                {
                    await Task.Yield();
                }
                else if (condition())
                {
                    return true;
                }
                else if (timeout.HasValue && Now - startTime >= timeout.Value)
                {
                    return false;
                }
                else
                {
                    await Task.Yield();
                }
            }
        }


        /// <summary>
        /// Gets the time function with the given id. Ids must be populated at the time it was
        /// added in order to be tracked.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ITimeFunction this[string id] { get => idMap[id]; }

        /// <summary>
        /// Adds the given time function to the model. This method must be called from the time thread.
        /// </summary>
        /// <typeparam name="T">The type of the time function</typeparam>
        /// <param name="timeFunction">the time function to add</param>
        /// <returns>the time function that was passed in</returns>
        public T Add<T>(T timeFunction) where T : ITimeFunction
        {
            AssertIsThisTimeThread();
            timeFunction.InternalState.AddedTime = Now;
            timeFunction.InternalState.AttachedTime = this;
            timeFunctions.Add(timeFunction);
            if(timeFunction.Id != null)
            {
                idMap.Add(timeFunction.Id, timeFunction);
            }

            timeFunction.Lifetime.OnDisposed(() =>
            {
                timeFunctions.Remove(timeFunction);
                if(timeFunction.Id != null && idMap.ContainsKey(timeFunction.Id))
                {
                    idMap.Remove(timeFunction.Id);
                }
                TimeFunctionRemoved.Fire(timeFunction);
                timeFunction.InternalState.AttachedTime = null;
            });

            TimeFunctionAdded.Fire(timeFunction);
            timeFunction.Added.Fire();
            return timeFunction;
        }

        /// <summary>
        /// Call this method to guard against code running on this model's time thread. It will throw an InvalidOperationException
        /// if the check fails.
        /// </summary>
        public void AssertIsThisTimeThread()
        {
            if (this != CurrentTime)
            {
                throw new InvalidOperationException("Code not running on time thread");
            }
        }

        /// <summary>
        /// Asserts that there is a time model running on the current thread
        /// </summary>
        public static void AssertTimeThread()
        {
            if (CurrentTime == null)
            {
                throw new InvalidOperationException("Code not running on time thread");
            }
        }

        private IEnumerable<ITimeFunction> EnumerateFunctions() => timeFunctions.ToArray();
    }
}
