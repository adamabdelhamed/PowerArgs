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

        public override void OnKeyInputReceived(ConsoleKeyInfo info)
        {
            if(info.Key == ConsoleKey.Enter || info.Key == ConsoleKey.Spacebar)
            {
                if(Activated != null)
                {
                    Activated();
                }
            }
        }

        private void UpdateDrawState()
        {
            drawState = "[".ToConsoleString(ConsoleColor.Yellow);
            if (Text != null)
            {
                drawState += Text.ToConsoleString(HasFocus ? ConsoleColor.Cyan : ConsoleColor.White);
            }
            drawState += "]".ToConsoleString(ConsoleColor.Yellow);
            Width = drawState.Length;
        }

        internal override void OnPaint(ConsoleBitmap context)
        {
            context.DrawString(drawState, 0, 0);
        }
    }
}
