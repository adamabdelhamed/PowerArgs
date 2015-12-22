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
                textState.CursorPosition = value.Length;
            }
        }

        public TextBox()
        {
            this.textState = new RichTextEditor();
            this.Height = 1;
            CanFocus = true;
            this.Focused += TextBox_Focused;
            this.Unfocused += TextBox_Unfocused;
            this.textState.PropertyChanged += TextValueChanged;
        }

        private void TextValueChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(RichTextEditor.CurrentValue)) return;
            FirePropertyChanged(nameof(Value));
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
            Application.GlobalKeyHandlers.Push(ConsoleKey.Backspace, (info)=> { OnKeyInputReceived(info); });
        }

        private void TextBox_Unfocused()
        {
            Application.GlobalKeyHandlers.Pop(ConsoleKey.Backspace);
            Application.MessagePump.ClearInterval(blinkTimerHandle);
            blinkState = false;
        }

        public override bool OnKeyInputReceived(ConsoleKeyInfo info)
        {
            textState.RegisterKeyPress(info);
            blinkState = true;
            blinkTimerHandle.Change(BlinkInterval, BlinkInterval);
            return true;
        }

        internal override void OnPaint(ConsoleBitmap context)
        {
            var toPaint = textState.CurrentValue;
            context.DrawString(textState.CurrentValue, 0, 0);

            if (blinkState)
            {
                char blinkChar = textState.CursorPosition >= toPaint.Length ? ' ' : toPaint[textState.CursorPosition].Value;
                context.Pen = new ConsoleCharacter(blinkChar, Application.Theme.FocusContrastColor, Application.Theme.FocusColor);
                context.DrawPoint(textState.CursorPosition, 0);
            }
        }
    }
}
