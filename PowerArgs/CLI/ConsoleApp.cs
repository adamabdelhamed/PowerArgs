
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A class representing a console application that uses a message pump to synchronize work on a UI thread
    /// </summary>
    public class ConsoleApp
    {
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
        private ConsoleControl focusedControl;
        private List<ConsoleControl> focusableControls;

        /// <summary>
        /// Creates a new console app given a set of boundaries
        /// </summary>
        /// <param name="x">The left position on the target console to bound this app</param>
        /// <param name="y">The right position on the target console to bound this app</param>
        /// <param name="w">The width of the app</param>
        /// <param name="h">The height of the app</param>
        public ConsoleApp(int x, int y, int w, int h)
        {
            Bitmap = new ConsoleBitmap(x,y, w, h);
            MessagePump = new CliMessagePump(Bitmap.Console, KeyPressed);
            LayoutRoot = new ConsolePanel { Width = w, Height = h };
            LayoutRoot.Application = this;
            focusableControls = new List<ConsoleControl>();
            focusIndex = -1;
            LayoutRoot.Controls.Added += (c) =>
            {
                c.Application = this;
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
                c.Application = null;
                if (c.CanFocus) focusableControls.Remove(c);

                if (c is ConsolePanel)
                {
                    var children = TraverseControlTree(c as ConsolePanel);
                    foreach (var child in children)
                    {
                        focusableControls.Remove(child);
                        child.Application = null;
                    }
                }
            };
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

            MessagePump.QueueAction(()=> { MoveFocus(); });
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

            if (newFocusControl != focusedControl)
            {
                if (focusedControl != null)
                {
                    focusedControl.HasFocus = false;
                    focusedControl.FireFocused(false);
                }

                focusedControl = newFocusControl;
                focusedControl.HasFocus = true;
                focusIndex = index;

                if (focusedControl != null) focusedControl.FireFocused(true);
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

        private void KeyPressed(ConsoleKeyInfo info)
        {
            if (info.Key == ConsoleKey.Tab)
            {
                MoveFocus(info.Modifiers.HasFlag(ConsoleModifiers.Shift) == false);
            }
            else if (focusedControl != null)
            {
                focusedControl.OnKeyInputReceived(info);
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
