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
            Grid.NoDataMessage = "No table entities";
            Grid.NoVisibleColumnsMessage = "Loading..."; // override NoVisibleColumnsMessage because we won't know the columns until the data arrives
            Grid.PropertyResolver = ResolveProperty;
           
            deleteButton = CommandBar.Add(new Button() { Text = "Delete entity", Shortcut = new KeyboardShortcut(ConsoleKey.Delete, null), CanFocus = false });
            CommandBar.Add(new NotificationButton(ProgressOperationManager));
            deleteButton.Activated.SubscribeForLifetime(BeginDeleteSelectedEntityIfExists, LifetimeManager);
            Grid.SubscribeForLifetime(nameof(Grid.SelectedItem), SelectedTableEntityChanged, this.LifetimeManager);
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            var accountName = RouteVariables["account"];
            var tableName = RouteVariables["table"];
            var accountInfo = (from account in StorageAccountInfo.Load() where account.AccountName == accountName select account).FirstOrDefault();
            currentStorageAccount = new CloudStorageAccount(new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(accountName, accountInfo.Key), accountInfo.UseHttps);
            table = currentStorageAccount.CreateCloudTableClient().GetTableReference(tableName);
            Grid.DataSource = new TableEntityDataSource(currentStorageAccount.CreateCloudTableClient().GetTableReference(tableName), Application.MessagePump);
            Grid.DataSource.DataChanged += OnDataLoad;
        }

        private void OnDataLoad()
        {
            if (Application != null && Grid.DataView != null && Grid.DataView.Items.Count > 0)
            {
                var prototype = Grid.DataView.Items.First() as DynamicTableEntity;
                Grid.VisibleColumns.Clear();
                Grid.VisibleColumns.Add(new ColumnViewModel("PartitionKey".ToConsoleString(Application.Theme.H1Color)));
                Grid.VisibleColumns.Add(new ColumnViewModel("RowKey".ToConsoleString(Application.Theme.H1Color)));

                for (int i = 0; i < prototype.Properties.Count && i < 2; i++)
                {
                    Grid.VisibleColumns.Add(new ColumnViewModel(prototype.Properties.Keys.ToList()[i].ToConsoleString(Application.Theme.H1Color)));
                }
            }
            else
            {
                Grid.VisibleColumns.Clear();
                Grid.NoVisibleColumnsMessage = "No table entities";
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

        private void SelectedTableEntityChanged()
        {
            deleteButton.CanFocus = Grid.SelectedItem != null;
        }

        private void BeginDeleteSelectedEntityIfExists()
        {
            if (Grid.SelectedItem == null)
            {
                return;
            }

            var rowKey = (Grid.SelectedItem as ITableEntity).RowKey;

            Dialog.ConfirmYesOrNo("Are you sure you want to delete entity ".ToConsoleString() + rowKey.ToConsoleString(ConsoleColor.Yellow) + "?", () =>
             {
                 var entityToDelete = Grid.SelectedItem as ITableEntity;

                 ProgressOperation operation = new ProgressOperation()
                 {
                     State = OperationState.InProgress,
                     Message = "Deleting entity ".ToConsoleString() + rowKey.ToConsoleString(Application.Theme.H1Color) + " from table " + table.Name,
                 };

                 ProgressOperationManager.Operations.Add(operation);

                 var applicationRef = Application;
                 Application.MessagePump.QueueAsyncAction(table.ExecuteAsync(TableOperation.Delete(entityToDelete)), (t) =>
                 {
                      if (t.Exception != null)
                      {
                          operation.Message = "Failed to delete entity ".ToConsoleString(ConsoleColor.Red) + rowKey.ToConsoleString(applicationRef.Theme.H1Color) + " from table " + table.Name;
                          operation.Details = t.Exception.ToString().ToConsoleString();
                          operation.State = OperationState.Failed;
                      }
                      else
                      {
                          operation.Message = "Finished deleting entity ".ToConsoleString() + rowKey.ToConsoleString(applicationRef.Theme.H1Color) + " from table " + table.Name;
                          operation.State = OperationState.Completed;
                      }

                     Grid.NoVisibleColumnsMessage = "Loading...";
                     Grid.DataSource.ClearCachedData();
                     Grid.Refresh();
                  });
             });
        }
    }
}
