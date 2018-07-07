using System;
using System.Collections.Generic;
using System.Threading;

namespace PowerArgs.Cli.Physics
{
    /// <summary>
    /// A model of time that lets you plug time functions and play them out on a thread. Each iteration of the time loop processes queued actions,
    /// executes time functions in order, and then increments the Now value.
    /// </summary>
    public class Time
    {
        private class StopTimeException : Exception { }


        public ConsoleApp Application { get; set; }

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
        private Queue<WorkItem> syncQueue = new Queue<WorkItem>();

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
                                syncActions.Add(syncQueue.Dequeue());
                            }
                        }

                        foreach (var syncAction in syncActions)
                        {
                            syncAction.Work();
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

        /// <summary>
        /// Queues an action that will run at the beginning of the next time iteration
        /// </summary>
        /// <param name="action">code to run at the beginning of the next time iteration</param>
        public Promise QueueAction(Action action)
        {
            lock (syncQueue)
            {
                var workItem = new WorkItem(action);
                syncQueue.Enqueue(workItem);
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

            timeFunction.Initialize();
            TimeFunctionAdded.Fire(timeFunction);
            timeFunction.Added.Fire();
            return timeFunction;
        }

        public ITimeFunction Run(Action action, int delayInMilliseconds = 0) 
        {
            if (delayInMilliseconds == 0)
            {
                return Add(TimeFunction.Create(null, init: action));
            }
            else
            {
                return Add(TimeFunction.CreateDelayed(delayInMilliseconds, null, init: action));
            }
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
                    timeFunctions[i].Evaluate();
                }
            }

            for (var i = 0; i < toAdd.Count; i++)
            {
                timeFunctions.Add(toAdd[i]);

                if (toAdd[i].Lifetime.IsExpired == false && toAdd[i].Governor.ShouldFire(Now))
                {
                    toAdd[i].Evaluate();
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
