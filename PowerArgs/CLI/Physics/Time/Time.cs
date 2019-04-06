using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PowerArgs.Cli.Physics
{
    /// <summary>
    /// A model of time that lets you plug time functions and play them out on a thread. Each iteration of the time loop processes queued actions,
    /// executes time functions in order, and then increments the Now value.
    /// </summary>
    public class Time : IDelayProvider
    {
        internal class CustomSyncContext : SynchronizationContext
        {
            private Time t;
            public CustomSyncContext(Time t) { this.t = t; }

            public override void Post(SendOrPostCallback d, object state) => t.QueueAction(() => d.Invoke(state));

            public override void Send(SendOrPostCallback d, object state) => t.QueueAction(() => d.Invoke(state));
        }

        private class StopTimeException : Exception { }


        public ConsoleApp Application { get; set; }

        public ITimeFunction CurrentlyRunningFunction { get; private set; }

        private class WorkItem
        {
            public Action Work { get; set; }

            public Deferred Deferred { get; set; }

            public WorkItem(Action work)
            {
                this.Work = work;
                Deferred = Deferred.Create();
            }
        }

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
        /// An event that fires when there is an unhandled exception on the model's thread
        /// </summary>
        public Event<Exception> UnhandledException { get; private set; } = new Event<Exception>();

        /// <summary>
        /// An event that fires after the time is incremented.
        /// </summary>
        public Event AfterTick { get; private set; } = new Event();

        /// <summary>
        /// The current time
        /// </summary>
        public TimeSpan Now { get; private set; }

        /// <summary>
        /// The amount to add to the value of 'Now' after each tick.
        /// </summary>
        public TimeSpan Increment { get; private set; }


        /// <summary>
        /// Tells you if the time thread is currently running
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Enumerates all of the time functions that are a part of the model as of now.
        /// </summary>
        public IEnumerable<ITimeFunction> Functions => EnumerateFunctions();

        private List<ITimeFunction> timeFunctions = new List<ITimeFunction>();
        private List<ITimeFunction> toAdd = new List<ITimeFunction>();
        private List<ITimeFunction> toRemove = new List<ITimeFunction>();
        private Deferred runDeferred;
        private List<WorkItem> syncQueue = new List<WorkItem>();

        /// <summary>
        /// Creates a new time model, optionally providing a starting time and increment
        /// </summary>
        /// <param name="increment">The amount of time to increment on each iteration, defaults to one 100 nanosecond tick</param>
        /// <param name="now">The starting time, defaults to zero</param>
        public Time(TimeSpan? increment = null, TimeSpan? now = null)
        {
            Increment = increment.HasValue ? increment.Value : TimeSpan.FromTicks(1);
            Now = now.HasValue ? now.Value : TimeSpan.Zero;
        }

        /// <summary>
        /// Starts the time simulation thread
        /// </summary>
        /// <returns>A promise that represents the end of the time simulation</returns>
        public Promise Start()
        {
            runDeferred = Deferred.Create();
            runDeferred.Promise.Finally((p) => { runDeferred = null; });
            Thread t = new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(new CustomSyncContext(this));

                IsRunning = true;
                try
                {
                    current = this;
                    while (true)
                    {
                        List<WorkItem> syncActions = new List<WorkItem>();
                        lock (syncQueue)
                        {
                            while (syncQueue.Count > 0)
                            {
                                var workItem = syncQueue[0];
                                syncQueue.RemoveAt(0);
                                syncActions.Add(workItem);
                            }
                        }

                        foreach (var syncAction in syncActions)
                        {
                            try
                            {
                                syncAction.Work();
                            }
                            catch (Exception ex)
                            {
                                if (syncAction.Deferred.HasExceptionListeners)
                                {
                                    syncAction.Deferred.Reject(ex);
                                }
                                else
                                {
                                    throw;
                                }
                            }
                            syncAction.Deferred.Resolve();
                        }

                        Tick();
                        AfterTick.Fire();
                    }
                }
                catch (StopTimeException)
                {
                    IsRunning = false;
                    runDeferred.Resolve();
                }
                catch (Exception ex)
                {
                    IsRunning = false;
                    UnhandledException.Fire(ex);
                    runDeferred.Reject(ex);
                }
            })
            { Name = "TimeThread" };
            t.Start();

            return runDeferred.Promise;
        }

        /// <summary>
        /// Stops the time model
        /// </summary>
        /// <returns>A promise that will complete when the simulation finishes</returns>
        public Promise Stop()
        {
            if (runDeferred == null)
            {
                throw new InvalidOperationException("Not running");
            }

            var p = runDeferred.Promise;
            QueueAction(() => { throw new StopTimeException(); });
            return p;
        }

        public async Task DelayAsync(double ms) => await DelayAsync(TimeSpan.FromMilliseconds(ms));

        public async Task DelayAsync(TimeSpan timeout)
        {
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

        public async Task SetInterval(Action action, TimeSpan interval, ILifetimeManager lifetime)
        {
            var shouldRun = true;
            lifetime.OnDisposed(() => shouldRun = false);
            while (shouldRun)
            {
                await DelayAsync(interval);
                action();
            }
        }


        /// <summary>
        /// Queues an action that will run at the beginning of the next time iteration
        /// </summary>
        /// <param name="action">code to run at the beginning of the next time iteration</param>
        public Promise QueueAction(Action action)
        {
            lock (syncQueue)
            {
                var workItem = new WorkItem(action);
                syncQueue.Add(workItem);
                return workItem.Deferred.Promise;
            }
        }

        public Promise QueueActionInFront(Action action)
        {
            lock (syncQueue)
            {
                var workItem = new WorkItem(action);
                syncQueue.Insert(0, workItem);
                return workItem.Deferred.Promise;
            }
        }

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
            toAdd.Add(timeFunction);

            timeFunction.Lifetime.OnDisposed(() =>
            {
                toRemove.Add(timeFunction);
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


        private void Tick()
        {
            for (var i = 0; i < timeFunctions.Count; i++)
            {
                if (timeFunctions[i].Lifetime.IsExpired == false && timeFunctions[i].Governor.ShouldFire(Now))
                {
                    CurrentlyRunningFunction = timeFunctions[i];
                    CurrentlyRunningFunction.Evaluate();
                    CurrentlyRunningFunction = null;
                }
            }

            for (var i = 0; i < toAdd.Count; i++)
            {
                timeFunctions.Add(toAdd[i]);

                if (toAdd[i].Lifetime.IsExpired == false && toAdd[i].Governor.ShouldFire(Now))
                {
                    CurrentlyRunningFunction = toAdd[i];
                    CurrentlyRunningFunction.Evaluate();
                    CurrentlyRunningFunction = null;
                }
            }

            toAdd.Clear();

            for (var i = 0; i < toRemove.Count; i++)
            {
                timeFunctions.Remove(toRemove[i]);
            }

            toRemove.Clear();

            Now += Increment;
        }

        private IEnumerable<ITimeFunction> EnumerateFunctions()
        {
            foreach (var func in timeFunctions)
            {
                if (func.Lifetime.IsExpired == false)
                {
                    yield return func;
                }
            }

            foreach (var func in toAdd)
            {
                if (func.Lifetime.IsExpired == false)
                {
                    yield return func;
                }
            }
        }
    }
}
