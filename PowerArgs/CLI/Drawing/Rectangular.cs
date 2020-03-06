using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs.Cli
{
    public interface IRectangular
    {
        int X { get; set; }
        int Y { get; set; }
        int Width { get; set; }
        int Height { get; set; }

        Size Size { get; set; }
    }

    public class Rectangular : ObservableObject, IRectangular, IRectangularF
    {
        public Rectangle Bounds
        {
            get { return Get<Rectangle>(); }
            set
            {
                if(value.Width < 0 || value.Height < 0)
                {
                    throw new ArgumentOutOfRangeException("Width and height cannot be negative");
                }

                Set(value);
            }
        }

        public int Width
        {
            get
            {
                return Bounds.Width;
            }
            set
            {
                Bounds = new Rectangle(Bounds.Location, new Size(value, Bounds.Height));
            }
        }
        public int Height
        {
            get
            {
                return Bounds.Height;
            }
            set
            {
                Bounds = new Rectangle(Bounds.Location, new Size(Bounds.Width, value));
            }
        }
        public int X
        {
            get
            {
                return Bounds.X;
            }
            set
            {
                Bounds = new Rectangle(new Point(value, Bounds.Y), Bounds.Size);
            }
        }
        public int Y
        {
            get
            {
                return Bounds.Y;
            }
            set
            {
                Bounds = new Rectangle(new Point(Bounds.X, value), Bounds.Size);
            }
        }

        public Size Size
        {
            get
            {
                return Bounds.Size;
            }
            set
            {
                Bounds = new Rectangle(Bounds.Location, value);
            }
        }

        public Point Location
        {
            get
            {
                return Bounds.Location;
            }
            set
            {
                Bounds = new Rectangle(value, Bounds.Size);
            }
        }

        public float Left => X;

        public float Top => Y;

        float IRectangularF.Width => Width;


        float IRectangularF.Height => Height;

    }
}
