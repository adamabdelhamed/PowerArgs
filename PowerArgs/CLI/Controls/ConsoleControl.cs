using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs.Cli
{
    public class ConsoleControl : Rectangular
    {
        public static ConsoleCharacter TransparantColor = new ConsoleCharacter(' ');

        public event Action Focused;
        public event Action Unfocused;
        public event Action Added;
        public event Action Removed;

        public event Action<ConsoleKeyInfo> KeyInputReceived;

        public ConsoleApp Application { get; internal set; }
        public ConsoleCharacter Background { get; set; }
        public ConsoleCharacter Foreground { get; set; }

        public virtual bool CanFocus { get { return Get<bool>(); } set { Set<bool>(value); } }

        public bool HasFocus { get { return Get<bool>(); } internal set { Set<bool>(value); } }

        public ConsoleCharacter FocusForeground { get; set; }
        public ConsoleCharacter FocusBackground { get; set; }

        public ConsoleControl()
        {
            CanFocus = true;
            Background = ConsoleControl.TransparantColor;
            FocusBackground = ConsoleControl.TransparantColor;
            this.FocusForeground = new ConsoleCharacter('X', ConsoleColor.Cyan);
            this.Foreground = new ConsoleCharacter('X', ConsoleColor.White);
            this.PropertyChanged += ConsoleControl_PropertyChanged;
        }

        void ConsoleControl_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (Application != null)
            {
                this.Application.Paint();
            }
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

            if (Background != ConsoleControl.TransparantColor)
            {
                context.Pen = Background;
                context.FillRect(0, 0, Width, Height);
            }

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
