using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.RegularExpressions;

namespace PowerArgs;
[ArgReviverType]
public readonly struct RGB
{
    public static readonly float MaxDistance = (float)Math.Sqrt((255 * 255) + (255 * 255) + (255 * 255));

    public static readonly RGB[] ConsoleColorMap = new RGB[]
    {
            new RGB(0,0,0) ,      // Black = 0
            new RGB(0,0,139) ,      // DarkBlue = 1
            new RGB(0,139,0) ,      // DarkGreen = 2
            new RGB(0,139,139) ,      // DarkCyan = 3
            new RGB(139,0,0) ,      // DarkRed = 4
            new RGB(139,0,139) ,      // DarkMagenta = 5
            new RGB(204,204,0) ,      // DarkYellow = 6
            new RGB(200,200,200) ,      // Gray = 7
            new RGB(128,128,128) ,      // DarkGray = 8
            new RGB(0,0,255) ,      // Blue = 9
            new RGB(0,255,0) ,      // Green = 10
            new RGB(0,255,255) ,      // Cyan = 11
            new RGB(255,0,0) ,      // Red = 12
            new RGB(255,0,255) ,      // Magenta = 13
            new RGB(255,255,0) ,      // Yellow = 14
            new RGB(255,255,255) ,      // White = 15
    };

    private static IReadOnlyDictionary<string, RGB> NamesToColorsFunc() => new ReadOnlyDictionary<string, RGB>(typeof(RGB)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.FieldType == typeof(RGB))
        .ToDictionary(f => f.Name, f => (RGB)f.GetValue(null)));

    public static IReadOnlyDictionary<RGB, string> ColorsToNamesFunc()
    {
        var ret = new Dictionary<RGB, string>();
        foreach (var name in NamesToColors)
        {
            ret.Add(NamesToColors[name.Key], name.Key);
        }
        return new ReadOnlyDictionary<RGB, string>(ret);
    }

    private static IReadOnlyDictionary<string, RGB> _NamesToColors;
    private static IReadOnlyDictionary<RGB, string> _ColorsToNames;
    public static IReadOnlyDictionary<string, RGB> NamesToColors => _NamesToColors ?? (_NamesToColors = NamesToColorsFunc());
    public static IReadOnlyDictionary<RGB, string> ColorsToNames => _ColorsToNames ?? (_ColorsToNames = ColorsToNamesFunc());




    public static readonly RGB Black =          new RGB(0, 0, 0);
    public static readonly RGB DarkBlue =       new RGB(0, 0, 139);
    public static readonly RGB DarkGreen =      new RGB(0, 139, 0);
    public static readonly RGB DarkCyan =       new RGB(0, 139, 139);
    public static readonly RGB DarkGray =       new RGB(128, 128, 128);
    public static readonly RGB DarkRed =        new RGB(139, 0, 0);
    public static readonly RGB DarkMagenta =    new RGB(139, 0, 139);
    public static readonly RGB DarkYellow =     new RGB(204, 204, 0);
    public static readonly RGB Gray =           new RGB(200, 200, 200);
    public static readonly RGB Blue =           new RGB(0, 0, 255);
    public static readonly RGB Green =          new RGB(0, 255, 0);
    public static readonly RGB Cyan =           new RGB(0, 255, 255);
    public static readonly RGB Red =            new RGB(255, 0, 0);
    public static readonly RGB Magenta =        new RGB(255, 0, 255);
    public static readonly RGB Yellow =         new RGB(255, 255, 0);
    public static readonly RGB White =          new RGB(255, 255, 255);
    public static readonly RGB Orange = new RGB(255, 140, 0);

    public readonly byte R;
    public readonly byte G;
    public readonly byte B;

    public float Brightness => (((int)R + (int)G + (int)B) / 3f) / byte.MaxValue;

    public RGB(byte r, byte g, byte b)
    {
        this.R = r;
        this.G = g;
        this.B = b;
    }

    public override bool Equals(object obj)
    {
        if (obj is RGB == false) return false;
        var other = (RGB)obj;
        if (other == null) return false;
        return this.R == other.R && this.G == other.G && this.B == other.B;
    }

    public bool EqualsIn(in RGB obj)
    {
        return this.R == obj.R && this.G == obj.G && this.B == obj.B;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + R.GetHashCode();
            hash = hash * 23 + G.GetHashCode();
            hash = hash * 23 + B.GetHashCode();
            return hash;
        }
    }

    public float CalculateDistanceTo(in RGB other) => (float)Math.Sqrt(
            Math.Pow(R - other.R, 2) +
            Math.Pow(G - other.G, 2) +
            Math.Pow(B - other.B, 2));

    public RGB GetCompliment()
    {
        byte max = 255;
        return new RGB((byte)(max - R), (byte)(max - G), (byte)(max - B));
    }

    public RGB Darker => ToOther(Black, .5f);
    public RGB Brighter => ToOther(White, .5f);
    public static bool operator ==(in RGB a, in RGB b) => a.EqualsIn(b);
    public static bool operator !=(in RGB a, in RGB b) => !a.EqualsIn(b);


   
    public static RGB Convert(in ConsoleColor color)
    {
        var ret = (int)color < ConsoleColorMap.Length ? ConsoleColorMap[(int)color] : ConsoleString.DefaultForegroundColor;
        return ret;
    }

    public bool TryConvert(out ConsoleColor c)
    {
        if(ColorsToNames.TryGetValue(this, out string name) && Enum.TryParse(name, out ConsoleColor ret))
        {
            c = ret;
            return true;
        }
        else
        {
            c = default;
            return false;
        }
    }
    

    private static Regex RGBRegex;

    public static RGB Parse(string value)
    {
        if (TryParse(value, out RGB ret) == false)
        {
            throw new FormatException($"{value} is not a valid RGB color");
        }
        else
        {
            return ret;
        }
    }

    public static bool TryParse(string value, out RGB ret)
    {
        RGBRegex = RGBRegex ?? new Regex(@"^\s*(?<r>\d+)\s*,\s*(?<g>\d+)\s*,\s*(?<b>\d+)\s*$");
        var match = RGBRegex.Match(value);
        if (match.Success)
        {
            var r = byte.Parse(match.Groups["r"].Value);
            var g = byte.Parse(match.Groups["g"].Value);
            var b = byte.Parse(match.Groups["b"].Value);
            ret = new RGB(r, g, b);
            return true;
        }
        else if (NamesToColors.TryGetValue(value, out RGB c))
        {
            ret = c;
            return true;
        }
        else
        {
            ret = default(RGB);
            return false;
        }
    }

    [ArgReviver]
    public static RGB Revive(string key, string val)
    {
        if (TryParse(val, out RGB ret))
        {
            return ret;
        }
        else
        {
            throw new ArgException($"'{val}' is not a valid RGB color");
        }
    }

    public override string ToString() => ColorsToNames.TryGetValue(this, out string s) ? s : ToRGBString();
    public string ToRGBString() => $"{R},{G},{B}";
    public string ToWebString() => "#" + BitConverter.ToString(new byte[] { R, G, B }).Replace("-", "");

    /// <summary>
    /// Converts this color to a new color that is closer to the other
    /// color provided.
    /// </summary>
    /// <param name="other">The other color to change to</param>
    /// <param name="percentage">The percentage to change. A value of zero will result in the original color being returned. A value of 1 will result in the other color returned. A value between zero and one will result in the color being a mix of the two colors.</param>
    /// <returns></returns>
    public RGB ToOther(in RGB other, float percentage)
    {
        var dR = other.R - R;
        var dG = other.G - G;
        var dB = other.B - B;

        var r = R + dR * percentage;
        var g = G + dG * percentage;
        var b = B + dB * percentage;

        r = r > 255 ? 255 : r < 0 ? 0 : r;
        g = g > 255 ? 255 : g < 0 ? 0 : g;
        b = b > 255 ? 255 : b < 0 ? 0 : b;

        return new RGB((byte)r, (byte)g, (byte)b);
    }

}

public static class NullableRGBReviver
{
    [ArgReviver]
    public static RGB? Revive(string key, string val)
    {
        if (RGB.TryParse(val, out RGB ret))
        {
            return ret;
        }
        {
            throw new ArgException($"'{val}' is not a valid RGB color");
        }
    }
}
