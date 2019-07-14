using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PowerArgs.Cli.Physics
{
    public class ObjectiveOptions
    {
        public Func<Objective,Task> Main { get; set; }
        public List<Action<Objective>> Watches { get; set; }
        public Action<AggregateException> OnException { get; set; }
        public Action<string> OnAbort { get; set; }
        public Event<string> Log { get; set; }
    }

    public class AbortObjectiveException : Exception
    {
        public AbortObjectiveException(string message) : base(message) { }
    }

    public class Objective : IDelayProvider
    {
        private InterjectableProcess Focus { get; set; }
        private ObjectiveOptions options;

        private Stack<IDisposable> exclusivityHandles = new Stack<IDisposable>();

        public int InterjectionCount { get; private set; }

        public Objective(ObjectiveOptions options)
        {
            this.options = options;
        }
        public bool IsInterjecting => Focus == null ? false : Focus.IsInterjecting;
        public Task DelayAsync(double ms) => Focus.DelayAsync(ms);
        public Task DelayAsync(TimeSpan timeout) => DelayAsync(timeout.TotalMilliseconds);
        public Task DelayAsync(Event ev, TimeSpan? timeout = null, TimeSpan? evalFrequency = null) => Focus.DelayAsync(ev, timeout, evalFrequency);
        public Task DelayAsync(Func<bool> condition, TimeSpan? timeout = null, TimeSpan? evalFrequency = null) => Focus.DelayAsync(condition, timeout, evalFrequency);
        public Task<bool> TryDelayAsync(Func<bool> condition, TimeSpan? timeout = null, TimeSpan? evalFrequency = null) => Focus.TryDelayAsync(condition, timeout, evalFrequency);
        public Task YieldAsync() => Focus.YieldAsync();
        public void Interject(Func<Task> work) => Focus.Interject(work);

        public void Evaluate()
        {
            if(Focus != null && Focus.IsInterjecting)
            {
                return;
            }

            GetFocused();
            if (Focus != null && Focus.HasStarted == false)
            {
                Focus.Start();
            }

            if (options.Watches != null && Focus.IsInterjecting == false && exclusivityHandles.Count == 0)
            {
                foreach (var watcher in options.Watches)
                {
                    if (Focus.IsInterjecting == false)
                    {
                        watcher.Invoke(this);
                    }
                }
            }
        }

        public IDisposable GoExclusive()
        {
            var ret = new Lifetime();
            exclusivityHandles.Push(ret);
            ret.OnDisposed(()=> exclusivityHandles.Pop());
            return ret;
        }

        private void GetFocused()
        {
            if (Focus == null)
            {
                Focus = new InterjectableProcess(this, options.Main);
                options.Log?.Fire("Starting main objective");
            }
            else if(Focus.IsComplete == false)
            {
                // already focused
            }
            else if(Focus.Exception != null && Focus.Exception.InnerExceptions.Count == 1 && Focus.Exception.InnerException is AbortObjectiveException)
            {
                options.OnAbort(Focus.Exception.InnerException.Message);
                options.Log?.Fire("Refocusing after "+ Focus.Exception.InnerException.Message);
                Focus = new InterjectableProcess(this, options.Main);
            }
            else if (Focus.Exception != null && options.OnException != null)
            {
                options.OnException.Invoke(Focus.Exception);
                options.Log?.Fire("Refocusing after handled exception");
                Focus = new InterjectableProcess(this, options.Main);
            }
            else if(Focus.Exception != null)
            {
                throw new AggregateException(Focus.Exception);
            }
            else
            {
                Focus = new InterjectableProcess(this, options.Main);
                options.Log?.Fire("Objective met, refocusing on main objective");
            }
        }
 
        private class InterjectableProcess
        {
            private Task task;
            private Func<Objective,Task> mainProcess;
            private Queue<Func<Task>> interjections = new Queue<Func<Task>>();
            public bool IsInterjecting { get; private set; }
            public bool HasStarted => task != null;
            public bool IsComplete => task == null ? false : task.IsCompleted;
            public AggregateException Exception => task == null ? null : task.Exception;
            private Objective o;
            public InterjectableProcess(Objective o, Func<Objective,Task> mainProcess)
            {
                this.o = o;
                this.mainProcess = mainProcess;
            }

            public void Interject(Func<Task> work)
            {
                lock (interjections)
                {
                    interjections.Enqueue(work);
                }
            }

            public void Start()
            {
                task = mainProcess(o);
                task.ContinueWith((t) =>
                {
                    task = DrainInterjections();
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }

            public async Task YieldAsync()
            {
                await DrainInterjections();
                await Time.CurrentTime.YieldAsync();
            }

            public async Task DelayAsync(double ms)
            {
                await DrainInterjections();
                await Time.CurrentTime.DelayAsync(ms);
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

            public async Task DelayAsync(Func<bool> condition, TimeSpan? timeout = null, TimeSpan? evalFrequency = null)
            {
                if (await TryDelayAsync(condition, timeout, evalFrequency) == false)
                {
                    throw new TimeoutException("Timed out awaiting delay condition");
                }
            }

            public async Task<bool> TryDelayAsync(Func<bool> condition, TimeSpan? timeout = null, TimeSpan? evalFrequency = null)
            {
                var startTime = Time.CurrentTime.Now;
                var governor = evalFrequency.HasValue ? new RateGovernor(evalFrequency.Value, lastFireTime: startTime) : null;
                while (true)
                {
                    if (governor != null && governor.ShouldFire(Time.CurrentTime.Now) == false)
                    {
                        await DrainInterjections();
                        await Task.Yield();
                    }
                    else if (condition())
                    {
                        return true;
                    }
                    else if (timeout.HasValue && Time.CurrentTime.Now - startTime >= timeout.Value)
                    {
                        return false;
                    }
                    else
                    {
                        await DrainInterjections();
                        await Task.Yield();
                    }
                }
            }

            private async Task DrainInterjections()
            {
                List<Func<Task>> interjectionsBeforeYield = null;
                lock (interjections)
                {
                    while (interjections.Count > 0)
                    {
                        interjectionsBeforeYield = interjectionsBeforeYield ?? new List<Func<Task>>();
                        interjectionsBeforeYield.Add(interjections.Dequeue());
                    }
                }

                if (interjectionsBeforeYield != null)
                {
                    foreach (var interjection in interjectionsBeforeYield)
                    {
                        IsInterjecting = true;
                        try
                        {
                            var t = interjection();
                            await t;
                            this.o.InterjectionCount++;
                        }
                        finally
                        {
                            IsInterjecting = false;
                        }
                    }
                }
            }
        }
    }
}
