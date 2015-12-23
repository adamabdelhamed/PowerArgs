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
                Width = value == null ? 2 : value.Length + 2;
            }
        }

        public Button()
        {
            Height = 1;
            this.Foreground = Theme.DefaultTheme.ButtonColor;
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

        internal override void OnPaint(ConsoleBitmap context)
        {
            var drawState = new ConsoleString();

            drawState = "[".ToConsoleString(Application.Theme.H1Color);
            if (Text != null)
            {
                ConsoleColor fg, bg;

                if(HasFocus)
                {
                    fg = Application.Theme.FocusContrastColor;
                    bg = Application.Theme.FocusColor;
                }
                else if(CanFocus)
                {
                    fg = Foreground;
                    bg = Background;
                }
                else
                {
                    fg = Application.Theme.DisabledColor;
                    bg = Background;
                }

                drawState += new ConsoleString(Text, fg, bg);
            }

            drawState += "]".ToConsoleString(Application.Theme.H1Color);
            Width = drawState.Length;
            context.DrawString(drawState, 0, 0);
        }
    }
}
