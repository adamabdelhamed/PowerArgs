using PowerArgs;
using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ArgsTests.CLI
{
    public class CliUnitTestConsole : IConsoleProvider
    {
        public ConsoleColor BackgroundColor { get; set; }

        public int BufferWidth { get; set; }

        public int WindowHeight { get; set; }

        public int CursorLeft { get; set; }

        public int CursorTop { get; set; }

        public ConsoleColor ForegroundColor { get; set; }

        public Queue<ConsoleKeyInfo> InputQueue { get; private set; }

        public ConsoleBitmap Buffer { get; private set; }

        int w, h;
        public CliUnitTestConsole(int w = 80, int h = 80)
        {
            this.w = w;
            this.h = h;
            Clear();
        }

        public bool KeyAvailable
        {
            get
            {
                return InputQueue.Count > 0;
            }
        }

        public void Clear()
        {
            Buffer = new ConsoleBitmap(0, 0, w, h);
            InputQueue = new Queue<ConsoleKeyInfo>();
            this.BufferWidth = Buffer.Width;
            this.CursorLeft = 0;
            this.CursorTop = 0;
        }

        public int Read()
        {
            return (int)ReadKey().KeyChar;
        }

        public ConsoleKeyInfo ReadKey()
        {
            while (KeyAvailable == false)
            {
                Thread.Sleep(10);
            }

            return InputQueue.Dequeue();
        }

        public ConsoleKeyInfo ReadKey(bool intercept)
        {
            return ReadKey();
        }

        public string ReadLine()
        {
            string ret = null;
            
            while(true)
            {
                var key = ReadKey();
                if(key.KeyChar == '\r' || key.KeyChar == '\n')
                {
                    return ret;
                }
                else
                {
                    ret += key.KeyChar;
                }
            }
        }

        public void Write(ConsoleCharacter consoleCharacter)
        {
            Buffer.Pen = consoleCharacter;
            Buffer.DrawPoint(CursorLeft, CursorTop);

            if(CursorLeft == BufferWidth - 1)
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
            Write(((string)(output == null ? "" : output.ToString())).ToConsoleString());
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
