using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{

    internal class TimerDisposer : Disposable
    {
        public Timer Timer { get; private set; }
        private List<IDisposable> tracker;
        public TimerDisposer(Timer t, List<IDisposable> tracker)
        {
            this.Timer = t;
            this.tracker = tracker;
            lock (tracker)
            {
                tracker.Add(this);
            }
        }
        protected override void DisposeManagedResources()
        {
            Timer.Dispose();
            lock (tracker)
            {
                tracker.Remove(this);
            }
        }
    }

    /// <summary>
    /// A class that defines the exception handling contract for a ConsoleApp
    /// </summary>
    public class PumpExceptionArgs
    {
        /// <summary>
        /// Gets the exception that a handler may handle
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Gets or sets whether or not this exception has been handled by a handler.  Your event handler should set this to true
        /// if you want the pump to continue processing messages.  If no handler sets this to true then the pump thread will throw
        /// and the process will crash.
        /// </summary>
        public bool Handled { get; set; }

        internal PumpExceptionArgs(Exception ex)
        {
            this.Exception = ex;
            this.Handled = false;
        }
    }

    /// <summary>
    /// A class that defines a message that can be executed by the message pump
    /// </summary>
    public class PumpMessage
    {

        /// <summary>
        /// A description for this message, used for debugging
        /// </summary>
        public string Description { get; set; }

        private Action pumpAction;

        internal Deferred Deferred { get; set; }

        /// <summary>
        /// Creates a pump message with the given action and idempotency id.  
        /// </summary>
        /// <param name="a">The action to execute when this message is dequeued by a message pump</param>
       /// <param name="description">A description of this message that can be used for debugging purposes</param>
        public PumpMessage(Action a, string description = "Not specified")
        {
            this.Description = description;
            this.pumpAction = a;
        }

        /// <summary>
        /// Executes the action associated with this message
        /// </summary>
        internal void Execute()
        {
            if(pumpAction != null)
            {
                pumpAction();
            }
        }
    }

    /// <summary>
    /// A message that indicates a paint request that gets processed differently from other message types to reduce
    /// the number of times the message pump processes paint requests for a console app
    /// </summary>
    internal class PaintMessage : PumpMessage
    {
        public const string PaintDescription = nameof(PaintMessage);
        public PaintMessage(Action paintAction) : base(paintAction, PaintDescription) { }
    }

    internal class CustomSyncContext : SynchronizationContext
    {
        private CliMessagePump pump;
        public CustomSyncContext(CliMessagePump pump) { this.pump = pump; }

        public override void Post(SendOrPostCallback d, object state) => pump.QueueAction(() => d.Invoke(state));

        public override void Send(SendOrPostCallback d, object state) => pump.QueueAction(() => d.Invoke(state));
    }

    /// <summary>
    /// A class that is used to manage a CLI thread in a similar way that other platforms synchronize work
    /// on a UI thread
    /// </summary>
    public class CliMessagePump : ObservableObject
    {
        private Queue<ConsoleKeyInfo> sendKeys = new Queue<ConsoleKeyInfo>();

        private class StopPumpMessage : PumpMessage
        {
            public StopPumpMessage() : base(() => { }, description: "Stops the pump") { }
        }

        /// <summary>
        /// True by default. When true, discards key presses that come in too fast
        /// likely because the user is holding the key down. You can set the
        /// MinTimeBetweenKeyPresses property to suit your needs.
        /// </summary>
        public bool KeyThrottlingEnabled { get; set; } = true;

        /// <summary>
        /// When key throttling is enabled this lets you set the minimum time that must
        /// elapse before we forward a key press to the app, provided it is the same key
        /// that was most recently clicked.
        /// </summary>
        public TimeSpan MinTimeBetweenKeyPresses { get; set; } = TimeSpan.FromMilliseconds(35);

        public Event OnKeyInputThrottled { get; private set; } = new Event();
        private ConsoleKey lastKey;
        private DateTime lastKeyPressTime = DateTime.MinValue;

        /// <summary>
        /// An event that fires when a pump message throws an exception while executing.  Handlers can mark the exception as handled
        /// if they want to keep the pump running.  If no handler is registered or no handler marks the exception as handled then the
        /// pump thread will throw and the process will crash.
        /// </summary>
        public Event<PumpExceptionArgs> PumpException { get; private set; } = new Event<PumpExceptionArgs>();


        /// <summary>
        /// An event that fires when the console window has been resized by the user
        /// </summary>
        public Event WindowResized { get; private set; } = new Event();

        /// <summary>
        /// A boolean that can be checked to see if the pump is currently running
        /// </summary>
        public bool IsRunning { get; private set; } = false;

        private List<PumpMessage> pumpMessageQueue = new List<PumpMessage>();
        private IConsoleProvider console;
        private int lastConsoleWidth, lastConsoleHeight;
        private Deferred runDeferred;
        private List<IDisposable> timerHandles = new List<IDisposable>();

        private FrameRateMeter cycleRateMeter;

        /// <summary>
        /// Gets the total number of event loop cycles that have run
        /// </summary>
        public int TotalCycles => cycleRateMeter != null ? cycleRateMeter.TotalFrames : 0;

        /// <summary>
        /// Gets the total number of times a paint actually happened
        /// </summary>
        public int TotalPaints => paintRateMeter != null ? paintRateMeter.TotalFrames : 0;

        /// <summary>
        /// Gets the current frame rate for the app
        /// </summary>
        public int CyclesPerSecond => cycleRateMeter != null ? cycleRateMeter.CurrentFPS : 0;
            

        private FrameRateMeter paintRateMeter;
        /// <summary>
        /// Gets the current paint rate for the app
        /// </summary>
        public int PaintRequestsProcessedPerSecond
        {
            get
            {
                return paintRateMeter != null ? paintRateMeter.CurrentFPS : 0;
            }
        }

        /// <summary>
        /// Creates a new message pump given a console to use for keyboard input
        /// </summary>
        /// <param name="console">the console to use for keyboard input</param>
        public CliMessagePump(IConsoleProvider console)
        {
            this.console = console;
            this.lastConsoleWidth = this.console.BufferWidth;
            this.lastConsoleHeight = this.console.WindowHeight;
        }

        /// <summary>
        /// Simulates a key press
        /// </summary>
        /// <param name="key">the key press info</param>
        public Promise SendKey(ConsoleKeyInfo key) => QueueAction(() => { sendKeys.Enqueue(key); });

        /// <summary>
        /// Handles key input for the message pump
        /// </summary>
        /// <param name="info"></param>
        protected virtual void HandleKeyInput(ConsoleKeyInfo info) { }

        /// <summary>
        /// Queues the given action for processing on the pump thread
        /// </summary>
        /// <param name="a">the action that will be processed in order on the pump thread</param>
        public Promise QueueAction(Action a)
        {
            var d = Deferred.Create();
            var pumpMessage = new PumpMessage(a) { Deferred = d };
            QueueAction(pumpMessage);
            return d.Promise;
        }

        /// <summary>
        /// Invokes the given action now if we're currently
        /// on this app's thread or else queues it up
        /// </summary>
        /// <param name="a">the action to run</param>
        /// <returns>A promise that resolves after the action is run</returns>
        public Promise InvokeSafe(Action a)
        {
            if(ConsoleApp.Current == this)
            {
                a();
                var d = Deferred.Create();
                d.Resolve();
                return d.Promise;
            }
            else
            {
                return QueueAction(a);
            }
        }

        /// <summary>
        /// Schedules the given async action for work on the UI thread.
        /// </summary>
        /// <param name="asyncAction">the async work to do</param>
        /// <returns>an async task</returns>
        public async Task QueueActionAsync(Func<Task> asyncAction)
        {
            await QueueActionAsync<bool>(async () =>
            {
                await asyncAction();
                return true;
            });
        }

        /// <summary>
        /// Schedules the given async action for work on the UI thread.
        /// </summary>
        /// <typeparam name="T">the expected result of the work</typeparam>
        /// <param name="asyncAction">the async work to do</param>
        /// <returns>an async result of type t</returns>
        public async Task<T> QueueActionAsync<T>(Func<Task<T>> asyncAction)
        {
            var done = false;
            Exception toForward = null;
            T ret = default(T);
            await QueueAction(async () =>
            {
                try
                {
                    ret = await asyncAction();
                }
                catch (Exception ex)
                {
                    toForward = ex;
                }
                done = true;
            }).AsAwaitable();

            while (done == false)
            {
                await Task.Delay(1);
            }

            if (toForward != null)
            {
                throw new PromiseWaitException(toForward);
            }

            return ret;
        }

        /// <summary>
        /// Queues the given message for processing on the pump thread
        /// </summary>
        /// <param name="pumpMessage">the message that will be processed in order on the pump thread</param>
        protected void QueueAction(PumpMessage pumpMessage)
        {
            lock (pumpMessageQueue)
            {
                pumpMessageQueue.Add(pumpMessage);
            }
        }

        /// <summary>
        /// Puts the given action into the work queue, but skips it to the front of the queue
        /// </summary>
        /// <param name="a">the action code to run</param>
        /// <returns>a promise that will resolve when the work is done</returns>
        protected Promise QueueActionInFront(Action a)
        {
            var d = Deferred.Create();
            var pumpMessage = new PumpMessage(a) { Deferred = d };
            QueueActionInFront(pumpMessage);
            return d.Promise;
        }

        /// <summary>
        /// Puts the given action into the work queue, but skips it to the front of the queue
        /// </summary>
        /// <param name="pumpMessage">the message to process</param>
        private void QueueActionInFront(PumpMessage pumpMessage)
        {
            lock (pumpMessageQueue)
            {
                pumpMessageQueue.Insert(0, pumpMessage);
            }
        }

       

        /// <summary>
        /// Schedules the given action for periodic processing by the message pump
        /// </summary>
        /// <param name="a">The action to schedule for periodic processing</param>
        /// <param name="interval">the execution interval for the action</param>
        /// <returns>A handle that can be passed to ClearInterval if you want to cancel the work</returns>
        public IDisposable SetInterval(Action a, TimeSpan interval)
        {
            return new TimerDisposer(new Timer((o) => QueueAction(a), null, (int)interval.TotalMilliseconds, (int)interval.TotalMilliseconds), timerHandles);
        }

        /// <summary>
        /// Schedules the given action for a one time execution after the given period elapses
        /// </summary>
        /// <param name="a">The action to schedule</param>
        /// <param name="period">the period of time to wait before executing the action</param>
        /// <returns></returns>
        public IDisposable SetTimeout(Action a, TimeSpan period)
        {
            return new TimerDisposer(new Timer((o) => QueueAction(a), null, (int)period.TotalMilliseconds,Timeout.Infinite), timerHandles);
        }

        /// <summary>
        /// Updates a previously scheduled interval
        /// </summary>
        /// <param name="handle">the handle that was returned by a previous call to setInterval</param>
        /// <param name="newInterval">the new interval</param>
        public void ChangeInterval(IDisposable handle, TimeSpan newInterval)
        {
            var disposer = handle as TimerDisposer;
            if (disposer == null) throw new ArgumentException($"The argument was not provided by {nameof(SetInterval)}");
            disposer.Timer.Change(newInterval, newInterval);
        }
        
        /// <summary>
        /// Starts the message pump which will begin processing messages
        /// </summary>
        /// <returns>A task that will complete when the message pump starts</returns>
        public virtual Promise Start()
        {
            if(runDeferred != null)
            {
                throw new InvalidOperationException("Already running");
            }
            IsRunning = true;
            runDeferred = Deferred.Create();
            var pumpThread = new Thread(Pump) { Name = "CliMessagePump" };
            pumpThread.Priority = ThreadPriority.AboveNormal;
            pumpThread.IsBackground = true;
            pumpThread.Start();
            return runDeferred.Promise;
        }

        /// <summary>
        /// Stops the pump thread
        /// </summary>
        public void Stop()
        {
            if (IsRunning)
            {
                QueueAction(new StopPumpMessage());
            }
        }

        protected virtual void OnThredStart()
        {

        }

        private void Pump()
        {
            try
            {
                OnThredStart();
                SynchronizationContext.SetSynchronizationContext(new CustomSyncContext(this));
                bool stopRequested = false;
                cycleRateMeter = new FrameRateMeter();
                paintRateMeter = new FrameRateMeter();
                while (true)
                {
                    cycleRateMeter.Increment();
                    if ((lastConsoleWidth != this.console.BufferWidth || lastConsoleHeight != this.console.WindowHeight))
                    {
                        DebounceResize();
                        WindowResized.Fire();
                    }

                    bool idle = true;
                    List<PumpMessage> iterationQueue;
                    PaintMessage iterationPaintMessage = null;
                    var paintDeferreds = new List<Deferred>();
                    lock (pumpMessageQueue)
                    {
                        iterationQueue = new List<PumpMessage>(pumpMessageQueue);
                        pumpMessageQueue.Clear();
                    }

                    foreach (var message in iterationQueue)
                    {
                        idle = false;
                        if (message is StopPumpMessage)
                        {
                            stopRequested = true;
                            break;
                        }
                        else if (message is PaintMessage)
                        {
                            if(message.Deferred != null)
                            {
                                paintDeferreds.Add(message.Deferred);
                            }
                            iterationPaintMessage = message as PaintMessage;
                        }
                        else
                        {
                            TryWork(message);
                        }
                    }

                    if (iterationPaintMessage != null)
                    {
                        TryWork(iterationPaintMessage);

                        // since we debounce paints, make sure the paints
                        // that got debounced get resolved
                        foreach(var deferred in paintDeferreds)
                        {
                            if(deferred != iterationPaintMessage.Deferred)
                            {
                                deferred.Resolve();
                            }
                        }
                        paintRateMeter.Increment();
                    }

                    if (stopRequested)
                    {
                        break;
                    }

                    if (this.console.KeyAvailable)
                    {
                        idle = false;
                        var info = this.console.ReadKey(true);

                        var effectiveMinTimeBetweenKeyPresses = MinTimeBetweenKeyPresses;
                        if (KeyThrottlingEnabled && info.Key == lastKey && DateTime.UtcNow - lastKeyPressTime < effectiveMinTimeBetweenKeyPresses)
                        {
                            // the user is holding the key down and throttling is enabled
                            OnKeyInputThrottled.Fire();
                        }
                        else
                        {
                            lastKeyPressTime = DateTime.UtcNow;
                            lastKey = info.Key;
                            QueueAction(() => HandleKeyInput(info));
                        }
                    }
                    else if(sendKeys.Count > 0)
                    {
                        idle = false;
                        var info = sendKeys.Dequeue();
                        QueueAction(() => HandleKeyInput(info));
                    }

                    if (idle)
                    {
                        Thread.Sleep(1);
                    }
                }
                runDeferred.Resolve();
            }
            catch(Exception ex)
            {
                runDeferred.Reject(ex);
            }
            finally
            {
                IsRunning = false;
                runDeferred = null;
            }
        }

        private void TryWork(PumpMessage work)
        {
            try
            {
                work.Execute();
                if (work.Deferred != null)
                {
                    work.Deferred.Resolve();
                }
            }
            catch (Exception ex)
            {
                work.Deferred?.Reject(ex);
                PumpExceptionArgs exceptionArgs = new PumpExceptionArgs(ex);
                PumpException.Fire(exceptionArgs);
      
                if (exceptionArgs.Handled == false)
                {
                    if (ex.InnerException != null)
                    {
                        ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    }
                    else
                    {
                        ExceptionDispatchInfo.Capture(ex).Throw();
                    }
                }
            }
        }

        private void DebounceResize()
        {
            console.Clear();
            bool done = false;
            ActionDebouncer debouncer = new ActionDebouncer(TimeSpan.FromSeconds(.25), () =>
            {
                done = true;
            });

            debouncer.Trigger();
            while(done == false)
            {
                if(console.BufferWidth != lastConsoleWidth || console.WindowHeight != lastConsoleHeight)
                {
                    lastConsoleWidth = console.BufferWidth;
                    lastConsoleHeight = console.WindowHeight;
                    debouncer.Trigger();
                }
            }
        }
    }
}
