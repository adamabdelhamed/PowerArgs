using System;
using System.Collections.Generic;

namespace PowerArgs.Cli
{
    public abstract class Container : ConsoleControl
    {
        internal Container(): this(1,1) { }

        internal Container(int w, int h) : base(w,h) { }
        public abstract IEnumerable<ConsoleControl> Children { get; }

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
        /// Visits every control in the control tree, recursively, using the visit action provided
        /// </summary>
        /// <param name="visitAction">the visitor function that will be run for each child control, the function can return true if it wants to stop further visitation</param>
        /// <param name="root">set to null, used for recursion</param>
        /// <returns>true if the visitation was short ciruited by a visitor, false otherwise</returns>
        public bool VisitControlTree(Func<ConsoleControl, bool> visitAction, Container root = null)
        {
            bool shortCircuit = false;
            root = root ?? this;

            foreach (var child in root.Children)
            {
                shortCircuit = visitAction(child);
                if (shortCircuit) return true;

                if (child is Container)
                {
                    shortCircuit = VisitControlTree(visitAction, child as Container);
                    if (shortCircuit) return true;
                }
            }

            return false;
        }

        protected void Compose(ConsoleControl control)
        {
            if (control.IsVisible == false) return;
            control.Paint();

            foreach(var filter in control.RenderFilters)
            {
                filter.Control = control;
                filter.Filter(control.Bitmap);
            }

            if (control.CompositionMode == CompositionMode.PaintOver)
            {
                ComposePaintOver(control);
            }
            else if(control.CompositionMode == CompositionMode.BlendBackground)
            {
                ComposeBlendBackground(control);
            }
            else 
            {
                ComposeBlendVisible(control);
            }

        }

        protected virtual (int X, int Y) Transform(ConsoleControl c) => (c.X, c.Y);

        private void ComposePaintOver(ConsoleControl control)
        {
            var position = Transform(control);

            var minX = Math.Max(position.X, 0);
            var minY = Math.Max(position.Y, 0);
            var maxX = Math.Min(Width, position.X + control.Width);
            var maxY = Math.Min(Height, position.Y + control.Height);

            var myPixX = Bitmap.Pixels.AsSpan();
            for (var x = minX; x < maxX; x++)
            {
                var myPixY = myPixX[x].AsSpan();
                for (var y = minY; y < maxY; y++)
                {
                    myPixY[y] = control.Bitmap.Pixels[x - position.X][y - position.Y];
                }
            }
        }

        private void ComposeBlendBackground (ConsoleControl control)
        {
            var position = Transform(control);
            var minX = Math.Max(position.X, 0);
            var minY = Math.Max(position.Y, 0);
            var maxX = Math.Min(Width, position.X + control.Width);
            var maxY = Math.Min(Height, position.Y + control.Height);
            for (var x = minX; x < maxX; x++)
            {
                for (var y = minY; y < maxY; y++)
                {
                    var controlPixel = control.Bitmap.Pixels[x - position.X][y - position.Y];

                    if (controlPixel.BackgroundColor != ConsoleString.DefaultBackgroundColor)
                    {
                        Bitmap.Pixels[x][y] = controlPixel;
                    }
                    else
                    {
                        var myPixel = Bitmap.Pixels[x][y];
                        if (myPixel.BackgroundColor != ConsoleString.DefaultBackgroundColor)
                        {
                            var composedValue = new ConsoleCharacter(controlPixel.Value, controlPixel.ForegroundColor, myPixel.BackgroundColor);
                            Bitmap.Pixels[x][y] = composedValue;
                        }
                        else
                        {
                            Bitmap.Pixels[x][y] = controlPixel;
                        }
                    }
                }
            }
        }

        private bool IsVisibleOnMyPanel(in ConsoleCharacter pixel)
        {
            var c = pixel.Value;

            if(c == ' ')
            {
                return pixel.BackgroundColor != Background;
            }
            else
            {
                return pixel.ForegroundColor != Background || pixel.BackgroundColor != Background;
            }
        }

        private void ComposeBlendVisible(ConsoleControl control)
        {
            var position = Transform(control);
            var minX = Math.Max(position.X, 0);
            var minY = Math.Max(position.Y, 0);
            var maxX = Math.Min(Width, position.X + control.Width);
            var maxY = Math.Min(Height, position.Y + control.Height);
            for (var x = minX; x < maxX; x++)
            {
                for (var y = minY; y < maxY; y++)
                {
                    var controlPixel = control.Bitmap.Pixels[x - position.X][ y - position.Y];

                    var controlPixelHasRenderableContent = IsVisibleOnMyPanel(controlPixel);

 
                    if (controlPixelHasRenderableContent)
                    {
                        Bitmap.Pixels[x][y] = controlPixel;
                    }
                }
            }
        }
    }
}
