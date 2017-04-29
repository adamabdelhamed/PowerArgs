using System;
using System.Text;
using System.Text.RegularExpressions;

namespace PowerArgs.Cli
{
    internal class ConsoleBitmapFrameSerializer
    {
        private Tokenizer<Token> tokenizer;

        public ConsoleBitmapFrameSerializer()
        {
            tokenizer = new Tokenizer<Token>();
            tokenizer.WhitespaceBehavior = WhitespaceBehavior.Include;
            tokenizer.Delimiters.Add("[");
            tokenizer.Delimiters.Add("]");
        }

        public string SerializeFrame(ConsoleBitmapRawFrame frame)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append($"[{frame.Timestamp.Ticks}]");
            builder.Append("[Raw]");
            ConsoleColor? lastFg = null;
            ConsoleColor? lastBg = null;
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

        public string SerializeFrame(ConsoleBitmapDiffFrame frame)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append($"[{frame.Timestamp.Ticks}]");
            builder.Append("[Diff]");

            ConsoleColor? lastFg = null;
            ConsoleColor? lastBg = null;

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

                ConsoleColor lastBackground = ConsoleString.DefaultBackgroundColor;
                ConsoleColor lastForeground = ConsoleString.DefaultForegroundColor;
                while (reader.CanAdvance())
                {
                    reader.Expect("[");
                    if (reader.Peek().Value.StartsWith("F=") || reader.Peek().Value.StartsWith("B="))
                    {
                        reader.Advance();
                        var match = Regex.Match(reader.Current.Value, @"(?<ForB>F|B)=(?<color>\w+)");
                        if (match.Success == false) throw new FormatException($"Unexpected token {reader.Current.Value} at position {reader.Current.Position} ");

                        var isForeground = match.Groups["ForB"].Value == "F";

                        if (isForeground)
                        {
                            lastForeground = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), match.Groups["color"].Value);
                        }
                        else
                        {
                            lastBackground = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), match.Groups["color"].Value);
                        }

                        reader.Expect("]");
                    }
                    else
                    {
                        var match = Regex.Match(reader.Advance().Value, "(?<x>\\d+),(?<y>\\d+),(?<val>.+)");
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
                while (reader.CanAdvance())
                {
                    reader.Expect("[");
                    var next = reader.Advance();
                    var match = Regex.Match(next.Value, @"(?<ForB>F|B)=(?<color>\w+)");
                    if (match.Success)
                    {  
                        var isForeground = match.Groups["ForB"].Value == "F";

                        if (isForeground)
                        {
                            lastFg = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), match.Groups["color"].Value);
                        }
                        else
                        {
                            lastBg = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), match.Groups["color"].Value);
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
