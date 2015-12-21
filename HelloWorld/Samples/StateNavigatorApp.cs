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

            public string Country
            {
                get { return "USA"; }
            }

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

            // initialize data
            var statesModel = new GridViewModel(StatePickerAssistant.States.Select(s => new State { Name = s } as object).ToList());

            // initialize controls
            Grid statesGrid = new Grid(statesModel) { Y = 2 };
            Label filterLabel = new Label() { Y = 1, Text  = "Filter:".ToConsoleString(), Width = "Filter:" .Length};
            TextBox filterTextBox = new TextBox() { Y=1, X = filterLabel.Text.Length };

            // connect controls
            statesGrid.FilterTextBox = filterTextBox;

            homePage.Controls.Add(filterTextBox);
            homePage.Controls.Add(statesGrid);
            homePage.Controls.Add(filterLabel);

            homePage.Loaded += () =>
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
                Dialog.Show("Are you sure you want tp navigate to ".ToConsoleString() + (statesGrid.ViewModel.SelectedItem as State).Name.ToConsoleString(ConsoleColor.Yellow) + "?", (choice) =>
                {
                    if (choice != null && choice.DisplayText == "Yes")
                    {   
                        homePage.PageStack.Navigate("states/" + (statesGrid.ViewModel.SelectedItem as State).Name);
                    }
                }, true, new DialogButton() { DisplayText = "Yes" }, new DialogButton() { DisplayText = "No" });
            };
        }

        private void InitStatePage()
        {
            statePage = new Page();
            var stateLabel = new Label() { Height = 1, Y = 1, Foreground = new ConsoleCharacter(' ', ConsoleColor.Green) };
            
            statePage.Loaded += () =>
            {
                statePage.Width = statePage.Application.LayoutRoot.Width;
                statePage.Height = statePage.Application.LayoutRoot.Height;
                stateLabel.Width = statePage.Width;
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
