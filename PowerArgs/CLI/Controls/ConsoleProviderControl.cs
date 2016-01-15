using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class ConsoleProviderControl : ConsoleControl, IConsoleProvider
    {
        private ConsoleBitmap buffer;

        public ConsoleColor BackgroundColor {get; set;}

        public int BufferWidth
        {
            get
            {
                return this.Width;
            }

            set
            {
                this.Width = value;
            }
        }

        public int WindowHeight
        {
            get
            {
                return this.Height;
            }

            set
            {
                this.Height = value;
            }
        }

        public int CursorLeft
        {
            get; set;
        }

        public int CursorTop
        {
            get; set;
        }

        public ConsoleColor ForegroundColor
        {
            get; set;
        }

        public ConsoleProviderControl()
        {
            this.buffer = new ConsoleBitmap(Bounds, new ConsoleCharacter(' ', null, this.Background));
            this.PropertyChanged += Resizer;
            this.PropertyChanged += BGChangeHandler;
        }

        internal override void OnPaint(ConsoleBitmap context)
        {
            for (int x = 0; x < buffer.Width; x++)
            {
                if (x >= context.Width)
                {
                    continue;
                }

                for (int y = 0; y < buffer.Height; y++)
                {
                 if(y >= context.Height)
                    {
                        continue;
                    }
                    var pixel = buffer.GetPixel(x, y);
                    if (pixel.Value.HasValue)
                    {
                        context.Pen = pixel.Value.Value;
                        context.DrawPoint(x, y);
                    }
                }
            }
        }

        private void BGChangeHandler(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(Background)) return;
            buffer.Background = this.BackgroundCharacter;
        }

        private void Resizer(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(Bounds)) return;

            if (Bounds.Equals(buffer.Bounds)) return;

            var newBuffer = new ConsoleBitmap(Bounds, this.BackgroundCharacter);

            for(int x = 0; x < buffer.Width; x++)
            {
                for(int y = 0; y < buffer.Height; y++)
                {
                    var pixel = buffer.GetPixel(x, y);
                    if (pixel.Value.HasValue)
                    {
                        newBuffer.Pen = pixel.Value.Value;
                        newBuffer.DrawPoint(x, y);
                    }
                }
            }

            buffer = newBuffer;
        }

 


        public void Clear()
        {
            buffer = new ConsoleBitmap(Bounds, this.BackgroundCharacter);
        }

       

        public void Write(ConsoleCharacter consoleCharacter)
        {
            buffer.Pen = consoleCharacter;
            buffer.DrawPoint(CursorLeft, CursorTop);
            CursorLeft++;
            if(CursorLeft >= BufferWidth)
            {
                CursorLeft = 0;
                CursorTop++;
            }
        }

        public void Write(ConsoleString consoleString)
        {
            foreach(var c in consoleString)
            {
                Write(c);
            }
        }

        public void Write(object output)
        {
            output = output ?? "";
            Write(output.ToString().ToConsoleString());
        }

        public void WriteLine()
        {
            CursorLeft = 0;
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

        public bool KeyAvailable
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int Read()
        {
            throw new NotImplementedException();
        }

        public ConsoleKeyInfo ReadKey()
        {
            throw new NotImplementedException();
        }

        public ConsoleKeyInfo ReadKey(bool intercept)
        {
            throw new NotImplementedException();
        }

        public string ReadLine()
        {
            throw new NotImplementedException();
        }
    }
}
