using System;
using System.IO;
using System.Text;

namespace PowerArgs.Cli
{
    internal class TextBroacaster : TextWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
        public Event<ConsoleString> TextReady { get; private set; } = new Event<ConsoleString>();
        public override void Write(char value) 
        {
            TextReady.Fire(new ConsoleString(value+"", Console.ForegroundColor, Console.BackgroundColor));
        }

        public override void Write(string value)
        {
            if (value.Length == 0) return;
            value = value == "\r\n" ? "\n" : value;
            TextReady.Fire(new ConsoleString(value, Console.ForegroundColor, Console.BackgroundColor));
        }
    }
}
