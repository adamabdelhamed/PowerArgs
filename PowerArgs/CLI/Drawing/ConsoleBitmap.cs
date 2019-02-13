using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A data structure representing a 2d image that can be pained in
    /// a console window
    /// </summary>
    public class ConsoleBitmap
    {
        private const double DrawPrecision = .25;


        /// <summary>
        /// The width of the image, in number of character pixels
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// The height of the image, in number of character pixels
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// The left of the image, in number of character pixels
        /// </summary>
        public int X { get; private set; }

        /// <summary>
        /// The top of the image, in number of character pixels
        /// </summary>
        public int Y { get; private set; }

        /// <summary>
        /// The character to draw when calling the various Draw methods
        /// </summary>
        public ConsoleCharacter Pen { get; set; }

        /// <summary>
        /// The inner rectangle that is used to temporarily
        /// reduce the drawing area. When a scope is set calls to the draw
        /// methods will be scoped to this area
        /// </summary>
        public Rectangle Scope { get; set; }

        /// <summary>
        /// The console to target when the Paint method is called 
        /// </summary>
        public IConsoleProvider Console { get; set; }

        private ConsolePixel[][] pixels;

        private int lastBufferWidth;

        /// <summary>
        /// Creates a new ConsoleBitmap
        /// </summary>
        /// <param name="x">the left offset to use when painting</param>
        /// <param name="y">the top offset to use when painting</param>
        /// <param name="w">the width of the image</param>
        /// <param name="h">the height of the image</param>
        public ConsoleBitmap(int x, int y, int w, int h) : this(new Rectangle(x, y, w, h)) { }

        /// <summary>
        /// Creates a new ConsoleBitmap
        /// </summary>
        /// <param name="w">the width of the image</param>
        /// <param name="h">the height of the image</param>
        public ConsoleBitmap(int w, int h) : this(new Rectangle(0, 0, w, h)) { }

        /// <summary>
        /// Creates a new ConsoleBitmap
        /// </summary>
        /// <param name="bounds">the area of the image</param>
        public ConsoleBitmap(Rectangle bounds)
        {
            bounds = new Rectangle(0, 0, bounds.Width, bounds.Height);

            this.X = bounds.X;
            this.Y = bounds.Y;
            this.Width = bounds.Width;
            this.Height = bounds.Height;
            this.Scope = bounds;
            this.Console = ConsoleProvider.Current;
            this.lastBufferWidth = this.Console.BufferWidth;
            this.Pen = new ConsoleCharacter('*');
            pixels = new ConsolePixel[this.Width][];
            for (int x = 0; x < this.Width; x++)
            {
                pixels[x] = new ConsolePixel[this.Height];
                for (int y = 0; y < pixels[x].Length; y++)
                {
                    pixels[x][y] = new ConsolePixel() { Value = new ConsoleCharacter(' ') };
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

        /// <summary>
        /// Resizes this image, preserving the data in the pixels that remain in the new area
        /// </summary>
        /// <param name="w">the new width</param>
        /// <param name="h">the new height</param>
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
                        newPixels[x][y] = new ConsolePixel() { Value = new ConsoleCharacter(' ') };
                    }
                }
            }

            pixels = newPixels;
            this.Width = w;
            this.Height = h;
            this.Scope = new Rectangle(X,Y,Width,Height);
            this.Invalidate();
        }

        /// <summary>
        /// Gets the pixel at the given location
        /// </summary>
        /// <param name="x">the x coordinate</param>
        /// <param name="y">the y coordinate</param>
        /// <returns>the pixel at the given location</returns>
        public ConsolePixel GetPixel(int x, int y)
        {
            return pixels[x][y];
        }

        /// <summary>
        /// Creates a snapshot of the cursor position
        /// </summary>
        /// <returns>a snapshot of the cursor positon</returns>
        public ConsoleSnapshot CreateSnapshot()
        {
            var snapshot = new ConsoleSnapshot(X, Y, Console);
            return snapshot;
        }

        internal ConsoleWiper CreateWiper()
        {
            var wiper = new ConsoleWiper(CreateSnapshot());
            wiper.Bottom = wiper.Top + Height + 1;
            return wiper;
        }

        /// <summary>
        /// Incrementally adjusts the current scope
        /// </summary>
        /// <param name="xIncrement">the x increment</param>
        /// <param name="yIncrement">the y increment</param>
        /// <param name="w">the width of the scope</param>
        /// <param name="h">the height of the scope</param>
        public void Rescope(int xIncrement, int yIncrement, int w, int h)
        {
            w = Math.Min(w, Scope.Width - xIncrement);
            h = Math.Min(h, Scope.Height - yIncrement);
            Scope = new Rectangle(Scope.X + xIncrement, Scope.Y + yIncrement, w, h);
        }

        private bool IsInScope(int x, int y)
        {
            if (x < 0 || x >= Width) return false;
            if (y < 0 || y >= Height) return false;
            return Scope.Contains(x, y);
        }

        private bool IsInBounds(int x, int y)
        {
            if (x < 0 || x >= Width) return false;
            if (y < 0 || y >= Height) return false;
            return true;
        }

        /// <summary>
        /// Draws the given string onto the bitmap
        /// </summary>
        /// <param name="str">the value to write</param>
        /// <param name="x">the x coordinate to draw the string's fist character</param>
        /// <param name="y">the y coordinate to draw the string's first character </param>
        /// <param name="vert">if true, draw vertically, else draw horizontally</param>
        public void DrawString(string str, int x, int y, bool vert = false)
        {
            DrawString(new ConsoleString(str), x, y, vert);
        }

        /// <summary>
        /// Draws a filled in rectangle bounded by the given coordinates
        /// using the current pen
        /// </summary>
        /// <param name="x">the left of the rectangle</param>
        /// <param name="y">the top of the rectangle</param>
        /// <param name="w">the width of the rectangle</param>
        /// <param name="h">the height of the rectangle</param>
        public void FillRect(int x, int y, int w, int h)
        {
            for (int xd = x; xd <= x + w; xd++)
            {
                DrawLine(xd, y, xd, y + h);
            }
        }

        /// <summary>
        /// Draws an unfilled in rectangle bounded by the given coordinates
        /// using the current pen
        /// </summary>
        /// <param name="x">the left of the rectangle</param>
        /// <param name="y">the top of the rectangle</param>
        /// <param name="w">the width of the rectangle</param>
        /// <param name="h">the height of the rectangle</param>
        public void DrawRect(int x, int y, int w, int h)
        {
            DrawLine(x, y, x, y + h);                       // Left, vertical line
            DrawLine(x + w - 1, y, x + w - 1, y + h);       // Right, vertical line
            DrawLine(x, y, x + w, y);                       // Top, horizontal line
            DrawLine(x, y + h - 1, x + w, y + h - 1);       // Bottom, horizontal line
        }

        /// <summary>
        /// Draws the given string onto the bitmap
        /// </summary>
        /// <param name="str">the value to write</param>
        /// <param name="x">the x coordinate to draw the string's fist character</param>
        /// <param name="y">the y coordinate to draw the string's first character </param>
        /// <param name="vert">if true, draw vertically, else draw horizontally</param>
        public void DrawString(ConsoleString str, int x, int y, bool vert = false)
        {
            var xStart = Scope.X + x;
            x = Scope.X + x;
            y = Scope.Y + y;
            foreach (var character in str)
            {
                if (character.Value == '\n')
                {
                    y++;
                    x = xStart;
                }
                else if (character.Value == '\r')
                {
                    // ignore
                }
                else if (IsInScope(x, y))
                {
                    pixels[x][y].Value = character;
                    if (vert) y++;
                    else x++;
                }
            }
        }

        /// <summary>
        /// Draw a single pixel value at the given point using the current pen
        /// </summary>
        /// <param name="x">the x coordinate</param>
        /// <param name="y">the y coordinate</param>
        public void DrawPoint(int x, int y)
        {
            x = Scope.X + x;
            y = Scope.Y + y;

            if (IsInScope(x, y))
            {
                pixels[x][y].Value = Pen;
            }
        }

        /// <summary>
        /// Draw a line segment between the given points
        /// </summary>
        /// <param name="x1">the x coordinate of the first point</param>
        /// <param name="y1">the y coordinate of the first point</param>
        /// <param name="x2">the x coordinate of the second point</param>
        /// <param name="y2">the y coordinate of the second point</param>
        public void DrawLine(int x1, int y1, int x2, int y2)
        {
            x1 = Scope.X + x1;
            y1 = Scope.Y + y1;

            x2 = Scope.X + x2;
            y2 = Scope.Y + y2;

            foreach (var point in DefineLine(x1, y1, x2, y2))
            {
                if (IsInScope(point.X, point.Y))
                {
                    pixels[point.X][point.Y].Value = Pen;
                }
            }
        }

        /// <summary>
        /// Gets the points that would represent a line between the two given points
        /// </summary>
        /// <param name="x1">the x coordinate of the first point</param>
        /// <param name="y1">the y coordinate of the first point</param>
        /// <param name="x2">the x coordinate of the second point</param>
        /// <param name="y2">the y coordinate of the second point</param>
        /// <returns></returns>
        public static List<Point> DefineLine(int x1, int y1, int x2, int y2)
        {
            var ret = new List<Point>();
            if (x1 == x2)
            {
                int yMin = y1 >= y2 ? y2 : y1;
                int yMax = y1 >= y2 ? y1 : y2;
                for (int y = yMin; y < yMax; y++)
                {
                    ret.Add(new Point(x1, y));
                }
            }
            else if (y1 == y2)
            {
                int xMin = x1 >= x2 ? x2 : x1;
                int xMax = x1 >= x2 ? x1 : x2;
                for (int x = xMin; x < xMax; x++)
                {
                    ret.Add(new Point(x, y1));
                }
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
                        int xInt = (int)Math.Round(x);
                        int yInt = (int)Math.Round(y);
                        ret.Add(new Point(xInt, yInt));
                    }

                    for (double x = x2; x < x1; x += DrawPrecision)
                    {
                        double y = slope + (x - x1) + y1;
                        int xInt = (int)Math.Round(x);
                        int yInt = (int)Math.Round(y);
                        ret.Add(new Point(xInt, yInt));
                    }
                }
                else
                {
                    for (double y = y1; y < y2; y += DrawPrecision)
                    {
                        double x = ((y - y1) / slope) + x1;
                        int xInt = (int)Math.Round(x);
                        int yInt = (int)Math.Round(y);
                        ret.Add(new Point(xInt, yInt));
                    }

                    for (double y = y2; y < y1; y += DrawPrecision)
                    {
                        double x = ((y - y1) / slope) + x1;
                        int xInt = (int)Math.Round(x);
                        int yInt = (int)Math.Round(y);
                        ret.Add(new Point(xInt, yInt));
                    }
                }
            }

            return ret.Distinct().ToList();
        }

        /// <summary>
        /// Makes a copy of this bitmap
        /// </summary>
        /// <returns>a copy of this bitmap</returns>
        public ConsoleBitmap Clone()
        {
            var ret = new ConsoleBitmap(X,Y,Width,Height);
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

        /// <summary>
        /// Paints this image to the current Console
        /// </summary>
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
                for (int y = Scope.Y; y < Scope.Y + Scope.Height; y++)
                {
                    var changeOnLine = false;
                    for (int x = Scope.X; x < Scope.X + Scope.Width; x++)
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
                        var left = Scope.X;
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
                    Console.CursorLeft = X;
                    Console.CursorTop = Y;
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

        /// <summary>
        /// Clears the cached paint state of each pixel so that
        /// all pixels will forcefully be painted the next time Paint
        /// is called
        /// </summary>
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

        /// <summary>
        /// Gets a string representation of this image 
        /// </summary>
        /// <returns>a string representation of this image</returns>
        public override string ToString() => ToConsoleString().ToString();

        /// <summary>
        /// Returns true if the given object is a ConsoleBitmap with
        /// equivalent values as this bitmap, false otherwise
        /// </summary>
        /// <param name="obj">the object to compare</param>
        /// <returns>true if the given object is a ConsoleBitmap with
        /// equivalent values as this bitmap, false otherwise</returns>
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

        /// <summary>
        /// Gets a hashcode for this bitmap
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        // new style methods that don't require you to set the pen before drawing

        /// <summary>
        /// Draws a line between the two points using the given character as a pen
        /// </summary>
        /// <param name="character">the temporary pen</param>
        /// <param name="x1">the x coordinate of the first point</param>
        /// <param name="y1">the y coordinate of the first point</param>
        /// <param name="x2">the x coordinate of the second point</param>
        /// <param name="y2">the y coordinate of the second point</param>
        /// <returns>this ConsoleBitmap</returns>
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

        /// <summary>
        /// Draws an unfilled rectangle at the given coordinates using the specified character
        /// as a temporary pen
        /// </summary>
        /// <param name="character">the temporary character</param>
        /// <param name="x">the left of the rectangle</param>
        /// <param name="y">the top of the rectangle</param>
        /// <param name="w">the width of the rectangle</param>
        /// <param name="h">the height of the rectangle</param>
        /// <returns>this ConsoleBitmap</returns>
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

        /// <summary>
        /// Draws a filled rectangle at the given coordinates using the specified character
        /// as a temporary pen
        /// </summary>
        /// <param name="character">the temporary character</param>
        /// <param name="x">the left of the rectangle</param>
        /// <param name="y">the top of the rectangle</param>
        /// <param name="w">the width of the rectangle</param>
        /// <param name="h">the height of the rectangle</param>
        /// <returns>this ConsoleBitmap</returns>
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

        /// <summary>
        /// Draws a single pixel at the given coordinates using the specified character
        /// as a temporary pen
        /// </summary>
        /// <param name="character">the temporary pen</param>
        /// <param name="x">the x coordinate</param>
        /// <param name="y">the y coordinate</param>
        /// <returns>this ConsoleBitmap</returns>
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
