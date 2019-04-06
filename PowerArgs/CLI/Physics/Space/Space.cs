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
        float Left { get; }
        float Top { get; }
    }

    public static class Location
    {
        private class LocationImpl : ILocation
        {
            public float Left { get; internal set; }
            public float Top { get; internal set; }
        }

        public static ILocation Create(float x, float y) => new LocationImpl() { Left = x, Top = y };
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

        public override bool Equals(object obj)
        {
            var other = obj as IRectangular;
            if (other == null) return false;
            return Left == other.Left && Top == other.Top && Width == other.Width && Height == other.Height;
        }

        public override string ToString() => $"X={Left}, Y={Top}, W={Width}, H={Height}";

        public static IRectangular Create(float x, float y, float w, float h) => new Rectangular(x, y, w, h);
        public static IRectangular Create(ILocation location, ISize size) => new Rectangular(location.Left, location.Top, size.Width, size.Height);
    }

    public class Route
    {
        public List<IRectangular> Steps { get; private set; } = new List<IRectangular>();
        public List<IRectangular> Obstacles { get; private set; } = new List<IRectangular>();
    }

    public static class SpaceExtensions
    {
        public static float Right(this IRectangular rectangle) => rectangle.Left + rectangle.Width;
        public static float Bottom(this IRectangular rectangle) => rectangle.Top + rectangle.Height;

        public static float CenterX(this IRectangular rectangular) => rectangular.Left + (rectangular.Width / 2);
        public static float CenterY(this IRectangular rectangular) => rectangular.Top + (rectangular.Height / 2);

        public static ILocation Center(this IRectangular rectangular) => Location.Create(rectangular.CenterX(), rectangular.CenterY());

        public static ILocation TopLeft(this IRectangular rectangular) => Location.Create(rectangular.Left, rectangular.Top);

        public static IRectangular CopyBounds(this IRectangular rectangular) => Rectangular.Create(rectangular.Left, rectangular.Top, rectangular.Width, rectangular.Height);

        public static float Hypotenous(this IRectangular rectangular)
        {
            return (float)Math.Sqrt(rectangular.Width * rectangular.Width + rectangular.Height + rectangular.Height);
        }

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

        public static IRectangular Resize(this IRectangular me, float ratio)
        {
            var newW = me.Width * ratio;
            var newH = me.Height * ratio;

            var leftAdjust = (me.Width - newW) / 2;
            var topAdjust = (me.Height - newH) / 2;

            var ret = Rectangular.Create(me.Left + leftAdjust, me.Top + topAdjust, newW, newH);
            return ret;
        }

        public static float NumberOfPixelsThatOverlap(this IRectangular rectangle, IRectangular other)
        {
            var rectangleRight = rectangle.Right();
            var otherRight = other.Right();

            var rectangleBottom = rectangle.Bottom();
            var otherBottom = other.Bottom();

            var ret = 
                Math.Max(0, Math.Min(rectangleRight, otherRight) - Math.Max(rectangle.Left, other.Left)) *
                Math.Max(0, Math.Min(rectangleBottom, otherBottom) - Math.Max(rectangle.Top, other.Top));

            ret = (float)Math.Round(ret, 4);
            return ret;
        }

        public static float OverlapPercentage(this IRectangular rectangle, IRectangular other)
        {
            var numerator = NumberOfPixelsThatOverlap(rectangle, other);
            var denominator = other.Width * other.Height;

            if (numerator == 0) return 0;
            else if (numerator == denominator) return 1;

            var amount = numerator / denominator;
            if (amount < 0) amount = 0;
            else if (amount > 1) amount = 1;
            return amount;
        }
        public static bool Contains(this IRectangular rectangle, IRectangular other) => OverlapPercentage(rectangle, other) == 1;
        public static bool Touches(this IRectangular rectangle, IRectangular other) => OverlapPercentage(rectangle, other) > 0;


        public static float CalculateDistanceTo(this IRectangular a, IRectangular b)
        {
            return NormalizeQuantity(CalculateDistanceToRaw(a, b), a.CalculateAngleTo(b));
        }

        private static float CalculateDistanceToRaw(this IRectangular a, IRectangular b)
        {
            var left = b.Right() < a.Left;
            var right = a.Right() < b.Left;
            var bottom = b.Bottom() < a.Top;
            var top = a.Bottom() < b.Top;
            if (top && left)
                return Location.Create(a.Left, a.Bottom()).CalculateDistanceTo(Location.Create(b.Right(), b.Top));
            else if (left && bottom)
                return Location.Create(a.Left, a.Top).CalculateDistanceTo(Location.Create(b.Right(), b.Bottom()));
            else if (bottom && right)
                return Location.Create(a.Right(), a.Top).CalculateDistanceTo(Location.Create(b.Left, b.Bottom()));
            else if (right && top)
                return Location.Create(a.Right(), a.Bottom()).CalculateDistanceTo(Location.Create(b.Left, b.Top));
            else if (left)
                return a.Left - b.Right();
            else if (right)
                return b.Left - a.Right();
            else if (bottom)
                return a.Top - b.Bottom();
            else if (top)
                return b.Top - a.Bottom();
            else
                return 0;
        }
        

        public static float CalculateDistanceTo(this ILocation start, ILocation end)
        {
            return (float)Math.Sqrt(((start.Left - end.Left) * (start.Left - end.Left)) + ((start.Top - end.Top) * (start.Top - end.Top)));
        }

        public static bool HasLineOfSight(this IRectangular from, IRectangular to, List<IRectangular> obstacles, float increment)
        {
            Route ret = new Route();
            IRectangular current = from;
            var currentDistance = current.CalculateDistanceTo(to);
            var firstDistance = currentDistance;
            while (currentDistance > increment)
            {
                current = Rectangular.Create(SpaceExtensions.MoveTowards(current.Center(), to.Center(), 1), current);
                current = Rectangular.Create(current.Left - current.Width / 2, current.Top - current.Height / 2, current.Width, current.Height);

                foreach (var obstacle in obstacles)
                {
                    if(obstacle == to || obstacle == from)
                    {
                        continue;
                    }
                    else if (obstacle.OverlapPercentage(current) > 0)
                    {
                        return false;
                    }
                }

                currentDistance = current.CalculateDistanceTo(to);
            }

            return true;
        }

        public static float CalculateAngleTo(this IRectangular from, IRectangular to) => CalculateAngleTo(from.Center(), to.Center());

        public static float CalculateAngleTo(this ILocation start, ILocation end)
        {
            float dx = end.Left - start.Left;
            float dy = end.Top - start.Top;
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
            float slope = (a.Top - b.Top) / (a.Left - b.Left);
            bool forward = a.Left <= b.Left;
            bool up = a.Top <= b.Top;

            float abDistance = a.CalculateDistanceTo(b);
            double angle = Math.Asin(Math.Abs(b.Top - a.Top) / abDistance);
            float dy = (float)Math.Abs(distance * Math.Sin(angle));
            float dx = (float)Math.Sqrt((distance * distance) - (dy * dy));

            float x2 = forward ? a.Left + dx : a.Left - dx;
            float y2 = up ? a.Top + dy : a.Top - dy;

            var ret = Location.Create(x2, y2);
            return ret;
        }

        public static ILocation MoveTowards(this ILocation a, float angle, float distance)
        {

            var forward = !(angle > 270 || angle < 90);
            var up = angle > 180;

            // convert to radians
            angle = (float)(angle * Math.PI / 180);
            float dy = (float)Math.Abs(distance * Math.Sin(angle));
            float dx = (float)Math.Sqrt((distance * distance) - (dy * dy));


            float x2 = forward ? a.Left + dx : a.Left - dx;
            float y2 = up ? a.Top + dy : a.Top - dy;

            var ret = Location.Create(x2, y2);
            return ret;
        }

        public static IEnumerable<SpacialElement> GetObstacles(this SpacialElement element, IEnumerable<SpacialElement> exclusions = null) => SpaceTime.CurrentSpaceTime
                   .Elements
                   .Where(e => e == element == false &&
                           (exclusions == null ||  exclusions.Contains(e) == false) &&
                           e.ZIndex == element.ZIndex &&
                           e.HasSimpleTag("passthru") == false);
        

        /// <summary>
        /// In most consoles the recrtangles allocated to characters are about twice as tall as they
        /// are wide. Since we want to treat the console like a uniform grid we'll have to account for that.
        /// 
        /// This method takes in some quantity and an angle and normalizes it so that if the angle were flat (e.g. 0 or 180)
        /// then you'll get back the same quantity you gave in. If the angle is vertical (e.g. 90 or 270) then you will get back
        /// a quantity that is only half of what you gave. The degree to which we normalize the quantity is linear.
        /// </summary>
        /// <param name="quantity">The quantity to normalize</param>
        /// <param name="angle">the angle to use to adjust the quantity</param>
        /// <param name="reverse">if true, grows the quantity instead of shrinking it. This is useful for angle quantities.</param>
        /// <returns></returns>
        public static float NormalizeQuantity(float quantity, float angle, bool reverse = false)
        {
            float degreesFromFlat;
            if (angle <= 180)
            {
                degreesFromFlat = Math.Min(180 - angle, angle);
            }
            else
            {
                degreesFromFlat = Math.Min(angle - 180, 360-angle);
            }

            var skewPercentage = 1+(degreesFromFlat / 90);

            return reverse ? quantity * skewPercentage :  quantity / skewPercentage;
        }

        public static float AddToAngle(float angle, float toAdd)
        {
            var ret = angle + toAdd;
            ret = ret % 360;
            if(ret < 0)
            {
                ret += 360;
            }
            return ret;
        }

        public static bool TryNudgeFreeOFObstacles(this SpacialElement element, List<IRectangular> obstacles, float range = 3.5f)
        {
            if (obstacles.Where(o => o.Touches(element)).Any() == false)
            {
                return true;
            }

            for (var x = element.Left - range/2; x < element.Left + range / 2; x++)
            {
                for (var y = element.Top - range / 2; y < element.Top + range / 2; y++)
                {
                    var testArea = Rectangular.Create(x, y, element.Width * 1.1f, element.Height * 1.1f);
                    if(obstacles.Where(o => o.Touches(testArea)).Any())
                    {
                        continue;
                    }
                    else
                    {
                        element.MoveTo(x, y);
                        return true;
                    }
                }
            }
            return false;
        }
    }

     
}
