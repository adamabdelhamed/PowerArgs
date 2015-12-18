
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    // todos before fully supporting the .Cli namespace
    //
    // Command bar
    // Notifications
    // Implement a proper focus manager with support for stacking focus management contexts (for dialogs)
    // Pull out view model concepts
    // Implement ConsoleControl.Visible
    // Samples for different data sources (e.g. An azure table, a file system)  
    // Lots of testing
    // Final code review and documentation

    /// <summary>
    /// A class representing a console application that uses a message pump to synchronize work on a UI thread
    /// </summary>
    public class ConsoleApp
    {
        [ThreadStatic]
        private static ConsoleApp _current;

        public static ConsoleApp Current
        {
            get
            {
                return _current;
            }
        }

        /// <summary>
        /// An event that fired when the application stops, after the message pump is no longer running
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

        private int focusIndex;
        public ConsoleControl FocusedControl { get; private set; }
        private List<ConsoleControl> focusableControls;
        public GlobalKeyHandlerStack GlobalKeyHandlers { get; private set; }

        public bool SetFocusOnStart
        {
            get; set;
        }

        public IReadOnlyCollection<ConsoleControl> FocusableControls
        {
            get
            {
                return focusableControls.AsReadOnly();
            }
        }

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
            Bitmap = new ConsoleBitmap(x,y, w, h);
            MessagePump = new CliMessagePump(Bitmap.Console, KeyPressed);
            LayoutRoot = new ConsolePanel { Width = w, Height = h };
            GlobalKeyHandlers = new GlobalKeyHandlerStack();
            LayoutRoot.Application = this;
            focusableControls = new List<ConsoleControl>();
            focusIndex = -1;
            LayoutRoot.Controls.Added += (c) =>
            {
                c.Application = this;
                // todo - CanFocus is currently a one time setting.  It needs to be changeable.
                if (c.CanFocus) focusableControls.Add(c);

                if (c is ConsolePanel)
                {
                    var children = TraverseControlTree(c as ConsolePanel);
                    focusableControls.AddRange(children.Where(child => child.CanFocus));
                    focusableControls = focusableControls.Distinct().ToList();
                    foreach (var child in children) child.Application = this;
                }
            };

            LayoutRoot.Controls.Removed += (c) =>
            {
                bool focusChanged = false;
        
                focusableControls.Remove(c);
                if(FocusedControl == c)
                {
                    focusChanged = true;
                    GracefullyUnfocus();
                }
                c.Application = null;


                if (c is ConsolePanel)
                {
                    var children = TraverseControlTree(c as ConsolePanel);
                    foreach (var child in children)
                    {
                        focusableControls.Remove(child);
                        if(FocusedControl == child)
                        {
                            focusChanged = true;
                            GracefullyUnfocus();
                        }
                        child.Application = null;
                    }
                }

                if(focusChanged)
                {
                    MoveFocus();
                }
            };

            MessagePump.WindowResized += Paint;
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
                MessagePump.QueueAction(() => { MoveFocus(); });
            }

            MessagePump.QueueAction(() => 
            {
                if(_current != null)
                {
                    throw new NotSupportedException("An application is already running on this thread.");
                }
                // ensures that the current app is set on the message pump thread
                _current = this;
            });
            Paint();

            return ret;
        }

        /// <summary>
        /// Queues up a request to paint the app.  The system will dedupe multiple paint requests when there are multiple in the pump's work queue
        /// </summary>
        public void Paint()
        {
            MessagePump.QueueAction(new PaintMessage(PaintInternal));
        }

        /// <summary>
        /// Gives focus to the given control.  The control must be present in the app or else an exception will be thrown.
        /// </summary>
        /// <param name="newFocusControl">the control to focus</param>
        public void SetFocus(ConsoleControl newFocusControl)
        {
            var index = focusableControls.IndexOf(newFocusControl);
            if (index < 0) throw new InvalidOperationException("The given control is not in the control tree");

            if (newFocusControl != FocusedControl)
            {
                if (FocusedControl != null)
                {
                    GracefullyUnfocus();
                }

                FocusedControl = newFocusControl;
                FocusedControl.HasFocus = true;
                focusIndex = index;

                if (FocusedControl != null) FocusedControl.FireFocused(true);
                Paint();
            }
        }

        /// <summary>
        /// Moves focus to the next or previous control
        /// </summary>
        /// <param name="forward">if true, the focus moves to the next control, otherwise focus moves to the previous control</param>
        public void MoveFocus(bool forward = true)
        {
            if (forward) focusIndex++;
            else focusIndex--;

            if (focusIndex >= focusableControls.Count) focusIndex = 0;
            if (focusIndex < 0) focusIndex = focusableControls.Count - 1;

            if (focusableControls.Count == 0) return;
            SetFocus(focusableControls[focusIndex]);
        }

        private void GracefullyUnfocus()
        {
            FocusedControl.HasFocus = false;
            FocusedControl.FireFocused(false);
            FocusedControl = null;
        }

        private void KeyPressed(ConsoleKeyInfo info)
        {
            if(GlobalKeyHandlers.TryHandle(info))
            {
                // great, it was handled
            }
            else if (info.Key == ConsoleKey.Tab)
            {
                MoveFocus(info.Modifiers.HasFlag(ConsoleModifiers.Shift) == false);
            }
            else if(info.Key == ConsoleKey.Escape)
            {
                MessagePump.Stop();
                return;
            }
            else if (FocusedControl != null)
            {
                FocusedControl.OnKeyInputReceived(info);
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
        }

        private void PaintInternal()
        {
            Bitmap.Pen = ConsoleControl.TransparantColor;
            Bitmap.FillRect(0, 0, LayoutRoot. Width, LayoutRoot.Height);
            LayoutRoot.Paint(Bitmap);
            Bitmap.Paint();
        }

        private List<ConsoleControl> TraverseControlTree(ConsolePanel toTraverse)
        {
            List<ConsoleControl> ret = new List<ConsoleControl>();
            foreach (var control in toTraverse.Controls)
            {
                if (control is ConsolePanel)
                {
                    ret.AddRange(TraverseControlTree(control as ConsolePanel));
                }
                ret.Add(control);

            }
            return ret;
        }
    }
}
