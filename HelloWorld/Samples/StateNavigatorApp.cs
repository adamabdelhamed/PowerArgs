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
            var statesData = new GridViewModel(StatePickerAssistant.States.Select(s => new State { Name = s } as object).ToList());

            // initialize controls
            Grid statesGrid = homePage.Add(new Grid(statesData) { Y = 2 });
            Label filterLabel = homePage.Add(new Label() { Y = 1, Text = "Filter:".ToConsoleString(), Width = "Filter:".Length });
            TextBox filterTextBox = homePage.Add(new TextBox() { Y=1, X = filterLabel.Text.Length });
            Label debugLabel = homePage.Add(new Label() { Background = ConsoleColor.Red});

            // connect controls
            statesGrid.FilterTextBox = filterTextBox;

            homePage.Loaded += () =>
            {
                statesGrid.Width = homePage.Width;
                statesGrid.Height = homePage.Height-2;
                filterTextBox.Width = homePage.Width;
                debugLabel.Y = homePage.Height-1;
                debugLabel.X = homePage.Width - 30;

                homePage.Application.FocusManager.PropertyChanged += (sender, e) =>
                {
                    if (homePage.Application == null) return;
                    debugLabel.Text = new ConsoleString(homePage.Application.FocusManager.FocusedControl == null ? "null" : homePage.Application.FocusManager.FocusedControl.GetType().Name, ConsoleColor.Red);
                    debugLabel.X = homePage.Application.LayoutRoot.Width - debugLabel.Width - 1;
                };
                statesGrid.TryFocus();
            };

            homePage.Unloaded += () => 
            {
                filterTextBox.Value = ConsoleString.Empty;
            };


            statesGrid.ViewModel.SelectedItemActivated += () =>
            {
                Dialog.ConfirmYesOrNo("Are you sure you want tp navigate to ".ToConsoleString() + (statesGrid.ViewModel.SelectedItem as State).Name.ToConsoleString(ConsoleColor.Yellow) + "?", () =>
                {
                    homePage.PageStack.Navigate("states/" + (statesGrid.ViewModel.SelectedItem as State).Name);
                });
            };
        }

        private void InitStatePage()
        {
            statePage = new Page();
            var stateLabel = statePage.Add(new Label() { Height = 1, Y = 1, Foreground = ConsoleColor.Green});
            Label debugLabel = statePage.Add(new Label() { Background = ConsoleColor.Red });
            statePage.Loaded += () =>
            {
                stateLabel.Width = statePage.Width;
                stateLabel.Text = "You picked ".ToConsoleString(ConsoleColor.Gray)+ statePage.RouteVariables["state"].ToConsoleString(ConsoleColor.Green);

                debugLabel.Y = statePage.Height - 1;

                statePage.Application.FocusManager.PropertyChanged += (sender, e) =>
                {
                    if (statePage.Application == null) return;
                    debugLabel.Text = new ConsoleString(statePage.Application.FocusManager.FocusedControl == null ? "null" : statePage.Application.FocusManager.FocusedControl.GetType().Name, ConsoleColor.Red);
                    debugLabel.X = statePage.Application.LayoutRoot.Width - debugLabel.Width - 1;
                };
                var worked = statePage.BreadcrumbBar.Controls.FirstOrDefault()?.TryFocus();
            };
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
