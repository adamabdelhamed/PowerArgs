using System;
using System.Collections.Generic;

namespace PowerArgs.Cli
{
    internal class EnterKeyHandler : IKeyHandler
    {
        public IEnumerable<ConsoleKey> KeysHandled
        {
            get
            {
                return new ConsoleKey[] 
                { 
                    ConsoleKey.Enter 
                };
            }
        }
        public void Handle(RichCommandLineContext context)
        {
            context.IsFinished = true;
            context.Intercept = true;
        }
    }
}
