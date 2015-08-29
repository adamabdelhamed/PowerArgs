using System;
using System.Collections.Generic;

namespace PowerArgs.Cli
{
    internal class BackspaceAndDeleteKeysHandler : IKeyHandler
    {
        public IEnumerable<ConsoleKey> KeysHandled
        {
            get
            {
                return new ConsoleKey[] 
                { 
                    ConsoleKey.Backspace, 
                    ConsoleKey.Delete 
                };
            }
        }

        public void Handle(RichCommandLineContext context)
        {
            if(context.KeyPressed.Key == ConsoleKey.Delete)
            {
                HandleDelete(context);
            }
            else if(context.KeyPressed.Key == ConsoleKey.Backspace)
            {
                HandleBackspace(context);
            }
        }

        private void HandleDelete(RichCommandLineContext context)
        {
            if (context.BufferPosition < context.Buffer.Count)
            {
                context.Buffer.RemoveAt(context.BufferPosition);
                context.RefreshConsole(0, 0);
            }
            context.Intercept = true;
        }

        private void HandleBackspace(RichCommandLineContext context)
       {
            context.Intercept = true;

            if(context.BufferPosition == 0)
            {
                return;
            }

            context.BufferPosition--;

            if (context.BufferPosition < context.Buffer.Count)
            {
                context.Buffer.RemoveAt(context.BufferPosition);
                context.RefreshConsole(-1, 0);
            }
        }
    }
}
