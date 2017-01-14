using PowerArgs;
using PowerArgs.Cli;

namespace ArgsTests
{
    public class BasicXmlAppViewModel : ObservableObject
    {
        public Customer Customer { get; set; }

        public BasicXmlAppViewModel()
        {
            Customer = new Customer();
        }

        public void SubmitClicked()
        {
            ConsoleApp.Current.Stop();
        }
    }

    public class Customer : ObservableObject
    {
        public ConsoleString Name { get { return Get<ConsoleString>(); } set { Set(value); } }
    }
}
