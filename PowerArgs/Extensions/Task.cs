using System;
using System.Threading.Tasks;

namespace PowerArgs
{
    /// <summary>
    /// Extensions to System.Threading.Tasks.Task
    /// </summary>
    public static class TaskEx
    {
        /// <summary>
        /// Creates a new task that will throw a TimeoutException if the initial task fails to complete before the given timeout
        /// </summary>
        /// <param name="runningTask">the task to wrap</param>
        /// <param name="timeout">the amount of time to wait before throwing a TimeoutException</param>
        /// <param name="timeoutMessage">Optionally control the exception message</param>
        /// <returns>a new task that will throw a TimeoutException if the initial task fails to complete before the given timeout</returns>
        public static async Task TimeoutAfter(this Task runningTask, TimeSpan timeout, string timeoutMessage = "The operation timed out")
        {
            if (await Task.WhenAny(runningTask, Task.Delay(timeout)) != runningTask)
            {
                throw new TimeoutException(timeoutMessage);
            }
        }

        /// <summary>
        /// Creates a new task that will throw a TimeoutException if the initial task fails to complete before the given timeout
        /// </summary>
        /// <typeparam name="T">The type of result that the task produces</typeparam>
        /// <param name="runningTask">the task to wrap</param>
        /// <param name="timeout">the amount of time to wait before throwing a TimeoutException</param>
        /// <param name="timeoutMessage">Optionally control the exception message</param>
        /// <returns>a new task, with a result, that will throw a TimeoutException if the initial task fails to complete before the given timeout</returns>
        public static async Task<T> TimeoutAfter<T>(this Task<T> runningTask, TimeSpan timeout, string timeoutMessage = "The operation timed out")
        {
            if (await Task.WhenAny(runningTask, Task.Delay(timeout)) != runningTask)
            {
                throw new TimeoutException(timeoutMessage);
            }
            else
            {
                return runningTask.Result;
            }
        }

        public static Task Then(this Task t, Action a)
        {
            return t.ContinueWith((t2)=> a(), TaskScheduler.FromCurrentSynchronizationContext());
        }

        public static ILifetimeManager ToLifetime(this Task t)
        {
            var lt = new Lifetime();
            t.ContinueWith((t2) => lt.Dispose(), TaskScheduler.FromCurrentSynchronizationContext());
            return lt;
        }
    }
}
