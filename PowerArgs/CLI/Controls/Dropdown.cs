using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A control that lets the user view and edit the current value among a set of options.
    /// </summary>
    public class Dropdown : ProtectedConsolePanel
    {
        public List<DialogOption> Options { get; private set; } = new List<DialogOption>();
        private Label valueLabel;
        private bool isOpen;

        /// <summary>
        /// The currently selected option
        /// </summary>
        public DialogOption Value { get => Get<DialogOption>(); set => Set(value); }

        public bool EnableWAndSKeysForUpDown { get; set; }

        /// <summary>
        /// Creates a new Dropdown
        /// </summary>
        public Dropdown(IEnumerable<DialogOption> options)
        {
            this.Options.AddRange(options);
            Value = this.Options.FirstOrDefault();
            CanFocus = true;
            Height = 1;
            valueLabel = ProtectedPanel.Add(new Label());

            SynchronizeForLifetime(AnyProperty, SyncValueLabel, this);
            Focused.SubscribeForLifetime(SyncValueLabel, this);
            Unfocused.SubscribeForLifetime(SyncValueLabel, this);

            this.KeyInputReceived.SubscribeForLifetime(k =>
            {
                if (k.Key == ConsoleKey.Enter || k.Key == ConsoleKey.DownArrow)
                {
                    Open();
                }
                else if(EnableWAndSKeysForUpDown && (k.Key == ConsoleKey.W || k.Key == ConsoleKey.S))
                {
                    Open();
                }

            }, this);
        }

        private void SyncValueLabel()
        {
            var text = Value.DisplayText.StringValue;
            if (text.Length > Width-3 && Width > 0)
            {
                text = text.Substring(0, Math.Max(0, Width - 3));
            }

            while(text.Length < Width-2 && Width > 0)
            {
                text += " ";
            }

            if (HasFocus || isOpen)
            {
                text += isOpen ? "^ " : "v ";
            }
            else
            {
                text += "  ";
            }

            valueLabel.Text = HasFocus ? text.ToBlack(RGB.Cyan) : isOpen ? text.ToCyan(RGB.DarkGray) : text.ToWhite();
        }

        private async void Open()
        {
            isOpen = true;
            SyncValueLabel();
            TryUnfocus();
            try
            {
                Application.FocusManager.Push();
                var appropriatePopupWidth = 2 + 1 + Options.Select(o => o.DisplayText.Length).Max() + 1 + 1 + 2;
                var scrollPanel = new ScrollablePanel();
                scrollPanel.Width = appropriatePopupWidth - 4;
                scrollPanel.Height = Math.Min(8, Options.Count);

                var optionsStack = scrollPanel.ScrollableContent.Add(new StackPanel());
                optionsStack.Height = Options.Count;
                optionsStack.Width = scrollPanel.Width-3;
                optionsStack.X = 1;
                optionsStack.AddRange(Options.Select(option => new Label() { CanFocus=true, Text = option.DisplayText, Tag = option }));
                scrollPanel.ScrollableContent.Width = optionsStack.Width + 2;
                scrollPanel.ScrollableContent.Height = optionsStack.Height;

                var popup = new BorderPanel(scrollPanel) { BorderColor = RGB.DarkCyan };
                popup.Width = scrollPanel.Width+4;
                popup.Height = scrollPanel.Height + 2;
                popup.X = this.AbsoluteX;
                popup.Y = this.AbsoluteY + 1;
                Application.LayoutRoot.Add(popup);

                var index = Options.IndexOf(Value);

                Action syncSelectedIndex = () =>
                {
                    var labels = optionsStack.Children.WhereAs<Label>().ToArray();
                    for(var i = 0; i < Options.Count; i++)
                    {
                        labels[i].Text = Options[i].DisplayText;

                        // This value won't show so we need to invert its colors
                        if(labels[i].Text.Where(c => c.BackgroundColor == popup.Background && c.ForegroundColor == popup.Background).Count() == labels[i].Text.Length)
                        {
                            labels[i].Text = new ConsoleString(labels[i].Text.Select(c => new ConsoleCharacter(c.Value, c.ForegroundColor.GetCompliment(), popup.Background)));
                        }

                    }

                    var label = optionsStack.Children.ToArray()[index] as Label;
                    label.TryFocus();
                    label.Text = label.Text.ToBlack(bg: RGB.Cyan);
                };
                syncSelectedIndex();

                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.Enter, null, () =>
                {
                    Value = Options[index];
                    popup.Dispose();
                }, popup);

                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.Escape, null, popup.Dispose, popup);

                Action up = () =>
                {
                    if (index > 0)
                    {
                        index--;
                        syncSelectedIndex();
                    }
                    else
                    {
                        index = Options.Count - 1;
                        syncSelectedIndex();
                    }
                };

                Action down = () =>
                {
                    if (index < Options.Count - 1)
                    {
                        index++;
                        syncSelectedIndex();
                    }
                    else
                    {
                        index = 0;
                        syncSelectedIndex();
                    }
                };

                if (EnableWAndSKeysForUpDown)
                {
                    Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.W, null, up, popup);
                    Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.S, null, down, popup);
                }

                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.UpArrow, null, up, popup);
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.DownArrow, null, down, popup);
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.Tab,  ConsoleModifiers.Shift, up, popup);
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.Tab, null, down, popup);

                await popup.AwaitEndOfLifetime();
            }
            finally
            {
                isOpen = false;
                Application.FocusManager.Pop();
                TryFocus();
            }
        }
    }
}
