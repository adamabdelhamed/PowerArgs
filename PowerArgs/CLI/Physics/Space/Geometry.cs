using System;
using System.Linq;

namespace PowerArgs.Cli.Physics
{
    public enum Direction
    {
        None = -10000,
        Right = 345,
        RightDown = 15,
        DownRight = 45,
        Down = 75,
        DownLeft = 105,
        LeftDown = 135,
        Left = 165,
        LeftUp = 195,
        UpLeft = 225,
        Up = 255,
        UpRight = 285,
        RightUp = 315
    }

    public interface IRectangularF : ISizeF, ILocationF
    {
        float Left { get; }
        float Top { get; }
        float Width { get; }
        float Height { get; }
    }

    public interface ISizeF
    {
        float Width { get; }
        float Height { get; }
    }

    public interface ILocationF
    {
        float Left { get; }
        float Top { get; }
    }

    public static class LocationF
    {
        private class LocationImpl : ILocationF
        {
            public float Left { get; internal set; }
            public float Top { get; internal set; }

            public override bool Equals(object obj)
            {
                var other = obj as ILocationF;
                if (other == null) return false;
                return Left == other.Left && Top == other.Top;
            }
        }

        public static ILocationF Create(float x, float y) => new LocationImpl() { Left = x, Top = y };
    }

    public static class SizeF
    {
        private class SizeImpl : ISizeF
        {
            public float Width { get; internal set; }
            public float Height { get; internal set; }

            public override bool Equals(object obj)
            {
                var other = obj as ISizeF;
                if (other == null) return false;
                return Width == other.Width&& Height== other.Height;
            }
        }

        public static ISizeF Create(float w, float h) => new SizeImpl() { Width = w, Height = h };
    }

    public class RectangularF : IRectangularF
    {
        public float Left { get; private set; }

        public float Top { get; private set; }

        public float Width { get; private set; }

        public float Height { get; private set; }

        private RectangularF(float x, float y, float w, float h)
        {
            this.Left = x;
            this.Top = y;
            this.Width = w;
            this.Height = h;
        }

        public override bool Equals(object obj)
        {
            var other = obj as IRectangularF;
            if (other == null) return false;
            return Left == other.Left && Top == other.Top && Width == other.Width && Height == other.Height;
        }

        public override string ToString() => $"X={Left}, Y={Top}, W={Width}, H={Height}";

        public static IRectangularF Create(float x, float y, float w, float h) => new RectangularF(x, y, w, h);
        public static IRectangularF Create(ILocationF location, ISizeF size) => new RectangularF(location.Left, location.Top, size.Width, size.Height);
    }

    public static class Geometry
    {
        public static float Right(this IRectangularF rectangle) => rectangle.Left + rectangle.Width;
        public static float Bottom(this IRectangularF rectangle) => rectangle.Top + rectangle.Height;
        public static float CenterX(this IRectangularF rectangular) => rectangular.Left + (rectangular.Width / 2);
        public static float CenterY(this IRectangularF rectangular) => rectangular.Top + (rectangular.Height / 2);
        public static ILocationF Center(this IRectangularF rectangular) => LocationF.Create(rectangular.CenterX(), rectangular.CenterY());
        public static IRectangularF CenterRect(this IRectangularF rectangular) => RectangularF.Create(rectangular.CenterX(), rectangular.CenterY(), 0,0);
        public static ILocationF TopLeft(this IRectangularF rectangular) => LocationF.Create(rectangular.Left, rectangular.Top);
        public static IRectangularF CopyBounds(this IRectangularF rectangular) => RectangularF.Create(rectangular.Left, rectangular.Top, rectangular.Width, rectangular.Height);
        public static float Hypotenous(this IRectangularF rectangular) => (float)Math.Sqrt(rectangular.Width * rectangular.Width + rectangular.Height + rectangular.Height);
        public static float DiffAngle(this int a, float b) => DiffAngle((float)a, b);
        public static float AddToAngle(this int a, float b) => AddToAngle((float)a, b);
        public static float CalculateNormalizedDistanceTo(ILocationF a, ILocationF b) => NormalizeQuantity(a.CalculateDistanceTo(b), a.CalculateAngleTo(b));
        public static float CalculateNormalizedDistanceTo(IRectangularF a, IRectangularF b) => NormalizeQuantity(a.CalculateDistanceTo(b), a.CalculateAngleTo(b));
        public static float CalculateDistanceTo(this ILocationF start, ILocationF end) => (float)Math.Sqrt(((start.Left - end.Left) * (start.Left - end.Left)) + ((start.Top - end.Top) * (start.Top - end.Top)));
        public static Direction GetDirection(float a) => Enums.GetEnumValues<Direction>().OrderBy(slice => ((float)slice + 15).DiffAngle(a)).First();
        public static Direction GetHitDirection(this IRectangularF rectangle, IRectangularF other) => GetDirection(rectangle.CalculateAngleTo(other));
        public static float NormalizeQuantity(this int quantity, float angle, bool reverse = false) => NormalizeQuantity((float)quantity, angle, reverse);
        public static bool Contains(this IRectangularF rectangle, IRectangularF other) => OverlapPercentage(rectangle, other) == 1;
        public static bool Touches(this IRectangularF rectangle, IRectangularF other) => OverlapPercentage(rectangle, other) > 0;

        public static float GetOppositeAngle(this float angle)
        {
            float ret = angle < 180 ? angle + 180 : angle - 180;
            ret = ret == 360 ? 0 : ret;
            return ret;
        }

        public static IRectangularF Resize(this IRectangularF me, float ratio)
        {
            var newW = me.Width * ratio;
            var newH = me.Height * ratio;
            var leftAdjust = (me.Width - newW) / 2;
            var topAdjust = (me.Height - newH) / 2;
            var ret = RectangularF.Create(me.Left + leftAdjust, me.Top + topAdjust, newW, newH);
            return ret;
        }

        public static float DiffAngle(this float a, float b)
        {
            var c = Math.Abs(a - b);
            c = c <= 180 ? c : Math.Abs(360 - c); 
            return c;
        }

        public static float AddToAngle(this float angle, float toAdd)
        {
            var ret = angle + toAdd;
            ret = ret % 360;
            ret = ret >= 0 ? ret : ret + 360;
            return ret;
        }

        public static float CalculateAngleTo(this IRectangularF from, IRectangularF to) => CalculateAngleTo(from.Center(), to.Center());
        public static float CalculateAngleTo(this ILocationF start, ILocationF end)
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


        public static float CalculateDistanceTo(this IRectangularF a, IRectangularF b)
        {
            var left = b.Right() < a.Left;
            var right = a.Right() < b.Left;
            var bottom = b.Bottom() < a.Top;
            var top = a.Bottom() < b.Top;
            if (top && left)
                return LocationF.Create(a.Left, a.Bottom()).CalculateDistanceTo(LocationF.Create(b.Right(), b.Top));
            else if (left && bottom)
                return LocationF.Create(a.Left, a.Top).CalculateDistanceTo(LocationF.Create(b.Right(), b.Bottom()));
            else if (bottom && right)
                return LocationF.Create(a.Right(), a.Top).CalculateDistanceTo(LocationF.Create(b.Left, b.Bottom()));
            else if (right && top)
                return LocationF.Create(a.Right(), a.Bottom()).CalculateDistanceTo(LocationF.Create(b.Left, b.Top));
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

        public static float NumberOfPixelsThatOverlap(this IRectangularF rectangle, IRectangularF other)
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

        public static float OverlapPercentage(this IRectangularF rectangle, IRectangularF other)
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
        public static float NormalizeQuantity(this float quantity, float angle, bool reverse = false)
        {
            float degreesFromFlat;
            if (angle <= 180)
            {
                degreesFromFlat = Math.Min(180 - angle, angle);
            }
            else
            {
                degreesFromFlat = Math.Min(angle - 180, 360 - angle);
            }

            var skewPercentage = 1 + (degreesFromFlat / 90);

            return reverse ? quantity * skewPercentage : quantity / skewPercentage;
        }
    }
}
