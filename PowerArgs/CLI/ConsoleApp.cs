
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A class representing a console application that uses a message pump to synchronize work on a UI thread
    /// </summary>
    public class ConsoleApp : EventLoop, IObservableObject, ILifetime
    {
        [ThreadStatic]
        private static ConsoleApp _current;


        private List<Deferred> paintRequests = new List<Deferred>();
        private FrameRateMeter paintRateMeter = new FrameRateMeter();
        private Queue<ConsoleKeyInfo> sendKeys = new Queue<ConsoleKeyInfo>();

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
        /// An event that fires when the console window has been resized by the user
        /// </summary>
        public Event WindowResized { get; private set; } = new Event();


        private IConsoleProvider console;
        private int lastConsoleWidth, lastConsoleHeight;
 
        private List<IDisposable> timerHandles = new List<IDisposable>();

        private FrameRateMeter cycleRateMeter;

        /// <summary>
        /// Gets the total number of event loop cycles that have run
        /// </summary>
        public int TotalCycles => cycleRateMeter != null ? cycleRateMeter.TotalFrames : 0;


        /// <summary>
        /// Gets the current frame rate for the app
        /// </summary>
        public int CyclesPerSecond => cycleRateMeter != null ? cycleRateMeter.CurrentFPS : 0;

        /// <summary>
        /// Gets a reference to the current app running on this thread.  This will only be populated by the thread
        /// that is running the message pump (i.e. it will never be your main thread).
        /// </summary>
        public static ConsoleApp Current
        {
            get
            {
                return _current;
            }
        }

        /// <summary>
        /// Asserts that the current thread is running a ConsoleApp
        /// </summary>
        /// <param name="expectedApp">The specific app that is expected to be running on this thread or null to just check that any app is running</param>
        public static void AssertAppThread(ConsoleApp expectedApp = null)
        {
            if (Current == null)
            {
                throw new InvalidOperationException("There is no ConsoleApp running on this thread");
            }
            else if (expectedApp != null && Current != expectedApp)
            {
                throw new InvalidOperationException("The ConsoleApp on this thread is different from the one expected");
            }
        }

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
        /// Gets the total number of times a paint actually happened
        /// </summary>
        public int TotalPaints => paintRateMeter != null ? paintRateMeter.TotalFrames : 0;


        /// <summary>
        /// The writer used to record the contents of the screen while the app
        /// is running. If not set then recording does not take place
        /// </summary>
        public ConsoleBitmapStreamWriter Recorder { get; set; }

        /// <summary>
        /// An event that fires when the application is about to stop, before the console is wiped
        /// </summary>
        public Event Stopping { get; private set; } = new Event();

        /// <summary>
        /// An event that fires after the message pump is completely stopped and the console is wiped
        /// </summary>
        public Event Stopped { get; private set; } = new Event();

        /// <summary>
        /// An event that fires when a control is added to the visual tree
        /// </summary>
        public Event<ConsoleControl> ControlAdded { get; private set; } = new Event<ConsoleControl>();

        /// <summary>
        /// An event that fires when a control is removed from the visual tree
        /// </summary>
        public Event<ConsoleControl> ControlRemoved { get; private set; } = new Event<ConsoleControl>();

        /// <summary>
        /// Gets the bitmap that will be painted to the console
        /// </summary>
        public ConsoleBitmap Bitmap { get; private set; }

        /// <summary>
        /// Gets the root panel that contains the controls being used by the app
        /// </summary>
        public ConsolePanel LayoutRoot { get; private set; }

        /// <summary>
        /// Gets the focus manager used to manage input focus
        /// </summary>
        public FocusManager FocusManager { get; private set; }

        /// <summary>
        /// If set to true then the app will automatically update its layout to fill the entire window.  If false the app
        /// will not react to resizing, which means it may clip or wrap in unexpected ways when the window is resized.
        /// 
        /// If you use the constructor that takes no parameters then this is set to true and assumes you want to take the
        /// whole window and respond to window size changes.  If you use the constructor that takes in coordinates and boudnds
        /// then it is set to false and it is assumed that you only want the app to live within those bounds
        /// </summary>
        public bool AutoFillOnConsoleResize { get; set; }

        /// <summary>
        /// Gets or set whether or not to give focus to a control when the app starts.  The default is true.
        /// </summary>
        public bool SetFocusOnStart { get; set; }

        /// <summary>
        /// An event that fires just after painting the app
        /// </summary>
        public Event AfterPaint { get; private set; } = new Event();

        /// <summary>
        /// Creates a new console app given a set of boundaries
        /// </summary>
        /// <param name="x">The left position on the target console to bound this app</param>
        /// <param name="y">The right position on the target console to bound this app</param>
        /// <param name="w">The width of the app</param>
        /// <param name="h">The height of the app</param>
        public ConsoleApp(int x, int y, int w, int h) 
        {
            this.console = ConsoleProvider.Current;
            this.lastConsoleWidth = this.console.BufferWidth;
            this.lastConsoleHeight = this.console.WindowHeight;
            this.observable = new ObservableObject(this);

            cycleRateMeter = new FrameRateMeter();

            this.StartOfCycle.SubscribeForLifetime(Cycle, this);

            SetFocusOnStart = true;
            Bitmap = new ConsoleBitmap(x, y, w, h);
            LayoutRoot = new ConsolePanel { Width = w, Height = h };
            FocusManager = new FocusManager();
            LayoutRoot.Application = this;
            AutoFillOnConsoleResize = false;
            FocusManager.SubscribeForLifetime(nameof(FocusManager.FocusedControl), ()=> Paint(), this);
            LayoutRoot.Controls.BeforeAdded.SubscribeForLifetime((c) => { c.Application = this; c.BeforeAddedToVisualTreeInternal(); }, this);
            LayoutRoot.Controls.BeforeRemoved.SubscribeForLifetime((c) => { c.BeforeRemovedFromVisualTreeInternal(); }, this);
            LayoutRoot.Controls.Added.SubscribeForLifetime(ControlAddedToVisualTree, this);
            LayoutRoot.Controls.Removed.SubscribeForLifetime(ControlRemovedFromVisualTree, this);
            WindowResized.SubscribeForLifetime(HandleDebouncedResize, this);
            this.LoopStarted.SubscribeOnce(() => _current = this);
            this.EndOfCycle.SubscribeForLifetime(DrainPaints, this);
        }

        private void DrainPaints()
        {
            if(paintRequests.Count > 0)
            {
                PaintInternal();
                foreach(var request in paintRequests)
                {
                    request.Resolve();
                }
                paintRequests.Clear();
                paintRateMeter.Increment();
            }
        }

        /// <summary>
        /// Creates a new console app of the given width and height, positioned at x=0,y=0
        /// </summary>
        /// <param name="w">The width of the app</param>
        /// <param name="h">The height of the app</param>
        public ConsoleApp(int w, int h) : this(0, 0, w, h) { }

        /// <summary>
        /// Creates a full screen console app that will automatically adjust its layout if the window size changes
        /// </summary>
        public ConsoleApp() : this(0,0,ConsoleProvider.Current.BufferWidth, ConsoleProvider.Current.WindowHeight-1)
        {
            this.AutoFillOnConsoleResize = true;
        }

        /// <summary>
        /// Adds the given control to a ConsoleApp, fills the space, and blocks until the app terminates
        /// </summary>
        /// <param name="control">the control to show</param>
        public static void Show(ConsoleControl control)
        {
            var app = new ConsoleApp();
            app.LayoutRoot.Add(control).Fill();
            app.Start().Wait();
        }

        /// <summary>
        /// Starts a new ConsoleApp and waits for it to finish
        /// </summary>
        /// <param name="init">the function that initializes the app</param>
        public static void Show(Action<ConsoleApp> init)
        {
            var app = new ConsoleApp();
            app.InvokeNextCycle(()=>init(app));
            app.Start().Wait();
        }

   

        /// <summary>
        /// Starts the app, asynchronously.
        /// </summary>
        /// <returns>A task that will complete when the app exits</returns>
        public override Promise Start(string name = "ConsoleApp")
        {
            if (SetFocusOnStart)
            {
                InvokeNextCycle(() => 
                {
                    FocusManager.TryMoveFocus();
                });
            }

            Paint();

            return base.Start().Finally((p)=>
            {
                ExitInternal();
            });
        }

        private void HandleDebouncedResize()
        {
            if(Bitmap.Console.BufferWidth < 1 || Bitmap.Console.WindowHeight - 1 < 1)
            {
                return;
            }

            if(AutoFillOnConsoleResize)
            {
               
                Bitmap.Resize(Bitmap.Console.BufferWidth, Bitmap.Console.WindowHeight - 1);
                this.LayoutRoot.Size = new Size(Bitmap.Console.BufferWidth, Bitmap.Console.WindowHeight - 1);
            }

            Paint();
        }

        /// <summary>
        /// Queues up a request to paint the app.  The system will dedupe multiple paint requests when there are multiple in the pump's work queue
        /// <returns>a promise that resolves after the paint happens</returns>
        /// </summary>
        public Promise Paint()
        {
            var d = Deferred.Create();
            Invoke(() => paintRequests.Add(d));
            return d.Promise;
        }

        private void ControlAddedToVisualTree(ConsoleControl c)
        {
            c.Application = this;
            c.OnDisposed(() =>
            {
                if(c.Application == this && c.Parent != null && c.Parent.Application == this)
                {
                    if (c.Parent is ConsolePanel)
                    {
                        (c.Parent as ConsolePanel).Controls.Remove(c);
                    }
                    else
                    {
                        throw new NotSupportedException($"You cannot manually dispose child controls of parent type {c.Parent.GetType().Name}");
                    }
                }
            });

            if (c is ConsolePanel)
            {
                var childPanel = c as ConsolePanel;
                childPanel.Controls.SynchronizeForLifetime((cp) => { ControlAddedToVisualTree(cp); }, (cp) => { ControlRemovedFromVisualTree(cp); }, () => { }, c);
            }
            else if(c is ProtectedConsolePanel)
            {
                var childPanel = c as ProtectedConsolePanel;
                ControlAddedToVisualTree(childPanel.ProtectedPanelInternal);
                childPanel.OnDisposed(() => ControlRemovedFromVisualTree(childPanel.ProtectedPanelInternal));
            }

            FocusManager.Add(c);
            c.AddedToVisualTreeInternal();

            ControlAdded.Fire(c);
        }

        private void ControlRemovedFromVisualTree(ConsoleControl c)
        {
            c.IsBeingRemoved = true;
            if (ControlRemovedFromVisualTreeRecursive(c))
            {
                FocusManager.TryRestoreFocus();
            }
        }

        private bool ControlRemovedFromVisualTreeRecursive(ConsoleControl c)
        {
            bool focusChanged = false;

            if (c is ConsolePanel)
            {
                foreach (var child in (c as ConsolePanel).Controls)
                {
                    child.IsBeingRemoved = true;
                    focusChanged = ControlRemovedFromVisualTreeRecursive(child) || focusChanged;
                }
            }

            if (FocusManager.FocusedControl == c)
            {
                FocusManager.ClearFocus();
                focusChanged = true;
            }

            FocusManager.Remove(c);

            c.RemovedFromVisualTreeInternal();
            c.Application = null;
            ControlRemoved.Fire(c);
            if (c.IsExpired == false && c.IsExpiring == false)
            {
                c.Dispose();
            }
            return focusChanged;
        }

        /// <summary>
        /// Handles key input for the application
        /// </summary>
        /// <param name="info">The key that was pressed</param>
        protected virtual void HandleKeyInput(ConsoleKeyInfo info)
        {
            if (FocusManager.GlobalKeyHandlers.TryIntercept(info))
            {
                // great, it was handled
            }
            else if (info.Key == ConsoleKey.Tab)
            {
                FocusManager.TryMoveFocus(info.Modifiers.HasFlag(ConsoleModifiers.Shift) == false);
            }
            else if (info.Key == ConsoleKey.Escape)
            {
                Stop();
                return;
            }
            else if (FocusManager.FocusedControl != null)
            {
                if (FocusManager.FocusedControl.IsExpired == false)
                {
                    FocusManager.FocusedControl.HandleKeyInput(info);
                }
            }
            else
            {
                // not handled
            }
            Paint();
        }

        private void ExitInternal()
        {
            Stopping.Fire();
            Recorder?.WriteFrame(Bitmap, true);
            Recorder?.Dispose();
            using (var snapshot = Bitmap.CreateSnapshot())
            {
                Bitmap.CreateWiper().Wipe();
                Bitmap.Console.ForegroundColor = ConsoleString.DefaultForegroundColor;
                Bitmap.Console.BackgroundColor = ConsoleString.DefaultBackgroundColor;
            }
            _current = null;
            Stopped.Fire();
            Dispose();
        }

        private ConsoleCharacter defaultPen = new ConsoleCharacter(' ', null, DefaultColors.BackgroundColor);
        private void PaintInternal()
        {
            LayoutRoot.Bitmap = Bitmap;
            Bitmap.Pen = defaultPen;
            Bitmap.FillRect(0, 0, LayoutRoot.Width, LayoutRoot.Height);
            LayoutRoot.Paint();

            Recorder?.WriteFrame(Bitmap);
            Bitmap.Paint();
            AfterPaint.Fire();
        }


        private void Cycle()
        {
            cycleRateMeter.Increment();
            if ((lastConsoleWidth != this.console.BufferWidth || lastConsoleHeight != this.console.WindowHeight))
            {
                DebounceResize();
                WindowResized.Fire();
            }

            if (this.console.KeyAvailable)
            {
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
                    InvokeNextCycle(() => HandleKeyInput(info));
                }
            }
            else if (sendKeys.Count > 0)
            {
                var info = sendKeys.Dequeue();
                InvokeNextCycle(() => HandleKeyInput(info));
            }
        }



        /// <summary>
        /// Simulates a key press
        /// </summary>
        /// <param name="key">the key press info</param>
        public Promise SendKey(ConsoleKeyInfo key) => Invoke(() => { sendKeys.Enqueue(key); });


        /// <summary>
        /// Schedules the given action for periodic processing by the message pump
        /// </summary>
        /// <param name="a">The action to schedule for periodic processing</param>
        /// <param name="interval">the execution interval for the action</param>
        /// <returns>A handle that can be passed to ClearInterval if you want to cancel the work</returns>
        public IDisposable SetInterval(Action a, TimeSpan interval)
        {
            return new TimerDisposer(new Timer((o) => Invoke(a), null, (int)interval.TotalMilliseconds, (int)interval.TotalMilliseconds), timerHandles);
        }

        /// <summary>
        /// Schedules the given action for a one time execution after the given period elapses
        /// </summary>
        /// <param name="a">The action to schedule</param>
        /// <param name="period">the period of time to wait before executing the action</param>
        /// <returns></returns>
        public IDisposable SetTimeout(Action a, TimeSpan period)
        {
            return new TimerDisposer(new Timer((o) => Invoke(a), null, (int)period.TotalMilliseconds, Timeout.Infinite), timerHandles);
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






        private void DebounceResize()
        {
            console.Clear();
            bool done = false;
            ActionDebouncer debouncer = new ActionDebouncer(TimeSpan.FromSeconds(.25), () =>
            {
                done = true;
            });

            debouncer.Trigger();
            while (done == false)
            {
                if (console.BufferWidth != lastConsoleWidth || console.WindowHeight != lastConsoleHeight)
                {
                    lastConsoleWidth = console.BufferWidth;
                    lastConsoleHeight = console.WindowHeight;
                    debouncer.Trigger();
                }
            }
        }

        private ObservableObject observable;
        public bool SuppressEqualChanges { get => observable.SuppressEqualChanges; set => observable.SuppressEqualChanges = value; }
        public IDisposable SubscribeUnmanaged(string propertyName, Action handler) => observable.SubscribeUnmanaged(propertyName, handler);
        public void SubscribeForLifetime(string propertyName, Action handler, ILifetimeManager lifetimeManager) => observable.SubscribeForLifetime(propertyName, handler, lifetimeManager);
        public IDisposable SynchronizeUnmanaged(string propertyName, Action handler) => observable.SynchronizeUnmanaged(propertyName, handler);
        public void SynchronizeForLifetime(string propertyName, Action handler, ILifetimeManager lifetimeManager) => SynchronizeForLifetime(propertyName, handler, lifetimeManager);
        public object GetPrevious(string propertyName) => ((IObservableObject)observable).GetPrevious(propertyName);
        public Lifetime GetPropertyValueLifetime(string propertyName) => observable.GetPropertyValueLifetime(propertyName);

        public T Get<T>([CallerMemberName]string name = null) => observable.Get<T>(name);
        public void Set<T>(T value, [CallerMemberName]string name = null) => observable.Set<T>(value, name);

        private Lifetime lifetime = new Lifetime();
        public Promise OnDisposed(Action cleanupCode) => lifetime.OnDisposed(cleanupCode);
        public Promise OnDisposed(IDisposable obj) => lifetime.OnDisposed(obj);
        public bool IsExpired => lifetime.IsExpired;
        public bool TryDispose() => lifetime.TryDispose();
        public void Dispose() => lifetime.Dispose();

      
    }

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
}
