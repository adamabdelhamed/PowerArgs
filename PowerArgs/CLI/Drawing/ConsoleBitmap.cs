using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs.Cli
{
    public class ConsoleBitmap : Rectangular
    {
        private const double DrawPrecision = .5;

        public ConsoleCharacter Background { get; set; }
        public ConsoleCharacter Pen { get; set; }

        public Rectangle scope { get; private set; }

        public IConsoleProvider Console { get; set; }

        private ConsolePixel[][] pixels;

        private int lastBufferWidth;

        public int Top { get; private set; }
        public int Left { get; private set; }

        public ConsoleBitmap(int x, int y, int w, int h, ConsoleCharacter? bg = null) : this(new Rectangle(x, y, w, h), bg) { }
        public ConsoleBitmap(int w, int h, ConsoleCharacter? bg = null) : this(new Rectangle(0, 0, w, h), bg) { }

        public ConsoleBitmap(Rectangle bounds, ConsoleCharacter? bg = null)
        {
            this.Top = bounds.Y;
            this.Left = bounds.X;

            bounds = new Rectangle(0, 0, bounds.Width, bounds.Height);

            this.Bounds = bounds;
            this.scope = bounds;
            this.Console = ConsoleProvider.Current;
            this.lastBufferWidth = this.Console.BufferWidth;
            this.Background = bg.HasValue ? bg.Value : new ConsoleCharacter(' ');
            this.Pen = new ConsoleCharacter('*');
            pixels = new ConsolePixel[this.Width][];
            for (int x = 0; x < this.Width; x++)
            {
                pixels[x] = new ConsolePixel[this.Height];
                for (int y = 0; y < pixels[x].Length; y++)
                {
                    pixels[x][y] = new ConsolePixel() { Value = bg };
                }
            }
        }

        /// <summary>
        /// Converts this ConsoleBitmap to a ConsoleString
        /// </summary>
        /// <param name="trimMode">if false (the default), unformatted whitespace at the end of each line will be included as whitespace in the return value. If true, that whitespace will be trimmed from the return value.</param>
        /// <returns>the bitmap as a ConsoleString</returns>
        public ConsoleString ToConsoleString(bool trimMode = false)
        {
            List<ConsoleCharacter> chars = new List<ConsoleCharacter>();
            for (var y = 0; y < this.Height; y++)
            {
                for (var x = 0; x < this.Width; x++)
                {
                    if (trimMode && IsRestOfLineWhitespaceWithDefaultBackground(x, y))
                    {
                        break;
                    }
                    else
                    {
                        var pixel = this.GetPixel(x, y);
                        var pixelValue = pixel.Value.HasValue ? pixel.Value.Value : new ConsoleCharacter(' ');
                        chars.Add(pixelValue);
                    }
                }
                if (y < this.Height - 1)
                {
                    chars.Add(new ConsoleCharacter('\n'));
                }
            }

            return new ConsoleString(chars);
        }

        private bool IsRestOfLineWhitespaceWithDefaultBackground(int xStart, int y)
        {
            var defaultBg = new ConsoleCharacter(' ').BackgroundColor;

            for (var x = xStart; x < this.Width; x++)
            {
                if (this.GetPixel(x, y).Value.HasValue == false)
                {
                    // this is whitespace
                }
                else if (char.IsWhiteSpace(this.GetPixel(x, y).Value.Value.Value) && this.GetPixel(x, y).Value.Value.BackgroundColor == defaultBg)
                {
                    // this is whitespace
                }
                else
                {
                    return false;
                }
            }

            return true;
        }


        // todo - is rectangular, but if you try to set width and height there will be inconsistent state
        public void Resize(int w, int h)
        {
            var newPixels = new ConsolePixel[w][];
            for (int x = 0; x < w; x++)
            {
                newPixels[x] = new ConsolePixel[h];
                for (int y = 0; y < newPixels[x].Length; y++)
                {
                    if (IsInBounds(x, y))
                    {
                        newPixels[x][y] = pixels[x][y];
                    }
                    else
                    {
                        newPixels[x][y] = new ConsolePixel() { Value = Background };
                    }
                }
            }

            pixels = newPixels;
            this.Size = new Size(w, h);
            this.Scope(Bounds);
            this.Invalidate();
        }

        public ConsolePixel GetPixel(int x, int y)
        {
            return pixels[x][y];
        }

        public ConsoleSnapshot CreateSnapshot()
        {
            var snapshot = new ConsoleSnapshot(Left, Top, Console);
            return snapshot;
        }

        internal ConsoleWiper CreateWiper()
        {
            var wiper = new ConsoleWiper(CreateSnapshot());
            wiper.Bottom = wiper.Top + Height + 1;
            return wiper;
        }

        public Rectangle GetScope()
        {
            return this.scope;
        }

        public void Scope(Rectangle bounds)
        {
            this.scope = bounds;
        }

        public void Rescope(int xIncrement, int yIncrement, int w, int h)
        {
            w = Math.Min(w, scope.Width - xIncrement);
            h = Math.Min(h, scope.Height - yIncrement);

            scope = new Rectangle(scope.X + xIncrement, scope.Y + yIncrement, w, h);
        }

        private bool IsInScope(int x, int y)
        {
            if (x < 0 || x >= Width) return false;
            if (y < 0 || y >= Height) return false;
            return scope.Contains(x, y);
        }

        private bool IsInBounds(int x, int y)
        {
            if (x < 0 || x >= Width) return false;
            if (y < 0 || y >= Height) return false;
            return true;
        }


        public void DrawString(string str, int x, int y, bool vert = false)
        {
            DrawString(new ConsoleString(str), x, y, vert);
        }

        public void FillRect(int x, int y, int w, int h)
        {
            for (int xd = x; xd <= x + w; xd++)
            {
                DrawLine(xd, y, xd, y + h);
            }
        }

        public void DrawRect(int x, int y, int w, int h)
        {
            DrawLine(x, y, x, y + h);                       // Left, vertical line
            DrawLine(x + w - 1, y, x + w - 1, y + h);       // Right, vertical line
            DrawLine(x, y, x + w, y);                       // Top, horizontal line
            DrawLine(x, y + h - 1, x + w, y + h - 1);       // Bottom, horizontal line
        }

        public void DrawString(ConsoleString str, int x, int y, bool vert = false)
        {
            var xStart = scope.X + x;
            x = scope.X + x;
            y = scope.Y + y;
            foreach (var character in str)
            {
                if (character.Value == '\n')
                {
                    y++;
                    x = xStart;
                }
                else if (IsInScope(x, y))
                {
                    pixels[x][y].Value = character;
                    if (vert) y++;
                    else x++;
                }
            }
        }

        public void DrawPoint(int x, int y)
        {
            x = scope.X + x;
            y = scope.Y + y;

            if (IsInScope(x, y))
            {
                pixels[x][y].Value = Pen;
            }
        }
        public void DrawLine(int x1, int y1, int x2, int y2)
        {
            x1 = scope.X + x1;
            y1 = scope.Y + y1;

            x2 = scope.X + x2;
            y2 = scope.Y + y2;

            if (x1 == x2)
            {
                int yMin = y1 >= y2 ? y2 : y1;
                int yMax = y1 >= y2 ? y1 : y2;
                for (int y = yMin; y < yMax; y++)
                {
                    if (IsInScope(x1, y))
                    {
                        pixels[x1][y].Value = Pen;
                    }
                }
            }
            else if (y1 == y2)
            {
                int xMin = x1 >= x2 ? x2 : x1;
                int xMax = x1 >= x2 ? x1 : x2;
                for (int x = xMin; x < xMax; x++)
                {
                    if (IsInScope(x, y1))
                    {
                        pixels[x][y1].Value = Pen;
                    }
                }
                return;
            }
            else
            {
                double slope = ((double)y2 - y1) / ((double)x2 - x1);

                int dx = Math.Abs(x1 - x2);
                int dy = Math.Abs(y1 - y2);

                if (dy > dx)
                {
                    for (double x = x1; x < x2; x += DrawPrecision)
                    {
                        double y = slope + (x - x1) + y1;
                        int xInt = (int)x;
                        int yInt = (int)y;
                        if (IsInScope(xInt, yInt))
                        {
                            pixels[xInt][yInt].Value = Pen;
                        }
                    }

                    for (double x = x2; x < x1; x += DrawPrecision)
                    {
                        double y = slope + (x - x1) + y1;
                        int xInt = (int)x;
                        int yInt = (int)y;
                        if (IsInScope(xInt, yInt))
                        {
                            pixels[xInt][yInt].Value = Pen;
                        }
                    }
                }
                else
                {
                    for (double y = y1; y < y2; y += DrawPrecision)
                    {
                        double x = ((y - y1) / slope) + x1;
                        int xInt = (int)x;
                        int yInt = (int)y;
                        if (IsInScope(xInt, yInt))
                        {
                            pixels[xInt][yInt].Value = Pen;
                        }
                    }

                    for (double y = y2; y < y1; y += DrawPrecision)
                    {
                        double x = ((y - y1) / slope) + x1;
                        int xInt = (int)x;
                        int yInt = (int)y;
                        if (IsInScope(xInt, yInt))
                        {
                            pixels[xInt][yInt].Value = Pen;
                        }
                    }
                }
            }
        }

        public ConsoleBitmap Clone()
        {
            var ret = new ConsoleBitmap(this.Bounds, this.Background);
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    ret.Pen = this.GetPixel(x, y).Value.HasValue ? this.GetPixel(x, y).Value.Value : new ConsoleCharacter(' ');
                    ret.DrawPoint(x, y);
                }
            }
            return ret;
        }

        private class Chunk
        {
            public List<ConsoleCharacter> Characters { get; set; } = new List<ConsoleCharacter>();
            public bool HasChanged { get; set; }
        }

        public void Paint()
        {
            var changed = false;
            if (lastBufferWidth != this.Console.BufferWidth)
            {
                lastBufferWidth = this.Console.BufferWidth;
                Invalidate();
                this.Console.Clear();
                changed = true;
            }
            try
            {
                var currentChunk = new Chunk();
                var chunksOnLine = new List<Chunk>();
                for (int y = scope.Y; y < scope.Y + scope.Height; y++)
                {
                    var changeOnLine = false;
                    for (int x = scope.X; x < scope.X + scope.Width; x++)
                    {
                        var pixel = pixels[x][y];
                        changeOnLine = changeOnLine || pixel.HasChanged;
                        var val = pixel.Value.HasValue ? pixel.Value.Value.Value : ' ';
                        var fg = pixel.Value.HasValue ? pixel.Value.Value.ForegroundColor : ConsoleString.DefaultForegroundColor;
                        var bg = pixel.Value.HasValue ? pixel.Value.Value.BackgroundColor : ConsoleString.DefaultBackgroundColor;
                        var character = pixel.Value.HasValue ? pixel.Value.Value : new ConsoleCharacter(val, fg, bg);
                        if (currentChunk.Characters.Count == 0)
                        {
                            // first pixel always gets added to the current empty chunk
                            currentChunk.HasChanged = pixel.HasChanged;
                            currentChunk.Characters.Add(character);
                        }
                        else if (currentChunk.HasChanged == false && pixel.HasChanged == false)
                        {
                            // characters that have not changed get chunked even if their styles differ
                            currentChunk.Characters.Add(character);
                        }
                        else if (currentChunk.HasChanged && pixel.HasChanged && fg == currentChunk.Characters[0].ForegroundColor && bg == currentChunk.Characters[0].BackgroundColor)
                        {
                            // characters that have changed only get chunked if their styles match to minimize the number of writes
                            currentChunk.Characters.Add(character);
                        }
                        else
                        {
                            // either the styles of consecutive changing characters differ or we've gone from a non changed character to a changed one
                            // in either case we end the current chunk and start a new one
                            chunksOnLine.Add(currentChunk);
                            currentChunk = new Chunk();
                            currentChunk.HasChanged = pixel.HasChanged;
                            currentChunk.Characters.Add(character);
                        }
                        pixel.Sync();
                    }

                    if (currentChunk.Characters.Count > 0)
                    {
                        chunksOnLine.Add(currentChunk);
                        currentChunk = new Chunk();
                    }

                    if (changeOnLine)
                    {
                        Console.CursorTop = y; // we know there will be a change on this line so move the cursor top
                        var left = scope.X;
                        var leftChanged = true;
                        for (var i = 0; i < chunksOnLine.Count; i++)
                        {
                            var chunk = chunksOnLine[i];
                            if (chunk.HasChanged)
                            {
                                if (leftChanged)
                                {
                                    Console.CursorLeft = left;
                                    leftChanged = false;
                                }

                                Console.ForegroundColor = chunk.Characters[0].ForegroundColor;
                                Console.BackgroundColor = chunk.Characters[0].BackgroundColor;
                                Console.Write((string)(new ConsoleString(chunk.Characters).StringValue));
                                left += chunk.Characters.Count;
                                changed = true;
                            }
                            else
                            {
                                left += chunk.Characters.Count;
                                leftChanged = true;
                            }
                        }
                    }
                    chunksOnLine.Clear();
                }

                if (changed)
                {
                    Console.CursorLeft = Left;
                    Console.CursorTop = Top;
                    Console.ForegroundColor = ConsoleString.DefaultForegroundColor;
                    Console.BackgroundColor = ConsoleString.DefaultBackgroundColor;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                Invalidate();
                Paint();
            }
        }

        public void Invalidate()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var pixel = pixels[x][y];
                    pixel.Invalidate();
                }
            }
        }

        public override string ToString()
        {
            var ret = "";

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    ret += this.pixels[x][y].Value.HasValue ? this.pixels[x][y].Value.Value.Value : ' ';
                }
                if (y < Height - 1)
                {
                    ret += Environment.NewLine;
                }
            }

            return ret;
        }

        public override bool Equals(Object obj)
        {
            var other = obj as ConsoleBitmap;
            if (other == null) return false;

            if (this.Width != other.Width || this.Height != other.Height)
            {
                return false;
            }

            for (var x = 0; x < this.Width; x++)
            {
                for (var y = 0; y < this.Height; y++)
                {
                    var thisVal = this.GetPixel(x, y).Value;
                    var otherVal = other.GetPixel(x, y).Value;

                    if (thisVal.HasValue != otherVal.HasValue) return false;

                    if (thisVal.HasValue && thisVal.Value != otherVal.Value) return false;

                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        // new style methods that don't require you to set the pen before drawing

        public ConsoleBitmap DrawLine(ConsoleCharacter character, int x1, int y1, int x2, int y2)
        {
            var oldPen = this.Pen;
            try
            {
                this.Pen = character;
                DrawLine(x1, y1, x2, y2);
            }
            finally
            {
                this.Pen = oldPen;
            }
            return this;
        }

        public ConsoleBitmap DrawRect(ConsoleCharacter character, int x = 0, int y = 0, int w = -1, int h = -1)
        {
            var oldPen = this.Pen;
            try
            {
                w = w < 0 ? Width : w;
                h = h < 0 ? Height : h;
                this.Pen = character;
                DrawRect(x, y, w, h);
            }
            finally
            {
                this.Pen = oldPen;
            }
            return this;
        }

        public ConsoleBitmap FillRect(ConsoleCharacter character, int x = 0, int y = 0, int w = -1, int h = -1)
        {
            var oldPen = this.Pen;
            try
            {
                w = w < 0 ? Width : w;
                h = h < 0 ? Height : h;
                this.Pen = character;
                FillRect(x, y, w, h);
            }
            finally
            {
                this.Pen = oldPen;
            }
            return this;
        }

        public ConsoleBitmap DrawPoint(ConsoleCharacter character, int x, int y)
        {
            var oldPen = this.Pen;
            try
            {
                this.Pen = character;
                DrawPoint(x, y);
            }
            finally
            {
                this.Pen = oldPen;
            }

            return this;
        }
    }
}
