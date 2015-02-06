using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace PowerArgs
{
    /// <summary>
    ///  A utility that can be used to drive input to Console.In programatically.
    /// </summary>
    public class ConsoleInDriver : TextReader
    {
        private static Lazy<ConsoleInDriver> _driver = new Lazy<ConsoleInDriver>(() => new ConsoleInDriver());

        private Queue<char> input;

        /// <summary>
        /// Gets a value indicating whether or not the driver is currently attached to Console.In
        /// </summary>
        public bool IsAttached { get; private set; }

        /// <summary>
        /// Gets the singleton instance of the driver
        /// </summary>
        public static ConsoleInDriver Instance
        {
            get
            {
                return _driver.Value;
            }
        }

        /// <summary>
        /// Attaches the drives to Console.In
        /// </summary>
        public void Attach()
        {
            if (IsAttached) return;
            lock(input)
            {
                input.Clear();
            }
            Console.SetIn(this);
            IsAttached = true;
        }

        /// <summary>
        /// Detaches the driver from Console.In and reopens and reconnects the standard input stream.
        /// </summary>
        public void Detach()
        {
            if (IsAttached == false) return;

            IsAttached = false;
            Stream s = Console.OpenStandardInput();
            Console.SetIn(new StreamReader(s));

        }

        private ConsoleInDriver() 
        {
            input = new Queue<char>();
        }

        /// <summary>
        /// Drives a string of text into Console.In
        /// </summary>
        /// <param name="s">the string to drive</param>
        public void Drive(string s)
        {
            if (IsAttached == false) throw new InvalidOperationException("Detached");
            lock (input)
            {
                foreach (var c in s)
                {
                    input.Enqueue(c);
                }
            }
        }
        
        /// <summary>
        /// Drives a string of text into Console.In, followed by a newline character
        /// </summary>
        /// <param name="s">the string to drive</param>
        public void DriveLine(string s = null)
        {
            if (IsAttached == false) throw new InvalidOperationException("Detached");
            s = s ?? string.Empty;
            s += '\n';
            Drive(s);
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <returns></returns>
        public override int Peek()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads the next char from the driver as an int.
        /// </summary>
        /// <returns>the next char from the driver as an int.</returns>
        public override int Read()
        {
            while (true)
            {
                char? next = null;
                lock (input)
                {
                    if (input.Count > 0)
                    {
                        next = input.Dequeue();
                    }
                }

                if (next.HasValue == false)
                {
                    Thread.Sleep(10);
                    continue;
                }
                else
                {
                    return (int)next.Value;
                }
            }
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="buffer">Not implemented</param>
        /// <param name="index">Not implemented</param>
        /// <param name="count">Not implemented</param>
        /// <returns>Not implemented</returns>
        public override int Read(char[] buffer, int index, int count)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <param name="buffer">Not implemented</param>
        /// <param name="index">Not implemented</param>
        /// <param name="count">Not implemented</param>
        /// <returns>Not implemented</returns>
        public override int ReadBlock(char[] buffer, int index, int count)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// reads a line of input from the driver
        /// </summary>
        /// <returns>a line of input from the driver</returns>
        public override string ReadLine()
        {
            string ret = "";
            while (true)
            {
                var read = (char)Read();
                if (read == '\r' || read == '\n')
                {
                    break;
                }
                else
                {
                    ret += read;
                }

            }

            return ret;
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        /// <returns>Not implemented</returns>
        public override string ReadToEnd()
        {
            throw new NotImplementedException();
        }
    }
}
