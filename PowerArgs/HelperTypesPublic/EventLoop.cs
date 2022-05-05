using System.Runtime.ExceptionServices;

namespace PowerArgs;
public class EventLoop : Lifetime
{
    private class SynchronizedEventPool  
    {
        private SynchronizedEvent[] pool;
        private int count;
        public SynchronizedEventPool()
        {
            pool = new SynchronizedEvent[5];
        }

        public SynchronizedEvent Get()
        {
            lock (pool)
            {
                for (var i = 0; i < pool.Length; i++)
                {
                    if (pool[i] != null)
                    {
                        var ret = pool[i];
                        pool[i] = null;
                        count--;
                        MaybeShrink();
                        return ret;
                    }
                }
                return new SynchronizedEvent();
            }
        }

        public void Return(SynchronizedEvent done)
        {
            lock (pool)
            {
                done.Clean();
                for (var i = 0; i < pool.Length; i++)
                {
                    if (pool[i] == null)
                    {
                        pool[i] = done;
                        count++;
                        MaybeShrink();
                        return;
                    }
                }

                Grow();
                pool[count++] = done;
            }
        }

        private void Grow()
        {
            var tmp = pool;
            pool = new SynchronizedEvent[tmp.Length * 2];
            Array.Copy(tmp, pool, tmp.Length);
        }

        private void MaybeShrink()
        {
            if (count == 0) return;
            if(count <= pool.Length * .15)
            {
                var tmp = pool;
                pool = new SynchronizedEvent[count * 2];
                var newI = 0;
                for(var i = 0; i < tmp.Length; i++)
                {
                    if(tmp[i] != null)
                    {
                        pool[newI++] = tmp[i];
                    }
                }
            }
        }
    }

    private class SynchronizedEvent
    {
        public Func<Task> AsyncWork { get;  set; }
        public SendOrPostCallback Callback { get; set; }
        public object CallbackState { get; set; }
        public Task Task { get; private set; }
        public bool IsFinished => Task == null || (Task.IsCompleted || Task.IsFaulted || Task.IsCanceled);
        public bool IsFailed => Task?.Exception != null;
        public Exception Exception => Task?.Exception;

        public SynchronizedEvent() { }

        public void Clean()
        {
            AsyncWork = null;
            Callback = null;
            Task = null;
        }

        public void Run()
        {
            Task = AsyncWork?.Invoke();
            Callback?.Invoke(CallbackState);
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

        public long Posts;
        public long Sends;

        public CustomSyncContext(EventLoop loop)
        {
            this.loop = loop;
            loop.OnDisposed(() => this.loop = null);
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            if (loop != null && loop.IsRunning && loop.IsDrainingOrDrained == false)
            {
                Posts++;
                loop.InvokeNextCycle(d, state);
            }
        }
            
        public override void Send(SendOrPostCallback d, object state)
        {
            if (Thread.CurrentThread != loop?.Thread && loop != null && loop.IsRunning && loop.IsDrainingOrDrained == false)
            {
                Sends++;
                loop.InvokeNextCycle(() => d.Invoke(state));
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }

    private SynchronizedEventPool pool = new SynchronizedEventPool();

    public Event StartOfCycle { get; private set; } = new Event();
    public Event EndOfCycle { get; private set; } = new Event();
    public Event LoopStarted { get; private set; } = new Event();
    public Event LoopStopped { get; private set; } = new Event();
    public Thread Thread { get; private set; }
    public long Posts => syncContext.Posts;
    public long Sends => syncContext.Sends;

    public long AsyncContinuations => Posts + Sends;

    public ThreadPriority Priority { get; set; } = ThreadPriority.AboveNormal;
    public bool IsRunning => runDeferred != null;
    public long Cycle { get; private set; }
    protected string Name { get; set; }
    private List<SynchronizedEvent> workQueue = new List<SynchronizedEvent>();
    private List<SynchronizedEvent> pendingWorkItems = new List<SynchronizedEvent>();
    private TaskCompletionSource<bool> runDeferred;
    private bool stopRequested;
    private CustomSyncContext syncContext;
    public bool IsDrainingOrDrained { get; private set; }

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

    private bool runMode;
    private Task runTask;
    /// <summary>
    /// Runs the event loop using the current thread
    /// </summary>
    public virtual void Run()
    {
        runMode = true;
        Thread = System.Threading.Thread.CurrentThread;
        runDeferred = new TaskCompletionSource<bool>();
        RunCommon();
        runTask.Wait();
    }

    private void RunCommon()
    {
        syncContext = new CustomSyncContext(this);
        SynchronizationContext.SetSynchronizationContext(syncContext);
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
            if (runMode)
            {
                runTask = runDeferred.Task;
            }
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
            List<SynchronizedEvent> todoOnThisCycle = new List<SynchronizedEvent>();
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
                        return;
                    }
                    else if (handling == EventLoopExceptionHandling.Swallow)
                    {
                        // swallow
                    }
                }

                   
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

                        var wi = pendingWorkItems[i];
                        pendingWorkItems.RemoveAt(i--);
                        pool.Return(wi);
                        if (stopRequested)
                        {
                            return;
                        }
                    }
                    else if (pendingWorkItems[i].IsFinished)
                    {
                        var wi = pendingWorkItems[i];
                        pendingWorkItems.RemoveAt(i--);
                        pool.Return(wi);
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
                            pool.Return(workItem);
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

                todoOnThisCycle.Clear();

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

        Invoke(() =>
        {
            throw new StopLoopException();
        });
    }

    public void Invoke(Action work) => Invoke(()=>
    {
        work();
        return Task.CompletedTask;
    });

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

        lock (workQueue)
        {
            var workItem = pool.Get();
            workItem.AsyncWork = work;
            workQueue.Add(workItem);
        }
    }

    public void InvokeNextCycle(SendOrPostCallback callback, object state)
    {
        if (IsRunning == false && IsDrainingOrDrained)
        {
            return;
        }
        lock (workQueue)
        {
            var workItem = pool.Get();
            workItem.Callback = callback;
            workItem.CallbackState = state;
            workQueue.Add(workItem);
        }
    }

    public Task InvokeAsync(Action a)
    {
        return InvokeAsync(() =>
        {
            a();
            return Task.CompletedTask;
        });
    }

    public Task InvokeAsync(Func<Task> work)
    {
        var tcs = new TaskCompletionSource<bool>();
        Invoke(() =>
        {
            try
            {
                var ret = work();
                tcs.SetResult(true);
                return ret;
            }
            catch(Exception ex)
            {
                tcs.SetException(ex);
                throw;
            }
        });
        return tcs.Task;
    }

    public void Invoke(Func<Task> work)
    {
        if (IsRunning == false && IsDrainingOrDrained)
        {
            return;
        }

        if (Thread.CurrentThread == Thread)
        {
            var workItem = pool.Get();
            workItem.AsyncWork = work;
            workItem.Run();
            if (workItem.IsFinished == false)
            {
                pendingWorkItems.Add(workItem);
            }
            else if(workItem.IsFailed)
            {
                var handling = HandleWorkItemException(workItem.Exception, workItem);
                if (handling == EventLoopExceptionHandling.Throw)
                {
                    throw new AggregateException(workItem.Exception);
                }
                else if (handling == EventLoopExceptionHandling.Stop)
                {
                    return;
                }
                else if (handling == EventLoopExceptionHandling.Swallow)
                {
                    // swallow
                }
                pool.Return(workItem);
            }
            else
            {
                pool.Return(workItem);
            }
        }
        else
        {
            lock (workQueue)
            {
                var workItem = pool.Get();
                workItem.AsyncWork = work;
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
        return EventLoopExceptionHandling.Throw;
    }
}
