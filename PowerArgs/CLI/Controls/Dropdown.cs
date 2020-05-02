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
        private List<DialogOption> options = new List<DialogOption>();
        private Label valueLabel;
        private Label caretLabel;

        /// <summary>
        /// The currently selected option
        /// </summary>
        public DialogOption Value { get => Get<DialogOption>(); set => Set(value); }

        /// <summary>
        /// Creates a new ToggleControl
        /// </summary>
        public Dropdown(IEnumerable<DialogOption> options)
        {
            this.options.AddRange(options);
            Value = this.options.FirstOrDefault();
            CanFocus = true;
            Height = 1;
            valueLabel = ProtectedPanel.Add(new Label());
            caretLabel = ProtectedPanel.Add(new Label() { Text = "v".ToWhite() }).DockToRight();
            SynchronizeForLifetime(nameof(Value), SyncValueLabel, this);
            Focused.SubscribeForLifetime(SyncValueLabel, this);
            Unfocused.SubscribeForLifetime(SyncValueLabel, this);

            Focused.SubscribeForLifetime(()=> caretLabel.Text = caretLabel.Text.StringValue.ToCyan(), this);
            Unfocused.SubscribeForLifetime(() => caretLabel.Text = caretLabel.Text.StringValue.ToWhite(), this);
            this.KeyInputReceived.SubscribeForLifetime(k =>
            {
                if(k.Key == ConsoleKey.Enter)
                {
                    Open();
                }
            }, this);
        }

        private void SyncValueLabel() => valueLabel.Text = HasFocus ? Value.DisplayText.StringValue.ToBlack(RGB.Cyan) : Value.DisplayText;

        private async void Open()
        {
            TryUnfocus();
            try
            {
                Application.FocusManager.Push();
                var scrollPanel = new ScrollablePanel();
                scrollPanel.Width = Width - 4;
                scrollPanel.Height = Math.Min(8, options.Count + 2);

                var optionsStack = scrollPanel.ScrollableContent.Add(new StackPanel());
                optionsStack.Height = options.Count;
                optionsStack.Width = scrollPanel.Width-3;
                optionsStack.X = 1;
                optionsStack.Y = 1;
                optionsStack.AddRange(options.Select(option => new Label() { CanFocus=true, Text = option.DisplayText, Tag = option }));
                scrollPanel.ScrollableContent.Width = optionsStack.Width + 2;
                scrollPanel.ScrollableContent.Height = optionsStack.Height + 2;

                var popup = new BorderPanel(scrollPanel) { BorderColor = RGB.White };
                popup.Width = scrollPanel.Width+4;
                popup.Height = scrollPanel.Height + 2;
                popup.X = this.AbsoluteX;
                popup.Y = this.AbsoluteY + 1;
                Application.LayoutRoot.Add(popup);

                var index = 0;

                Action syncSelectedIndex = () =>
                {
                    var labels = optionsStack.Children.WhereAs<Label>().ToArray();
                    for(var i = 0; i < options.Count; i++)
                    {
                        labels[i].Text = options[i].DisplayText;
                    }

                    var label = optionsStack.Children.ToArray()[index] as Label;
                    label.TryFocus();
                    label.Text = label.Text.ToBlack(bg: RGB.Cyan);

                    var h = scrollPanel.Height;
                };
                syncSelectedIndex();

                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.Enter, null, () =>
                {
                    Value = options[index];
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
                        index = options.Count - 1;
                        syncSelectedIndex();
                    }
                };

                Action down = () =>
                {
                    if (index < options.Count - 1)
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

                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.UpArrow, null, up, popup);
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.DownArrow, null, down, popup);
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.Tab,  ConsoleModifiers.Shift, up, popup);
                Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.Tab, null, down, popup);

                await popup.AwaitEndOfLifetime();
            }
            finally
            {
                Application.FocusManager.Pop();
                TryFocus();
            }
        }
    }
}
