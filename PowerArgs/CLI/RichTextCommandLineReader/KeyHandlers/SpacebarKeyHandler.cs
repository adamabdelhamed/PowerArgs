using System;
using System.Collections.Generic;

namespace PowerArgs.Cli
{
    internal class SpacebarKeyHandler : IKeyHandler
    {
        public IContextAssistProvider ContextAssistProvider { get; set; }
        public IEnumerable<ConsoleKey> KeysHandled  { get { return new ConsoleKey[]  {  ConsoleKey.Spacebar, }; } }

        public SpacebarKeyHandler()
        {
            ContextAssistProvider = new ContextAssistPicker();
        }

        public void Handle(RichCommandLineContext context)
        {
            if (context.KeyPressed.Modifiers.HasFlag(ConsoleModifiers.Control) == false)
            {
                return;
            }

            context.Intercept = true;
            context.RefreshTokenInfo();

            if (ContextAssistProvider == null || ContextAssistProvider.CanAssist(context) == false)
            {
                return;
            }

            int left = context.Console.CursorLeft;
            int top = context.Console.CursorTop;

            ContextAssistResult result = ContextAssistResult.NoOp;

            try
            {
                context.Console.WriteLine("\n");
                result = ContextAssistProvider.DrawMenu(context);

                while (result.IsTerminal == false)
                {
                    var key = context.Console.ReadKey(true);
                    result = ContextAssistProvider.OnKeyboardInput(context, key);
                }
            }
            finally
            {
                ContextAssistProvider.ClearMenu(context);
                context.Console.CursorLeft = left;
                context.Console.CursorTop = top;
            }

            if (result.StatusCode == ContextAssistResultStatusCode.Success)
            {
                context.ClearConsole();
                context.Console.CursorLeft = left;
                context.Console.CursorTop = top;
                context.Buffer.Clear();
                context.Buffer.AddRange(result.NewBuffer);
                context.RefreshConsole(result.ConsoleRefreshLeftOffset, 0);
            }
        }
    }
}
