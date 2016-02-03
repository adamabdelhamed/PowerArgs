using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class KeyboardShortcut
    {
        public ConsoleKey Key { get; set; }
        public bool Alt { get; set; }

        public KeyboardShortcut(ConsoleKey key, bool alt)
        {
            this.Key = key;
            this.Alt = alt;
        }
    }

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

        public KeyboardShortcut Shortcut
        {
            get
            {
                return Get<KeyboardShortcut>();
            }
            set
            {
                if (Application != null) throw new InvalidOperationException("Button shortcuts must be configured before adding the button to your application.");
                Set(value);
            }
        }

        public Button()
        {
            Height = 1;
            this.Foreground = Theme.DefaultTheme.ButtonColor;
            this.SynchronizeForLifetime(nameof(Text), UpdateWidth, this.LifetimeManager);
            this.SynchronizeForLifetime(nameof(Shortcut), UpdateWidth, this.LifetimeManager);
        }

        private void UpdateWidth()
        {
            int w = Text == null ? 2 : Text.Length + 2;

            if (Shortcut != null && Shortcut.Alt)
            {
                w += "ALT+".Length;
            }

            if (Shortcut != null)
            {
                w += Shortcut.Key.ToString().Length + " ()".Length;
            }
            Width = w;
        }

        public override void OnAddedToVisualTree()
        {
            base.OnAddedToVisualTree();
            if (Shortcut != null)
            {
                Application.GlobalKeyHandlers.Push(Shortcut.Key, (i) => 
                {
                    if (CanFocus)
                    {
                        this.Click();
                    }
                }, Shortcut.Alt);
            }
        }

        public override void OnRemovedFromVisualTree()
        {
            base.OnRemovedFromVisualTree();
            if (Shortcut != null)
            {
                Application.GlobalKeyHandlers.Pop(Shortcut.Key, Shortcut.Alt);
            }
        }

        public override bool OnKeyInputReceived(ConsoleKeyInfo info)
        {
            if(info.Key == ConsoleKey.Enter || info.Key == ConsoleKey.Spacebar)
            {
                Click();
                return true;
            }
            return false;
        }

        private void Click()
        {
            if (Activated != null)
            {
                Activated();
            }
        }

        internal override void OnPaint(ConsoleBitmap context)
        {
            var drawState = new ConsoleString();

            drawState = "[".ToConsoleString(Application.Theme.H1Color, Background = Background);
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

                if(Shortcut != null)
                {
                    if(Shortcut.Alt)
                    {
                        drawState += new ConsoleString($" (ALT+{Shortcut.Key})", CanFocus ? Application.Theme.H1Color : Application.Theme.DisabledColor);
                    }
                    else
                    {
                        drawState += new ConsoleString($" ({Shortcut.Key})", CanFocus ? Application.Theme.H1Color : Application.Theme.DisabledColor);
                    }
                }
            }

            drawState += "]".ToConsoleString(Application.Theme.H1Color, Background);
            Width = drawState.Length;
            context.DrawString(drawState, 0, 0);
        }
    }
}
