using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli
{
    public class Label : ConsoleControl
    {
        internal static readonly ConsoleString Null = "<null>".ToConsoleString(Theme.DefaultTheme.DisabledColor);

        public ConsoleString Text { get { return Get<ConsoleString>(); } set
            {
                Set(value);
                if(AutoSize)
                {
                    Width = value.Length;
                }
            } }

        public bool AutoSize { get; set; }

        public Label()
        {
            Height = 1;
            this.AutoSize = true;
            this.CanFocus = false;
        }

        public void Bind(INotifyPropertyChanged observable, string propertyName)
        {
            observable.PropertyChanged += (sender, args) =>
            {
                if(args.PropertyName == propertyName)
                {
                    var val = sender.GetType().GetProperty(propertyName).GetValue(sender);
                    if(val == null)
                    {
                        Text = Null;
                    }
                    else if(val is ConsoleString)
                    {
                        Text = val as ConsoleString;
                    }
                    else
                    {
                        Text = val.ToString().ToConsoleString(Foreground);
                    }
                }
            };
        }

        internal override void OnPaint(ConsoleBitmap context)
        {
            if (Text == null)
            {
                context.DrawString(Null, 0, 0);
            }
            else
            {
                context.DrawString(HasFocus ? new ConsoleString(Text.ToString(),Application.Theme.FocusContrastColor, Application.Theme.FocusColor) : Text, 0, 0);
            }
        }
    }
}
