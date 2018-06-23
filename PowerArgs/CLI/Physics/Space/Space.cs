using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli.Physics
{
    public enum Direction
    {
        None,
        Left,
        Right,
        Up,
        Down,
        UpRight,
        UpLeft,
        DownRight,
        DownLeft,
    }

    public interface IRectangular : ISize
    {
        float Left { get; }
        float Top { get; }
        float Width { get; }
        float Height { get; }
    }

    public interface ISize
    {
        float Width { get; }
        float Height { get; }
    }

    public interface ILocation
    {
        float X { get; }
        float Y { get; }
    }

    public static class Location
    {
        private class LocationImpl : ILocation
        {
            public float X { get; internal set; }
            public float Y { get; internal set; }
        }

        public static ILocation Create(float x, float y) => new LocationImpl() { X = x, Y = y };
    }

    public static class Size
    {
        private class SizeImpl : ISize
        {
            public float Width { get; internal set; }
            public float Height { get; internal set; }
        }

        public static ISize Create(float w, float h) => new SizeImpl() { Width = w, Height = h };
    }

    public class Rectangular : IRectangular
    {
        public float Left { get; private set; }

        public float Top { get; private set; }

        public float Width { get; private set; }

        public float Height { get; private set; }

        private Rectangular(float x, float y, float w, float h)
        {
            this.Left = x;
            this.Top = y;
            this.Width = w;
            this.Height = h;
        }

        public static IRectangular Create(float x, float y, float w, float h) => new Rectangular(x, y, w, h);
        public static IRectangular Create(ILocation location, ISize size) => new Rectangular(location.X, location.Y, size.Width, size.Height);
    }

    public class Route
    {
        public List<ILocation> Steps { get; private set; } = new List<ILocation>();
        public List<IRectangular> Obstacles { get; private set; } = new List<IRectangular>();
    }

    public static class SpaceExtensions
    {
        public static float Right(this IRectangular rectangle) => rectangle.Left + rectangle.Width;
        public static float Bottom(this IRectangular rectangle) => rectangle.Top + rectangle.Height;

        public static float CenterX(this IRectangular rectangular) => rectangular.Left + (rectangular.Width / 2);
        public static float CenterY(this IRectangular rectangular) => rectangular.Top + (rectangular.Height / 2);

        public static ILocation Center(this IRectangular rectangular) => Location.Create(rectangular.CenterX(), rectangular.CenterY());

        public static IRectangular CopyBounds(this IRectangular rectangular) => Rectangular.Create(rectangular.Left, rectangular.Top, rectangular.Width, rectangular.Height);

        public static float GetOppositeAngle(float angle)
        {
            float ret;
            if (angle < 180)
            {
                ret = angle + 180;
            }
            else
            {
                ret = angle - 180;
            }

            if (ret == 360) ret = 0;

            return ret;
        }


        public static float NumberOfPixelsThatOverlap(this IRectangular rectangle, IRectangular other)
        {
            return
                Math.Max(0, Math.Min(rectangle.Right(), other.Right()) - Math.Max(rectangle.Left, other.Left)) *
                Math.Max(0, Math.Min(rectangle.Bottom(), other.Bottom()) - Math.Max(rectangle.Top, other.Top));
        }

        public static float OverlapPercentage(this IRectangular rectangle, IRectangular other) => NumberOfPixelsThatOverlap(rectangle, other) / (other.Width * other.Height);
        public static bool Contains(this IRectangular rectangle, IRectangular other) => OverlapPercentage(rectangle, other) == 1;
        public static bool Touches(this IRectangular rectangle, IRectangular other) => OverlapPercentage(rectangle, other) > 0;


        public static float CalculateDistanceTo(this IRectangular rectangular, IRectangular other) => rectangular.Center().CalculateDistanceTo(other.Center());
        

        public static float CalculateDistanceTo(this ILocation start, ILocation end)
        {
            return (float)Math.Sqrt(((start.X - end.X) * (start.X - end.X)) + ((start.Y - end.Y) * (start.Y - end.Y)));
        }

        public static Route CalculateLineOfSight(IRectangular from, ILocation to, float increment)
        {
            Route ret = new Route();
            IRectangular current = from;
            var dest = Rectangular.Create(to.X, to.Y, 0, 0);
            while (current.CalculateDistanceTo(dest) > increment)
            {
                current = Rectangular.Create(MoveTowards(current.Center(), to, increment), from);
                ret.Steps.Add(current.Center());

                var obstacles = SpaceTime.CurrentSpaceTime.Elements
                    .Where(el => el.NumberOfPixelsThatOverlap(current) > 0);

                foreach (var obstacle in obstacles)
                {
                    if (ret.Obstacles.Contains(obstacle) == false)
                    {
                        ret.Obstacles.Add(obstacle);
                    }
                }
            }

            return ret;
        }

        public static float CalculateAngleTo(this IRectangular from, IRectangular to) => CalculateAngleTo(from.Center(), to.Center());

        public static float CalculateAngleTo(this ILocation start, ILocation end)
        {
            float dx = end.X - start.X;
            float dy = end.Y - start.Y;
            float d = CalculateDistanceTo(start, end);

            if (dy == 0 && dx > 0) return 0;
            else if (dy == 0) return 180;
            else if (dx == 0 && dy > 0) return 90;
            else if (dx == 0) return 270;

            double radians, increment;
            if (dx >= 0 && dy >= 0)
            {
                // Sin(a) = dy / d
                radians = Math.Asin(dy / d);
                increment = 0;

            }
            else if (dx < 0 && dy > 0)
            {
                // Sin(a) = dx / d
                radians = Math.Asin(-dx / d);
                increment = 90;
            }
            else if (dy < 0 && dx < 0)
            {
                radians = Math.Asin(-dy / d);
                increment = 180;
            }
            else if (dx > 0 && dy < 0)
            {
                radians = Math.Asin(dx / d);
                increment = 270;
            }
            else
            {
                throw new Exception();
            }

            var ret = (float)(increment + radians * 180 / Math.PI);

            if (ret == 360) ret = 0;

            return ret;
        }

        public static Direction GetHitDirection(this IRectangular rectangle,  IRectangular other)
        {
            float rightProximity = Math.Abs(other.Left - (rectangle.Left + rectangle.Width));
            float leftProximity = Math.Abs(other.Left + other.Width - rectangle.Left);
            float topProximity = Math.Abs(other.Top + other.Height - rectangle.Top);
            float bottomProximity = Math.Abs(other.Top - (rectangle.Top + rectangle.Height));

            rightProximity = ((int)((rightProximity * 10) + .5f)) / 10f;
            leftProximity = ((int)((leftProximity * 10) + .5f)) / 10f;
            topProximity = ((int)((topProximity * 10) + .5f)) / 10f;
            bottomProximity = ((int)((bottomProximity * 10) + .5f)) / 10f;

            if (leftProximity == topProximity) return Direction.UpLeft;
            if (rightProximity == topProximity) return Direction.UpRight;
            if (leftProximity == bottomProximity) return Direction.DownLeft;
            if (rightProximity == bottomProximity) return Direction.DownRight;

            List<KeyValuePair<Direction, float>> items = new List<KeyValuePair<Direction, float>>();
            items.Add(new KeyValuePair<Direction, float>(Direction.Right, rightProximity));
            items.Add(new KeyValuePair<Direction, float>(Direction.Left, leftProximity));
            items.Add(new KeyValuePair<Direction, float>(Direction.Up, topProximity));
            items.Add(new KeyValuePair<Direction, float>(Direction.Down, bottomProximity));
            return items.OrderBy(item => item.Value).First().Key;
        }

        public static ILocation MoveTowards(this ILocation a, ILocation b, float distance)
        {
            float slope = (a.Y - b.Y) / (a.X - b.X);
            bool forward = a.X <= b.X;
            bool up = a.Y <= b.Y;

            float abDistance = a.CalculateDistanceTo(b);
            double angle = Math.Asin(Math.Abs(b.Y - a.Y) / abDistance);
            float dy = (float)Math.Abs(distance * Math.Sin(angle));
            float dx = (float)Math.Sqrt((distance * distance) - (dy * dy));

            float x2 = forward ? a.X + dx : a.X - dx;
            float y2 = up ? a.Y + dy : a.Y - dy;

            var ret = Location.Create(x2, y2);
            return ret;
        }

     
    }
}
