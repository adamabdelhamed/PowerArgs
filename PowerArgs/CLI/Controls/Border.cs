using PowerArgs.Cli;
using System;

namespace PowerArgs.Cli
{
    public class Border : ConsolePanel
    {
        public ConsoleColor BorderColor { get; set; } = ConsoleColor.Yellow;

        protected override void OnPaint(ConsoleBitmap context)
        {
            var borderChar = new ConsoleCharacter(' ', Foreground, BorderColor);
            context.DrawRect(borderChar,0,0,Width,Height-1);
            context.DrawLine(borderChar, 1, 1, 1, Height-1);
            context.DrawLine(borderChar, Width-2, 1, Width - 2, Height-1);
            // todo - that minus 3 should be a 2, but I'm not sure why this makes it work.
            // I'm afraid there's an off by 1 somewhere deep in the code that processes rectangles. 
            // I'll need to investigate at some point. If I end up fixing that bug then it's likely this line
            // will stop working.  So if it ever looks like the content of this panel is not having it's last line
            // painted then it's likely that I fixed the other bug and this this.Height - 3 needs to be changed to a this.Height - 2.
            context.NarrowScope(2, 1, this.Width - 4, this.Height - 3); 
            base.OnPaint(context);
        }
    }

    public class Border2 : ConsolePanel
    {
        public ConsolePanel Content { get; private set; }

        public Border2(ConsolePanel content = null)
        {
            this.Content= content ?? new ConsolePanel();
            this.Add(Content).Fill(padding: new Thickness(2, 2, 1, 1));
        }
    }
}
