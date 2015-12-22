using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class Button : ConsoleControl
    {
        public event Action Activated;
        public string Text
        {
            get { return Get<string>(); }
            set
            {
                Set(value);
            }
        }

        private ConsoleString drawState;

        public Button()
        {
            Height = 1;
            this.Foreground = Theme.DefaultTheme.ButtonColor;
            this.PropertyChanged += Button_PropertyChanged;
            this.Focused += UpdateDrawState;
            this.Unfocused += UpdateDrawState;
        }

        private void Button_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(Text))
            {
                UpdateDrawState();
            }
        }

        public override bool OnKeyInputReceived(ConsoleKeyInfo info)
        {
            if(info.Key == ConsoleKey.Enter || info.Key == ConsoleKey.Spacebar)
            {
                if(Activated != null)
                {
                    Activated();
                }
                return true;
            }
            return false;
        }

        private void UpdateDrawState()
        {
            var anchorColor = Application != null ? Application.Theme.H1Color : Theme.DefaultTheme.H1Color;
            var focusColor = Application != null ? Application.Theme.FocusColor : Theme.DefaultTheme.FocusColor;

            drawState = "[".ToConsoleString(anchorColor);
            if (Text != null)
            {
                drawState += Text.ToConsoleString(HasFocus ? focusColor : Foreground);
            }
            drawState += "]".ToConsoleString(anchorColor);
            Width = drawState.Length;
        }

        internal override void OnPaint(ConsoleBitmap context)
        {
            context.DrawString(drawState, 0, 0);
        }
    }
}
