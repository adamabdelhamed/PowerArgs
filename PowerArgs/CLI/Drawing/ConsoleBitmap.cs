using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs.Cli
{
    public class ConsoleBitmap : Rectangular
    {
        private const double DrawPrecision = .5;

        private object _syncLock;

        public object SyncLock
        {
            get
            {
                return _syncLock;
            }
        }


        public ConsoleCharacter Background { get; set; }
        public ConsoleCharacter Pen { get; set; }

        public Rectangle scope { get; private set; }

        public IConsoleProvider Console { get; set; }

        private ConsolePixel[][] pixels;

        public int Top { get; private set; }
        public int Left { get; private set; }

        public bool IsLocked { get; private set; }

        public ConsoleBitmap(int x, int y, int w, int h, ConsoleCharacter? bg = null) : this(new Rectangle(x,y,w,h), bg){}
        
        public ConsoleBitmap(Rectangle bounds, ConsoleCharacter? bg = null)
        {
            _syncLock = new object();
            this.Top = bounds.Y;
            this.Left = bounds.X;

            bounds = new Rectangle(0, 0, bounds.Width, bounds.Height);

            this.Bounds = bounds;
            this.scope = bounds;
            this.Console = ConsoleProvider.Current;
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

        public void Lock()
        {
            IsLocked = true;
        }

        public void Unlock(bool paint = true)
        {
            IsLocked = false;
            Paint();
        }

        public IDisposable GetDisposableLock()
        {
            return new PaintOnceContext(this);
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

        public void DrawPoint(int x, int y)
        {
            if(IsInScope(x,y))
            {
                pixels[x][y].Value = Pen;
            }
        }

        public void DrawString(string str, int x, int y, bool vert = false)
        {
            DrawString(new ConsoleString(str), x, y, vert);
        }

        public void DrawString(ConsoleString str, int x, int y, bool vert = false)
        {
            var xStart = scope.X + x;
            x = scope.X + x;
            y = scope.Y + y;
            foreach (var character in str)
            {
                if(character.Value == '\n')
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


        public void Paint()
        {
            lock (SyncLock)
            {
                if (IsLocked)
                {
                    return;
                }

                for (int y = scope.Y; y < scope.Y + scope.Height; y++)
                {
                    for (int x = scope.X; x < scope.X + scope.Width; x++)
                    {
                        var pixel = pixels[x][y];
                        if (pixel.HasChanged)
                        {
                            if (pixel.Value.HasValue)
                            {
                                DrawPixel(x, y, pixel, pixel.Value.Value);
                            }
                            else
                            {
                                DrawPixel(x, y, null, Background);
                            }
                        }
                    }
                }
            }
        }

        private void DrawPixel(int x, int y, ConsolePixel pixel, ConsoleCharacter value)
        {
            x = Left + x;
            y = Top + y;

            if (Console.CursorLeft != x)
            {
                Console.CursorLeft = x;
            }

            if (Console.CursorTop != y)
            {
                Console.CursorTop = y;
            }

            if (Console.ForegroundColor != value.ForegroundColor)
            {
                Console.ForegroundColor = value.ForegroundColor;
            }

            if (Console.BackgroundColor != value.BackgroundColor)
            {
                Console.BackgroundColor = value.BackgroundColor;
            }

            Console.Write(value.Value);

            if(pixel != null)
            {
                pixel.Sync();
            }
        }
    }

    public class PaintOnceContext : IDisposable
    {
        private ConsoleBitmap bitmap;
        public PaintOnceContext(ConsoleBitmap bitmap)
        {
            this.bitmap = bitmap;
            bitmap.Lock();
        }

        ~PaintOnceContext()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                if (bitmap != null)
                {
                    bitmap.Unlock();
                    bitmap = null;
                }
            }
        }
    }
}
