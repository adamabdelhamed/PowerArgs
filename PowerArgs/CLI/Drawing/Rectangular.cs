using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs.Cli
{

    public class Rectangular : ObservableObject, ICollider
    {
        public RectF ToRectF() => new RectF(x, y, w, h);

        private int x, y, w, h;

  
        public RectF Bounds
        {
            get { return ToRectF(); }
            set
            {
                x = (int)value.Left;
                y= (int)value.Top;
                w = (int)value.Width;
                h = (int)value.Height;
                FirePropertyChanged(nameof(Bounds));
            }
        }

        public int Width
        {
            get
            {
                return w;
            }
            set
            {
                if (w == value) return;
                w = value;
                FirePropertyChanged(nameof(Bounds));
            }
        }
        public int Height
        {
            get
            {
                return h;
            }
            set
            {
                if (h == value) return;
                h = value;
                FirePropertyChanged(nameof(Bounds));
            }
        }
        public int X
        {
            get
            {
                return x;
            }
            set
            {
                if (x == value) return;
                x = value;
                FirePropertyChanged(nameof(Bounds));
            }
        }
        public int Y
        {
            get
            {
                return y;
            }
            set
            {
                if (y == value) return;
                y = value;
                FirePropertyChanged(nameof(Bounds));
            }
        }

        public float Left => X;

        public float Top => Y;

        RectF ICollider.Bounds => new RectF(X,Y,Width, Height);

        public RectF MassBounds => new RectF(X, Y, Width, Height);
    }
}
