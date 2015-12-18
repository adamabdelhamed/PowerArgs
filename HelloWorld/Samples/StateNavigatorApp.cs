using PowerArgs;
using PowerArgs.Cli;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HelloWorld.Samples
{
    public class StateNavigatorApp
    {
        public class State
        {
            [Filterable]
            public string Name { get; set; }

        }

        private Page homePage, statePage;

        public StateNavigatorApp()
        {
            InitHomePage();
            InitStatePage();
        }

        private void InitHomePage()
        {
            homePage = new Page();

            var statesModel = new GridViewModel(StatePickerAssistant.States.Select(s => new State { Name = s } as object).ToList());
            
            Grid statesGrid = new Grid(statesModel) { Y = 2 };
            Label filterLabel = new Label() { Y = 1, Text  = "Filter:".ToConsoleString(), Width = "Filter:" .Length};
            TextBox filterTextBox = new TextBox() { Y=1, X = filterLabel.Text.Length };
            statesGrid.FilterTextBox = filterTextBox;
            homePage.Controls.Add(filterTextBox);
            homePage.Controls.Add(statesGrid);
            homePage.Controls.Add(filterLabel);
            homePage.Added += () =>
            {
                homePage.Width = homePage.Application.LayoutRoot.Width;
                homePage.Height = homePage.Application.LayoutRoot.Height;
                statesGrid.Width = homePage.Width;
                statesGrid.Height = homePage.Height-2;
                filterTextBox.Width = homePage.Width;
            };

            homePage.Unloaded += () => 
            {
                filterTextBox.Value = ConsoleString.Empty;
            };

            statesGrid.ViewModel.SelectedItemActivated += () =>
            {
                (statesGrid.Application as ConsolePageApp).PageStack.Navigate("states/"+(statesGrid.ViewModel.SelectedItem as State).Name);
            };
        }

        private void InitStatePage()
        {
            statePage = new Page();
            var stateLabel = new Label() { Height = 1, Y = 1, Foreground = new ConsoleCharacter(' ', ConsoleColor.Green) };

            statePage.Added += () =>
            {
                statePage.Width = statePage.Application.LayoutRoot.Width;
                statePage.Height = statePage.Application.LayoutRoot.Height;
                stateLabel.Width = statePage.Width;
            };

            statePage.Loaded += () =>
            {
                stateLabel.Text = "You picked ".ToConsoleString(ConsoleColor.Gray)+ statePage.RouteVariables["state"].ToConsoleString(ConsoleColor.Green);
            };

            statePage.Controls.Add(stateLabel);
        }


        public Task Start()
        {
            ConsolePageApp app = new ConsolePageApp(0, 0, ConsoleProvider.Current.BufferWidth, 20);
            app.PageStack.RegisterDefaultRoute("states", ()=> homePage);
            app.PageStack.RegisterRoute("states/{state}", () => statePage);
            app.PageStack.Navigate("");
            return app.Start();
        }
    }
}
