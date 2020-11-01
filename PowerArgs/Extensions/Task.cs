using PowerArgs.Samples;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
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
            if (await TaskEx.WhenAny(runningTask, Task.Delay(timeout)) != runningTask)
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
            if (await TaskEx.WhenAny(runningTask, Task.Delay(timeout)) != runningTask)
            {
                throw new TimeoutException(timeoutMessage);
            }
            else
            {
                return runningTask.Result;
            }
        }

      

        public static Task<Task> WhenAny(params Task[] tasks) => WhenAny((IEnumerable<Task>)tasks);

        public static async Task<Task> WhenAny(IEnumerable<Task> tasks)
        {
            var ret = await Task.WhenAny(tasks);
            if(ret.Exception != null)
            {
                ExceptionDispatchInfo.Capture(ret.Exception).Throw();
            }
            return ret;
        }

        public static ILifetimeManager ToLifetime(this Task t)
        {
            var lt = new Lifetime();
            t.ContinueWith((t2) => lt.Dispose(), TaskScheduler.FromCurrentSynchronizationContext());
            return lt;
        }

        public static bool IsFulfilled(this Task t) => t.IsCanceled || t.IsCompleted || t.IsFaulted;

        public static Task Then(this Task t, Action a)
        {
            return t.ContinueWith(t2 =>
            {
                if(t2.Status == TaskStatus.RanToCompletion)
                {
                    a();
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        public static Task Fail(this Task t, Action<Exception> a)
        {
            return t.ContinueWith(t2 =>
            {
                if (t2.Status == TaskStatus.Faulted)
                {
                    a(t2.Exception);
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        public static Task Finally(this Task t, Action<Task> a)
        {
            return t.ContinueWith(t2 =>
            {
                a(t);
            }, TaskContinuationOptions.ExecuteSynchronously);
        }


        public static Task Then<T>(this Task<T> t, Action<T> a)
        {
            return t.ContinueWith(t2 =>
            {
                if (t2.Status == TaskStatus.RanToCompletion)
                {
                    a(t.Result);
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        public static Task Fail<T>(this Task<T> t, Action<Exception> a)
        {
            return t.ContinueWith(t2 =>
            {
                if (t2.Status == TaskStatus.Faulted)
                {
                    a(t2.Exception);
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        public static Task Finally<T>(this Task<T> t, Action<Task<T>> a)
        {
            return t.ContinueWith(t2 =>
            {
                a(t);
            }, TaskContinuationOptions.ExecuteSynchronously);
        }
    }
}
