using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    /// <summary>
    /// A singleton text writer that can be used to intercept console output.
    /// </summary>
    public class ConsoleOutInterceptor : TextWriter
    {
        private static Lazy<ConsoleOutInterceptor> _interceptor = new Lazy<ConsoleOutInterceptor>(() => new ConsoleOutInterceptor());


        /// <summary>
        /// returns true if the instance is initialized and is intercepting
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Gets the interceptor, initializing it if needed.  
        /// </summary>
        public static ConsoleOutInterceptor Instance
        {
            get
            {
                return _interceptor.Value;
            }
        }

        /// <summary>
        /// Attaches the interceptor to the Console so that it starts intercepting output
        /// </summary>
        public void Attach()
        {
            Console.SetOut(this);
        }

        /// <summary>
        /// Detaches the interceptor.  Console output will be written as normal.
        /// </summary>
        public void Detatch()
        {
            StreamWriter standardOutput = new StreamWriter(Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);
        }

        private ConsoleOutInterceptor() { }


        List<ConsoleCharacter> intercepted = new List<ConsoleCharacter>();
        
        /// <summary>
        /// Intercepts the Write event
        /// </summary>
        /// <param name="buffer">the string buffer</param>
        /// <param name="index">the start index</param>
        /// <param name="count">number of chars to write</param>
        public override void Write(char[] buffer, int index, int count)
        {
            lock (intercepted)
            {
                for (int i = index; i < index + count; i++)
                {
                    intercepted.Add(new ConsoleCharacter(buffer[i]));
                }
            }
        }

        /// <summary>
        /// Intercepts the Write event
        /// </summary>
        /// <param name="value">the char to write</param>
        public override void Write(char value)
        {
            lock (intercepted)
            {
                intercepted.Add(new ConsoleCharacter(value));
            }
        }

        /// <summary>
        /// Intercepts the Write event
        /// </summary>
        /// <param name="value">the string to write</param>
        public override void Write(string value)
        {
            lock (intercepted)
            {
                foreach (var c in value)
                {
                    intercepted.Add(new ConsoleCharacter(c));
                }
            }
        }

        /// <summary>
        /// Pretends to intercept a ConsoleString
        /// </summary>
        /// <param name="value">the string to intercept</param>
        public void Write(ConsoleString value)
        {
            lock (intercepted)
            {
                foreach (var c in value)
                {
                    intercepted.Add(c);
                }
            }
        }

        /// <summary>
        /// Reads the queued up intercepted characters and then clears the queue as an atomic operation.
        /// This method is thread safe.
        /// </summary>
        /// <returns>The queued up intercepted characters</returns>
        public Queue<ConsoleCharacter> ReadAndClear()
        {
            lock (intercepted)
            {
                var ret = new Queue<ConsoleCharacter>(intercepted);
                intercepted.Clear();
                return ret;
            }
        }

        /// <summary>
        /// Returns System.Text.Encoding.Default
        /// </summary>
        public override System.Text.Encoding Encoding
        {
            get { return System.Text.Encoding.Default; }
        }
    }
}
