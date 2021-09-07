using System;
using System.Collections.Generic;
using System.Linq;
namespace PowerArgs.Cli
{
    public class LogTailControl : ConsolePanel
    {
        public int MaxLines { get; set; } = 1000;
        private ScrollablePanel scrollPanel;

        private Label logLabel;

        private List<ConsoleString> logLines = new List<ConsoleString>() { ConsoleString.Empty };
        public LogTailControl()
        {
            scrollPanel = Add(new ScrollablePanel()).Fill();
            logLabel = scrollPanel.ScrollableContent.Add(new Label() { Text = ConsoleString.Empty, Mode = LabelRenderMode.ManualSizing }).FillHorizontally();
            this.SubscribeForLifetime(nameof(Background),() => 
            {
                scrollPanel.Background = Background;
                logLabel.Background = Background;
            }, this);
        }

        public void AppendLine(ConsoleString str) => Append(str + "\n".ToConsoleString());

        public void Append(ConsoleString str)
        {
            foreach (var ch in str)
            {
                var c = ch.ToConsoleString();
                if (c.StringValue == "\n")
                {
                    WriteNewlineInternal();
                }
                else
                {
                    if (logLines[logLines.Count - 1].Length > logLabel.Width - 10 && c.StringValue == " ")
                    {
                        WriteNewlineInternal();
                    }
                    else if (logLines[logLines.Count - 1].Length > logLabel.Width - 3)
                    {
                        WriteNewlineInternal();
                        logLines[logLines.Count - 1] = (logLines[logLines.Count - 1]) + c;
                    }
                    else
                    {
                        logLines[logLines.Count - 1] = (logLines[logLines.Count - 1]) + c;
                    }
                }
            }
            var linesUsed = 0;
            var text = ConsoleString.Empty;
            for (var i = logLines.Count - 1; i >= 0; i--)
            {
                var line = logLines[i];
                text = line + "\n" + text;
                linesUsed++;
                
            }
            logLabel.Text = text;
            logLabel.Height = linesUsed;
            scrollPanel.ScrollableContent.Height = linesUsed;
            var focused = Application.FocusManager.FocusedControl;
            if (focused is Scrollbar && Descendents.Contains(focused))
            {
                // do nothing since the scrollbar is in focus
            }
            else
            {
                scrollPanel.VerticalScrollUnits = Math.Max(0, logLabel.Height - this.Height);
            }
            this.FirePropertyChanged(nameof(Bounds));
        }

        private void WriteNewlineInternal()
        {
            logLines.Add(ConsoleString.Empty);
            while (logLines.Count > MaxLines)
            {
                logLines.RemoveAt(0);
            }
        }
    }
}
