using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace PowerArgs
{
    public class EventLoop : Lifetime, IYieldProvider
    {
        private class SynchronizedEvent
        {
            public Func<Task> Work { get; private  set; }
            public TaskCompletionSource<bool> Deferred { get; private set; }
            public Task Task { get; private set; }
            public bool IsFinished => Task != null && (Task.IsCompleted || Task.IsFaulted || Task.IsCanceled);
            public bool IsFailed => Task?.Exception != null;
            public Exception Exception => Task?.Exception;

            public SynchronizedEvent(Func<Task> work)
            {
                this.Work = work;
                this.Deferred = new TaskCompletionSource<bool>();
            }

            public void Run()
            {
                Task = Work();
            }
        }

        public enum EventLoopExceptionHandling
        {
            Throw,
            Stop,
            Swallow,
        }

        public class EventLoopExceptionArgs
        {
            public Exception Exception { get; set; }
            public EventLoopExceptionHandling Handling { get; set; }
        }

        private class StopLoopException : Exception { }

        private class CustomSyncContext : SynchronizationContext
        {
            private EventLoop loop;

            public CustomSyncContext(EventLoop loop)
            {
                this.loop = loop;
                loop.OnDisposed(() => this.loop = null);
            }

            public override void Post(SendOrPostCallback d, object state)
            {
                if (loop != null && loop.IsRunning && loop.IsDrainingOrDrained == false)
                {
                    loop.InvokeNextCycle(() => d.Invoke(state));
                }
            }
            
            public override void Send(SendOrPostCallback d, object state)
            {
                if (Thread.CurrentThread != loop?.Thread && loop != null && loop.IsRunning && loop.IsDrainingOrDrained == false)
                {
                    loop.InvokeNextCycle(() => d.Invoke(state));
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        public Event StartOfCycle { get; private set; } = new Event();
        public Event EndOfCycle { get; private set; } = new Event();
        public Event LoopStarted { get; private set; } = new Event();
        public Event LoopStopped { get; private set; } = new Event();
        public Thread Thread { get; private set; }

        public ThreadPriority Priority { get; set; } = ThreadPriority.AboveNormal;
        public bool IsRunning => runDeferred != null;
        public long Cycle { get; private set; }
        protected string Name { get; set; }
        private List<SynchronizedEvent> workQueue = new List<SynchronizedEvent>();
        private List<SynchronizedEvent> pendingWorkItems = new List<SynchronizedEvent>();
        private TaskCompletionSource<bool> runDeferred;
        private bool stopRequested;
        
        public bool IsDrainingOrDrained { get; private set; }
        private List<TaskCompletionSource<bool>> pendingYields = new List<TaskCompletionSource<bool>>();

        /// <summary>
        /// Runs the event loop on a new thread
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual Task Start()
        {
            runDeferred = new TaskCompletionSource<bool>();
            Thread = new Thread(RunCommon) { Name = Name };
            Thread.Priority = Priority;
            Thread.IsBackground = true;
            Thread.Start();
            return runDeferred.Task;
        }

        /// <summary>
        /// Runs the event loop using the current thread
        /// </summary>
        public void Run()
        {
            runDeferred = new TaskCompletionSource<bool>();
            RunCommon();
            if (runDeferred.Task.Exception != null)
            {
                throw new AggregateException(runDeferred.Task.Exception);
            }
            runDeferred = null;
        }

        private void RunCommon()
        {
            SynchronizationContext.SetSynchronizationContext(new CustomSyncContext(this));
            try
            {
                Loop();
                runDeferred.SetResult(true);
            }
            catch (Exception ex)
            {
                 runDeferred.SetException(ex);                    
            }
            finally
            {
                runDeferred = null;
            }
        }

        private void Loop()
        {
            try
            {
                stopRequested = false;
                Cycle = -1;
                LoopStarted.Fire();
                while (stopRequested == false)
                {
                    if (Cycle == long.MaxValue)
                    {
                        Cycle = 0;
                    }
                    else
                    {
                        Cycle++;
                    }

                    try
                    {
                        StartOfCycle.Fire();
                    }
                    catch (Exception ex)
                    {
                        var handling = HandleWorkItemException(ex, null);
                        if (handling == EventLoopExceptionHandling.Throw)
                        {
                            throw;
                        }
                        else if (handling == EventLoopExceptionHandling.Stop)
                        {
                            break;
                        }
                        else if (handling == EventLoopExceptionHandling.Swallow)
                        {
                            // swallow
                        }
                    }


                    List<SynchronizedEvent> todoOnThisCycle = new List<SynchronizedEvent>();
                    lock (workQueue)
                    {
                        while (workQueue.Count > 0)
                        {
                            var workItem = workQueue[0];
                            workQueue.RemoveAt(0);
                            todoOnThisCycle.Add(workItem);
                        }
                    }

                    for (var i = 0; i < pendingWorkItems.Count; i++)
                    {
                        if (pendingWorkItems[i].IsFinished && pendingWorkItems[i].IsFailed)
                        {
                            var handling = HandleWorkItemException(pendingWorkItems[i].Exception, pendingWorkItems[i]);
                            if (handling == EventLoopExceptionHandling.Throw)
                            {
                                ExceptionDispatchInfo.Capture(pendingWorkItems[i].Exception).Throw();
                            }
                            else if (handling == EventLoopExceptionHandling.Stop)
                            {
                                return;
                            }
                            else if (handling == EventLoopExceptionHandling.Swallow)
                            {
                                // swallow
                            }

                            pendingWorkItems.RemoveAt(i--);
                            if (stopRequested)
                            {
                                return;
                            }
                        }
                        else if (pendingWorkItems[i].IsFinished)
                        {
                            pendingWorkItems[i].Deferred.SetResult(true);
                            pendingWorkItems.RemoveAt(i--);
                            if (stopRequested)
                            {
                                return;
                            }
                        }
                    }

                    foreach (var workItem in todoOnThisCycle)
                    {
                        try
                        {
                            workItem.Run();
                            if (workItem.IsFinished == false)
                            {
                                pendingWorkItems.Add(workItem);
                            }
                            else if(workItem.Exception != null)
                            {
                                throw new AggregateException(workItem.Exception);
                            }
                            else
                            {
                                workItem.Deferred.SetResult(true);
                                if (stopRequested)
                                {
                                    return;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            var handling = HandleWorkItemException(ex, workItem);
                            if (handling == EventLoopExceptionHandling.Throw)
                            {
                                throw;
                            }
                            else if (handling == EventLoopExceptionHandling.Stop)
                            {
                                return;
                            }
                            else if (handling == EventLoopExceptionHandling.Swallow)
                            {
                                // swallow
                            }
                        }
                    }

                    try
                    {
                        EndOfCycle.Fire();
                    }
                    catch (Exception ex)
                    {
                        var handling = HandleWorkItemException(ex, null);
                        if (handling == EventLoopExceptionHandling.Throw)
                        {
                            throw;
                        }
                        else if (handling == EventLoopExceptionHandling.Stop)
                        {
                            return;
                        }
                        else if (handling == EventLoopExceptionHandling.Swallow)
                        {
                            // swallow
                        }
                    }
                }
            }
            finally
            {
                IsDrainingOrDrained = true;
                foreach (var tcs in pendingYields.ToArray())
                {
                    tcs.TrySetCanceled();
                    pendingYields.Remove(tcs);
                }
                LoopStopped.Fire();
                SynchronizationContext.SetSynchronizationContext(null);
            }
        }

        public void Stop()
        {
            if(IsRunning == false)
            {
                throw new Exception("Not running");
            }
         
            Invoke(() => throw new StopLoopException());
        }

        public void Invoke(Action work) => Invoke(()=>
        {
            work();
            return Task.CompletedTask;
        });

        public Task YieldAsync()
        {
            if (IsRunning && !IsDrainingOrDrained)
            {
                var tcs = new TaskCompletionSource<bool>();
                pendingYields.Add(tcs);
                InvokeNextCycle(() =>
                {
                    tcs.SetResult(true);
                    pendingYields.Remove(tcs);
                });
                return tcs.Task;
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        public void InvokeNextCycle(Action work)
        {
            InvokeNextCycle(() =>
            {
                work();
                return Task.CompletedTask;
            });
        }

        public void InvokeNextCycle(Func<Task> work)
        {
            if (IsRunning == false && IsDrainingOrDrained)
            {
                return;
            }

            var workItem = new SynchronizedEvent(work);
            lock (workQueue)
            {
                workQueue.Add(workItem);
            }
        }
        

        public void Invoke(Func<Task> work)
        {
            if(IsRunning == false && IsDrainingOrDrained)
            {
                return;
            }
            var workItem = new SynchronizedEvent(work);

            if (Thread.CurrentThread == Thread)
            {
                workItem.Run();
                if (workItem.IsFinished == false)
                {
                    pendingWorkItems.Add(workItem);
                }
                else if(workItem.IsFailed)
                {
                    throw new AggregateException(workItem.Exception);
                }
                else
                {
                    workItem.Deferred.SetResult(true);
                }
            }
            else
            {
                lock (workQueue)
                {
                    workQueue.Add(workItem);
                }
            }
        }

        private EventLoopExceptionHandling HandleWorkItemException(Exception ex, SynchronizedEvent workItem)
        {
            var cleaned = ex.Clean();

            if(cleaned.Count == 1 && cleaned[0] is StopLoopException)
            {
                stopRequested = true;
                pendingWorkItems.Clear();
                workQueue.Clear();
                return EventLoopExceptionHandling.Stop;
            }

            if (IsDrainingOrDrained) return EventLoopExceptionHandling.Swallow;
            workItem?.Deferred.SetException(ex);
            return EventLoopExceptionHandling.Throw;
        }
    }
}
