using PowerArgs;
using PowerArgs.Cli;
using System;

namespace ArgsTests
{
    public class BasicXmlAppViewModel : ObservableObject
    {
        public ConsoleString Name { get { return Get<ConsoleString>(); } set { Set(value); } }

        public BasicXmlAppViewModel()
        {
            this.Name = new ConsoleString("", ConsoleColor.Green);
        }

        public void SubmitClicked()
        {
            ConsoleApp.Current.Stop();
        }
    }
}
