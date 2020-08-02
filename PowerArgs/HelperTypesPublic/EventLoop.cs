using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace PowerArgs
{
    public class EventLoop : Lifetime
    {
        private class SynchronizedEvent
        {
            public Func<Task> Work { get; private  set; }
            public Deferred Deferred { get; private set; }
            public Task Task { get; private set; }
            public bool IsFinished => Task != null && (Task.IsCompleted || Task.IsFaulted || Task.IsCanceled);
            public bool IsFailed => Task?.Exception != null;
            public Exception Exception => Task?.Exception;

            public SynchronizedEvent(Func<Task> work)
            {
                this.Work = work;
                this.Deferred = Deferred.Create();
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
            }

            public override void Post(SendOrPostCallback d, object state) => loop.InvokeNextCycle(() =>  d.Invoke(state));
            
            public override void Send(SendOrPostCallback d, object state)
            {
                if (Thread.CurrentThread != loop.Thread)
                {
                    loop.InvokeNextCycle(() => d.Invoke(state));
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        public Event<EventLoopExceptionArgs> UnhandledException { get; private set; } = new Event<EventLoopExceptionArgs>();
        public Event StartOfCycle { get; set; } = new Event();
        public Event EndOfCycle { get; set; } = new Event();
        public Event LoopStarted { get; set; } = new Event();
        public Thread Thread { get; private set; }

        public ThreadPriority Priority { get; set; } = ThreadPriority.AboveNormal;
        public bool IsRunning => runDeferred != null;
        public long Cycle { get; private set; }
        protected string Name { get; set; }
        private List<SynchronizedEvent> workQueue = new List<SynchronizedEvent>();
        private List<SynchronizedEvent> pendingWorkItems = new List<SynchronizedEvent>();
        private Deferred runDeferred;
        private bool stopRequested;


        /// <summary>
        /// Runs the event loop on a new thread
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual Promise Start()
        {
            runDeferred = Deferred.Create();
            runDeferred.Promise.Finally((p) => { runDeferred = null; });
            Thread = new Thread(RunCommon) { Name = Name };
            Thread.Priority = Priority;
            Thread.IsBackground = true;
            Thread.Start();
            return runDeferred.Promise;
        }

        /// <summary>
        /// Runs the event loop using the current thread
        /// </summary>
        public void Run()
        {
            runDeferred = Deferred.Create();
            runDeferred.Promise.Finally((p) => { runDeferred = null; });
            RunCommon();
            if(runDeferred.Exception != null)
            {
                throw new PromiseWaitException(runDeferred.Exception);
            }
        }

        private void RunCommon()
        {
            var syncContext = new CustomSyncContext(this);
            SynchronizationContext.SetSynchronizationContext(syncContext);
            try
            {
                Loop();
                runDeferred.Resolve();
            }
            catch (Exception ex)
            {
                runDeferred.Reject(ex);
            }
        }

        private void Loop()
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
                        stopRequested = true;
                        pendingWorkItems.Clear();
                        workQueue.Clear();
                        return;
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
                            stopRequested = true;
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
                        pendingWorkItems[i].Deferred.Resolve();
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
                        else
                        {
                            workItem.Deferred.Resolve();
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
                            stopRequested = true;
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
                        stopRequested = true;
                        return;
                    }
                    else if (handling == EventLoopExceptionHandling.Swallow)
                    {
                        // swallow
                    }
                }
            }
        }

        public Promise Stop()
        {
            if(IsRunning == false)
            {
                throw new Exception("Not running");
            }
            var ret = runDeferred;
            Invoke(() => throw new StopLoopException());
            return ret.Promise;
        }

        public Promise Invoke(Action work) => Invoke(()=>
        {
            work();
            return Task.CompletedTask;
        });

        public Promise InvokeNextCycle(Action work)
        {
            return InvokeNextCycle(() =>
            {
                work();
                return Task.CompletedTask;
            });
        }

        public Promise InvokeNextCycle(Func<Task> work)
        {
            var workItem = new SynchronizedEvent(work);
            lock (workQueue)
            {
                workQueue.Add(workItem);
            }
            return workItem.Deferred.Promise;
        }
        

        public Promise Invoke(Func<Task> work)
        {
            var workItem = new SynchronizedEvent(work);

            if (Thread.CurrentThread == Thread)
            {
                workItem.Run();
                if (workItem.IsFinished == false)
                {
                    pendingWorkItems.Add(workItem);
                }
                else
                {
                    workItem.Deferred.Resolve();
                }
            }
            else
            {
                lock (workQueue)
                {
                    workQueue.Add(workItem);
                }
            }
            return workItem.Deferred.Promise;
        }

        private EventLoopExceptionHandling HandleWorkItemException(Exception ex, SynchronizedEvent workItem)
        {
            var cleaned = PromiseWaitException.Clean(ex);

            if(cleaned.Count == 1 && cleaned[0] is StopLoopException)
            {
                stopRequested = true;
                return EventLoopExceptionHandling.Stop;
            }

            if (workItem != null && workItem.Deferred.HasExceptionListeners)
            {
                workItem.Deferred.Reject(ex);
                return EventLoopExceptionHandling.Swallow;
            }
            else
            {
                var args = new EventLoopExceptionArgs() { Exception = ex };
                UnhandledException.Fire(args);
                return args.Handling;
            }
        }
    }
}
