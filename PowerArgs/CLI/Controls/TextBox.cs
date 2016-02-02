using System;
using System.Collections.Generic;
using System.Threading;

namespace PowerArgs.Cli
{
    public class TextBox : ConsoleControl
    {
        private static readonly TimeSpan BlinkInterval = TimeSpan.FromMilliseconds(500);

        private RichTextEditor textState;
        private bool blinkState;

        private Timer blinkTimerHandle;

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
        private Timer backspaceNavigationOptimizationTimerHandle;
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
        }

        public override void OnAddedToVisualTree()
        {
            textState.Subscribe(nameof(textState.CurrentValue), TextValueChanged);
        }

        private void TextValueChanged()
        {
            FirePropertyChanged(nameof(Value));

            if (backspaceNavigationOptimizationTimerHandle != null)
            {
                Application.MessagePump.ClearTimeout(backspaceNavigationOptimizationTimerHandle);
            }

            enableBackspaceNavigationOptimization = false;

            if (Application is ConsolePageApp && (Value == null || Value.Length == 0))
            {
                backspaceNavigationOptimizationTimerHandle = Application.MessagePump.SetTimeout(()=> { enableBackspaceNavigationOptimization = true; }, TimeSpan.FromSeconds(1));
            }
        }

        private void TextBox_Focused()
        {
            enableBackspaceNavigationOptimization = true;
            blinkState = true;
            blinkTimerHandle = Application.MessagePump.SetInterval(() =>
            {
                if (HasFocus == false) return;
                blinkState = !blinkState;
                Application.Paint();
            }, BlinkInterval);
            Application.GlobalKeyHandlers.Push(ConsoleKey.Backspace, (info)=> 
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

            if (blinkState)
            {
                char blinkChar = textState.CursorPosition >= toPaint.Length ? ' ' : toPaint[textState.CursorPosition].Value;
                context.Pen = new ConsoleCharacter(blinkChar, Application.Theme.FocusContrastColor, Application.Theme.FocusColor);
                context.DrawPoint(textState.CursorPosition, 0);
            }
        }
    }
}
