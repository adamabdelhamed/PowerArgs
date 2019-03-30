using System;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A class that represents a visual element within a CLI application
    /// </summary>
    public class ConsoleControl : Rectangular
    {
        /// <summary>
        /// An id that can be used for debugging.  It is not used for anything internally.
        /// </summary>
        public string Id { get { return Get<string>(); } set { Set(value); } }

        /// <summary>
        /// Used to determine the order in which to paint a control within its parent.  Controls
        /// with higher ZIndex values are pained on top of controls with lower values.
        /// </summary>
        public int ZIndex { get; set; }

        /// <summary>
        /// An event that fires after this control gets focus
        /// </summary>
        public Event Focused { get; private set; } = new Event();
        /// <summary>
        /// An event that fires after this control loses focus
        /// </summary>
        public Event Unfocused { get; private set; } = new Event();

        /// <summary>
        /// An event that fires when this control is added to the visual tree of a ConsoleApp. 
        /// </summary>
        public Event AddedToVisualTree { get; private set; } = new Event();

        /// <summary>
        /// An event that fires just before this control is added to the visual tree of a ConsoleApp
        /// </summary>
        public Event BeforeAddedToVisualTree { get; private set; } = new Event();

        /// <summary>
        /// An event that fires when this control is removed from the visual tree of a ConsoleApp.
        /// </summary>
        public Event RemovedFromVisualTree { get; private set; } = new Event();

        /// <summary>
        /// An event that fires just before this control is removed from the visual tree of a ConsoleApp
        /// </summary>
        public Event BeforeRemovedFromVisualTree { get; private set; } = new Event();

        /// <summary>
        /// An event that fires when a key is pressed while this control has focus and the control has decided not to process
        /// the key press internally.
        /// </summary>
        public Event<ConsoleKeyInfo> KeyInputReceived { get; private set; } = new Event<ConsoleKeyInfo>();

        /// <summary>
        /// Gets a reference to the application this control is a part of
        /// </summary>
        public ConsoleApp Application { get; internal set; }

        /// <summary>
        /// Gets a reference to this control's parent in the visual tree.  It will be null if this control is not in the visual tree 
        /// and also if this control is the root of the visual tree.
        /// </summary>
        public ConsoleControl Parent { get { return Get<ConsoleControl>(); } internal set { Set(value); } }

        /// <summary>
        /// Gets or sets the background color
        /// </summary>
        public ConsoleColor Background { get { return Get<ConsoleColor>(); } set { Set(value); } }

        /// <summary>
        /// Gets or sets the foreground color
        /// </summary>
        public ConsoleColor Foreground { get { return Get<ConsoleColor>(); } set { Set(value); } }

        /// <summary>
        /// Gets or sets whether or not this control should paint its background color or leave it transparent.  By default
        /// this value is false.
        /// </summary>
        public bool TransparentBackground { get { return Get<bool>(); } set { Set(value); } }

        /// <summary>
        /// An arbitrary reference to an object to associate with this control
        /// </summary>
        public object Tag { get { return Get<object>(); } set { Set(value); } }

        /// <summary>
        /// Gets or sets whether or not this control is visible.  Invisible controls are still fully functional, except that they
        /// don't get painted
        /// </summary>
        public virtual bool IsVisible { get { return Get<bool>(); } set { Set(value); } }

        /// <summary>
        /// Gets or sets whether or not this control can accept focus.  By default this is set to true, but can
        /// be overridden by derived classes to be false by default.
        /// </summary>
        public virtual bool CanFocus { get { return Get<bool>(); } set { Set(value); } }

        /// <summary>
        /// Gets whether or not this control currently has focus
        /// </summary>
        public bool HasFocus { get { return Get<bool>(); } internal set { Set(value); } }

        /// <summary>
        /// Set to true if the Control is in the process of being removed
        /// </summary>
        internal bool IsBeingRemoved { get; set; }

        private bool hasBeenAddedToVisualTree;

        /// <summary>
        /// An event that fires when this control is both added to an app and that app is running
        /// </summary>
        public Event Ready { get; private set; } = new Event();

        /// <summary>
        /// Gets the x coordinate of this control relative to the application root
        /// </summary>
        public int AbsoluteX
        {
            get
            {
                var ret = this.X;
                var current = this;
                while (current.Parent != null)
                {
                    current = current.Parent;
                    ret += current.X;
                }
                return ret;
            }
        }

        /// <summary>
        /// Gets the y coordinate of this control relative to the application root
        /// </summary>
        public int AbsoluteY
        {
            get
            {
                var ret = this.Y;
                var current = this;
                while (current.Parent != null)
                {
                    current = current.Parent;
                    ret += current.Y;
                }
                return ret;
            }
        }


        /// <summary>
        /// Creates a new ConsoleControl
        /// </summary>
        public ConsoleControl()
        {
            CanFocus = true;
            Background = DefaultColors.BackgroundColor;
            this.Foreground = DefaultColors.ForegroundColor;
            this.IsVisible = true;
            this.Id = GetType().FullName+"-"+ Guid.NewGuid().ToString();
            this.SubscribeForLifetime(ObservableObject.AnyProperty,()=> 
            {
                if (Application != null && Application.IsRunning)
                {
                    ConsoleApp.AssertAppThread(Application);
                    Application.Paint();
                }
            }, this);

            this.AddedToVisualTree.SubscribeOnce(() =>
            {
                if(Application.IsRunning)
                {
                    Ready.Fire();
                }
                else
                {
                    Application.QueueAction(Ready.Fire);
                }
            });
        }

        /// <summary>
        /// Tries to give this control focus. If the focus is in the visual tree, and is in the current focus layer, 
        /// and has it's CanFocus property to true then focus should be granted.
        /// </summary>
        /// <returns>True if focus was granted, false otherwise.  </returns>
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

        /// <summary>
        /// Tries to unfocus this control.
        /// </summary>
        /// <returns>True if focus was cleared and moved.  False otherwise</returns>
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

        /// <summary>
        /// Gets the type and Id of this control
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return GetType().Name+" ("+Id+")";
                }
        
        /// <summary>
        /// You should override this method if you are building a custom control, from scratch, and need to control
        /// every detail of the painting process.  If possible, prefer to create your custom control by deriving from
        /// ConsolePanel, which will let you assemble a new control from others.
        /// </summary>
        /// <param name="context">The scoped bitmap that you can paint on</param>
        protected virtual void OnPaint(ConsoleBitmap context)
        {

        }
        
        internal void FireFocused(bool focused)
        {
            if (focused) Focused.Fire();
            else Unfocused.Fire();
        }

      
        internal void AddedToVisualTreeInternal()
        {
            if (hasBeenAddedToVisualTree)
            {
                throw new ObjectDisposedException(Id, "This control has already been added to a visual tree and cannot be reused.");
            }

            hasBeenAddedToVisualTree = true;
            AddedToVisualTree.Fire();
            SubscribeForLifetime(ObservableObject.AnyProperty, ()=> Application.Paint(), this);
        }

        internal void BeforeAddedToVisualTreeInternal()
        {
            BeforeAddedToVisualTree.Fire();
        }

 

        internal void BeforeRemovedFromVisualTreeInternal()
        {
            BeforeRemovedFromVisualTree.Fire();
        }

        internal void RemovedFromVisualTreeInternal()
                {
            RemovedFromVisualTree.Fire();
        }

        internal void Paint(ConsoleBitmap context)
        {
            if (IsVisible == false)
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

        internal void PaintTo(ConsoleBitmap context)
        {
            OnPaint(context);
        }

        internal void HandleKeyInput(ConsoleKeyInfo info)
        {
            KeyInputReceived.Fire(info);
        }

        internal Point CalculateAbsolutePosition()
        {
            var x = X;
            var y = Y;

            var tempParent = Parent;
            while (tempParent != null)
        {
                x += tempParent.X;
                y += tempParent.Y;
                tempParent = tempParent.Parent;
            }
          
            return new Point(x, y);
        }

        internal Point CalculateRelativePosition(ConsoleControl parent)
        {
            var x = X;
            var y = Y;

            var tempParent = Parent;
            while (tempParent != null && tempParent != parent)
            {
                if (tempParent is ScrollablePanel)
        {
                    throw new InvalidOperationException("Controls within scrollable panels cannot have their relative positions calculated");
                }

                x += tempParent.X;
                y += tempParent.Y;
                tempParent = tempParent.Parent;
            }

            return new Point(x, y);
        }
    }
}
