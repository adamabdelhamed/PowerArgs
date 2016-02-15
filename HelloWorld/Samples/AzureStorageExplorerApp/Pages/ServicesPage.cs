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
            CommandBar.Add(new NotificationButton(ProgressOperationManager));

            Grid.DataSource = new MemoryDataSource() { Items = new List<object>() { new Service("containers"), new Service("tables"), new Service("queues") } };
            Grid.VisibleColumns.Add(new ColumnViewModel(nameof(Service.Name).ToConsoleString(Theme.DefaultTheme.H1Color)));
            Grid.SelectedItemActivated += Navigate;
        }

        private void Navigate()
        {
            var route = $"accounts/{RouteVariables["account"]}/{(Grid.SelectedItem as Service).Name}";
            if(PageStack.TryNavigate(route) == false)
            {
                Dialog.ShowMessage("Not implemented");
            }
        }
    }
}
