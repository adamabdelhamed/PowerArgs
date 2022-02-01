using System;
using System.Text;
using System.Text.RegularExpressions;

namespace PowerArgs.Cli
{
    /// <summary>
    /// The code that converts console bitmap frames from their in memory structure to lines of text, and vice versa
    /// </summary>
    internal class ConsoleBitmapFrameSerializer
    {
        /// <summary>
        /// The tokenizer that will parse each line during deserialization
        /// </summary>
        private Tokenizer<Token> tokenizer;

        /// <summary>
        /// Creates a new instance of the serializer
        /// </summary>
        public ConsoleBitmapFrameSerializer()
        {
            tokenizer = new Tokenizer<Token>();
            tokenizer.EscapeSequenceIndicator = null;
            tokenizer.WhitespaceBehavior = WhitespaceBehavior.Include;
            tokenizer.Delimiters.Add("[");
            tokenizer.Delimiters.Add("]");
        }

        /// <summary>
        /// Serializes the given raw frame.
        /// 
        /// A serialized raw frame is always a single line with this structure:
        /// 
        /// All data values are surrounded in square brackets like [dataValue]
        /// 
        /// Segment1 - Timestamp in the format: [$timestampInTicks$] where $timestampInTicks$ represents a 64 bit non-negative integer
        /// Segment2 - The type of frame, in this case [Raw]
        /// Segment3 - The raw bitmap data
        ///     
        ///     The first pixel will be preceded by color markers for foreground (e.g. [F=Red]) and background (e.g. [B=Red]) which means that subsequence characters have those color characteristics.
        ///     If the next pixel is a different foreground and/or background color then there will be color markers for those changes in between the pixel data values
        ///     If the next pixel shares the same foreground and background then there will be no color markers in between those pixels. This saves space.
        ///     Each pixel value is surrounded by square brackets like [A] if the pixel value was A.
        ///     Each pixel value is generally a single character, but square brackets are encoded a OB for the opening bracket and CB for the closing bracket
        ///     The pixels are ordered vertically starting at x = 0, y = 0.  
        ///     There are no markers for the end of a vertical scan line since you're assumed to know the size from the header.
        /// </summary>
        /// <param name="frame">a raw frame</param>
        /// <returns>a serialized string</returns>
        public string SerializeFrame(ConsoleBitmapRawFrame frame)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append($"[{frame.Timestamp.Ticks}]");
            builder.Append("[Raw]");
            RGB? lastFg = null;
            RGB? lastBg = null;
            for (var x = 0; x < frame.Pixels.Length; x++)
            {
                for (var y = 0; y < frame.Pixels[0].Length; y++)
                {
                    if (lastFg.HasValue == false || lastFg.Value != frame.Pixels[x][y].ForegroundColor)
                    {
                        lastFg = frame.Pixels[x][y].ForegroundColor;
                        builder.Append($"[F={lastFg}]");
                    }

                    if (lastBg.HasValue == false || lastBg.Value != frame.Pixels[x][y].BackgroundColor)
                    {
                        lastBg = frame.Pixels[x][y].BackgroundColor;
                        builder.Append($"[B={lastBg}]");
                    }

                    string appendValue;
                    var pixelCharValue = frame.Pixels[x][y].Value;
                    if(pixelCharValue == '[')
                    {
                        appendValue = "OB";
                    }
                    else if (pixelCharValue == ']')
                    {
                        appendValue = "CB";
                    }
                    else
                    {
                        appendValue = pixelCharValue+"";
                    }
                    

                    builder.Append('['+appendValue+']');
                }
            }
            builder.AppendLine();

            var ret = builder.ToString();
            return ret;
        }

        /// <summary>
        /// Serializes the given diff frame.
        /// 
        /// A serialized diff frame is always a single line with this structure:
        /// 
        /// All data values are surrounded in square brackets like [dataValue]
        /// 
        /// Segment1 - Timestamp in the format: [$timestampInTicks$] where $timestampInTicks$ represents a 64 bit non-negative integer
        /// Segment2 - The type of frame, in this case [Diff]
        /// Segment3 - The diff data
        ///     
        ///     The first pixel will be preceded by color markers for foreground (e.g. [F=Red]) and background (e.g. [B=Red]) which means that subsequence characters have those color characteristics.
        ///     If the next pixel is a different foreground and/or background color then there will be color markers for those changes in between the pixel data values
        ///     If the next pixel shares the same foreground and background then there will be no color markers in between those pixels. This saves space.
        ///     Diff pixels are surrounded in square brackets in this format: [xCoordinate,yCoordinate,pixelValue].
        ///     pixelValue is generally a single character, but square brackets are encoded a OB for the opening bracket and CB for the closing bracket    
        /// 
        /// </summary>
        /// <param name="frame">a raw frame</param>
        /// <returns>a serialized string</returns>
        public string SerializeFrame(ConsoleBitmapDiffFrame frame)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append($"[{frame.Timestamp.Ticks}]");
            builder.Append("[Diff]");

            RGB? lastFg = null;
            RGB? lastBg = null;

            foreach (var diff in frame.Diffs)
            {
                if (lastFg.HasValue == false || lastFg.Value != diff.Value.ForegroundColor)
                {
                    lastFg = diff.Value.ForegroundColor;
                    builder.Append($"[F={lastFg}]");
                }

                if (lastBg.HasValue == false || lastBg.Value != diff.Value.BackgroundColor)
                {
                    lastBg = diff.Value.BackgroundColor;
                    builder.Append($"[B={lastBg}]");
                }

                string appendValue;
                var pixelCharValue = diff.Value.Value;
                if (pixelCharValue == '[')
                {
                    appendValue = "OB";
                }
                else if (pixelCharValue == ']')
                {
                    appendValue = "CB";
                }
                else
                {
                    appendValue = pixelCharValue + "";
                }

                builder.Append($"[{diff.X},{diff.Y},{appendValue}]");
            }
            builder.AppendLine();
            var ret = builder.ToString();
            return ret;
        }
        private static readonly Regex ColorSpecifierRegex = new Regex(@"(?<ForB>F|B)=(?<color>\w+)");
        private static readonly Regex PixelDiffRegex = new Regex("(?<x>\\d+),(?<y>\\d+),(?<val>.+)");
        /// <summary>
        /// Deserializes the given frame given a known width and height.
        /// </summary>
        /// <param name="serializedFrame">the frame data</param>
        /// <param name="width">the known width of the frame</param>
        /// <param name="height">the known height of the frame</param>
        /// <returns>a deserialized frame that's either a raw frame or a diff frame, depending on what was in the serialized string</returns>
        public ConsoleBitmapFrame DeserializeFrame(string serializedFrame, int width, int height)
        {
            var tokens = tokenizer.Tokenize(serializedFrame);
            var reader = new TokenReader<Token>(tokens);

            reader.Expect("[");
            var timestampToken = reader.Advance();
            var timestamp = new TimeSpan(long.Parse(timestampToken.Value));
            reader.Expect("]");

            reader.Expect("[");
            reader.Advance();
            var isDiff = reader.Current.Value == "Diff";
            reader.Expect("]");

            if (isDiff)
            {
                var diffFrame = new ConsoleBitmapDiffFrame()
                {
                    Timestamp = timestamp,
                    Diffs = new System.Collections.Generic.List<ConsoleBitmapPixelDiff>()
                };

                var lastBackground = ConsoleString.DefaultBackgroundColor;
                var lastForeground = ConsoleString.DefaultForegroundColor;
                while (reader.CanAdvance(skipWhitespace: true))
                {
                    reader.Expect("[", skipWhiteSpace: true);
                    if (reader.Peek().Value.StartsWith("F=") || reader.Peek().Value.StartsWith("B="))
                    {
                        reader.Advance();
                        var match = ColorSpecifierRegex.Match(reader.Current.Value);
                        if (match.Success == false) throw new FormatException($"Unexpected token {reader.Current.Value} at position {reader.Current.Position} ");

                        var isForeground = match.Groups["ForB"].Value == "F";

                        if (isForeground)
                        {
                            if (Enum.TryParse(match.Groups["color"].Value, out ConsoleColor c))
                            {
                                lastForeground = (RGB)c;
                            }
                            else if (RGB.TryParse(match.Groups["color"].Value, out lastForeground) == false)
                            {
                                throw new ArgumentException($"Expected a color @ {reader.Position}");
                            }
                        }
                        else
                        {
                            if (Enum.TryParse(match.Groups["color"].Value, out ConsoleColor c))
                            {
                                lastBackground = (RGB)c;
                            }
                            else if (RGB.TryParse(match.Groups["color"].Value, out lastBackground) == false)
                            {
                                throw new ArgumentException($"Expected a color @ {reader.Position}");
                            }
                        }

                        reader.Expect("]");
                    }
                    else
                    {
                        var match = PixelDiffRegex.Match(reader.Advance().Value);
                        if (match.Success == false) throw new FormatException("Could not parse pixel diff");

                        var valGroup = match.Groups["val"].Value;

                        char? nextChar = valGroup.Length == 1 ? valGroup[0] : valGroup == "OB" ? '[' : valGroup == "CB" ? ']' : new char?();
                        if (nextChar.HasValue == false) throw new FormatException($"Unexpected token {nextChar} @ {reader.Position}");

                        diffFrame.Diffs.Add(new ConsoleBitmapPixelDiff()
                        {
                            X = int.Parse(match.Groups["x"].Value),
                            Y = int.Parse(match.Groups["y"].Value),
                            Value = new ConsoleCharacter(nextChar.Value, lastForeground, lastBackground),
                        });

                        reader.Expect("]");
                    }
                }

                return diffFrame;
            }
            else
            {
                var rawFrame = new ConsoleBitmapRawFrame()
                {
                    Timestamp = timestamp,
                    Pixels = new ConsoleCharacter[width][]
                };

                for (var i = 0; i < width; i++)
                {
                    rawFrame.Pixels[i] = new ConsoleCharacter[height];
                }

                var x = 0;
                var y = 0;
                var lastFg = ConsoleString.DefaultForegroundColor;
                var lastBg = ConsoleString.DefaultBackgroundColor;
                while (reader.CanAdvance(skipWhitespace:true))
                {
                    reader.Expect("[", skipWhiteSpace:true);
                    var next = reader.Advance();
                    var match = ColorSpecifierRegex.Match(next.Value);
                    if (match.Success)
                    {  
                        var isForeground = match.Groups["ForB"].Value == "F";

                        if (isForeground)
                        {

                            if(Enum.TryParse<ConsoleColor>(match.Groups["color"].Value, out ConsoleColor c))
                            {
                                lastFg = c;
                            }
                            else if(RGB.TryParse(match.Groups["color"].Value, out lastFg) == false)
                            {
                                throw new ArgumentException($"Expected a color @ {reader.Position}");
                            }
                        }
                        else
                        {
                            if (Enum.TryParse<ConsoleColor>(match.Groups["color"].Value, out ConsoleColor c))
                            {
                                lastBg = c;
                            }
                            else if (RGB.TryParse(match.Groups["color"].Value, out lastBg) == false)
                            {
                                throw new ArgumentException($"Expected a color @ {reader.Position}");
                            }
                        }
                    }
                    else
                    {
                        char? nextChar = next.Value.Length == 1 ? next.Value[0] : next.Value == "OB" ? '[' : next.Value == "CB" ? ']' : new char?();
                        if (nextChar.HasValue == false) throw new FormatException($"Unexpected token {nextChar} @ {next.Position}");
                        rawFrame.Pixels[x][y++] = new ConsoleCharacter(nextChar.Value, lastFg, lastBg);
                        if (y == height)
                        {
                            y = 0;
                            x++;
                        }
                    }

                    reader.Expect("]");
                }

                return rawFrame;
            }
        }
    }
}
