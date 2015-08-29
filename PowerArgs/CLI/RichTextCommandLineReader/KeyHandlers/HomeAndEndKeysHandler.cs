using System;
using System.Collections.Generic;

namespace PowerArgs.Cli
{
    internal class HomeAndEndKeysHandler : IKeyHandler
    {
        public IEnumerable<ConsoleKey> KeysHandled { get { return new ConsoleKey[]  {  ConsoleKey.Home,  ConsoleKey.End  }; } }
        public void Handle(RichCommandLineContext context)
        {
            if(context.KeyPressed.Key == ConsoleKey.Home)
            {
                context.Console.CursorTop = context.ConsoleStartTop;
                context.Console.CursorLeft = context.ConsoleStartLeft;
                context.Intercept = true;
            }
            else if(context.KeyPressed.Key == ConsoleKey.End)
            {
                context.Console.CursorTop = context.ConsoleStartTop + (int)(Math.Floor((context.ConsoleStartLeft + context.Buffer.Count) / (double)context.Console.BufferWidth));
                context.Console.CursorLeft = (context.ConsoleStartLeft + context.Buffer.Count) % context.Console.BufferWidth;
                context.Intercept = true;
            }
        }
    }
}
