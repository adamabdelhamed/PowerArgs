using System;

namespace PowerArgs.Cli
{
    internal class KeyboardShortcutProcessor : IMarkupProcessor
    {
        public void Process(ParserContext context)
        {
            var button = context.CurrentControl as Button;
            var textValue = context.CurrentElement["Shortcut"];
            var key = (ConsoleKey)Enum.Parse(typeof(ConsoleKey), textValue);

            ConsoleModifiers? modifier = null;

            if(context.CurrentElement["Shortcut-Modifier"] != null)
            {
                modifier = (ConsoleModifiers)Enum.Parse(typeof(ConsoleModifiers),context.CurrentElement["Shortcut-Modifier"]);
            }

            button.Shortcut = new KeyboardShortcut(key, modifier);
        }
    }
}
