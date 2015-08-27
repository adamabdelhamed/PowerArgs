using System;
using System.Collections.Generic;

namespace PowerArgs.Cli
{
    internal class ArrowKeysHandler : IKeyHandler
    {
        public IEnumerable<ConsoleKey> KeysHandled
        {
            get
            {
                return new ConsoleKey[] 
                { 
                    ConsoleKey.UpArrow,
                    ConsoleKey.DownArrow,
                    ConsoleKey.LeftArrow,
                    ConsoleKey.RightArrow,
                };
            }
        }
        public void Handle(RichCommandLineContext context)
        {
            if(context.KeyPressed.Key == ConsoleKey.UpArrow)
            {
                HandleUpArrow(context);
            }
            else if(context.KeyPressed.Key == ConsoleKey.DownArrow)
            {
                HandleDownArrow(context);
            }
            else if(context.KeyPressed.Key == ConsoleKey.LeftArrow)
            {
                HandleLeftArrow(context);
            }
            else if(context.KeyPressed.Key == ConsoleKey.RightArrow)
            {
                HandleRightArrow(context);
            }
        }

        private void HandleLeftArrow(RichCommandLineContext context)
        {
            if (context.Console.CursorTop == context.ConsoleStartTop && context.Console.CursorLeft > context.ConsoleStartLeft)
            {
                context.Console.CursorLeft -= 1;
            }
            else if (context.Console.CursorLeft > 0)
            {
                context.Console.CursorLeft -= 1;
            }
            else if (context.Console.CursorTop > context.ConsoleStartTop)
            {
                context.Console.CursorTop--;
                context.Console.CursorLeft = context.Console.BufferWidth - 1;
            }

            context.Intercept = true;
        }

        private void HandleRightArrow(RichCommandLineContext context)
        {
            if (context.Console.CursorLeft < context.Console.BufferWidth - 1 && context.BufferPosition < context.Buffer.Count)
            {
                context.Console.CursorLeft = context.Console.CursorLeft + 1;
            }
            else if (context.Console.CursorLeft == context.Console.BufferWidth - 1)
            {
                context.Console.CursorTop++;
                context.Console.CursorLeft = 0;
            }

            context.Intercept = true;
        }

        private void HandleDownArrow(RichCommandLineContext context)
        {
            if (context.HistoryManager.Values.Count == 0)
            {
                return;
            }

            context.Console.CursorLeft = context.ConsoleStartLeft;
            context.HistoryManager.Index--;
            if (context.HistoryManager.Index < 0)
            {
                context.HistoryManager.Index = context.HistoryManager.Values.Count - 1;
            }

            var newChars = context.HistoryManager.Values[context.HistoryManager.Index];
            context.ReplaceConsole(newChars);
            context.Intercept = true;
        }

        private void HandleUpArrow(RichCommandLineContext context)
        {
            if(context.HistoryManager.Values.Count == 0)
            {
                return;
            }
            
            context.Console.CursorLeft = context.ConsoleStartLeft;
            context.HistoryManager.Index++;
            if (context.HistoryManager.Index >= context.HistoryManager.Values.Count)
            {
                context.HistoryManager.Index = 0;
            }

            var newChars = context.HistoryManager.Values[context.HistoryManager.Index];
            context.ReplaceConsole(newChars);
            context.Intercept = true;
        }
    }
}
