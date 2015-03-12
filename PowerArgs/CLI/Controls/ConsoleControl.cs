using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    public class ConsoleControl : Rectangular
    {
        public event Action Focused;
        public event Action Unfocused;
        public event Action Added;
        public event Action Removed;

        public event Action<ConsoleKeyInfo> KeyInputReceived;

        public ConsoleApp Application { get; internal set; }
        public ConsoleCharacter Background { get; set; }
        public ConsoleCharacter Foreground { get; set; }

        public virtual bool CanFocus { get; set; }

        public bool HasFocus { get; internal set; }

        public ConsoleCharacter FocusForeground { get; set; }
        public ConsoleCharacter FocusBackground { get; set; }

        public ConsoleControl()
        {
            CanFocus = true;
            this.FocusForeground = new ConsoleCharacter('X', ConsoleColor.Cyan);
            this.FocusBackground = new ConsoleCharacter(' ');
            this.Foreground = new ConsoleCharacter('X', ConsoleColor.White);
        }

        public void Focus()
        {
            if (Application != null) Application.SetFocus(this);
        }

        public void UnFocus()
        {
            if (Application != null) Application.MoveFocus(true);
        }

        public virtual void OnRemove(ConsoleControl parent)
        {
            if(Removed != null)
            {
                Removed();
            }
        }

        public virtual void OnAdd(ConsoleControl parent)
        {
            if(Added != null)
            {
                Added();
            }
        }

        internal void Paint(ConsoleBitmap context)
        {
            Rectangle scope = context.GetScope();
            try
            {
                context.Rescope(this.X, this.Y, this.Width, this.Height);
                context.Pen = this.Foreground;
                OnPaint(context);
            }
            finally
            {
                context.Scope(scope);
            }
        }

        internal virtual void OnPaint(ConsoleBitmap context)
        {
            context.Pen = Foreground;
            context.FillRect(0, 0, Width, Height);
        }

        public virtual void OnKeyInputReceived(ConsoleKeyInfo info)
        {
            if(KeyInputReceived != null)
            {
                KeyInputReceived(info);
            }
        }

        internal void FireFocused(bool focused)
        {
            if (focused && Focused != null) Focused();
            if (!focused && Unfocused != null) Unfocused();
        }

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
