using PowerArgs;
using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Threading;

namespace PowerArgs.Cli.Physics
{
 
    public class Time
    {
        private class StopTimeException : Exception { }

        [ThreadStatic]
        private static Time current;
        public static Time CurrentTime => current;

        public Event<ITimeFunction> TimeFunctionAdded { get; private set; } = new Event<ITimeFunction>();
        public Event<ITimeFunction> TimeFunctionRemoved { get; private set; } = new Event<ITimeFunction>();
        public Event<Exception> UnhandledException { get; private set; } = new Event<Exception>();
        public Event AfterTick { get; private set; } = new Event();
        public TimeSpan Now { get; private set; }
        public TimeSpan Increment { get; private set; }

        private List<ITimeFunction> timeFunctions = new List<ITimeFunction>();
        private List<ITimeFunction> toAdd = new List<ITimeFunction>();
        private List<ITimeFunction> toRemove = new List<ITimeFunction>();
        private Deferred runDeferred;
        private Queue<Action> syncQueue = new Queue<Action>();
        public IEnumerable<ITimeFunction> Functions => EnumerateFunctions();

        internal int TimeFunctionsInternalCount => timeFunctions.Count;

        public Time(TimeSpan? increment = null, TimeSpan? now = null)
        {
            Increment = increment.HasValue ? increment.Value : TimeSpan.FromTicks(1);
            Now = now.HasValue ? now.Value : TimeSpan.Zero;
        }

        public Promise Start()
        {
            runDeferred = Deferred.Create();
            runDeferred.Promise.Finally((p) => { runDeferred = null; });
            Thread t = new Thread(() =>
            {
                try
                {
                    current = this;
                    while (true)
                    {
                        List<Action> syncActions = new List<Action>();
                        lock (syncQueue)
                        {
                            while (syncQueue.Count > 0)
                            {
                                syncActions.Add(syncQueue.Dequeue());
                            }
                        }

                        foreach (var syncAction in syncActions)
                        {
                            syncAction();
                        }

                        Tick();
                        AfterTick.Fire();
                    }
                }
                catch (StopTimeException)
                {
                    runDeferred.Resolve();
                }
                catch (Exception ex)
                {
                    UnhandledException.Fire(ex);
                    runDeferred.Reject(ex);
                }
            });
            t.Start();

            return runDeferred.Promise;
        }

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

        public void QueueAction(Action action)
        {
            lock (syncQueue)
            {
                syncQueue.Enqueue(action);
            }
        }

        public T Add<T>(T timeFunction) where T : ITimeFunction
        {
            AssertIsThisTimeThread();
            timeFunction.InternalState.AddedTime = Now;
            toAdd.Add(timeFunction);

            timeFunction.Lifetime.LifetimeManager.Manage(() =>
            {
                toRemove.Add(timeFunction);
                TimeFunctionRemoved.Fire(timeFunction);
            });

            timeFunction.Initialize();
            TimeFunctionAdded.Fire(timeFunction);
            timeFunction.Added.Fire();
            return timeFunction;
        }

        public void AssertIsThisTimeThread()
        {
            if (this != CurrentTime)
            {
                throw new InvalidOperationException("Code not running on time thread");
            }
        }

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
