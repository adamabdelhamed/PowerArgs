using System;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A class that represents a visual element within a CLI application
    /// </summary>
    public class ConsoleControl : Rectangular
    {
        /// <summary>
        /// An event that fires after this control gets focus
        /// </summary>
        public event Action Focused;
        /// <summary>
        /// An event that fires after this control loses focus
        /// </summary>
        public event Action Unfocused;

        /// <summary>
        /// An event that fires when this control is added to the visual tree of a ConsoleApp. 
        /// </summary>
        public event Action Added;

        /// <summary>
        /// An event that fires when this control is removed from the visual tree of a ConsoleApp.
        /// </summary>
        public event Action Removed;

        /// <summary>
        /// An event that fires when a key is pressed while this control has focus and the control has decided not to process
        /// the key press internally.
        /// </summary>
        public event Action<ConsoleKeyInfo> KeyInputReceived;

        /// <summary>
        /// Gets a reference to the application this control is a part of
        /// </summary>
        public ConsoleApp Application { get; internal set; }

        /// <summary>
        /// Gets a reference to this control's parent in the visual tree.  It will be null if this control is not in the visual tree 
        /// and also if this control is the root of the visual tree.
        /// </summary>
        public ConsoleControl Parent { get; internal set; }

        /// <summary>
        /// Gets or sets the background color
        /// </summary>
        public ConsoleColor Background { get { return Get<ConsoleColor>(); } set { Set(value); } }

        /// <summary>
        /// Gets or sets the foreground color
        /// </summary>
        public ConsoleColor Foreground { get { return Get<ConsoleColor>(); } set { Set(value); } }

        public ConsoleColor SelectedUnfocusedColor { get { return Get<ConsoleColor>(); } set { Set(value); } }
        public bool TransparentBackground { get { return Get<bool>(); } set { Set(value); } }
        public object Tag { get { return Get<object>(); } set { Set(value); } }
        public virtual bool IsVisible { get { return Get<bool>(); } set { Set(value); } }
        public virtual bool CanFocus { get { return Get<bool>(); } set { Set(value); } }
        public bool HasFocus { get { return Get<bool>(); } internal set { Set(value); } }


        public ConsoleCharacter BackgroundCharacter
        {
            get
            {
                return new ConsoleCharacter(' ', null, Background);
            }
        }

        public ConsoleControl()
        {
            CanFocus = true;
            Background = Theme.DefaultTheme.BackgroundColor;
            this.Foreground = Theme.DefaultTheme.ForegroundColor;
            this.SelectedUnfocusedColor = Theme.DefaultTheme.SelectedUnfocusedColor;
            this.PropertyChanged += ConsoleControl_PropertyChanged;
            this.IsVisible = true;
        }

        void ConsoleControl_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (Application != null)
            {
                this.Application.Paint();
            }
        }

        public bool TryFocus()
        {
            if (Application != null)
            {
                return Application.FocusManager.TrySetFocus(this);
            }
            else
            {
                return false;
            }
        }

        public bool TryUnfocus()
        {
            if (Application != null)
            {
                return Application.FocusManager.TryMoveFocus(true);
            }
            else
            {
                return false;
            }
        }

        internal void FireFocused(bool focused)
        {
            if (focused && Focused != null) Focused();
            if (!focused && Unfocused != null) Unfocused();
        }

        internal void AddedInternal()
        {
            if (Added != null)
            {
                Added();
            }
            OnAdd();
        }

        internal void RemovedInternal()
        {
            if (Removed != null)
            {
                Removed();
            }
            OnRemove();
        }

        public virtual void OnRemove() { }

        public virtual void OnAdd() { }

        internal void Paint(ConsoleBitmap context)
        {
            if(IsVisible == false)
            {
                return;
            }

            if (TransparentBackground == false)
            {
                context.Pen = new ConsoleCharacter(' ', null, Background);
                context.FillRect(0, 0, Width, Height);
            }

            OnPaint(context);
        }

        internal virtual void OnPaint(ConsoleBitmap context)
        {
 
        }

        public void HandleKeyInput(ConsoleKeyInfo info)
        {
            OnKeyInputReceived(info);
            if (KeyInputReceived != null)
            {
                KeyInputReceived(info);
            }
        }

        public virtual bool OnKeyInputReceived(ConsoleKeyInfo info)
        {
            return false;
        }

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}
