using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;

namespace PowerArgs.Cli
{
    /// <summary>
    /// An interfaced that, when implemented, allows you
    /// to edit the freshly painted bitmap of a control 
    /// just before it is composed onto its parent
    /// </summary>
    public interface IConsoleControlFilter
    {
        /// <summary>
        /// The control that was just painted
        /// </summary>
        ConsoleControl Control { get; set; }
        
        /// <summary>
        /// The filter implementation
        /// </summary>
        /// <param name="bitmap">The bitmap you can modify</param>
        void Filter(ConsoleBitmap bitmap);
    }

    /// <summary>
    /// A filter whose implementation is defined inline via an action
    /// </summary>
    public class ConsoleControlFilter : IConsoleControlFilter
    {
        /// <summary>
        /// The control that was just painted
        /// </summary>
        public ConsoleControl Control { get; set; }
        private Action<ConsoleBitmap> impl;
        
        /// <summary>
        /// Creates a new filter
        /// </summary>
        /// <param name="impl">the filter impl</param>
        public ConsoleControlFilter(Action<ConsoleBitmap> impl)
        {
            this.impl = impl;
        }

        /// <summary>
        /// Calls the filter impl action
        /// </summary>
        /// <param name="bmp">the bitmap to modify</param>
        public void Filter(ConsoleBitmap bmp) => impl(bmp);
    }

    /// <summary>
    /// A class that represents a visual element within a CLI application
    /// </summary>
    public class ConsoleControl : Rectangular
    {
        public List<IConsoleControlFilter> RenderFilters { get; private set; } = new List<IConsoleControlFilter>();

        /// <summary>
        /// Controls how controls are painted when multiple controls overlap
        /// </summary>
        public CompositionMode CompositionMode { get; set; } = CompositionMode.PaintOver;
        /// <summary>
        /// An id that can be used for debugging.  It is not used for anything internally.
        /// </summary>
        public string Id { get { return Get<string>(); } set { Set(value); } }



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
        public Container Parent { get { return Get<Container>(); } internal set { Set(value); } }


        private RGB _bg, _fg;
        /// <summary>
        /// Gets or sets the background color
        /// </summary>
        public RGB Background { get { return _bg; } set { SetHardIf(ref _bg, value, value != _bg); } }

        /// <summary>
        /// Gets or sets the foreground color
        /// </summary>
        public RGB Foreground { get { return _fg; } set { SetHardIf(ref _fg, value, value != _fg); } }

        private bool _transparent;
        /// <summary>
        /// Gets or sets whether or not this control should paint its background color or leave it transparent.  By default
        /// this value is false.
        /// </summary>
        public bool TransparentBackground { get { return _transparent; } set {  SetHardIf(ref _transparent, value, _transparent != value); } }

        /// <summary>
        /// An arbitrary reference to an object to associate with this control
        /// </summary>
        public object Tag { get { return Get<object>(); } set { Set(value); } }

        private bool _isVisible;
        /// <summary>
        /// Gets or sets whether or not this control is visible.  Invisible controls are still fully functional, except that they
        /// don't get painted
        /// </summary>
        public virtual bool IsVisible { get { return _isVisible; } set { SetHardIf(ref _isVisible, value, _isVisible != value); } }

        private bool _canFocus;
        /// <summary>
        /// Gets or sets whether or not this control can accept focus.  By default this is set to true, but can
        /// be overridden by derived classes to be false by default.
        /// </summary>
        public virtual bool CanFocus { get { return _canFocus; } set { SetHardIf(ref _canFocus, value, _canFocus != value); } }

        private bool _hasFocus;
        /// <summary>
        /// Gets whether or not this control currently has focus
        /// </summary>
        public bool HasFocus { get { return _hasFocus; } internal set { SetHardIf(ref _hasFocus, value, _hasFocus != value); } }

        /// <summary>
        /// The writer used to record the visual state of the control
        /// </summary>
        public ConsoleBitmapVideoWriter Recorder { get; private set; }

        /// <summary>
        /// An optional call back that lets you override the timestamp for each recorded frame. If not
        /// specified then the wallclock will be used
        /// </summary>
        public Func<TimeSpan> RecorderTimestampProvider { get; private set; } 

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
                ConsoleControl current = this;
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
                ConsoleControl current = this;
                while (current.Parent != null)
                {
                    current = current.Parent;
                    ret += current.Y;
                }
                return ret;
            }
        }

        public ConsoleBitmap Bitmap { get; internal set; }

        public ConsoleControl() : this(1, 1) { }
        /// <summary>
        /// Creates a new ConsoleControl
        /// </summary>
        public ConsoleControl(int w, int h)
        {
            CanFocus = true;
            this.Bitmap = new ConsoleBitmap(w, h);
            this.Width = w;
            this.Height = h;
            this.SubscribeForLifetime(nameof(Bounds), ResizeBitmapOnBoundsChanged, this);
            Background = DefaultColors.BackgroundColor;
            this.Foreground = DefaultColors.ForegroundColor;
            this.IsVisible = true;
            this.Id = GetType().FullName+"-"+ Guid.NewGuid().ToString();
            this.SubscribeForLifetime(ObservableObject.AnyProperty, PaintOnChange, this);

        }


        private void ResizeBitmapOnBoundsChanged()
        {
            if (this.IsExpired || this.IsExpiring) return;
            if (this.Width > 0 && this.Height > 0)
            {
                this.Bitmap.Resize(this.Width, this.Height);
            }
        }

        private void PaintOnChange()
        {
            if (this.Application != null && this.Application.IsRunning && this.Application.IsDrainingOrDrained == false)
            {
                ConsoleApp.AssertAppThread(this.Application);
                this.Application.RequestPaint();
            }
        }

        /// <summary>
        /// Enables recording the visual content of the control using the specified writer
        /// </summary>
        /// <param name="recorder">the writer to use</param>
        /// <param name="timestampFunc">an optional callback that will be called to determine the timestamp for each frame. If not specified the wall clock will be used.</param>
        public void EnableRecording(ConsoleBitmapVideoWriter recorder, Func<TimeSpan> timestampFunc = null)
        {
            if(Recorder != null)
            {
                throw new InvalidOperationException("This control is already being recorded");
            }
            var h = this.Height;
            var w = this.Width;
            this.SubscribeForLifetime(nameof(Bounds), () => 
            {
                if(Width != w || Height != h)
                {
                    throw new InvalidOperationException("You cannot resize a control that has recording enabled");
                }
            }, this);
            this.Recorder = recorder;
            this.RecorderTimestampProvider = timestampFunc;

            this.OnDisposed(() => Recorder.TryFinish());
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
            if (Application.IsRunning)
            {
                Ready.Fire();
            }
            else
            {
                Application.InvokeNextCycle(Ready.Fire);
            }
            AddedToVisualTree.Fire();
            SubscribeForLifetime(ObservableObject.AnyProperty, ()=> Application?.RequestPaint(), this);
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

        internal void Paint()
        {
            if (IsVisible == false || Height <= 0 || Width <= 0)
            {
                return;
            }
            
            if (TransparentBackground == false)
            {
                Bitmap.Fill(new ConsoleCharacter(' ', null, Background));
            }

            OnPaint(Bitmap);
            if (Recorder != null && Recorder.IsFinished == false)
            {
                Recorder.Window = new RectF(0,0,Width,Height);
                Recorder.WriteFrame(Bitmap, false, RecorderTimestampProvider != null ? new TimeSpan?(RecorderTimestampProvider()) : new TimeSpan?());
            }
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
