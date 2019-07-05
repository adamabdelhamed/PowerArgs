using System;
using System.Threading.Tasks;

namespace PowerArgs.Cli.Physics
{
    /// <summary>
    /// A standard model for an async time function. It implements IDelayProvider so that the implementation of ExecuteAsync
    /// can access them. If ExecuteAsync exclusively uses these delay methods then it can be sure that code after all await statements
    /// will first check to make sure this function is not expired before it continues. 
    /// </summary>
    public abstract class AsyncTimeFunction : TimeFunction, IDelayProvider
    {
        private Task task;

        /// <summary>
        /// The derived class should call this once it's ready to start it's async work
        /// </summary>
        protected void Start()
        {
            task = ExecuteAsync();
        }

        protected abstract Task ExecuteAsync();

        /// <summary>
        /// Checks the status of the async task. If it's completed then it will dispose this time function.
        /// If it fails with anything other than an AbortObjectiveException then it will throw. If it fails with an
        /// AbortObjectiveException then it will swallow and dispose.
        /// </summary>
        public override void Evaluate()
        {
            if(task == null)
            {
                // wait
            }
            else if (task.Status == TaskStatus.RanToCompletion || task.Status == TaskStatus.Canceled)
            {
                if (this.Lifetime.IsExpired == false)
                {
                    this.Lifetime.Dispose();
                }
            }
            else if (task.Status == TaskStatus.Faulted)
            {
                if (this.Lifetime.IsExpired == false)
                {
                    this.Lifetime.Dispose();
                }
                if (task.Exception.InnerExceptions.Count == 1 && task.Exception.InnerException is AbortObjectiveException)
                {
                    // do nothing
                }
                else
                {
                    throw new AggregateException(task.Exception);
                }
            }
        }

        public Task DelayAsync(double ms)
        {
            AssertAlive();
            return Time.CurrentTime.DelayAsync(ms);
        }

        public Task DelayAsync(TimeSpan timeout)
        {
            AssertAlive();
            return Time.CurrentTime.DelayAsync(timeout);
        }

        public Task DelayAsync(Event ev, TimeSpan? timeout = null, TimeSpan? evalFrequency = null)
        {
            AssertAlive();
            return Time.CurrentTime.DelayAsync(ev, timeout, evalFrequency);
        }

        public Task DelayAsync(Func<bool> condition, TimeSpan? timeout = null, TimeSpan? evalFrequency = null)
        {
            AssertAlive();
            return Time.CurrentTime.DelayAsync(condition, timeout, evalFrequency);
        }

        public Task<bool> TryDelayAsync(Func<bool> condition, TimeSpan? timeout = null, TimeSpan? evalFrequency = null)
        {
            AssertAlive();
            return Time.CurrentTime.TryDelayAsync(condition, timeout, evalFrequency);
        }

        public Task YieldAsync()
        {
            AssertAlive();
            return Time.CurrentTime.YieldAsync();
        }

        private void AssertAlive()
        {
            if (Lifetime.IsExpired) throw new AbortObjectiveException("TimeFunction expired");
        }
    }
}