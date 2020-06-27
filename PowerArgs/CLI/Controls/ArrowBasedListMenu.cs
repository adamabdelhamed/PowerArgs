using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.CLI.Controls
{
    public class ArrowBasedListMenu<T> : ProtectedConsolePanel where T : class
    {
        public int SelectedIndex { get => Get<int>(); set => Set(value); }
        public T SelectedItem => MenuItems.Count > 0 ? MenuItems[SelectedIndex] : null;

        public Event<T> ItemActivated { get; private set; } = new Event<T>();
        public List<T> MenuItems { get; private set; }
        private Func<T, ConsoleString> formatter;

        public ConsoleKey? AlternateUp { get; set; }
        public ConsoleKey? AlternateDown { get; set; }

        public ArrowBasedListMenu(List<T> menuItems, Func<T,ConsoleString> formatter = null)
        {
            MenuItems = menuItems;
            formatter = formatter ?? new Func<T, ConsoleString>(item => (""+item).ToConsoleString());
            this.formatter = formatter;

            var stack = ProtectedPanel.Add(new StackPanel() { Orientation = Orientation.Vertical, Margin = 1 }).Fill();
            this.CanFocus = true;

            this.Focused.SubscribeForLifetime(Sync, this);
            this.Unfocused.SubscribeForLifetime(Sync, this);

            foreach (var menuItem in menuItems)
            {
                var label = stack.Add(new Label() { Text = formatter(menuItem), Tag = menuItem }).FillHorizontally();
            }

            Sync();

            this.KeyInputReceived.SubscribeForLifetime(OnKeyPress, this);
        }

        private void OnKeyPress(ConsoleKeyInfo obj)
        {
            if(obj.Key == ConsoleKey.UpArrow || (AlternateUp.HasValue && obj.Key == AlternateUp.Value))
            {
                if(SelectedIndex > 0)
                {
                    SelectedIndex--;
                    FirePropertyChanged(nameof(SelectedItem));
                    Sync();
                }
            }
            else if(obj.Key == ConsoleKey.DownArrow || (AlternateDown.HasValue && obj.Key == AlternateDown.Value))
            {
                if (SelectedIndex < MenuItems.Count - 1)
                {
                    SelectedIndex++;
                    FirePropertyChanged(nameof(SelectedItem));
                    Sync();
                }
            }
            else if(obj.Key == ConsoleKey.Enter)
            {
                ItemActivated.Fire(SelectedItem);
            }
        }

        private void Sync()
        {
            foreach (var label in ProtectedPanel.Descendents.WhereAs<Label>().Where(l => l.Tag is T))
            {
                if (object.ReferenceEquals(label.Tag, SelectedItem))
                {
                    label.Text = formatter(label.Tag as T).StringValue.ToConsoleString(HasFocus ? RGB.Black : Foreground, HasFocus ? RGB.Cyan : Background);
                }
                else
                {
                    label.Text = formatter(label.Tag as T);
                }
            }
        }
    }
}
