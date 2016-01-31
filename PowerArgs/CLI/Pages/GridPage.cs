using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class GridPage : Page
    {
        public Grid Grid { get; private set; }
        public TextBox FilterTextBox { get; private set; }

        public CommandBar CommandBar { get; private set; }
        private Label filterLabel;

        public GridPage()
        {
            CommandBar = Add(new CommandBar() { Y = 1 });
            filterLabel = Add(new Label() { Y = 2, Text = "Filter:".ToConsoleString(), Width = "Filter:".Length });
            FilterTextBox = Add(new TextBox() { Y = 2, X = filterLabel.Text.Length });
            Grid = Add(new Grid() { Y = 3 });
            
            Grid.FilterTextBox = FilterTextBox;
        }

        private void HandleResize()
        {
            Grid.Width = this.Width;

            if (CommandBar.Controls.Count == 0)
            {
                Grid.Y = 2;
                FilterTextBox.Y = 1;
                filterLabel.Y = 1;
                Grid.Height = this.Height - 2;
            }
            else
            {
                Grid.Y = 3;
                FilterTextBox.Y = 2;
                filterLabel.Y = 2;
                Grid.Height = this.Height - 3;
            }
            FilterTextBox.Width = this.Width;
            CommandBar.Width = this.Width;
        }

        protected override void OnLoad()
        {
            this.Synchronize(nameof(Bounds), HandleResize);
            FilterTextBox.TryFocus();
        }
    }
}
