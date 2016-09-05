using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArgsTests.CLI
{
    using PowerArgs;
    using PowerArgs.Cli;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    namespace ArgsTests.CLI
    {
        public class WinFormsTestConsole : System.Windows.Forms.Control, IConsoleProvider
        {
            public ConsoleColor BackgroundColor { get; set; }
            public int BufferWidth { get; set; }

            public int CursorLeft { get; set; }

            public int CursorTop { get; set; }

            public ConsoleColor ForegroundColor { get; set; }

            public CliKeyboardInputQueue Input { get; } = new CliKeyboardInputQueue(true);
            private ConsoleBitmap buffer;

            private Bitmap offsreenBuffer;
            private Bitmap onScreenBuffer;

            private Graphics onScreenGraphics;
            private Graphics offScreenGraphics;

            public bool KeyAvailable
            {
                get
                {
                    return Input.KeyAvailable;
                }
            }

            public int WindowHeight { get; set; }

            private System.Drawing.SizeF charSize;

            public static void Run(ConsoleApp app, WinFormsTestConsole console, Action input)
            {
                var form = new Form();
                form.BackColor = Color.Black;
                form.Width = console.Width;
                form.Height = console.Height;
                var oldProvider = ConsoleProvider.Current;
                form.Controls.Add(console);
                app.Stopped.SubscribeForLifetime(() => { Application.Exit(); }, app.LifetimeManager);
                var task = app.Start();

                Task.Factory.StartNew(input);

                Application.Run(form);
            }

            public WinFormsTestConsole(int w, int h)
            {
                this.BufferWidth = w;
                this.WindowHeight = h;

                buffer = new ConsoleBitmap(0, 0, w, h);

                var timer = new System.Windows.Forms.Timer();
                timer.Interval = 100;
                timer.Tick += Timer_Tick;
                timer.Start();

                offsreenBuffer = new Bitmap(1000, 1000);
                onScreenBuffer = new Bitmap(1000, 1000);

                onScreenGraphics = Graphics.FromImage(onScreenBuffer);
                offScreenGraphics = Graphics.FromImage(offsreenBuffer);
            
                this.Font = new Font("Consolas", 12);
                this.charSize = onScreenGraphics.MeasureString("1", Font);
            }
            

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.DrawImage(onScreenBuffer, new PointF(0, 0));
            }

            private void Timer_Tick(object sender, EventArgs e)
            {
                offScreenGraphics.FillRectangle(Brushes.Black, new RectangleF(0, 0, offsreenBuffer.Width, offsreenBuffer.Height));
                for (int y = 0; y < buffer.Height-1; y++)
                {
                    for (int x = 0; x < buffer.Width; x++)
                    {
                        var pixel = buffer.GetPixel(x, y);
                        ConsoleCharacter c = pixel.Value.HasValue ? pixel.Value.Value : new ConsoleCharacter(' ',this.ForegroundColor, this.BackgroundColor);

                        var fg = (Color)typeof(Color).GetProperty(c.ForegroundColor.ToString(), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).GetValue(null);
                        var bg = (Color)typeof(Color).GetProperty(c.BackgroundColor.ToString(), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).GetValue(null);
                        float imgX = x * charSize.Width;
                        float imgY = y * charSize.Height;
                        offScreenGraphics.FillRectangle(new SolidBrush(bg), imgX, imgY, charSize.Width, charSize.Height);
                        offScreenGraphics.DrawString(c.ToString(), Font, new SolidBrush(fg), new PointF(imgX, imgY));
                    }
                }

                var tempImg = onScreenBuffer;
                var tempGraphics = onScreenGraphics;

                onScreenBuffer = offsreenBuffer;
                onScreenGraphics = offScreenGraphics;

                offsreenBuffer = tempImg;
                offScreenGraphics = tempGraphics;
                this.Invalidate();
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public int Read()
            {
                throw new NotImplementedException();
            }

            public ConsoleKeyInfo ReadKey()
            {
                return Input.ReadKey();
            }

            public ConsoleKeyInfo ReadKey(bool intercept)
            {
                return ReadKey();
            }

            public string ReadLine()
            {
                throw new NotImplementedException();
            }

            public void Write(ConsoleCharacter consoleCharacter)
            {
                buffer.Pen = consoleCharacter;
                buffer.DrawPoint(CursorLeft, CursorTop);

                if (CursorLeft == BufferWidth - 1)
                {
                    CursorLeft = 0;
                    CursorTop++;
                }
                else
                {
                    CursorLeft++;
                }
            }

            public void Write(ConsoleString consoleString)
            {
                foreach (var c in consoleString)
                {
                    Write(c);
                }
            }

            public void Write(object output)
            {
                Write(((string)(output == null ? "" : output.ToString())).ToConsoleString(ForegroundColor, BackgroundColor));
            }

            public void WriteLine()
            {
                CursorTop++;
            }

            public void WriteLine(ConsoleString consoleString)
            {
                Write(consoleString);
                WriteLine();
            }

            public void WriteLine(object output)
            {
                Write(output);
                WriteLine();
            }
        }
    }

}
