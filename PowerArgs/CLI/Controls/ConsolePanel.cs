using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A console control that has nested control within its bounds
    /// </summary>
    public class ConsolePanel : ConsoleControl
    {
        /// <summary>
        /// The nested controls
        /// </summary>
        public ObservableCollection<ConsoleControl> Controls { get; private set; }

        /// <summary>
        /// All nested controls, including those that are recursively nested within inner console panels
        /// </summary>
        public IReadOnlyCollection<ConsoleControl> Descendents
        {
            get
            {
                List<ConsoleControl> descendends = new List<ConsoleControl>();
                VisitControlTree((d) =>
                {
                    descendends.Add(d);
                    return false;
                });

                return descendends.AsReadOnly();
            }
        }

        /// <summary>
        /// Creates a new console panel
        /// </summary>
        public ConsolePanel()
        {
            Controls = new ObservableCollection<ConsoleControl>();
            Controls.Added.SubscribeForLifetime((c) => { c.Parent = this; }, this);
            Controls.AssignedToIndex.SubscribeForLifetime((assignment) => throw new NotSupportedException("Index assignment is not supported in Controls collection"), this);
            Controls.Removed.SubscribeForLifetime((c) => { c.Parent = null; }, this);
            this.CanFocus = false;
        }

        /// <summary>
        /// Adds a control to the panel
        /// </summary>
        /// <typeparam name="T">the type of controls being added</typeparam>
        /// <param name="c">the control to add</param>
        /// <returns>the control that was added</returns>
        public T Add<T>(T c) where T : ConsoleControl
        {
            Controls.Add(c);
            return c;
        }

        /// <summary>
        /// Visits every control in the control tree, recursively, using the visit action provided
        /// </summary>
        /// <param name="visitAction">the visitor function that will be run for each child control, the function can return true if it wants to stop further visitation</param>
        /// <param name="root">set to null, used for recursion</param>
        /// <returns>true if the visitation was short ciruited by a visitor, false otherwise</returns>
        public bool VisitControlTree(Func<ConsoleControl, bool> visitAction, ConsolePanel root = null)
        {
            bool shortCircuit = false;
            root = root ?? this;
            
            foreach(var child in root.Controls)
            {
                shortCircuit = visitAction(child);
                if (shortCircuit) return true;

                if(child is ConsolePanel)
                {
                    shortCircuit = VisitControlTree(visitAction, child as ConsolePanel);
                    if (shortCircuit) return true;
                }
            }

            return false;
        }

        private IEnumerable<ConsoleControl> GetPaintOrderedControls()
        {
            List<ConsoleControl> unordered = new List<ConsoleControl>();
            List<ConsoleControl> ordered = new List<ConsoleControl>();
            foreach (var control in Controls)
            {
                if(control.ZIndex <= 0)
                {
                    unordered.Add(control);
                }
                else
                {
                    ordered.Add(control);
                }
            }

            unordered.AddRange(ordered.OrderBy(c => c.ZIndex));
            return unordered;
        }

        /// <summary>
        /// Paints this control
        /// </summary>
        /// <param name="context">the drawing surface</param>
        protected override void OnPaint(ConsoleBitmap context)
        {
            foreach (var control in GetPaintOrderedControls())
            {
                Rectangle scope = context.Scope;
                try
                {
                    context.Rescope(control.X, control.Y, control.Width, control.Height);
                    if (control.Width > 0 && control.Height > 0)
                    {
                        control.Paint(context);
                    }
                }
                finally
                {
                    context.Scope = scope;
                }
            }
        }
    }

    /// <summary>
    /// A ConsolePanel that can prevent outside influences from
    /// adding to its Controls collection. You must use the internal
    /// Unlock method to add or remove controls.
    /// </summary>
    public class ProtectedConsolePanel : ConsolePanel
    {
        private int activeModifierCount;

        /// <summary>
        /// Gets or sets the exception message to use when an invalid add or remove is performed
        /// </summary>
        protected string ExceptionMessage { get; set; } = "You cannot add controls to this ConsolePanel";

        /// <summary>
        /// Creates a new ConsolePanel
        /// </summary>
        public ProtectedConsolePanel()
        {
            activeModifierCount = 0;
            Controls.Changed.SubscribeForLifetime(OnChanged, this);
        }

        private void OnChanged()
        {
            if(activeModifierCount == 0)
            {
                throw new InvalidOperationException(ExceptionMessage);
            }
        }

        /// <summary>
        /// Enables modification of the Controls connection until the given
        /// lifetime expires
        /// </summary>
        /// <returns>A lifetime that you should dispose when you want to disable controls modification</returns>
        protected Lifetime Unlock()
        {
            var ret = new Lifetime();
            activeModifierCount++;
            ret.OnDisposed(() => activeModifierCount--);
            return ret;
        }
    }
}
