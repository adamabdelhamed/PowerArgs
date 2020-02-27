using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PowerArgs
{
    /// <summary>
    /// An effect that makes all command line output get written in a green themed, futuristic fasion.   
    /// </summary>
    public static class SimulatedHumanOutputMode
    {
        /// <summary>
        /// Starts simulated human output mode.
        /// </summary>
        /// <returns>A lifetime that you can dispose when you want to stop the effect.</returns>
        public static Lifetime Start()
        {
            var writer = new SimulatedHumanOutputWriter();
            var lt = new Lifetime();
            Task t = new Task(() =>
            {
                Thread.CurrentThread.IsBackground = false;
                writer.Loop();
            });
            lt.OnDisposed(writer.Cancel);
            t.Start();
            Thread.Sleep(100);
            return lt;
        }
    }

    internal class SimulatedHumanOutputWriter : TextWriter
    {
        TextWriter wrapped;
        Queue<char> chars = new Queue<char>();
        bool cancelled;

        public SimulatedHumanOutputWriter()
        {
            wrapped = Console.Out;
        }

        public override Encoding Encoding
        {
            get { return Encoding.Default; }
        }

        public override void Write(char value)
        {
            Write("" + value);
        }

        public override void Write(string value)
        {
            lock (chars)
            {
                foreach (char c in value) chars.Enqueue(c);
            }
        }

        public override void WriteLine()
        {
            WriteLine("");
        }

        public override void WriteLine(string value)
        {
            Write(value + "\n");
        }

        public void Cancel()
        {
            cancelled = true;
        }

        public void Loop()
        {
            Console.SetOut(this);
            Random r = new Random();
            Console.CursorVisible = false;
            Stopwatch sw = new Stopwatch();
            double sleep = 30;
            sw.Start();

            ConsoleColor resetFG = Console.ForegroundColor, resetBG = Console.BackgroundColor;
            try
            {
                char last = ' ';
                while (true)
                {
                    if (Console.CursorLeft < Console.BufferWidth - 1)
                    {
                        Console.BackgroundColor = ConsoleColor.Green;
                        wrapped.Write(' ');
                        if (Console.CursorLeft > 0) Console.CursorLeft--;
                    }
                    Console.BackgroundColor = resetBG;

                    while (sw.ElapsedMilliseconds < sleep) ;
                    sleep = sleep * .97;

                    lock (chars)
                    {
                        if (chars.Count > 0)
                        {
                            var c = chars.Dequeue();
                            if (c == '\r') continue;
                            if (sleep < 6)
                            {
                                Thread.Sleep(r.Next(20, 100));
                                sleep = 30;
                            }

                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                            if (Console.CursorLeft < Console.BufferWidth - 1)
                            {
                                wrapped.Write(' ');
                                Console.CursorLeft--;
                            }
                            wrapped.Write(c);
                            last = c;
                        }
                        else if (cancelled)
                        {
                            break;
                        }
                    }

                    sw.Restart();
                }
            }
            finally
            {
                Console.ForegroundColor = resetFG;
                Console.BackgroundColor = resetBG;
            }
        }
    }
}
