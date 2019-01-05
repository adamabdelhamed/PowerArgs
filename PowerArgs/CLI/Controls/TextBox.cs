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

        // these next two properties are used to implement a UX optimization that allows the
        // backspace key to be used either as text input (since this is a text box) or as a back gesture
        // in the built in navigation system.  Basically if the text box is empty we will assume the user wants
        // to navigate rather than clear the already cleared text box.  
        //
        // To polish this interaction there is a timer that is used to suppress the navigation gesture in the case
        // where the user held down the backspace key to clear out the text or pressed the backspace key an extra time
        // or two when wanting to clear out the text. 
        // 
        private bool enableBackspaceNavigationOptimization;
        private IDisposable backspaceNavigationOptimizationTimerHandle;

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

            if (backspaceNavigationOptimizationTimerHandle != null)
            {
                backspaceNavigationOptimizationTimerHandle.Dispose();
            }

            enableBackspaceNavigationOptimization = false;

            if (Application is ConsolePageApp && (Value == null || Value.Length == 0))
            {
                backspaceNavigationOptimizationTimerHandle = Application.SetTimeout(()=> { enableBackspaceNavigationOptimization = true; }, TimeSpan.FromSeconds(1));
            }
        }

        Subscription backspacceSub;

        private void TextBox_Focused()
        {
            enableBackspaceNavigationOptimization = true;
            blinkState = true;
            blinkTimerHandle = Application.SetInterval(() =>
            {
                if (HasFocus == false) return;
                blinkState = !blinkState;
                Application.Paint();
            }, BlinkInterval);


            backspacceSub = Application.FocusManager.GlobalKeyHandlers.PushUnmanaged(ConsoleKey.Backspace, null, (info)=> 
            {
                if (enableBackspaceNavigationOptimization == false || Application is ConsolePageApp == false)
                {
                    OnKeyInputReceived(info);
                }
                else
                {
                    (Application as ConsolePageApp).PageStack.TryUp();
                }
            });
        }

        private void TextBox_Unfocused()
        {
            backspacceSub.Dispose();
            blinkTimerHandle.Dispose();
            blinkState = false;
        }

        private void OnKeyInputReceived(ConsoleKeyInfo info)
        {
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
                context.DrawPoint(textState.CursorPosition, 0);
            }
        }
    }
}
