using System;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A class that represents a visual element within a CLI application
    /// </summary>
    public class ConsoleControl : Rectangular
    {
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
        public event Action Added;

        public event Action BeforeAddedToVisualTree;

        /// <summary>
        /// An event that fires when this control is removed from the visual tree of a ConsoleApp.
        /// </summary>
        public event Action RemovedFromVisualTree;

        public event Action BeforeRemovedFromVisualTree;

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

        private bool hasBeenAddedToVisualTree;

        public ConsoleControl()
        {
            CanFocus = true;
            Background = Theme.DefaultTheme.BackgroundColor;
            this.Foreground = Theme.DefaultTheme.ForegroundColor;
            this.SelectedUnfocusedColor = Theme.DefaultTheme.SelectedUnfocusedColor;
            this.IsVisible = true;
        }

        void PropertyChanged()
        {
            this.Application.Paint();
        }

        public Point CalculateAbsolutePosition()
        {
            var x = X;
            var y = Y;

            var tempParent = Parent;
            while(tempParent != null)
            {
                x += tempParent.X;
                y += tempParent.Y;
                tempParent = tempParent.Parent;
            }

            return new Point(x, y);
        }

        public Point CalculateRelativePosition(ConsoleControl parent)
        {
            var x = X;
            var y = Y;

            var tempParent = Parent;
            while (tempParent != null && tempParent != parent)
            {
                x += tempParent.X;
                y += tempParent.Y;
                tempParent = tempParent.Parent;
            }

            return new Point(x, y);
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

        public Subscription RegisterKeyHandlerUnmanaged(ConsoleKey key, Action<ConsoleKeyInfo> handler)
        {
            Action<ConsoleKeyInfo> conditionalHandler = (info) =>
            {
                if(info.Key == key)
                {
                    handler(info);
                }
            };
            return this.KeyInputReceived.SubscribeUnmanaged(conditionalHandler);
        }
        
        public void RegisterKeyHandlerForLifetime(ConsoleKey key, Action<ConsoleKeyInfo> handler, LifetimeManager manager)
        {
            manager.Manage(RegisterKeyHandlerUnmanaged(key, handler));
        }

        public void RegisterKeyHandler(ConsoleKey key, Action<ConsoleKeyInfo> handler)
        {
            LifetimeManager.Manage(RegisterKeyHandlerUnmanaged(key, handler));
        }
        
        internal void FireFocused(bool focused)
        {
            if (focused) Focused.Fire();
            else Unfocused.Fire();
        }

        internal void AddedToVisualTreeInternal()
        {
            if (hasBeenAddedToVisualTree) throw new ObjectDisposedException(Id, "This control has already been added to a visual tree and cannot be reused.");
            hasBeenAddedToVisualTree = true;
            using (new AmbientLifetimeScope(LifetimeManager))
            {
                OnAddedToVisualTree();
                if (Added != null)
                {
                    Added();
                }

                Subscribe(ObservableObject.AnyProperty, Application.Paint);
            }
            
        }

        internal void BeforeAddedToVisualTreeInternal()
        {
            OnBeforeAddedToVisualTree();
            if (BeforeAddedToVisualTree != null)
            {
                BeforeAddedToVisualTree();
            }
        }

        internal void RemovedFromVisualTreeInternal()
        {
            OnRemovedFromVisualTree();
            if (RemovedFromVisualTree != null)
            {
                RemovedFromVisualTree();
            }
        }

        internal void BeforeRemovedFromVisualTreeInternal()
        {
            OnBeforeRemoveFromVisualTree();
            if (BeforeRemovedFromVisualTree != null)
            {
                BeforeRemovedFromVisualTree();
            }
        }

        public virtual void OnRemovedFromVisualTree() { }

        public virtual void OnBeforeRemoveFromVisualTree() { }

        public virtual void OnAddedToVisualTree() { }

        public virtual void OnBeforeAddedToVisualTree() { }


        /// <summary>
        /// Registers an action to be queued for execution on the message pump thread after the given async task
        /// completes.  The action will not execute if this control is no longer in the visual tree when the async task
        /// completes.
        /// </summary>
        /// <param name="t">The async task that, when completed, triggers the action</param>
        /// <param name="action">the action to execute after the async task completes, unless this control is not in the visual tree when the task completes</param>
        public void QueueAsyncActionIfStillInvisualTree(Task t, Action<Task> action)
        {
            if (Application == null) throw new NullReferenceException("This control is not in the visual control tree");

            Application.MessagePump.QueueAsyncAction(t, (tp) =>
            {
                if (Application != null)
                {
                    action(tp);
                }
            });
        }

        /// <summary>
        /// Registers an action to be queued for execution on the message pump thread after the given async task
        /// completes.  The action will not execute if this control is no longer in the visual tree when the async task
        /// completes.
        /// </summary>
        /// <typeparam name="TResult">The type of result returned by the async task</typeparam>
        /// <param name="t">The async task that, when completed, triggers the action</param>
        /// <param name="action">the action to execute after the async task completes, unless this control is not in the visual tree when the task completes</param>
        public void QueueAsyncActionIfStillInVisualTree<TResult>(Task<TResult> t, Action<Task<TResult>> action)
        {
            if (Application == null) throw new NullReferenceException("This control is not in the visual control tree");

            Application.MessagePump.QueueAsyncAction(t, (tp) =>
            {
                if (Application != null)
                {
                    action(tp);
                }
            });
        }
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
            KeyInputReceived.Fire(info);
        }

        public virtual void OnKeyInputReceived(ConsoleKeyInfo info)
        {
          
        }

        public override string ToString()
        {
            return GetType().Name+" ("+Id+")";
        }
    }
}
