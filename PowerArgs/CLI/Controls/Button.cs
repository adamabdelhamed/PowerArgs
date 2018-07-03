using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A class that represents a keyboard shortcut that can be activate a control that does not have focus
    /// </summary>
    public class KeyboardShortcut
    {
        /// <summary>
        /// The shortcut key
        /// </summary>
        public ConsoleKey Key { get; set; }

        /// <summary>
        /// A key modifier (e.g. shift, alt) that, when present, must be pressed in order for the shortcut key to trigger.  Note that control is not
        /// supported because it doesn't play well in a console
        /// </summary>
        public ConsoleModifiers? Modifier{ get; set; }

        /// <summary>
        /// Creates a new shortut
        /// </summary>
        /// <param name="key">the shortcut key</param>
        /// <param name="modifier">A key modifier (e.g. shift, alt) that, when present, must be pressed in order for the shortcut key to trigger.  Note that control is not
        /// supported because it doesn't play well in a console</param>
        public KeyboardShortcut(ConsoleKey key, ConsoleModifiers? modifier = null)
        {
            this.Key = key;
            this.Modifier = modifier;
            if(modifier == ConsoleModifiers.Control)
            {
                throw new InvalidOperationException("Control is not supported as a keyboard shortcut modifier");
            }
        }
    }

    /// <summary>
    /// A button control that can be 'pressed' by the user
    /// </summary>
    [MarkupIgnoreAttribute("Shortcut-Modifier")]
    public class Button : ConsoleControl
    {
        private bool shortcutRegistered;

        /// <summary>
        /// An event that fires when the button is clicked
        /// </summary>
        public Event Pressed { get; private set; } = new Event();

        /// <summary>
        /// Gets or sets the text that is displayed on the button
        /// </summary>
        public ConsoleString Text { get { return Get<ConsoleString>(); } set { Set(value); } }

        /// <summary>
        /// Gets or sets the keyboard shortcut info for this button.
        /// </summary>
        [MarkupProperty(typeof(KeyboardShortcutProcessor))]
        public KeyboardShortcut Shortcut
        {
            get
            {
                return Get<KeyboardShortcut>();
            }
            set
            {
                if (Shortcut != null) throw new InvalidOperationException("Button shortcuts can only be set once.");
                Set(value);
                RegisterShortcutIfPossibleAndNotAlreadyDone();
            }
        }

        /// <summary>
        /// Creates a new button control
        /// </summary>
        public Button()
        {
            Height = 1;
            this.Foreground = DefaultColors.ButtonColor;
            this.SynchronizeForLifetime(nameof(Text), UpdateWidth, this);
            this.SynchronizeForLifetime(nameof(Shortcut), UpdateWidth, this);
            this.AddedToVisualTree.SubscribeForLifetime(OnAddedToVisualTree, this);
            this.KeyInputReceived.SubscribeForLifetime(OnKeyInputReceived, this);
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

        /// <summary>
        /// Called when the button is added to an app
        /// </summary>
        public void OnAddedToVisualTree()
        {
            RegisterShortcutIfPossibleAndNotAlreadyDone();
        }

        private void RegisterShortcutIfPossibleAndNotAlreadyDone()
        {
            if (Shortcut != null && shortcutRegistered == false && Application != null)
            {
                shortcutRegistered = true;
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(Shortcut.Key, Shortcut.Modifier, Click, this);
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
            Pressed.Fire();
        }

        /// <summary>
        /// paints the button
        /// </summary>
        /// <param name="context">drawing context</param>
        protected override void OnPaint(ConsoleBitmap context)
        {
            var drawState = new ConsoleString();

            drawState = "[".ToConsoleString(CanFocus ? DefaultColors.H1Color : DefaultColors.DisabledColor, Background = Background);
            if (Text != null)
            {
                ConsoleColor fg, bg;

                var effectiveText = Text;
                if (effectiveText.IsUnstyled)
                {
                    if (HasFocus)
                    {
                        fg = DefaultColors.FocusContrastColor;
                        bg = DefaultColors.FocusColor;
                    }
                    else if (CanFocus)
                    {
                        fg = Foreground;
                        bg = Background;
                    }
                    else
                    {
                        fg = DefaultColors.DisabledColor;
                        bg = Background;
                    }
                    effectiveText = new ConsoleString(effectiveText.StringValue, fg, bg);
                }
                else if (HasFocus)
                {
                    effectiveText = effectiveText.ToDifferentBackground(DefaultColors.FocusColor);
                }

                drawState += effectiveText;

                if(Shortcut != null)
                {
                    if(Shortcut.Modifier.HasValue && Shortcut.Modifier == ConsoleModifiers.Alt)
                    {
                        drawState += new ConsoleString($" (ALT+{Shortcut.Key})", CanFocus ? DefaultColors.H1Color : DefaultColors.DisabledColor);
                    }
                    else if (Shortcut.Modifier.HasValue && Shortcut.Modifier == ConsoleModifiers.Shift)
                    {
                        drawState += new ConsoleString($" (SHIFT+{Shortcut.Key})", CanFocus ? DefaultColors.H1Color : DefaultColors.DisabledColor);
                    }
                    else if (Shortcut.Modifier.HasValue && Shortcut.Modifier == ConsoleModifiers.Control)
                    {
                        drawState += new ConsoleString($" (CTL+{Shortcut.Key})", CanFocus ? DefaultColors.H1Color : DefaultColors.DisabledColor);
                    }
                    else
                    {
                        drawState += new ConsoleString($" ({Shortcut.Key})", CanFocus ? DefaultColors.H1Color : DefaultColors.DisabledColor);
                    }
                }
            }

            drawState += "]".ToConsoleString(CanFocus ? DefaultColors.H1Color : DefaultColors.DisabledColor, Background);
            Width = drawState.Length;
            context.DrawString(drawState, 0, 0);
        }
    }
}
