using PowerArgs.Cli;
using System;
using System.Linq;
namespace PowerArgs.Cli
{
    public class LogTailControl : ConsolePanel
    {
        public int MaxLines { get; set; } = 100;
        private ScrollablePanel scrollPanel;

        private StackPanel logStack;

        public Label CurrentLine => logStack.Controls.WhereAs<Label>().LastOrDefault();
          
        public LogTailControl()
        {
            scrollPanel = Add(new ScrollablePanel()).Fill();
            logStack = scrollPanel.ScrollableContent.Add(new StackPanel() { Orientation = Orientation.Vertical }).FillHorizontally();
        }

        public void AppendLine(ConsoleString str) => Append(str + "\n".ToConsoleString());

        public void Append(ConsoleString str)
        {
            if (CurrentLine == null) AddLine();

            var lines = str.Split("\n");
            for(var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                CurrentLine.Text += line;
                AddLine();
            }

            while(logStack.Controls.Count > MaxLines+1) // +1 accounts for the empty line we usually have
            {
                logStack.Controls.RemoveAt(0);
            }

            
            scrollPanel.VerticalScrollUnits = Math.Max(0, logStack.Height - this.Height);
           
           
        }

        private void AddLine()
        {
            logStack.Add(new Label() { Text = ConsoleString.Empty, Mode = LabelRenderMode.ManualSizing }).FillHorizontally();
            logStack.Height = logStack.Controls.Count;
        }
    }
}
