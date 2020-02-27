using System;

namespace PowerArgs.Cli
{
    public class PerfTestPanel : ConsoleControl
    {
        public bool Even { get => Get<bool>(); set => Set(value); }

        protected override void OnPaint(ConsoleBitmap context)
        {
            var currentBool = Even;
            var evenPen = new ConsoleCharacter('O', ConsoleColor.Black, ConsoleColor.White);
            var oddPen = new ConsoleCharacter('O', ConsoleColor.White, ConsoleColor.Black);
            for (var x = 0; x < context.Width; x++)
            {
                for (var y = 0; y < context.Height; y++)
                {
                    context.DrawPoint(currentBool ? evenPen : oddPen, x, y);
                    currentBool = !currentBool;
                }
            }
        }
    }
}
