using System.Diagnostics.CodeAnalysis;

namespace PowerArgs.Cli.Physics;

[ArgReviverType]
public readonly struct Angle
{
    public static readonly Angle Left = 180f;
    public static readonly Angle Up = 270f;
    public static readonly Angle Right = 0f;
    public static readonly Angle Down = 90f;

    public static readonly Angle UpLeft = (Up.Value + Left.Value) / 2;
    public static readonly Angle UpRight = (Up.Value + 360) / 2;
    public static readonly Angle DownRight = (Down.Value + Right.Value) / 2;
    public static readonly Angle DownLeft = (Down.Value + Left.Value) / 2;

    public readonly float Value;

    public Angle(float val)
    {
        Value = val % 360f;
    }
  
    public override string ToString() => $"{Value} degrees";
    public bool Equals(Angle other) => Value == other.Value;
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Angle && Equals((Angle)obj);
    public static bool operator ==(Angle a, Angle b) => a.Equals(b);
    public static bool operator !=(Angle a, Angle b) => a.Equals(b) == false;
    public override int GetHashCode() => Value.GetHashCode();

    public Angle Add(float other)
    {
        var ret = Value + other;
        ret = ret % 360;
        ret = ret >= 0 ? ret : ret + 360;
        if (ret == 360) return new Angle(0);
        return new Angle(ret);
    }

    public Angle Add(Angle other) => Add(other.Value);

    public Angle Opposite()
    {
        float ret = Value < 180 ? Value + 180 : Value - 180;
        ret = ret == 360 ? 0 : ret;
        return new Angle(ret);
    }

    public Angle DiffShortest(Angle other)
    {
        var c = Math.Abs(Value - other.Value);
        c = c <= 180 ? c : Math.Abs(360 - c);
        if (c == 360) return new Angle(0);
        return new Angle(c);
    }

    public Angle DiffClockwise(Angle other)
    {
        var diff = DiffShortest(other);
        var clock = Add(diff); // 1
        return clock == other ? diff : new Angle(360f - diff.Value);
    }

    public Angle DiffCounterClockwise(Angle other)
    {
        var diff = DiffShortest(other);
        var clock = Add(diff);
        return clock == other ? new Angle(360f - diff.Value) : diff;
    }

    public Angle DiffRelative(Angle other) => new Angle(this.Value - other.Value);

    public Angle RoundAngleToNearest(Angle nearest) => new Angle(((float)ConsoleMath.Round(Value / nearest.Value) * nearest.Value) % 360);
    public bool IsClockwiseShortestPathToAngle(Angle other) => Add(DiffShortest(other)) == other;

    /// <summary>
    /// Finds the angle that is between these two angles
    /// </summary>
    /// <param name="from">the starting angle</param>
    /// <param name="to">the ending angle</param>
    /// <returns>the angle that is between these two angles</returns>
    public Angle Bisect(Angle to)
    {
        var max = Math.Max(Value, to.Value);
        var min = Math.Min(Value, to.Value);
        var range = max - min;
        if (range > 180)
        {
            min += 360;
        }
        var ret = (max + min) / 2;
        ret = ret % 360;
        return ret;
    }

    public string ArrowString => "" + Arrow;

    public char Arrow
    {
        get
        {
            if (Value >= 315 || Value < 45)
            {
                return '>';
            }
            else if (Value >= 45 && Value < 135)
            {
                return 'v';
            }
            else if (Value >= 135 && Value < 225)
            {
                return '<';
            }
            else
            {
                return '^';
            }
        }
    }

    public static float ToRadians(Angle degrees) => (float)(Math.PI * degrees.Value / 180.0);
    public static Angle ToDegrees(float radians) => new Angle((float)(radians * (180.0 / Math.PI)) % 360);

    public static implicit operator Angle(float a) => new Angle(a);

    [ArgReviver]
    public static Angle Revive(string key, string val)
    {
        if(float.TryParse(val, out float f))
        {
            if (f < 0 || f >= 360) throw new ValidationArgException($"Angles must be >=0 and < 360, given: {val}");
            return new Angle(f);
        }
        else if("Up".Equals(val, StringComparison.OrdinalIgnoreCase))
        {
            return Up;
        }
        else if ("Down".Equals(val, StringComparison.OrdinalIgnoreCase))
        {
            return Down;
        }
        else if ("Left".Equals(val, StringComparison.OrdinalIgnoreCase))
        {
            return Left;
        }
        else if ("Right".Equals(val, StringComparison.OrdinalIgnoreCase))
        {
            return Right;
        }
        else
        {
            throw new ValidationArgException($"Cannot parse angle: {val}");
        }
    }

}

public readonly struct Edge
{
    public readonly float X1;
    public readonly float Y1;

    public readonly float X2;
    public readonly float Y2;

    public Edge()
    {
        X1 = default;
        Y1 = default;
        X2 = default;
        Y2 = default;
    }

    public Edge(float x1, float y1, float x2, float y2)
    {
        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
    }

    public override string ToString() => $"{X1},{Y1} => {X2},{Y2}";
    public bool Equals(Edge other) => X1 == other.X1 && X2 == other.X2 && Y1 == other.Y1 && Y2 == other.Y2;
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is RectF && Equals((Edge)obj);
    public static bool operator ==(Edge a, Edge b) => a.Equals(b);
    public static bool operator !=(Edge a, Edge b) => a.Equals(b) == false;
    public override int GetHashCode()
    {
        unchecked // Overflow is fine, just wrap
        {
            int hash = 17;
            hash = hash * 23 + X1.GetHashCode();
            hash = hash * 23 + X2.GetHashCode();
            hash = hash * 23 + Y1.GetHashCode();
            hash = hash * 23 + Y2.GetHashCode();
            return hash;
        }
    }
}

public interface ICollider
{
    public RectF Bounds { get; }
    public RectF MassBounds { get; }
}


public static class IColliderEx
{
    public static float NumberOfPixelsThatOverlap(this ICollider c, RectF other) => c.Bounds.NumberOfPixelsThatOverlap(other);
    public static float NumberOfPixelsThatOverlap(this ICollider c, ICollider other) => c.Bounds.NumberOfPixelsThatOverlap(other.Bounds);

    public static float OverlapPercentage(this ICollider c, RectF other) => c.Bounds.OverlapPercentage(other);
    public static float OverlapPercentage(this ICollider c, ICollider other) => c.Bounds.OverlapPercentage(other.Bounds);

    public static bool Touches(this ICollider c, RectF other) => c.Bounds.Touches(other);
    public static bool Touches(this ICollider c, ICollider other) => c.Bounds.Touches(other.Bounds);

    public static bool Contains(this ICollider c, RectF other) => c.Bounds.Contains(other);
    public static bool Contains(this ICollider c, ICollider other) => c.Bounds.Contains(other.Bounds);

    public static float Top(this ICollider c) => c.Bounds.Top;
    public static float Left(this ICollider c) => c.Bounds.Left;

    public static float Bottom(this ICollider c) => c.Bounds.Bottom;
    public static float Right(this ICollider c) => c.Bounds.Right;

    public static float Width(this ICollider c) => c.Bounds.Width;
    public static float Height(this ICollider c) => c.Bounds.Height;

    public static LocF TopRight(this ICollider c) => c.Bounds.TopRight;
    public static LocF BottomRight(this ICollider c) => c.Bounds.BottomRight;
    public static LocF TopLeft(this ICollider c) => c.Bounds.TopLeft;
    public static LocF BottomLeft(this ICollider c) => c.Bounds.BottomLeft;

    public static LocF Center(this ICollider c) => c.Bounds.Center;
    public static float CenterX(this ICollider c) => c.Bounds.CenterX;
    public static float CenterY(this ICollider c) => c.Bounds.CenterY;

    public static RectF Round(this ICollider c) => c.Bounds.Round();

    public static RectF OffsetByAngleAndDistance(this ICollider c, Angle a, float d, bool normalized= true) => c.Bounds.OffsetByAngleAndDistance(a, d, normalized);
    public static RectF Offset(this ICollider c, float dx, float dy) => c.Bounds.Offset(dx, dy);

    public static Angle CalculateAngleTo(this ICollider c, RectF other) => c.Bounds.CalculateAngleTo(other);
    public static Angle CalculateAngleTo(this ICollider c, ICollider other) => c.Bounds.CalculateAngleTo(other.Bounds);

    public static float CalculateDistanceTo(this ICollider c, RectF other) => c.Bounds.CalculateDistanceTo(other);
    public static float CalculateDistanceTo(this ICollider c, ICollider other) => c.Bounds.CalculateDistanceTo(other.Bounds);

    public static float CalculateNormalizedDistanceTo(this ICollider c, RectF other) => c.Bounds.CalculateNormalizedDistanceTo(other);
    public static float CalculateNormalizedDistanceTo(this ICollider c, ICollider other) => c.Bounds.CalculateNormalizedDistanceTo(other.Bounds);
}

public class ColliderBox : ICollider
{
    public RectF Bounds{ get; private set; }
    public RectF MassBounds { get; private set; }
    public ColliderBox(RectF f)
    {
        Bounds = f;
        MassBounds = f;
    }
}

public readonly struct RectF
{
    public readonly float Left;
    public readonly float Top;
    public readonly float Width;
    public readonly float Height;

    public float Right => Left + Width;
    public float Bottom => Top + Height;

    public float CenterX => Left + Width / 2;
    public float CenterY => Top + Height / 2;
    public LocF Center => new LocF(CenterX, CenterY);
    public LocF TopLeft => new LocF(Left, Top);
    public LocF TopRight => new LocF(Right, Top);
    public LocF BottomLeft => new LocF(Left, Bottom);
    public LocF BottomRight => new LocF(Right, Bottom);

    public Edge LeftEdge => new Edge(Left, Top, Left, Bottom);
    public Edge RightEdge => new Edge(Right, Top, Right, Bottom);
    public Edge TopEdge => new Edge(Left, Top, Right, Top);
    public Edge BottomEdge => new Edge(Left, Bottom, Right, Bottom);

    public float Hypotenous => (float)Math.Sqrt(Width * Width + Height * Height);

    public ICollider Box() => new ColliderBox(this);

    public RectF(float x, float y, float w, float h)
    {
        this.Left = x;
        this.Top = y;
        this.Width = w;
        this.Height = h;
    }



    public override string ToString() => $"{Left},{Top} {Width}x{Height}";
    public bool Equals(in RectF other) => Left == other.Left && Top == other.Top && Width == other.Width && Height == other.Height;
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is RectF && Equals((RectF)obj);
    public static bool operator ==(RectF a, RectF b) => a.Equals(b);
    public static bool operator !=(RectF a, RectF b) => a.Equals(b) == false;

    public override int GetHashCode()
    {
        unchecked // Overflow is fine, just wrap
        {
            int hash = 17;
            hash = hash * 23 + Left.GetHashCode();
            hash = hash * 23 + Top.GetHashCode();
            hash = hash * 23 + Width.GetHashCode();
            hash = hash * 23 + Height.GetHashCode();
            return hash;
        }
    }

    public RectF Offset(float dx, float dy) => Offset(Left, Top, Width, Height, dx, dy);

    public RectF OffsetByAngleAndDistance(Angle angle, float distance, bool normalized = true) =>
        OffsetByAngleAndDistance(Left, Top, Width, Height, angle, distance, normalized);

    public RectF Round() => new RectF(ConsoleMath.Round(Left), ConsoleMath.Round(Top), ConsoleMath.Round(Width), ConsoleMath.Round(Height));


    public RectF Grow(float percentage)
    {
        var center = Center;
        var newW = Width * (1 + percentage);
        var newH = Height * (1 + percentage);
        return new RectF(center.Left - newW / 2, center.Top - newH / 2, newW, newH);
    }

    public RectF Shrink(float percentage)
    {
        var center = Center;
        var newW = Width * (1 - percentage);
        var newH = Height * (1 - percentage);
        return new RectF(center.Left - newW / 2, center.Top - newH / 2, newW, newH);
    }

    public Angle CalculateAngleTo(ICollider other) => CalculateAngleTo(Left, Top, Width, Height, other.Left(), other.Top(), other.Width(), other.Height());
    public Angle CalculateAngleTo(RectF other) => CalculateAngleTo(Left, Top, Width, Height, other.Left, other.Top, other.Width, other.Height);
    public Angle CalculateAngleTo(float bx, float by, float bw, float bh) => CalculateAngleTo(Left, Top, Width, Height, bx, by, bw, bh);


    public float CalculateDistanceTo(ICollider other) => CalculateDistanceTo(Left, Top, Width, Height, other.Left(), other.Top(), other.Width(), other.Height());
    public float CalculateDistanceTo(RectF other) => CalculateDistanceTo(this, other);
    public float CalculateDistanceTo(float bx, float by, float bw, float bh) => CalculateDistanceTo(Left, Top, Width, Height, bx, by, bw, bh);
    public float CalculateNormalizedDistanceTo(RectF other) => CalculateNormalizedDistanceTo(Left, Top, Width, Height, other.Left, other.Top, other.Width, other.Height);
    public float CalculateNormalizedDistanceTo(float bx, float by, float bw, float bh) => CalculateNormalizedDistanceTo(Left, Top, Width, Height, bx, by, bw, bh);

    public static Angle CalculateAngleTo(float ax, float ay, float aw, float ah, float bx, float by, float bw, float bh)
    {
        var aCenterX = ax + (aw / 2);
        var aCenterY = ay + (ah / 2);

        var bCenterX = bx + (bw / 2);
        var bCenterY = by + (bh / 2);
        return LocF.CalculateAngleTo(aCenterX, aCenterY, bCenterX, bCenterY);
    }

    public static Angle CalculateAngleTo(in RectF a, in RectF b)
    {
        var aCenterX = a.Left + (a.Width / 2);
        var aCenterY = a.Top + (a.Height / 2);

        var bCenterX = b.Left + (b.Width / 2);
        var bCenterY = b.Top + (b.Height / 2);
        return LocF.CalculateAngleTo(aCenterX, aCenterY, bCenterX, bCenterY);
    }

    public static float CalculateNormalizedDistanceTo(in RectF a, in RectF b)
    {
        var d = CalculateDistanceTo(a, b);
        var angle = CalculateAngleTo(a, b);
        return ConsoleMath.NormalizeQuantity(d, angle, true);
    }

    public static float CalculateNormalizedDistanceTo(float ax, float ay, float aw, float ah, float bx, float by, float bw, float bh)
    {
        var d = CalculateDistanceTo(ax, ay, aw, ah, bx, by, bw, bh);
        var a = CalculateAngleTo(ax, ay, aw, ah, bx, by, bw, bh);
        return ConsoleMath.NormalizeQuantity(d, a, true);
    }

    public static float CalculateDistanceTo(in RectF a, in RectF b)
    {
        var ar = a.Left + a.Width;
        var ab = a.Top + a.Height;

        var br = b.Left + b.Width;
        var bb = b.Top + b.Height;

        var left = br < a.Left;
        var right = ar < b.Left;
        var bottom = bb < a.Top;
        var top = ab < b.Top;
        if (top && left)
            return LocF.CalculateDistanceTo(a.Left, ab, br, b.Top);
        else if (left && bottom)
            return LocF.CalculateDistanceTo(a.Left, a.Top, br, bb);
        else if (bottom && right)
            return LocF.CalculateDistanceTo(ar, a.Top, b.Left, bb);
        else if (right && top)
            return LocF.CalculateDistanceTo(ar, ab, b.Left, b.Top);
        else if (left)
            return a.Left - br;
        else if (right)
            return b.Left - ar;
        else if (bottom)
            return a.Top - bb;
        else if (top)
            return b.Top - ab;
        else
            return 0;
    }

    public static float CalculateDistanceTo(float ax, float ay, float aw, float ah, float bx, float by, float bw, float bh)
    {
        var ar = ax + aw;
        var ab = ay + ah;

        var br = bx + bw;
        var bb = by + bh;

        var left = br < ax;
        var right = ar < bx;
        var bottom = bb < ay;
        var top = ab < by;
        if (top && left)
            return LocF.CalculateDistanceTo(ax, ab, br, by);
        else if (left && bottom)
            return LocF.CalculateDistanceTo(ax, ay, br, bb);
        else if (bottom && right)
            return LocF.CalculateDistanceTo(ar, ay, bx, bb);
        else if (right && top)
            return LocF.CalculateDistanceTo(ar, ab, bx, by);
        else if (left)
            return ax - br;
        else if (right)
            return bx - ar;
        else if (bottom)
            return ay - bb;
        else if (top)
            return by - ab;
        else
            return 0;
    }

    public float NumberOfPixelsThatOverlap(RectF other) => NumberOfPixelsThatOverlap(Left, Top, Width, Height, other.Left, other.Top, other.Width, other.Height);
    public float NumberOfPixelsThatOverlap(float x2, float y2, float w2, float h2) => NumberOfPixelsThatOverlap(Left, Top, Width, Height, x2, y2, w2, h2);

    public float OverlapPercentage(RectF other) => OverlapPercentage(Left, Top, Width, Height, other.Left, other.Top, other.Width, other.Height);
    public float OverlapPercentage(float x2, float y2, float w2, float h2) => OverlapPercentage(Left, Top, Width, Height, x2, y2, w2, h2);

    public bool Touches(RectF other) => Touches(Left, Top, Width, Height, other.Left, other.Top, other.Width, other.Height);
    public bool Touches(float x2, float y2, float w2, float h2) => Touches(Left, Top, Width, Height, x2, y2, w2, h2);
    public bool Contains(RectF other) => Contains(Left, Top, Width, Height, other.Left, other.Top, other.Width, other.Height);
    public bool Contains(float x2, float y2, float w2, float h2) => Contains(Left, Top, Width, Height, x2, y2, w2, h2);

    public static bool Contains(float x1, float y1, float w1, float h1, float x2, float y2, float w2, float h2) =>
        OverlapPercentage(x1, y1, w1, h1, x2, y2, w2, h2) == 1;

    public static bool Touches(float x1, float y1, float w1, float h1, float x2, float y2, float w2, float h2) =>
        NumberOfPixelsThatOverlap(x1, y1, w1, h1, x2, y2, w2, h2) > 0;

    public static float NumberOfPixelsThatOverlap(float x1, float y1, float w1, float h1, float x2, float y2, float w2, float h2)
    {
        var rectangleRight = x1 + w1;
        var otherRight = x2 + w2;
        var rectangleBottom = y1 + h1;
        var otherBottom = y2 + h2;
        var a = Math.Max(0, Math.Min(rectangleRight, otherRight) - Math.Max(x1, x2));
        if (a == 0) return 0;
        var b = Math.Max(0, Math.Min(rectangleBottom, otherBottom) - Math.Max(y1, y2));
        return a * b;
    }

    public static float OverlapPercentage(float x1, float y1, float w1, float h1, float x2, float y2, float w2, float h2)
    {
        var numerator = NumberOfPixelsThatOverlap(x1, y1, w1, h1, x2, y2, w2, h2);
        var denominator = w2 * h2;

        if (numerator == 0) return 0;
        else if (numerator == denominator) return 1;

        var amount = numerator / denominator;
        if (amount < 0) amount = 0;
        else if (amount > 1) amount = 1;

        if (amount > .999)
        {
            amount = 1;
        }

        return amount;
    }

    public bool IsAbove(RectF other)
    {
        return Top < other.Top;
    }

    public bool IsBelow(RectF other)
    {
        return Bottom > other.Bottom;
    }

    public bool IsLeftOf(RectF other)
    {
        return Left < other.Left;
    }

    public bool IsRightOf(RectF other)
    {
        return Right > other.Right;
    }

    public static RectF OffsetByAngleAndDistance(float x, float y, float w, float h, Angle angle, float distance, bool normalized = true)
    {
        var newLoc = LocF.OffsetByAngleAndDistance(x, y, angle, distance, normalized);
        return new RectF(newLoc.Left, newLoc.Top, w, h);
    }

    public static RectF Offset(float x, float y, float w, float h, float dx, float dy) => new RectF(x + dx, y + dy, w, h);
}

public readonly struct LocF
{
    public readonly float Left;
    public readonly float Top;

    public LocF(float x, float y)
    {
        this.Left = x;
        this.Top = y;
    }

    public RectF ToRect(float w, float h) => new RectF(Left - (w / 2), Top - (h / 2), w, h);

    public override string ToString() => $"{Left},{Top}";
    public LocF GetRounded() => new LocF(ConsoleMath.Round(Left), ConsoleMath.Round(Top));
    public LocF GetFloor() => new LocF((int)Left, (int)Top);
    public bool Equals(in LocF other) => Left == other.Left && Top == other.Top;
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is LocF && Equals((LocF)obj);
    public static bool operator ==(in LocF a, in LocF b) => a.Equals(b);
    public static bool operator !=(in LocF a, in LocF b) => a.Equals(b) == false;

    public override int GetHashCode()
    {
        unchecked // Overflow is fine, just wrap
        {
            int hash = 17;
            hash = hash * 23 + Left.GetHashCode();
            hash = hash * 23 + Top.GetHashCode();
            return hash;
        }
    }

    public Angle CalculateAngleTo(float x2, float y2) => CalculateAngleTo(Left, Top, x2, y2);
    public Angle CalculateAngleTo(LocF other) => CalculateAngleTo(this, other);

    public float CalculateDistanceTo(float x2, float y2) => CalculateDistanceTo(Left, Top, x2, y2);
    public float CalculateDistanceTo(LocF other) => CalculateDistanceTo(this, other);

    public float CalculateNormalizedDistanceTo(float x2, float y2) => CalculateNormalizedDistanceTo(Left, Top, x2, y2);
    public float CalculateNormalizedDistanceTo(LocF other) => CalculateNormalizedDistanceTo(this, other);

    public LocF Offset(float dx, float dy) => new LocF(Left + dx, Top + dy);
    
    public static LocF Offset(float x, float y, float dx, float dy) => new LocF(x + dx, y + dy);
        
    public static LocF OffsetByAngleAndDistance(float x, float y, Angle angle, float distance, bool normalized = true)
    {
        if (normalized)
        {
            distance = ConsoleMath.NormalizeQuantity(distance, angle.Value);
        }
        var forward = angle.Value > 270 || angle.Value < 90;
        var up = angle.Value > 180;

        // convert to radians
        angle = (float)(angle.Value * Math.PI / 180);
        float dy = (float)Math.Abs(distance * Math.Sin(angle.Value));
        float dx = (float)Math.Sqrt((distance * distance) - (dy * dy));

        float x2 = forward ? x + dx : x - dx;
        float y2 = up ? y - dy : y + dy;

        return new LocF(x2, y2);
    }

    public LocF OffsetByAngleAndDistance(Angle angle, float distance, bool normalized = true)
    {
        if (normalized)
        {
            distance = ConsoleMath.NormalizeQuantity(distance, angle.Value);
        }
        var forward = angle.Value > 270 || angle.Value < 90;
        var up = angle.Value > 180;

        // convert to radians
        angle = (float)(angle.Value * Math.PI / 180);
        float dy = (float)Math.Abs(distance * Math.Sin(angle.Value));
        float dx = (float)Math.Sqrt((distance * distance) - (dy * dy));

        float x2 = forward ? Left + dx : Left - dx;
        float y2 = up ? Top - dy : Top + dy;

        return new LocF(x2, y2);
    }

    public static float CalculateNormalizedDistanceTo(in LocF a, in LocF b)
    {
        var d = CalculateDistanceTo(a, b);
        var angle = CalculateAngleTo(a, b);
        return ConsoleMath.NormalizeQuantity(d, angle.Value, true);
    }

    public static float CalculateNormalizedDistanceTo(float ax, float ay, float bx, float by)
    {
        var d = CalculateDistanceTo(ax, ay, bx, by);
        var a = CalculateAngleTo(ax, ay, bx, by);
        return ConsoleMath.NormalizeQuantity(d, a.Value, true);
    }

    public static Angle CalculateAngleTo(in LocF a, in LocF b)
    {
        float dx = b.Left - a.Left;
        float dy = b.Top - a.Top;
        float d = a.CalculateDistanceTo(b);

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
            throw new Exception($"Failed to calculate angle from {a.Left},{a.Top} to {b.Left},{b.Top}");
        }

        var ret = (float)(increment + radians * 180 / Math.PI);

        if (ret == 360) ret = 0;

        return ret;
    }

    public static Angle CalculateAngleTo(float x1, float y1, float x2, float y2)
    {
        float dx = x2 - x1;
        float dy = y2 - y1;
        float d = CalculateDistanceTo(x1, y1, x2, y2);

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
            throw new Exception($"Failed to calculate angle from {x1},{y1} to {x2},{y2}");
        }

        var ret = (float)(increment + radians * 180 / Math.PI);

        if (ret == 360) ret = 0;

        return ret;
    }

    public static float CalculateDistanceTo(float x1, float y1, float x2, float y2) => (float)Math.Sqrt(((x1 - x2) * (x1 - x2)) + ((y1 - y2) * (y1 - y2)));
    public static float CalculateDistanceTo(in LocF a, in LocF b) => (float)Math.Sqrt(((a.Left - b.Left) * (a.Left - b.Left)) + ((a.Top - b.Top) * (a.Top - b.Top)));
}
 
public readonly struct Circle
{
    public static int FindLineCircleIntersections(float cx, float cy, float radius, float x1, float y1, float x2, float y2, out float ox1, out float oy1, out float ox2, out float oy2)
    {
        float dx, dy, A, B, C, det, t;

        dx = x2 - x1;
        dy = y2 - y1;

        A = dx * dx + dy * dy;
        B = 2 * (dx * (x1 - cx) + dy * (y1 - cy));
        C = (x1 - cx) * (x1 - cx) +
            (y1 - cy) * (y1 - cy) -
            radius * radius;

        det = B * B - 4 * A * C;
        if ((A <= 0.0000001) || (det < 0))
        {
            // No real solutions.
            ox1 = float.NaN;
            ox2 = float.NaN;
            oy1 = float.NaN;
            oy2 = float.NaN;
            return 0;
        }
        else if (det == 0)
        {
            // One solution.
            t = -B / (2 * A);

            ox1 = x1 + t * dx;
            oy1 = y1 + t * dy;

            ox2 = float.NaN;
            oy2 = float.NaN;
            return 1;
        }
        else
        {
            // Two solutions.
            t = (float)((-B + Math.Sqrt(det)) / (2 * A));

            ox1 = x1 + t * dx;
            oy1 = y1 + t * dy;

            t = (float)((-B - Math.Sqrt(det)) / (2 * A));

            ox2 = x1 + t * dx;
            oy2 = y1 + t * dy;
            return 2;
        }
    }
}

public static class ConsoleMath
{
    public static float NormalizeQuantity(this int quantity, Angle angle, bool reverse = false) => NormalizeQuantity((float)quantity, angle, reverse);
    public static float Round(float f, int digits) => (float)Math.Round(f, digits, MidpointRounding.AwayFromZero);
    public static float Round(double d, int digits) => (float)Math.Round(d, digits, MidpointRounding.AwayFromZero);
    public static int Round(float f) => (int)Math.Round(f, MidpointRounding.AwayFromZero);
    public static int Round(double d) => (int)Math.Round(d, MidpointRounding.AwayFromZero);


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
    public static float NormalizeQuantity(this float quantity, Angle angle, bool reverse = false)
    {
        float degreesFromFlat;
        if (angle.Value <= 180)
        {
            degreesFromFlat = Math.Min(180 - angle.Value, angle.Value);
        }
        else
        {
            degreesFromFlat = Math.Min(angle.Value - 180, 360 - angle.Value);
        }

        var skewPercentage = 1 + (degreesFromFlat / 90);

        return reverse ? quantity * skewPercentage : quantity / skewPercentage;
    }
}

