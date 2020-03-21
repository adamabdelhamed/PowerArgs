using System;
using System.Collections.Generic;
using System.Threading;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A control that lets the user provide text input
    /// </summary>
    public class TextBox : ConsoleControl
    {
        private static readonly TimeSpan BlinkInterval = TimeSpan.FromMilliseconds(500);

        private RichTextEditor textState;
        private bool blinkState;

        private IDisposable blinkTimerHandle;

        /// <summary>
        /// Gets the editor object that controls the rich text capabilities of the text box
        /// </summary>
        public RichTextEditor RichTextEditor
        {
            get
            {
                return textState;
            }
        }

        /// <summary>
        /// Gets or sets the value in the text box
        /// </summary>
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

        /// <summary>
        /// Gets or sets a flag that enables or disables the blinking cursor that appears when the text box has focus
        /// </summary>
        public bool BlinkEnabled { get; set; } = true;

        public bool IsInputBlocked { get; set; }

        /// <summary>
        /// Creates a new text box
        /// </summary>
        public TextBox()
        {
            this.textState = new RichTextEditor();
            this.Height = 1;
            this.Width = 15;
            CanFocus = true;
            this.Focused.SubscribeForLifetime(TextBox_Focused, this);
            this.Unfocused.SubscribeForLifetime(TextBox_Unfocused, this);
            textState.SubscribeForLifetime(nameof(textState.CurrentValue), TextValueChanged, this);
            KeyInputReceived.SubscribeForLifetime(OnKeyInputReceived, this);
        }

        private void TextValueChanged()
        {
            FirePropertyChanged(nameof(Value));
        }

        private void TextBox_Focused()
        {
            blinkState = true;
            blinkTimerHandle = Application.SetInterval(() =>
            {
                if (HasFocus == false) return;
                blinkState = !blinkState;
                Application.Paint();
            }, BlinkInterval);
        }

        private void TextBox_Unfocused()
        {
            blinkTimerHandle.Dispose();
            blinkState = false;
        }

        private void OnKeyInputReceived(ConsoleKeyInfo info)
        {
            if (IsInputBlocked) return;
            ConsoleCharacter? prototype = this.Value.Length == 0 ? (ConsoleCharacter?)null : this.Value[this.Value.Length - 1];
            textState.RegisterKeyPress(info, prototype);
            blinkState = true;
            Application.ChangeInterval(blinkTimerHandle, BlinkInterval);
        }

        /// <summary>
        /// paints the text box
        /// </summary>
        /// <param name="context"></param>
        protected override void OnPaint(ConsoleBitmap context)
        {
            var toPaint = textState.CurrentValue;

            var offset = 0;
            if(toPaint.Length >= Width && textState.CursorPosition > Width-1)
            {
                offset = (textState.CursorPosition + 1) - Width;
                toPaint = toPaint.Substring(offset);
            }

            var bgTransformed = new List<ConsoleCharacter>();

            foreach(var c in toPaint)
            {
                if(c.BackgroundColor == ConsoleString.DefaultBackgroundColor && Background != ConsoleString.DefaultBackgroundColor)
                {
                    bgTransformed.Add(new ConsoleCharacter(c.Value, Foreground, Background));
                }
                else
                {
                    bgTransformed.Add(c);
                }
            }

            context.DrawString(new ConsoleString(bgTransformed), 0, 0);

            if (blinkState && BlinkEnabled)
            {
                char blinkChar = textState.CursorPosition >= toPaint.Length ? ' ' : toPaint[textState.CursorPosition].Value;
                context.Pen = new ConsoleCharacter(blinkChar, DefaultColors.FocusContrastColor, DefaultColors.FocusColor);
                context.DrawPoint(textState.CursorPosition - offset, 0);
            }
        }
    }
}
