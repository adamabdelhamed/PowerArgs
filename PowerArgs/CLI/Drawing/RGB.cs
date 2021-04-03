using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PowerArgs
{
    public struct RGB
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

        public static readonly RGB Black = ConsoleColor.Black;
        public static readonly RGB DarkBlue = ConsoleColor.DarkBlue;
        public static readonly RGB DarkGreen = ConsoleColor.DarkGreen;
        public static readonly RGB DarkCyan = ConsoleColor.DarkCyan;
        public static readonly RGB DarkRed = ConsoleColor.DarkRed;
        public static readonly RGB DarkMagenta = ConsoleColor.DarkMagenta;
        public static readonly RGB DarkYellow = ConsoleColor.DarkYellow;
        public static readonly RGB Gray = ConsoleColor.Gray;
        public static readonly RGB DarkGray = ConsoleColor.DarkGray;
        public static readonly RGB Blue = ConsoleColor.Blue;
        public static readonly RGB Green = ConsoleColor.Green;
        public static readonly RGB Cyan = ConsoleColor.Cyan;
        public static readonly RGB Red = ConsoleColor.Red;
        public static readonly RGB Magenta = ConsoleColor.Magenta;
        public static readonly RGB Yellow = ConsoleColor.Yellow;
        public static readonly RGB White = ConsoleColor.White;

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

        public override int GetHashCode() => $"{R}/{G}/{B}".GetHashCode();

        public float CalculateDistanceTo(RGB other) => (float)Math.Sqrt(
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

                if (RGBToConsoleColorMap.Count < 10000)
                {
                    RGBToConsoleColorMap.Add(color, closestColor);
                }
                return closestColor;
            }
        }
        public static implicit operator RGB(ConsoleColor color) => (int)color < ConsoleColorMap.Length ? ConsoleColorMap[(int)color] : ConsoleString.DefaultForegroundColor;

        private static readonly Regex RGBRegex = new Regex(@"^\s*(?<r>\d+)\s*,\s*(?<g>\d+)\s*,\s*(?<b>\d+)\s*$");

        public static RGB Parse(string value)
        {
            if(TryParse(value, out RGB ret) == false)
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
            var match = RGBRegex.Match(value);
            if (match.Success)
            {
                var r = byte.Parse(match.Groups["r"].Value);
                var g = byte.Parse(match.Groups["g"].Value);
                var b = byte.Parse(match.Groups["b"].Value);
                ret = new RGB(r, g, b);
                return true;
            }
            else if (Enum.TryParse(value, out ConsoleColor c))
            {
                ret = (RGB)c;
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
            if(TryParse(val, out RGB ret))
            {
                return ret;
            }
            else
            {
                throw new ArgException($"'{val}' is not a valid RGB color");
            }
        }



        public override string ToString()
        {
            if (RGBToConsoleColorMap.TryGetValue(this, out ConsoleColor c))
            {
                return c.ToString();
            }
            else
            {
                return ToRGBString();
            }
        }

        public string ToRGBString() => $"{R},{G},{B}";
       
        public string ToWebString()
        {
            return "#"+BitConverter.ToString(new byte[] { R, G, B }).Replace("-", "");
        }
            
        /// <summary>
        /// Converts this color to a new color that is closer to the other
        /// color provided.
        /// </summary>
        /// <param name="other">The other color to change to</param>
        /// <param name="percentage">The percentage to change. A value of zero will result in the original color being returned. A value of 1 will result in the other color returned. A value between zero and one will result in the color being a mix of the two colors.</param>
        /// <returns></returns>
        public RGB ToOther(RGB other, float percentage)
        {
            var dR = other.R - R;
            var dG = other.G - G;
            var dB = other.B - B;

            var r = R + dR * percentage;
            var g = G + dG * percentage;
            var b = B + dB * percentage;

            return new RGB((byte)r, (byte)g, (byte)b);
        }

        public static Task AnimateAsync(RGBAnimationOptions options)
        {
            var deltaBufferR = new float[options.Transitions.Count];
            var deltaBufferG = new float[options.Transitions.Count];
            var deltaBufferB = new float[options.Transitions.Count];

            var deltaBufferRReversed = new float[options.Transitions.Count];
            var deltaBufferGReversed = new float[options.Transitions.Count];
            var deltaBufferBReversed = new float[options.Transitions.Count];

            for (var i = 0; i < options.Transitions.Count; i++)
            {
                deltaBufferR[i] = options.Transitions[i].Value.R - options.Transitions[i].Key.R;
                deltaBufferG[i] = options.Transitions[i].Value.G - options.Transitions[i].Key.G;
                deltaBufferB[i] = options.Transitions[i].Value.B - options.Transitions[i].Key.B;

                deltaBufferRReversed[i] = options.Transitions[i].Key.R - options.Transitions[i].Value.R;
                deltaBufferGReversed[i] = options.Transitions[i].Key.G - options.Transitions[i].Value.G;
                deltaBufferBReversed[i] = options.Transitions[i].Key.B - options.Transitions[i].Value.B;
            }
            var colorBuffer = new RGB[options.Transitions.Count];
            var isReversed = false;
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
                OnReversedChanged = (r) => isReversed=r,
                Setter = percentage =>
                {
                     
                    if(isReversed == false)
                    {
                        for (var i = 0; i < options.Transitions.Count; i++)
                        {

                            if (percentage == 1)
                            {
                                colorBuffer[i] = new RGB(
                               (byte)(options.Transitions[i].Value.R),
                               (byte)(options.Transitions[i].Value.G),
                               (byte)(options.Transitions[i].Value.B));
                            }
                            else
                            {
                                var rDeltaFrame = deltaBufferR[i] * percentage;
                                var gDeltaFrame = deltaBufferG[i] * percentage;
                                var bDeltaFrame = deltaBufferB[i] * percentage;

                                colorBuffer[i] = new RGB(
                                    (byte)(options.Transitions[i].Key.R + rDeltaFrame),
                                    (byte)(options.Transitions[i].Key.G + gDeltaFrame),
                                    (byte)(options.Transitions[i].Key.B + bDeltaFrame));
                            }
                        }
                    }
                    else
                    {
                        percentage = 1 - percentage;
                        for (var i = 0; i < options.Transitions.Count; i++)
                        {
                            var rDeltaFrame = deltaBufferRReversed[i] * percentage;
                            var gDeltaFrame = deltaBufferGReversed[i] * percentage;
                            var bDeltaFrame = deltaBufferBReversed[i] * percentage;
                            if (percentage == 1)
                            {
                                colorBuffer[i] = new RGB(
                               (byte)(options.Transitions[i].Key.R),
                               (byte)(options.Transitions[i].Key.G),
                               (byte)(options.Transitions[i].Key.B));
                            }
                            else
                            {
                                colorBuffer[i] = new RGB(
                                    (byte)(options.Transitions[i].Value.R + rDeltaFrame),
                                    (byte)(options.Transitions[i].Value.G + gDeltaFrame),
                                    (byte)(options.Transitions[i].Value.B + bDeltaFrame));
                            }
                        }
                    }
                   
                    options.OnColorsChanged(colorBuffer);
                }
            });
        }
    }

    public class RGBAnimationOptions
    {
        public List<KeyValuePair<RGB, RGB>> Transitions { get; set; } = new List<KeyValuePair<RGB, RGB>>();

        public float Duration { get; set; }
        /// <summary>
        /// The easing function to apply
        /// </summary>
        public EasingFunction EasingFunction { get; set; } = Animator.EaseInOut;

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

        public Action<RGB[]> OnColorsChanged { get; set; }
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
            else if (Enum.TryParse(val, out ConsoleColor c))
            {
                return c;
            }
            {
                throw new ArgException($"'{val}' is not a valid RGB color");
            }
        }
    }
}
