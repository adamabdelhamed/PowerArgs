using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using PowerArgs;
using PowerArgs.Cli;
using System;
using System.Linq;

namespace HelloWorld.Samples
{
    public class TablesPage : GridPage
    {
        CloudStorageAccount currentStorageAccount;
        private Button deleteButton;
        private Button addButton;
        public TablesPage()
        {
            Grid.ViewModel.VisibleColumns.Add(new ColumnViewModel(nameof(CloudTable.Name).ToConsoleString(Theme.DefaultTheme.H1Color)));
            Grid.ViewModel.NoDataMessage = "No tables";
            Grid.ViewModel.NoVisibleColumnsMessage = "Loading...";
            addButton = CommandBar.Add(new Button() { Text = "Add table" });
            deleteButton = CommandBar.Add(new Button() { Text = "Delete table", CanFocus = false });

            addButton.Activated += AddTable;
            Grid.ViewModel.PropertyChanged += SelectedItemChanged;
            deleteButton.Activated += DeleteSelectedTable;
            Grid.KeyInputReceived += HandleGridDeleteKeyPress;
            Grid.ViewModel.SelectedItemActivated += NavigateToTable;
        }

        private void NavigateToTable()
        {
            var tableName = (Grid.ViewModel.SelectedItem as CloudTable).Name;
            PageStack.Navigate("accounts/" + currentStorageAccount.Credentials.AccountName + "/tables/" + tableName);
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            var accountName = RouteVariables["account"];
            var accountInfo = (from account in StorageAccountInfo.Load() where account.AccountName == accountName select account).FirstOrDefault();
            currentStorageAccount = new CloudStorageAccount(new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(accountName, accountInfo.Key), accountInfo.UseHttps);
            Grid.ViewModel.DataSource = new TableListDataSource(currentStorageAccount.CreateCloudTableClient(), Application.MessagePump);
        }

        private void HandleGridDeleteKeyPress(ConsoleKeyInfo key)
        {
            if (key.Key == ConsoleKey.Delete && Grid.ViewModel.SelectedItem != null)
            {
                DeleteSelectedTable();
            }
        }

        private void AddTable()
        {
            Dialog.ShowTextInput("Enter table name".ToConsoleString(), (name) =>
            {
                if (name != null)
                {
                    var t = currentStorageAccount.CreateCloudTableClient().GetTableReference(name.ToString()).CreateAsync();
                    t.ContinueWith((tPrime) =>
                    {
                        if (Application != null)
                        {
                            PageStack.TryRefresh();
                        }
                    });
                }
            });
        }

        private void DeleteSelectedTable()
        {
            Dialog.ConfirmYesOrNo("Are you sure you want ot delete table " + (Grid.ViewModel.SelectedItem as CloudTable).Name + "?", () =>
            {
                var table = currentStorageAccount.CreateCloudTableClient().GetTableReference((Grid.ViewModel.SelectedItem as CloudTable).Name);
                var t = table.DeleteAsync();
                t.ContinueWith((tPrime) =>
                {
                    if (Application != null)
                    {
                        PageStack.TryRefresh();
                    }
                });
            });
        }

        private void SelectedItemChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GridViewModel.SelectedItem))
            {
                deleteButton.CanFocus = Grid.ViewModel.SelectedItem != null;
            }
        }
    }
}
