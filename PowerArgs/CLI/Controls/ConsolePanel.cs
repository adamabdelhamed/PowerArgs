using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli
{
    public interface IConsolePanel : IConsoleControl
    {
        IEnumerable<ConsoleControl> Descendents { get; }
        IEnumerable<ConsoleControl> Children { get; }
    }

    public enum CompositionMode
    {
        PaintOver = 0,
        Blend = 1,
    }


    /// <summary>
    /// A console control that has nested control within its bounds
    /// </summary>
    public class ConsolePanel : ConsoleControl, IConsolePanel
    {
        /// <summary>
        /// The nested controls
        /// </summary>
        public ObservableCollection<ConsoleControl> Controls { get; private set; }

        public IEnumerable<ConsoleControl> Children => Controls;

        /// <summary>
        /// All nested controls, including those that are recursively nested within inner console panels
        /// </summary>
        public IEnumerable<ConsoleControl> Descendents
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
                if (control.Width > 0 && control.Height > 0 && control.IsVisible)
                {
                    control.Paint();
                    Compose(control);
                }
            }
        }

        private void Compose(ConsoleControl control)
        {
            if(control.CompositionMode == CompositionMode.PaintOver)
            {
                ComposePaintOver(control);
                return;
            }

            var maxX = control.X + control.Width;
            var maxY = control.Y + control.Height;
            for (var x = control.X; x < maxX;x++)
            {
                for(var y = control.Y; y < maxY; y++)
                {
                    if (Bitmap.IsInBounds(x,y) == false)
                    {
                        continue;
                    }
                    var controlPixel = control.Bitmap.GetPixel(x - control.X, y - control.Y).Value;

                    if (controlPixel?.BackgroundColor != ConsoleString.DefaultBackgroundColor)
                    {
                        Bitmap.DrawPoint(controlPixel.Value, x, y);
                    }
                    else
                    {
                        var myPixel = Bitmap.GetPixel(x, y).Value;
                        if (myPixel.HasValue && myPixel.Value.BackgroundColor != ConsoleString.DefaultBackgroundColor)
                        {
                            var composedValue = new ConsoleCharacter(controlPixel.Value.Value, controlPixel.Value.ForegroundColor, myPixel.Value.BackgroundColor);
                            Bitmap.DrawPoint(composedValue, x, y);
                        }
                        else
                        {
                            Bitmap.DrawPoint(controlPixel.Value, x, y);
                        }
                    }
                }
            }
        }

        private void ComposePaintOver(ConsoleControl control)
        {
            var maxX = control.X + control.Width;
            var maxY = control.Y + control.Height;
            for (var x = control.X; x < maxX; x++)
            {
                for (var y = control.Y; y < maxY; y++)
                {

                    var controlPixel = control.Bitmap.GetPixel(x - control.X, y - control.Y).Value;
                    Bitmap.DrawPoint(controlPixel.Value, x, y);
                }
            }
        }
    }

    /// <summary>
    /// A ConsolePanel that can prevent outside influences from
    /// adding to its Controls collection. You must use the internal
    /// Unlock method to add or remove controls.
    /// </summary>
    public class ProtectedConsolePanel : ConsoleControl, IConsolePanel
    {
        protected ConsolePanel ProtectedPanel { get; private set; }

        internal ConsolePanel ProtectedPanelInternal => ProtectedPanel;

        public IEnumerable<ConsoleControl> Children
        {
            get
            {
                yield return ProtectedPanel;
            }
        }

        public IEnumerable<ConsoleControl> Descendents

        {
            get
            {
                yield return ProtectedPanel;
                foreach(var d in ProtectedPanel.Descendents)
                {
                    yield return d;
                }
            }
        }
        /// <summary>
        /// Gets or sets the exception message to use when an invalid add or remove is performed
        /// </summary>
        protected string ExceptionMessage { get; set; } = "You cannot add controls to this ConsolePanel";

        /// <summary>
        /// Creates a new ConsolePanel
        /// </summary>
        public ProtectedConsolePanel()
        {
            this.CanFocus = false;
            ProtectedPanel = new ConsolePanel();
            ProtectedPanel.Parent = this;
            ProtectedPanel.Fill();
        }

        protected override void OnPaint(ConsoleBitmap context)
        {
            ProtectedPanel.Paint();
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    var controlPixel = ProtectedPanel.Bitmap.GetPixel(x, y).Value;
                    Bitmap.DrawPoint(controlPixel.Value, x, y);
                }
            }
        }
    }
}
