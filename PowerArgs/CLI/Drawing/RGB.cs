using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.CommandLine.Rendering;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PowerArgs
{
    public struct RGB
    {
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

        public static readonly Dictionary<RGB, ConsoleColor> RGBToConsoleColorMap = new Dictionary<RGB, ConsoleColor>
        {
            { new RGB(0,0,0) ,  ConsoleColor.Black },    // Black = 0
            { new RGB(0,0,139) , ConsoleColor.DarkBlue },     // DarkBlue = 1
            { new RGB(0,139,0) ,  ConsoleColor.DarkGreen },    // DarkGreen = 2
            { new RGB(0,139,139) ,   ConsoleColor.DarkCyan },   // DarkCyan = 3
            { new RGB(139,0,0) ,    ConsoleColor.DarkRed },  // DarkRed = 4
            { new RGB(139,0,139) ,    ConsoleColor.DarkMagenta },  // DarkMagenta = 5
            { new RGB(204,204,0) ,   ConsoleColor.DarkYellow },   // DarkYellow = 6
            { new RGB(200,200,200) ,   ConsoleColor.Gray },  // Gray = 7
            { new RGB(128,128,128) ,    ConsoleColor.DarkGray },  // DarkGray = 8
            { new RGB(0,0,255) ,    ConsoleColor.Blue },  // Blue = 9
            { new RGB(0,255,0) ,   ConsoleColor.Green },   // Green = 10
            { new RGB(0,255,255) ,  ConsoleColor.Cyan },    // Cyan = 11
            { new RGB(255,0,0) ,    ConsoleColor.Red },  // Red = 12
            { new RGB(255,0,255) ,   ConsoleColor.Magenta },   // Magenta = 13
            { new RGB(255,255,0) ,    ConsoleColor.Yellow },  // Yellow = 14
            { new RGB(255,255,255) ,  ConsoleColor.White },    // White = 15
        };


        public byte R { get; private set; }
        public byte G { get; private set; }
        public byte B { get; private set; }

        public RGB(RgbColor color)
        {
            this.R = color.Red;
            this.G = color.Green;
            this.B = color.Blue;
        }

        public RGB(byte r, byte g, byte b) : this(new RgbColor(r, g, b)) { }

        public override bool Equals(object obj)
        {
            if (obj is RGB == false) return false;
            var other = (RGB)obj;
            if (other == null) return false;
            return this.R == other.R && this.G == other.G && this.B == other.B;
        }

        public override int GetHashCode() => $"{R}/{G}/{B}".GetHashCode();

        public float CalculateDistanceTo(RGB other) => (float)Math.Sqrt(
                Math.Pow(R - other.R, 2) +
                Math.Pow(G - other.G, 2) +
                Math.Pow(B - other.B, 2)
            );


        public static bool operator ==(RGB a, RGB b) => a.Equals(b);
        public static bool operator !=(RGB a, RGB b) => !a.Equals(b);
        public static bool operator ==(RGB a, ConsoleColor b) => a.Equals((RGB)b);
        public static bool operator !=(RGB a, ConsoleColor b) => !a.Equals((RGB)b);

        public static implicit operator ConsoleColor(RGB color)
        {
            if (color == null)
            {
                return ConsoleString.DefaultForegroundColor;
            }
            else if (RGBToConsoleColorMap.TryGetValue(color, out ConsoleColor ret))
            {
                return ret;
            }
            else
            {
                var smallestDistance = float.MaxValue;
                ConsoleColor closestColor = ConsoleColor.Black;
                for (var i = 0; i < ConsoleColorMap.Length; i++)
                {
                    var rgb = ConsoleColorMap[i];
                    var d = rgb.CalculateDistanceTo(color);
                    if (d < smallestDistance)
                    {
                        smallestDistance = d;
                        closestColor = (ConsoleColor)i;
                    }
                }

                RGBToConsoleColorMap.Add(color, closestColor);
                return closestColor;
            }
        }
        public static implicit operator RGB(ConsoleColor color) => (int)color < ConsoleColorMap.Length ? ConsoleColorMap[(int)color] : ConsoleString.DefaultForegroundColor;

        public RgbColor ToRgbColor() => new RgbColor(R, G, B);

        private static readonly Regex RGBRegex = new Regex(@"^\s*(?<r>d+)\s*,\s*(?<g>d+)\s*,\s*(?<b>d+)\s*$");

        internal static bool TryParse(string value, out RGB ret)
        {
            var match = RGBRegex.Match(value);
            if (match.Success == false)
            {
                ret = default(RGB);
                return false;
            }
            var r = byte.Parse(match.Groups["r"].Value);
            var g = byte.Parse(match.Groups["g"].Value);
            var b = byte.Parse(match.Groups["b"].Value);
            ret = new RGB(r, g, b);
            return true;
        }

        public override string ToString()
        {
            if (RGBToConsoleColorMap.TryGetValue(this, out ConsoleColor c))
            {
                return c.ToString();
            }
            else
            {
                return $"{R},{G},{B}";
            }
        }

        public static Task AnimateAsync(RGBAnimationOptions options)
        {
            var rDelta = options.To.R - options.From.R;
            var gDelta = options.To.G - options.From.G;
            var bDelta = options.To.B - options.From.B;

            return Animator.AnimateAsync(new FloatAnimatorOptions()
            {
                From = 0,
                To = 1,
                Duration = options.Duration,
                AutoReverse = options.AutoReverse,
                AutoReverseDelay = options.AutoReverseDelay,
                DelayProvider = options.DelayProvider,
                EasingFunction = options.EasingFunction,
                IsCancelled = options.IsCancelled,
                Loop = options.Loop,
                Setter = percentage =>
                {
                    if (percentage == 1)
                    {
                        options.OnColorChanged((RGB)options.To);
                    }
                    var rDeltaFrame = rDelta * percentage;
                    var gDeltaFrame = gDelta * percentage;
                    var bDeltaFrame = bDelta * percentage;

                    var c = new RGB(
                        (byte)(options.From.R + rDeltaFrame),
                        (byte)(options.From.G + gDeltaFrame),
                        (byte)(options.From.B + bDeltaFrame));
                    options.OnColorChanged(c);
                }
            });
        }
    }

    public class RGBAnimationOptions
    {
        public RGB From { get; set; }
        public RGB To { get; set; }

        public float Duration { get; set; }
        /// <summary>
        /// The easing function to apply
        /// </summary>
        public Func<float, float> EasingFunction { get; set; } = Animator.EaseInOut;

        /// <summary>
        /// If true then the animation will automatically reverse itself when done
        /// </summary>
        public bool AutoReverse { get; set; }

        /// <summary>
        /// When specified, the animation will loop until this lifetime completes
        /// </summary>
        public ILifetimeManager Loop { get; set; }

        /// <summary>
        /// The provider to use for delaying between animation frames
        /// </summary>
        public IDelayProvider DelayProvider { get; set; }

        /// <summary>
        /// If auto reverse is enabled, this is the pause, in milliseconds, after the forward animation
        /// finishes, to wait before reversing
        /// </summary>
        public float AutoReverseDelay { get; set; } = 0;

        /// <summary>
        /// A callback that indicates that we should end the animation early
        /// </summary>
        public Func<bool> IsCancelled { get; set; }

        public Action<RGB> OnColorChanged { get; set; }
    }



}
