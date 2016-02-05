using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using PowerArgs;
using PowerArgs.Cli;
using System;
using System.Linq;

namespace HelloWorld.Samples
{
    public class ContainerRecord
    {
        public CloudBlobContainer Container { get; private set; }

        public string Name { get { return Container.Name; } }

        public ContainerRecord(CloudBlobContainer container)
        {
            this.Container = container;
        }
    }

    public class ContainersPage : GridPage
    {
        CloudStorageAccount currentStorageAccount;
        private Button deleteButton;
        private Button addButton;
        public ContainersPage()
        {
            Grid.VisibleColumns.Add(new ColumnViewModel(nameof(CloudBlobContainer.Name).ToConsoleString(Theme.DefaultTheme.H1Color)));
            Grid.NoDataMessage = "No containers";

            addButton = CommandBar.Add(new Button() { Text = "Add container", Shortcut = new KeyboardShortcut(ConsoleKey.A, true) });
            deleteButton = CommandBar.Add(new Button() { Text = "Delete container", CanFocus = false, Shortcut = new KeyboardShortcut(ConsoleKey.Delete, false) });

            addButton.Activated += AddContainer;
            deleteButton.Activated += DeleteSelectedContainer;
            Grid.SelectedItemActivated += NavigateToContainer;

            CommandBar.Add(new NotificationButton(ProgressOperationManager));
        }

        public override void OnAddedToVisualTree()
        {
            base.OnAddedToVisualTree();
            Grid.Subscribe(nameof(Grid.SelectedItem), SelectedItemChanged);
        }

        private void NavigateToContainer()
        {
            var containerName = (Grid.SelectedItem as ContainerRecord).Name;
            PageStack.Navigate("accounts/" + currentStorageAccount.Credentials.AccountName + "/containers/" + containerName);
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            var accountName = RouteVariables["account"];
            var accountInfo = (from account in StorageAccountInfo.Load() where account.AccountName == accountName select account).FirstOrDefault();
            currentStorageAccount = new CloudStorageAccount(new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(accountName, accountInfo.Key), accountInfo.UseHttps);
            Grid.DataSource = new ContainerListDataSource(currentStorageAccount.CreateCloudBlobClient(), Application.MessagePump);
        }

        private void AddContainer()
        {
            Dialog.ShowTextInput("Enter container name".ToConsoleString(), (name) =>
            {
                if (name != null)
                {
                    var t = currentStorageAccount.CreateCloudBlobClient().GetContainerReference(name.ToString()).CreateAsync();

                    Application.MessagePump.QueueAsyncAction(t, (tp) =>
                    {
                        if (Application != null)
                        {
                            PageStack.TryRefresh();
                        }
                    });
                }
            });
        }

        private void DeleteSelectedContainer()
        {
            Dialog.ConfirmYesOrNo("Are you sure you want ot delete container " + (Grid.SelectedItem as ContainerRecord).Name + "?", () =>
            {
                var container = currentStorageAccount.CreateCloudBlobClient().GetContainerReference((Grid.SelectedItem as ContainerRecord).Name);
                
                Application.MessagePump.QueueAsyncAction(container.DeleteAsync(), (tp) =>
                {
                    if (Application != null && PageStack.CurrentPage == this)
                    {
                        PageStack.TryRefresh();
                    }
                });
            });
        }

        private void SelectedItemChanged()
        {
            deleteButton.CanFocus = Grid.SelectedItem != null;
        }
    }
}
