using PowerArgs;
using PowerArgs.Cli;
using System.Collections.Generic;
using System;

namespace HelloWorld.Samples
{
    public class StorageAccountsPage : GridPage
    {
        Button addButton;
        Button deleteButton;

        public StorageAccountsPage()
        {
            Grid.DataSource = new MemoryDataSource() { Items = new List<object>(StorageAccountInfo.Load()) };
            Grid.VisibleColumns.Add(new ColumnViewModel(nameof(StorageAccountInfo.AccountName).ToConsoleString(Theme.DefaultTheme.H1Color)));
            Grid.VisibleColumns.Add(new ColumnViewModel(nameof(StorageAccountInfo.Key).ToConsoleString(Theme.DefaultTheme.H1Color)));
            Grid.VisibleColumns.Add(new ColumnViewModel(nameof(StorageAccountInfo.UseHttps).ToConsoleString(Theme.DefaultTheme.H1Color)));
            Grid.NoDataMessage = "No storage accounts";
            Grid.RegisterKeyHandler(ConsoleKey.Delete,  HandleGridDeleteKeyPress);
            addButton = CommandBar.Add(new Button() { Text = "Add account", Shortcut = new KeyboardShortcut(ConsoleKey.A, ConsoleModifiers.Alt) });
            deleteButton = CommandBar.Add(new Button() { Text = "Forget account", CanFocus=false, Shortcut = new KeyboardShortcut(ConsoleKey.F, ConsoleModifiers.Alt) });
            CommandBar.Add(new NotificationButton(ProgressOperationManager));

            addButton.Activated.SubscribeForLifetime(AddStorageAccount, LifetimeManager);
            deleteButton.Activated.SubscribeForLifetime(ForgetSelectedStorageAccount, LifetimeManager);

            Grid.SelectedItemActivated += NavigateToStorageAccount;
        }

        public override void OnAddedToVisualTree()
        {
            base.OnAddedToVisualTree();
            Grid.Subscribe(nameof(Grid.SelectedItem), SelectedItemChanged);
        }



        private void HandleGridDeleteKeyPress(ConsoleKeyInfo key)
        {
            if(Grid.SelectedItem != null)
            {
                ForgetSelectedStorageAccount();
            }
        }

        private void SelectedItemChanged()
        {
            deleteButton.CanFocus = Grid.SelectedItem != null;
        }

        private void ForgetSelectedStorageAccount()
        {
            var selectedAccount = Grid.SelectedItem as StorageAccountInfo;
            Dialog.ConfirmYesOrNo("Are you sure you want to forget storage account " + selectedAccount.AccountName, () =>
            {
                (Grid.DataSource as MemoryDataSource).Items.Remove(selectedAccount);
                StorageAccountInfo.Save((Grid.DataSource as MemoryDataSource).Items);
                PageStack.TryRefresh();
            });
        }

        private void NavigateToStorageAccount()
        {
            var accountName = (Grid.SelectedItem as StorageAccountInfo).AccountName;
            PageStack.Navigate("accounts/" + accountName);
        }

        private void AddStorageAccount()
        {
            Dialog.ShowTextInput("Enter storage account name".ToConsoleString(), (name) =>
            {
                Dialog.ShowTextInput("Enter storage account key".ToConsoleString(), (key) =>
                {
                    var data = (Grid.DataSource as MemoryDataSource).Items;
                    data.Add(new StorageAccountInfo() { AccountName = name.ToString(), Key = key.ToString(), UseHttps = true });
                    StorageAccountInfo.Save(data);
                    (Grid.DataSource as MemoryDataSource).Invalidate();
                });
            });
        }
    }
}
