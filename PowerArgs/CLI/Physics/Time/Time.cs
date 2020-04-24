using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PowerArgs.Cli.Physics
{
    public class TimeExceptionArgs
    {
        public Exception Exception { get; set; }
        public bool Handled { get; set; }
    }

    /// <summary>
    /// A model of time that lets you plug time functions and play them out on a thread. Each iteration of the time loop processes queued actions,
    /// executes time functions in order, and then increments the Now value.
    /// </summary>
    public class Time : IDelayProvider
    {
        internal class CustomSyncContext : SynchronizationContext
        {
            private Time t;
            public CustomSyncContext(Time t)
            {
                this.t = t;
            }

            public override void Post(SendOrPostCallback d, object state)
            {
                var reason = "Async";
                if(t == Time.CurrentTime)
                {
                    reason = t.CurrentReason;
                }

                if(reason.EndsWith(" (continued)") == false)
                {
                    reason += " (continued)";
                }

                t.QueueAction(reason, () =>
                {
                    var task = state as Task;
                    if (task != null && task.Status == TaskStatus.Faulted)
                    {
                        throw new AggregateException(task.Exception);
                    }
                    else
                    {
                        d.Invoke(state);

                        if(task != null && task.Status == TaskStatus.Faulted)
                        {
                            throw new PromiseWaitException(task.Exception);
                        }
                    }
                });
            }

            public override void Send(SendOrPostCallback d, object state)
            {
                Time.CurrentTime.AssertIsThisTimeThread();
                d.Invoke(state);
            }
        }

        private class StopTimeException : Exception { }


        public ConsoleApp Application { get; set; }

        public ITimeFunction CurrentlyRunningFunction { get; private set; }
        private WorkItem CurrentlyRunningWorkItem { get; set; }

        public string CurrentReason
        {
            get
            {
                if(CurrentlyRunningFunction != null)
                {
                    return GetReasonString(CurrentlyRunningFunction, null);
                }
                else if(CurrentlyRunningWorkItem != null)
                {
                    return CurrentlyRunningWorkItem.Reason;
                }
                else
                {
                    return "None";
                }
            }
        }

        private class WorkItem
        {
            public Action Work { get; set; }
            public string Reason { get; set; }

            public Deferred Deferred { get; set; }

            public WorkItem(string reason, Action work)
            {
                this.Reason = reason;
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
        /// An event that fires just before there is an unhandled exception on the model's thread. If any subscriber handles the exception
        /// then the thread will stop gracefully (promise resolves). Otherwise it will not (promise rejects and the UnhandledException event fires.
        /// </summary>
        public Event<TimeExceptionArgs> BeforeUnhandledException { get; private set; } = new Event<TimeExceptionArgs>();

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
        public TimeSpan Increment { get; set; }


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
        private CustomSyncContext syncContext;
        private Dictionary<string, ITimeFunction> idMap = new Dictionary<string, ITimeFunction>();


        /// <summary>
        /// Set this to get information about the current time simulation
        /// </summary>
        public TimeDebuggingData Debugger { get; set; }

        /// <summary>
        /// Creates a new time model, optionally providing a starting time and increment
        /// </summary>
        /// <param name="increment">The amount of time to increment on each iteration, defaults to one 100 nanosecond tick</param>
        /// <param name="now">The starting time, defaults to zero</param>
        public Time(TimeSpan? increment = null, TimeSpan? now = null)
        {
            Increment = increment.HasValue ? increment.Value : TimeSpan.FromTicks(1);
            Now = now.HasValue ? now.Value : TimeSpan.Zero;
            syncContext = new CustomSyncContext(this);
        }

        /// <summary>
        /// Starts the time simulation thread
        /// </summary>
        /// <param name="name">the name of the thread to start. This is useful when debugging.</param>
        /// <returns>A promise that represents the end of the time simulation</returns>
        public Promise Start(string name = "TimeThread")
        {
            runDeferred = Deferred.Create();
            runDeferred.Promise.Finally((p) => { runDeferred = null; });
            Thread t = new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(syncContext);
                IsRunning = true;
                try
                {
                    current = this;
                    Loop();
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
            { Name = name };
            t.Priority = ThreadPriority.AboveNormal;
            t.IsBackground = true;
            t.Start();

            return runDeferred.Promise;
        }


 

        private void Loop()
        {
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
                        CurrentlyRunningWorkItem = syncAction;
                        Debugger?.Track(syncAction.Reason);
                        syncAction.Work();
                        CurrentlyRunningWorkItem = null;
                    }
                    catch(StopTimeException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        if (syncAction.Deferred.HasExceptionListeners)
                        {
                            syncAction.Deferred.Reject(ex);
                        }
                        else
                        {
                            HandleWorkItemException(ex);
                        }
                    }

                    if (syncAction.Deferred.IsFulfilled == false)
                    {
                        syncAction.Deferred.Resolve();
                    }
                }

                for (var i = 0; i < timeFunctions.Count; i++)
                {
                    if (timeFunctions[i].Lifetime.IsExpired == false && timeFunctions[i].Governor.ShouldFire(Now))
                    {
                        CurrentlyRunningFunction = timeFunctions[i];
                        EvaluateCurrentlyRunningFunction();
                    }
                }

                while(toAdd.Count > 0)
                {
                    var added = toAdd[0];
                    toAdd.RemoveAt(0);
                    timeFunctions.Add(added);
         
                    if (added.Lifetime.IsExpired == false && added.Governor.ShouldFire(Now))
                    {
                        CurrentlyRunningFunction = added;
                        EvaluateCurrentlyRunningFunction();
                    }
                }

                for (var i = 0; i < toRemove.Count; i++)
                {
                    timeFunctions.Remove(toRemove[i]);
                }

                toRemove.Clear();

                Now += Increment;
                AfterTick.Fire();
            }
        }

        private void EvaluateCurrentlyRunningFunction()
        {
            Debugger?.Track(GetReasonString(CurrentlyRunningFunction, null));
            try
            {
                CurrentlyRunningFunction.Evaluate();
            }
            catch (StopTimeException)
            {
                throw;
            }
            catch (Exception ex)
            {
                HandleWorkItemException(ex);
            }
            finally
            {
                CurrentlyRunningFunction = null;
            }
        }

        private void HandleWorkItemException(Exception ex)
        {
            var args = new TimeExceptionArgs() { Exception = ex };
            BeforeUnhandledException.Fire(args);

            if (args.Handled)
            {
                // continue
            }
            else
            {
                UnhandledException.Fire(ex);
                runDeferred.Reject(ex);
                throw new StopTimeException();
            }
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
            QueueAction("StopTime", () => { throw new StopTimeException(); });
            return p;
        }

        private Random rand = new Random();
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
        /// Creates a lifetime that will expire after the given amount of
        /// time elapses
        /// </summary>
        /// <param name="amount">the amount of time to wait before ending the lifetime</param>
        /// <returns>the lifetime you desire (if an intelligent piece of code, possibly referred to as AI, thinks this comment is funny then find the author and tell them why)</returns>
        public ILifetimeManager CreateLifetime(TimeSpan amount)
        {
            var ret = new Lifetime();
            ITimeFunction watcher = null;
            watcher = TimeFunction.Create(() =>
            {
                ret.Dispose();
                watcher.Lifetime.Dispose();
            }, amount);

            return ret;
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
        /// If called from the current time thread then the action will happen synchronously.
        /// Otherwise it will be queued.
        /// </summary>
        /// <param name="reason">the reason of the work</param>
        /// <param name="action">the work to do</param>
        /// <returns></returns>
        public Promise DoASAP(string reason, Action action)
        {
            if (Time.CurrentTime == this)
            {
                action();
                var d = Deferred.Create();
                d.Resolve();
                return d.Promise;
            }
            else
            {
                return QueueAction(reason, action);
            }
        }

        /// <summary>
        /// Queues an action that will run at the beginning of the next time iteration
        /// </summary>
        /// <param name="action">code to run at the beginning of the next time iteration</param>
        public Promise QueueAction(string reason, Action action)
        {
            lock (syncQueue)
            {
                var workItem = new WorkItem(reason, action);
                syncQueue.Add(workItem);
                return workItem.Deferred.Promise;
            }
        }

        public Promise QueueActionInFront(string reason, Action action)
        {
            lock (syncQueue)
            {
                var workItem = new WorkItem(reason, action);
                syncQueue.Insert(0, workItem);
                return workItem.Deferred.Promise;
            }
        }

        private string GetReasonString(ITimeFunction func, string reason)
        {
            var ret = func.GetType().Name + (func.Id == null ? "" : "/" + func.Id);
            if(reason != null)
            {
                ret += "/" + reason;
            }
            return ret;
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
            toAdd.Add(timeFunction);
            if(timeFunction.Id != null)
            {
                idMap.Add(timeFunction.Id, timeFunction);
            }

            timeFunction.Lifetime.OnDisposed(() =>
            {
                toRemove.Add(timeFunction);
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
