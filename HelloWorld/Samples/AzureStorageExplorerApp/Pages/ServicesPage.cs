using PowerArgs;
using PowerArgs.Cli;
using System.Collections.Generic;
using System;

namespace HelloWorld.Samples
{
    public class ServicesPage : GridPage
    {
        public class Service
        {
            [Filterable]
            public string Name { get; private set; }
            public Service(string name)
            {
                this.Name = name;
            }
        }

        public ServicesPage()
        {
            Grid.ViewModel.DataSource = new MemoryDataSource() { Items = new List<object>() { new Service("containers"), new Service("tables"), new Service("queues") } };
            Grid.ViewModel.VisibleColumns.Add(new ColumnViewModel(nameof(Service.Name).ToConsoleString(Theme.DefaultTheme.H1Color)));
            Grid.ViewModel.SelectedItemActivated += Navigate;
        }

        private void Navigate()
        {
            var route = $"accounts/{RouteVariables["account"]}/{(Grid.ViewModel.SelectedItem as Service).Name}";
            if(PageStack.TryNavigate(route) == false)
            {
                Dialog.ShowMessage("Not implemented");
            }
        }
    }
}
