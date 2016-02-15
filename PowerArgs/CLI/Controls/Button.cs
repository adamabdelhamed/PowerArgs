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
        public ConsoleModifiers? Modifier{ get; set; }

        public KeyboardShortcut(ConsoleKey key, ConsoleModifiers? modifier = null)
        {
            this.Key = key;
            this.Modifier = modifier;
        }
    }

    public class Button : ConsoleControl
    {
        public Event Activated { get; private set; } = new Event();
        public string Text { get { return Get<string>(); } set { Set(value); } }

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
            this.AddedToVisualTree.SubscribeForLifetime(OnAddedToVisualTree, this.LifetimeManager);
            this.KeyInputReceived.SubscribeForLifetime(OnKeyInputReceived, this.LifetimeManager);
        }

        private void UpdateWidth()
        {
            int w = Text == null ? 2 : Text.Length + 2;

            if (Shortcut != null && Shortcut.Modifier.HasValue && Shortcut.Modifier == ConsoleModifiers.Alt)
            {
                w += "ALT+".Length;
            }
            else if (Shortcut != null && Shortcut.Modifier.HasValue && Shortcut.Modifier == ConsoleModifiers.Shift)
            {
                w += "SHIFT+".Length;
            }
            else if (Shortcut != null && Shortcut.Modifier.HasValue && Shortcut.Modifier == ConsoleModifiers.Control)
            {
                w += "CTL+".Length;
            }

            if (Shortcut != null)
            {
                w += Shortcut.Key.ToString().Length + " ()".Length;
            }
            Width = w;
        }

        public void OnAddedToVisualTree()
        {
            if (Shortcut != null)
            {
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(Shortcut.Key, Shortcut.Modifier, Click, this.LifetimeManager);
            }
        }


        private void OnKeyInputReceived(ConsoleKeyInfo info)
        {
            if(info.Key == ConsoleKey.Enter || info.Key == ConsoleKey.Spacebar)
            {
                Click();
            }
        }

        private void Click()
        {
            Activated.Fire();
        }

        protected override void OnPaint(ConsoleBitmap context)
        {
            var drawState = new ConsoleString();

            drawState = "[".ToConsoleString(CanFocus ? Application.Theme.H1Color : Application.Theme.DisabledColor, Background = Background);
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
                    if(Shortcut.Modifier.HasValue && Shortcut.Modifier == ConsoleModifiers.Alt)
                    {
                        drawState += new ConsoleString($" (ALT+{Shortcut.Key})", CanFocus ? Application.Theme.H1Color : Application.Theme.DisabledColor);
                    }
                    else if (Shortcut.Modifier.HasValue && Shortcut.Modifier == ConsoleModifiers.Shift)
                    {
                        drawState += new ConsoleString($" (SHIFT+{Shortcut.Key})", CanFocus ? Application.Theme.H1Color : Application.Theme.DisabledColor);
                    }
                    else if (Shortcut.Modifier.HasValue && Shortcut.Modifier == ConsoleModifiers.Control)
                    {
                        drawState += new ConsoleString($" (CTL+{Shortcut.Key})", CanFocus ? Application.Theme.H1Color : Application.Theme.DisabledColor);
                    }
                    else
                    {
                        drawState += new ConsoleString($" ({Shortcut.Key})", CanFocus ? Application.Theme.H1Color : Application.Theme.DisabledColor);
                    }
                }
            }

            drawState += "]".ToConsoleString(CanFocus ? Application.Theme.H1Color : Application.Theme.DisabledColor, Background);
            Width = drawState.Length;
            context.DrawString(drawState, 0, 0);
        }
    }
}
