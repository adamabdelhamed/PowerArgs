using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs.Cli
{
    public struct Rectangle
    {
        public Size Size{get;set;}
        public Point Location{get;set;}

        public int Width
        {
            get
            {
                return Size.Width;
            }
            set
            {
                Size = new Size(value, Size.Height);  
            }
        }
        public int Height
        {
            get
            {
                return Size.Height;
            }
            set
            {
                Size = new Size(Size.Width, value);
            }
        }
        public int X
        {
            get
            {
                return Location.X;
            }
            set
            {
                Location = new Point(value, Location.Y);
            }
        }
        public int Y
        {
            get
            {
                return Location.Y;
            }
            set
            {
                Location = new Point(Location.X, value);
            }
        }

        public int Bottom
        {
            get
            {
                return Y + Height;
            }
        }

        public int Right
        {
            get
            {
                return X + Width;
            }
        }

        public int Top
        {
            get
            {
                return Y;
            }
        }

        public int Left
        {
            get
            {
                return X;
            }
        }

        public Rectangle(int x, int y, int w, int h) : this()
        {
            Location = new Point(x, y);
            Size = new Size(w, h);
        }

        public Rectangle(Point location, Size size) : this()
        {
            this.Location = location;
            this.Size = size;
        }

        public bool Contains(int x, int y)
        {
            if (x < X || x >= X + Width)
            {
                return false;
            }
            if (y < Y || y >= Y + Height)
            {
                return false;
            }
            return true;
        }

        public bool Contains(Rectangle other)
        {
            var insideLeftEdge = other.Left >= Left;
            var insideRightEdge = other.Right <= Right;

            var insideTopEdge = other.Top >= Top;
            var insideBottomEdge = other.Bottom <= Bottom;

            return insideLeftEdge && insideRightEdge && insideTopEdge && insideBottomEdge;
        }

        public bool IsAbove(Rectangle other)
        {
            return Top < other.Top;
        }

        public bool IsBelow(Rectangle other)
        {
            return Bottom > other.Bottom;
        }

        public bool IsLeftOf(Rectangle other)
        {
            return Left < other.Left;
        }

        public bool IsRightOf(Rectangle other)
        {
            return Right > other.Right;
        }
    }
}
