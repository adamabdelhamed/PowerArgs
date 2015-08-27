using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs.Cli
{
    public class ConsoleApp
    {
        private class ExitConsoleAppException : Exception { }

        public event Action ApplicationStopped;

        public ConsoleBitmap Bitmap { get; set; }
        public ConsolePanel LayoutRoot { get; private set; }
        public bool ExitOnEscapeCharacter { get; set; }
        public bool IsRunning { get; private set; }

        public ObservableCollection<ConsoleControl> Controls
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

        private int focusIndex;
        private ConsoleControl focusedControl;
        private List<ConsoleControl> focusableControls;

        public ConsoleApp(int x, int y, int w, int h)
        {
            Bitmap = new ConsoleBitmap(x,y, w, h);
            LayoutRoot = new ConsolePanel { Width = w, Height = h };
            LayoutRoot.Application = this;
            focusableControls = new List<ConsoleControl>();
            focusIndex = -1;
            ExitOnEscapeCharacter = true;
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

        public void Run()
        {
            IsRunning = true;
            MoveFocus();
            Paint();

            try
            {
                while (IsRunning)
                {
                    try
                    {
                        var info = Bitmap.Console.ReadKey(true);

                        if (info.Key == ConsoleKey.Escape && ExitOnEscapeCharacter)
                        {
                            break;
                        }
                        else if (info.Key == ConsoleKey.Tab)
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
                    Bitmap.Console.ForegroundColor = ConsoleString.DefaultForegroundColor;
                    Bitmap.Console.BackgroundColor = ConsoleString.DefaultBackgroundColor;
                }
            }
        }

        public void Paint()
        {
            lock (Bitmap.SyncLock)
            {
                Bitmap.Pen = ConsoleControl.TransparantColor;
                Bitmap.FillRect(0, 0, Width, Height);
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
            }
        }

        public void MoveFocus(bool forward = true)
        {
            if (forward) focusIndex++;
            else focusIndex--;

            if (focusIndex >= focusableControls.Count) focusIndex = 0;
            if (focusIndex < 0) focusIndex = focusableControls.Count - 1;

            if (focusableControls.Count == 0) return;
            SetFocus(focusableControls[focusIndex]);
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
