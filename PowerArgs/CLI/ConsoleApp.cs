
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    // todos before fully supporting the .Cli namespace
    //
    // Command bar
    // Notifications
    // Pull out view model concepts
    // Samples for different data sources (e.g. An azure table, a file system)  
    // Lots of testing
    // Final code review and documentation

    /// <summary>
    /// A class representing a console application that uses a message pump to synchronize work on a UI thread
    /// </summary>
    public class ConsoleApp : Lifetime
    {
        [ThreadStatic]
        private static ConsoleApp _current;

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
        /// An event that fires when a control is added to the visual tree
        /// </summary>
        public event Action<ConsoleControl> ControlAdded;

        /// <summary>
        /// An event that fires when a control is removed from the visual tree
        /// </summary>
        public event Action<ConsoleControl> ControlRemoved;

        /// <summary>
        /// An event that fired when the application stops, after the message pump is no longer running, and the console
        /// has been cleared of the app's visuals
        /// </summary>
        public event Action ApplicationStopped;

        /// <summary>
        /// Gets the bitmap that will be painted to the console
        /// </summary>
        public ConsoleBitmap Bitmap { get; private set; }

        /// <summary>
        /// Gets the root panel that contains the controls being used by the app
        /// </summary>
        public ConsolePanel LayoutRoot { get; private set; }

        /// <summary>
        /// Gets the message pump that is used to synchronize work
        /// </summary>
        public CliMessagePump MessagePump { get; private set; }

        /// <summary>
        /// Gets the focus manager used to manage input focus
        /// </summary>
        public FocusManager FocusManager { get; private set; }

        public bool AutoFillOnConsoleResize { get; set; }

        /// <summary>
        /// Gets or sets the theme
        /// </summary>
        public Theme Theme { get; set; }

        /// <summary>
        /// Gets or set whether or not to give focus to a control when the app starts.  The default is true.
        /// </summary>
        public bool SetFocusOnStart { get; set; }

        /// <summary>
        /// Creates a new console app given a set of boundaries
        /// </summary>
        /// <param name="x">The left position on the target console to bound this app</param>
        /// <param name="y">The right position on the target console to bound this app</param>
        /// <param name="w">The width of the app</param>
        /// <param name="h">The height of the app</param>
        public ConsoleApp(int x, int y, int w, int h)
        {
            SetFocusOnStart = true;
            Theme = new Theme();
            Bitmap = new ConsoleBitmap(x, y, w, h);
            MessagePump = new CliMessagePump(Bitmap.Console, KeyPressed);
            LayoutRoot = new ConsolePanel { Width = w, Height = h };
            FocusManager = new FocusManager();
            LayoutRoot.Application = this;
            AutoFillOnConsoleResize = false;
            FocusManager.SubscribeForLifetime(nameof(FocusManager.FocusedControl), Paint, LifetimeManager);
            LayoutRoot.Controls.BeforeAdded.SubscribeForLifetime((c) => { c.Application = this; c.BeforeAddedToVisualTreeInternal(); }, LifetimeManager);
            LayoutRoot.Controls.BeforeRemoved.SubscribeForLifetime((c) => { c.BeforeRemovedFromVisualTreeInternal(); }, LifetimeManager);
            LayoutRoot.Controls.Added.SubscribeForLifetime(ControlAddedToVisualTree, LifetimeManager);
            LayoutRoot.Controls.Removed.SubscribeForLifetime(ControlRemovedFromVisualTree, LifetimeManager);
            MessagePump.WindowResized += HandleDebouncedResize;
        }

        /// <summary>
        /// Creates a full screen console app
        /// </summary>
        public ConsoleApp() : this(0,0,ConsoleProvider.Current.BufferWidth, ConsoleProvider.Current.WindowHeight-1)
        {
            this.AutoFillOnConsoleResize = true;
        }

        /// <summary>
        /// Starts the app, asynchronously.
        /// </summary>
        /// <returns>A task that will complete when the app exits</returns>
        public Task Start()
        {
            Task pumpTask = MessagePump.Start();
            var ret = pumpTask.ContinueWith((t) =>
            {
                ExitInternal();
            });

            if (SetFocusOnStart)
            {
                MessagePump.QueueAction(() => { FocusManager.TryMoveFocus(); });
            }

            MessagePump.QueueAction(() =>
            {
                if (_current != null)
                {
                    throw new NotSupportedException("An application is already running on this thread.");
                }
                // ensures that the current app is set on the message pump thread
                _current = this;
            });
            Paint();

            return ret;
        }

        private void HandleDebouncedResize()
        {
            if(AutoFillOnConsoleResize)
            {
                Bitmap.Resize(Bitmap.Console.BufferWidth, Bitmap.Console.WindowHeight - 1);
                this.LayoutRoot.Size = new Size(Bitmap.Console.BufferWidth, Bitmap.Console.WindowHeight - 1);
            }

            Paint();
        }

        /// <summary>
        /// Queues up a request to paint the app.  The system will dedupe multiple paint requests when there are multiple in the pump's work queue
        /// </summary>
        public void Paint()
        {
            MessagePump.QueueAction(new PaintMessage(PaintInternal));
        }

        private void ControlAddedToVisualTree(ConsoleControl c)
        {
            c.Application = this;

            if (c is ConsolePanel)
            {
                var childPanel = c as ConsolePanel;
                childPanel.Controls.SynchronizeForLifetime((cp) => { ControlAddedToVisualTree(cp); }, (cp) => { ControlRemovedFromVisualTree(cp); }, () => { }, c.LifetimeManager);
            }

            FocusManager.Add(c);
            c.AddedToVisualTreeInternal();

            if (ControlAdded != null)
            {
                ControlAdded(c);
            }
        }

        private void ControlRemovedFromVisualTree(ConsoleControl c)
        {
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
            if (ControlRemoved != null)
            {
                ControlRemoved(c);
            }
            c.Dispose();
            return focusChanged;
        }

        private void KeyPressed(ConsoleKeyInfo info)
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
                MessagePump.Stop();
                return;
            }
            else if (FocusManager.FocusedControl != null)
            {
                FocusManager.FocusedControl.HandleKeyInput(info);
            }

            Paint();
        }

        private void ExitInternal()
        {
            using (var snapshot = Bitmap.CreateSnapshot())
            {
                Bitmap.CreateWiper().Wipe();
                if (ApplicationStopped != null)
                {
                    ApplicationStopped();
                }
                Bitmap.Console.ForegroundColor = ConsoleString.DefaultForegroundColor;
                Bitmap.Console.BackgroundColor = ConsoleString.DefaultBackgroundColor;
            }
            Dispose();
        }

        private void PaintInternal()
        {

            Bitmap.Pen = new ConsoleCharacter(' ', null, Theme.BackgroundColor);
            Bitmap.FillRect(0, 0, LayoutRoot.Width, LayoutRoot.Height);
#if PROFILING
            using (new TimeProfiler("LayoutRoot.Paint"))
            {
#endif
                LayoutRoot.Paint(Bitmap);
#if PROFILING
            }
#endif

#if PROFILING
            using (new TimeProfiler("Bitmap.Paint"))
            {
#endif
                Bitmap.Paint();
#if PROFILING
            }
#endif
        }
    }
}
