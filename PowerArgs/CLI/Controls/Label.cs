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
        internal static readonly ConsoleString Null = "<null>".ToConsoleString(ConsoleColor.Gray);
        public override bool CanFocus
        {
            get
            {
                return false;
            }

            set
            {
                base.CanFocus = value;
            }
        }

        public ConsoleString Text { get { return Get<ConsoleString>(); } set { Set(value); } }

        public Label()
        {
            Height = 1;
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
                        Text = val.ToString().ToConsoleString(Foreground.ForegroundColor);
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
                context.DrawString(Text, 0, 0);
            }
        }
    }
}
