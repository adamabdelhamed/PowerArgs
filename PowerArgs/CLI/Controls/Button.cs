using System;

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

        private void UpdateWidth() => Width = GetButtonDisplayString().Length;

        private ConsoleString GetButtonDisplayString()
        {
            var startAnchor = "[".ToConsoleString(HasFocus ? DefaultColors.BackgroundColor : CanFocus ? DefaultColors.H1Color : DefaultColors.DisabledColor, HasFocus ? (RGB)DefaultColors.FocusColor : Background);
            var effectiveText = Text ?? ConsoleString.Empty;
            var shortcut = ConsoleString.Empty;
            if (Text != null)
            {
                ConsoleColor fg, bg;

                if (effectiveText.IsUnstyled)
                {
                    if (CanFocus)
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
            }

            if (Shortcut != null)
            {
                if (Shortcut.Modifier.HasValue && Shortcut.Modifier == ConsoleModifiers.Alt)
                {
                    shortcut = new ConsoleString($" (ALT+{Shortcut.Key})", CanFocus ? DefaultColors.H1Color : DefaultColors.DisabledColor);
                }
                else if (Shortcut.Modifier.HasValue && Shortcut.Modifier == ConsoleModifiers.Shift)
                {
                    shortcut = new ConsoleString($" (SHIFT+{Shortcut.Key})", CanFocus ? DefaultColors.H1Color : DefaultColors.DisabledColor);
                }
                else if (Shortcut.Modifier.HasValue && Shortcut.Modifier == ConsoleModifiers.Control)
                {
                    shortcut = new ConsoleString($" (CTL+{Shortcut.Key})", CanFocus ? DefaultColors.H1Color : DefaultColors.DisabledColor);
                }
                else
                {
                    shortcut = new ConsoleString($" ({Shortcut.Key})", CanFocus ? DefaultColors.H1Color : DefaultColors.DisabledColor);
                }
            }

            var endAnchor = "]".ToConsoleString(HasFocus ? DefaultColors.BackgroundColor : CanFocus ? DefaultColors.H1Color : DefaultColors.DisabledColor, HasFocus ? (RGB)DefaultColors.FocusColor : Background);
            var ret = startAnchor + effectiveText + shortcut + endAnchor;
            return ret;
        }

        /// <summary>
        /// Called when the button is added to an app
        /// </summary>
        public void OnAddedToVisualTree() =>  RegisterShortcutIfPossibleAndNotAlreadyDone();

        private void RegisterShortcutIfPossibleAndNotAlreadyDone()
        {
            if (Shortcut != null && shortcutRegistered == false && Application != null)
            {
                shortcutRegistered = true;
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(Shortcut.Key, Shortcut.Modifier,()=>
                {
                    if (this.CanFocus)
                    {
                        Pressed.Fire();
                    }
                }, this);
            }
        }

        private void OnKeyInputReceived(ConsoleKeyInfo info)
        {
            if(info.Key == ConsoleKey.Enter)
            {
                Pressed.Fire();
            }
        }

        /// <summary>
        /// paints the button
        /// </summary>
        /// <param name="context">drawing context</param>
        protected override void OnPaint(ConsoleBitmap context) => context.DrawString(GetButtonDisplayString(), 0, 0);
    }
}
