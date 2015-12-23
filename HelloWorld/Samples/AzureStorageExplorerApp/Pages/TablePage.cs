using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using PowerArgs;
using PowerArgs.Cli;
using System;
using System.Linq;

namespace HelloWorld.Samples
{
    public class TablePage : GridPage
    {
        CloudStorageAccount currentStorageAccount;
        Button deleteButton;
        CloudTable table;
        public TablePage()
        {
            Grid.ViewModel.NoDataMessage = "No table entities";
            Grid.ViewModel.NoVisibleColumnsMessage = "Loading..."; // override NoVisibleColumnsMessage because we won't know the columns until the data arrives
            Grid.ViewModel.PropertyResolver = ResolveProperty;
            Grid.ViewModel.PropertyChanged += SelectedItemChanged;
            Grid.KeyInputReceived += HandleGridDeleteKeyPress;

            deleteButton = CommandBar.Add(new Button() { Text = "Delete entity", CanFocus = false });

            deleteButton.Activated += DeleteSelectedEntity;
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            var accountName = RouteVariables["account"];
            var tableName = RouteVariables["table"];
            var accountInfo = (from account in StorageAccountInfo.Load() where account.AccountName == accountName select account).FirstOrDefault();
            currentStorageAccount = new CloudStorageAccount(new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(accountName, accountInfo.Key), accountInfo.UseHttps);
            table = currentStorageAccount.CreateCloudTableClient().GetTableReference(tableName);
            Grid.ViewModel.DataSource = new TableEntityDataSource(currentStorageAccount.CreateCloudTableClient().GetTableReference(tableName), Application.MessagePump);
            Grid.ViewModel.DataSource.DataChanged += RespondToDataLoad;
        }

        private void RespondToDataLoad()
        {
            if (Grid.ViewModel.DataView != null && Grid.ViewModel.DataView.Items.Count > 0)
            {
                var prototype = Grid.ViewModel.DataView.Items.First() as DynamicTableEntity;
                Grid.ViewModel.VisibleColumns.Clear();
                Grid.ViewModel.VisibleColumns.Add(new ColumnViewModel("PartitionKey".ToConsoleString(Application.Theme.H1Color)));
                Grid.ViewModel.VisibleColumns.Add(new ColumnViewModel("RowKey".ToConsoleString(Application.Theme.H1Color)));

                for (int i = 0; i < prototype.Properties.Count && i < 2; i++)
                {
                    Grid.ViewModel.VisibleColumns.Add(new ColumnViewModel(prototype.Properties.Keys.ToList()[i].ToConsoleString(Application.Theme.H1Color)));
                }
            }
            else
            {
                Grid.ViewModel.VisibleColumns.Clear();
                Grid.ViewModel.NoVisibleColumnsMessage = "No table entities";
            }
        }

        private object ResolveProperty(object item, string propName)
        {
            var dynamicEntity = (item as DynamicTableEntity);

            var prop = dynamicEntity?.GetType()?.GetProperty(propName)?.GetValue(item);
            
            if (prop == null && dynamicEntity.Properties.ContainsKey(propName))
            {
                return dynamicEntity.Properties[propName].PropertyAsObject;
            }
            else
            {
                return prop;
            }
        }

        private void SelectedItemChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GridViewModel.SelectedItem))
            {
                deleteButton.CanFocus = Grid.ViewModel.SelectedItem != null;
            }
        }

        private void HandleGridDeleteKeyPress(ConsoleKeyInfo key)
        {
            if (key.Key == ConsoleKey.Delete && Grid.ViewModel.SelectedItem != null)
            {
                DeleteSelectedEntity();
            }
        }

        private void DeleteSelectedEntity()
        {
            Dialog.ConfirmYesOrNo("Are you sure you want ot delete entity " + (Grid.ViewModel.SelectedItem as ITableEntity).RowKey+ "?", () =>
            {
                var entityToDelete = Grid.ViewModel.SelectedItem as ITableEntity;
                var t = table.ExecuteAsync(TableOperation.Delete(entityToDelete));
                t.ContinueWith((tPrime) =>
                {
                    if (Application != null)
                    {
                        PageStack.TryRefresh();
                    }
                });                     
            });
        }
    }
}
