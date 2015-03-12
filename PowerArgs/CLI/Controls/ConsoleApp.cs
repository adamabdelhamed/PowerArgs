using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    public class ConsoleApp
    {
        private class ExitConsoleAppException : Exception { }

        public ConsoleBitmap Bitmap { get; set; }
        public ConsolePanel LayoutRoot { get; private set; }

        private ConsoleControl focusedControl;

        private int focusIndex;
        List<ConsoleControl> tabbableControls;

        public bool ExitOnEscapeCharacter { get; set; }

        public event Action ApplicationStopped;

        public bool IsRunning { get; private set; }

        public ControlCollection Controls
        {
            get
            {
                return LayoutRoot.Controls;
            }
        }

        public int Width
        {
            get
            {
                return LayoutRoot.Width;
            }
        }

        public int Height
        {
            get
            {
                return LayoutRoot.Height;
            }
        }

        public ConsoleApp(int x, int y, int w, int h)
        {
            Bitmap = new ConsoleBitmap(x,y, w, h);
            LayoutRoot = new ConsolePanel { Width = w, Height = h };
            LayoutRoot.Application = this;
            tabbableControls = new List<ConsoleControl>();
            focusIndex = -1;
            ExitOnEscapeCharacter = true;
            LayoutRoot.Controls.Added += (c) =>
            {
                c.Application = this;
                if (c.CanFocus) tabbableControls.Add(c);

                if (c is ConsolePanel)
                {
                    var children = TraverseControlTree(c as ConsolePanel);
                    tabbableControls.AddRange(children.Where(child => child.CanFocus));
                    tabbableControls = tabbableControls.Distinct().ToList();
                    foreach (var child in children) child.Application = this;
                }
            };

            LayoutRoot.Controls.Removed += (c) =>
            {
                c.Application = null;
                if (c.CanFocus) tabbableControls.Remove(c);

                if (c is ConsolePanel)
                {
                    var children = TraverseControlTree(c as ConsolePanel);
                    foreach (var child in children)
                    {
                        tabbableControls.Remove(child);
                        child.Application = null;
                    }
                }
            };
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

        public void Run()
        {
            IsRunning = true;
            MoveFocus();
            LayoutRoot.Paint(this.Bitmap);
            Bitmap.Paint();

            try
            {
                while (IsRunning)
                {
                    try
                    {
                        var info = Console.ReadKey(true);

                        if (info.Key == ConsoleKey.Escape && ExitOnEscapeCharacter)
                        {
                            break;
                        }

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
                    catch (ExitConsoleAppException)
                    {
                        break;
                    }
                }
            }
            finally
            {
                IsRunning = false;
                using (var snapshot = Bitmap.CreateSnapshot())
                {
                    Bitmap.CreateWiper().Wipe();
                    if (ApplicationStopped != null)
                    {
                        ApplicationStopped();
                    }
                    Console.ForegroundColor = ConsoleString.DefaultForegroundColor;
                    Console.BackgroundColor = ConsoleString.DefaultBackgroundColor;
                }
            }
        }

        

        public void Paint()
        {
            lock (Bitmap.SyncLock)
            {
                LayoutRoot.Paint(Bitmap);
            }
            Bitmap.Paint();
        }

        public IDisposable GetDisposableLock()
        {
            return Bitmap.GetDisposableLock();
        }
    
        public void SetFocus(ConsoleControl newFocusControl)
        {
            var index = tabbableControls.IndexOf(newFocusControl);
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
            }
        }

        public void MoveFocus(bool forward = true)
        {
            if (forward) focusIndex++;
            else focusIndex--;

            if (focusIndex >= tabbableControls.Count) focusIndex = 0;
            if (focusIndex < 0) focusIndex = tabbableControls.Count - 1;

            if (tabbableControls.Count == 0) return;
            SetFocus(tabbableControls[focusIndex]);
        }
    }
}
