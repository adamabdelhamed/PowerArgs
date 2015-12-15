using System;
using System.Threading;

namespace PowerArgs.Cli
{
    public class TextBox : ConsoleControl
    {
        private static readonly TimeSpan BlinkInterval = TimeSpan.FromMilliseconds(500);

        private RichTextEditor textState;
        private bool blinkState;
        private Timer blinkTimerHandle;

        public ConsoleString Value
        {
            get
            {
                return textState.CurrentValue;
            }
            set
            {
                textState.CurrentValue = value;
            }
        }

        public TextBox()
        {
            this.textState = new RichTextEditor();
            this.Height = 1;
            textState.Highlighter = new SimpleSyntaxHighlighter();
            textState.Highlighter.AddKeyword("adam",ConsoleColor.Green);
            CanFocus = true;
            this.Focused += TextBox_Focused;
            this.Unfocused += TextBox_Unfocused;
        }

        private void TextBox_Focused()
        {
            blinkState = true;
            blinkTimerHandle = Application.MessagePump.SetInterval(() =>
            {
                if (HasFocus == false) return;
                blinkState = !blinkState;
                Application.Paint();
            }, BlinkInterval);
        }

        private void TextBox_Unfocused()
        {
            Application.MessagePump.ClearInterval(blinkTimerHandle);
            blinkState = false;
        }

        public override void OnKeyInputReceived(ConsoleKeyInfo info)
        {
            textState.RegisterKeyPress(info);
            blinkState = true;
            blinkTimerHandle.Change(BlinkInterval, BlinkInterval);
        }

        internal override void OnPaint(ConsoleBitmap context)
        {
            var toPaint = textState.CurrentValue;
            context.DrawString(textState.CurrentValue, 0, 0);

            if (blinkState)
            {
                char blinkChar = textState.CursorPosition >= toPaint.Length ? ' ' : toPaint[textState.CursorPosition].Value;
                context.Pen = new ConsoleCharacter(blinkChar, ConsoleColor.Black, FocusForeground.ForegroundColor);
                context.DrawPoint(textState.CursorPosition, 0);
            }
        }
    }
}
