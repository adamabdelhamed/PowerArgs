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
            Grid.ViewModel.DataSource = new MemoryDataSource() { Items = new List<object>(StorageAccountInfo.Load()) };
            Grid.ViewModel.VisibleColumns.Add(new ColumnViewModel(nameof(StorageAccountInfo.AccountName).ToConsoleString(Theme.DefaultTheme.H1Color)));
            Grid.ViewModel.VisibleColumns.Add(new ColumnViewModel(nameof(StorageAccountInfo.Key).ToConsoleString(Theme.DefaultTheme.H1Color)));
            Grid.ViewModel.VisibleColumns.Add(new ColumnViewModel(nameof(StorageAccountInfo.UseHttps).ToConsoleString(Theme.DefaultTheme.H1Color)));
            Grid.ViewModel.NoDataMessage = "No storage accounts";
            Grid.KeyInputReceived += HandleGridDeleteKeyPress;
            Grid.ViewModel.PropertyChanged += SelectedItemChanged;
            addButton = CommandBar.Add(new Button() { Text = "Add account" });
            deleteButton = CommandBar.Add(new Button() { Text = "Forget account", CanFocus=false });

            addButton.Activated += AddStorageAccount;
            deleteButton.Activated += ForgetSelectedStorageAccount;

            Grid.ViewModel.SelectedItemActivated += NavigateToStorageAccount;
        }



        private void HandleGridDeleteKeyPress(ConsoleKeyInfo key)
        {
            if(key.Key == ConsoleKey.Delete && Grid.ViewModel.SelectedItem != null)
            {
                ForgetSelectedStorageAccount();
            }
        }

        private void SelectedItemChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GridViewModel.SelectedItem))
            {
                deleteButton.CanFocus = Grid.ViewModel.SelectedItem != null;
            }
        }

        private void ForgetSelectedStorageAccount()
        {
            var selectedAccount = Grid.ViewModel.SelectedItem as StorageAccountInfo;
            Dialog.ConfirmYesOrNo("Are you sure you want to forget storage account " + selectedAccount.AccountName, () =>
            {
                (Grid.ViewModel.DataSource as MemoryDataSource).Items.Remove(selectedAccount);
                StorageAccountInfo.Save((Grid.ViewModel.DataSource as MemoryDataSource).Items);
                PageStack.TryRefresh();
            });
        }

        private void NavigateToStorageAccount()
        {
            var accountName = (Grid.ViewModel.SelectedItem as StorageAccountInfo).AccountName;
            PageStack.Navigate("accounts/" + accountName);
        }

        private void AddStorageAccount()
        {
            Dialog.ShowTextInput("Enter storage account name".ToConsoleString(), (name) =>
            {
                Dialog.ShowTextInput("Enter storage account key".ToConsoleString(), (key) =>
                {
                    var data = (Grid.ViewModel.DataSource as MemoryDataSource).Items;
                    data.Add(new StorageAccountInfo() { AccountName = name.ToString(), Key = key.ToString(), UseHttps = true });
                    StorageAccountInfo.Save(data);
                    (Grid.ViewModel.DataSource as MemoryDataSource).Invalidate();
                });
            });
        }
    }
}
