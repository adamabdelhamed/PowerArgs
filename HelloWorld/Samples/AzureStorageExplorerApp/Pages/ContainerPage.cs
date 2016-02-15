using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using PowerArgs;
using PowerArgs.Cli;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace HelloWorld.Samples
{
    public class ContainerPage : GridPage
    {
        public const string SizeColumn = "Size";
        CloudStorageAccount currentStorageAccount;
        CloudBlobContainer container;
        private Button deleteButton;
        private Button uploadButton;
        private Button openButton;

        public ContainerPage()
        {
            Grid.VisibleColumns.Add(new ColumnViewModel(nameof(CloudBlob.Name).ToConsoleString(Theme.DefaultTheme.H1Color)));
            Grid.VisibleColumns.Add(new ColumnViewModel(SizeColumn.ToConsoleString(Theme.DefaultTheme.H1Color)));
            Grid.NoDataMessage = "No blobs";

            uploadButton = CommandBar.Add(new Button() { Text = "Upload blob", Shortcut = new KeyboardShortcut(ConsoleKey.U, ConsoleModifiers.Alt) });
            deleteButton = CommandBar.Add(new Button() { Text = "Delete blob", CanFocus = false, Shortcut = new KeyboardShortcut(ConsoleKey.Delete, null) });
            openButton = CommandBar.Add(new Button() { Text = "Open blob", CanFocus = false, Shortcut = new KeyboardShortcut(ConsoleKey.O, ConsoleModifiers.Alt) });

            uploadButton.Activated.SubscribeForLifetime(UploadBlob, LifetimeManager);
            deleteButton.Activated.SubscribeForLifetime(DeleteSelectedBlob, LifetimeManager);
            openButton.Activated.SubscribeForLifetime(OpenSelectedBlob, LifetimeManager);

            Grid.PropertyResolver = (o, prop) =>
            {
                if(prop == nameof(CloudBlob.Name))
                {
                    return (o as CloudBlob).Name;
                }
                else if(prop == SizeColumn)
                {
                    return Friendlies.ToFriendlyFileSize((o as CloudBlockBlob).Properties.Length);
                }
                else
                {
                    throw new NotSupportedException("Property not supported: "+prop);
                }
            };

            CommandBar.Add(new NotificationButton(ProgressOperationManager));

            Grid.SubscribeForLifetime(nameof(Grid.SelectedItem), SelectedItemChanged, this.LifetimeManager);
        }
 

        protected override void OnLoad()
        {
            base.OnLoad();
            var accountName = RouteVariables["account"];
            var containerName = RouteVariables["container"];
            var accountInfo = (from account in StorageAccountInfo.Load() where account.AccountName == accountName select account).FirstOrDefault();
            currentStorageAccount = new CloudStorageAccount(new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(accountName, accountInfo.Key), accountInfo.UseHttps);

            var client = currentStorageAccount.CreateCloudBlobClient();

            container = client.GetContainerReference(containerName);
            Grid.DataSource = new BlobDataSource(container, Application.MessagePump);
        }

        private void OpenSelectedBlob()
        {
            var blob = Grid.SelectedItem as CloudBlob;
            var tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), GetFileName(blob));

            var operation = new ProgressOperation()
            {
                State = OperationState.InProgress,
                Message = "Downloading blob ".ToConsoleString()+blob.Uri.ToString().ToConsoleString(ConsoleColor.Cyan)
            };

            ProgressOperationManager.Operations.Add(operation);

            Application.MessagePump.QueueAsyncAction(blob.DownloadToFileAsync(tempFile, FileMode.OpenOrCreate), (t) =>
            {
                if (t.Exception != null)
                {
                    operation.State = OperationState.Failed;
                    operation.Message = "Failed to delete blob ".ToConsoleString() + blob.Uri.ToString().ToConsoleString(ConsoleColor.Cyan);
                    operation.Details = t.Exception.ToString().ToConsoleString();
                }
                else
                {
                    operation.State = OperationState.Completed;
                    operation.Message = "Fiinished downloading blob ".ToConsoleString() + blob.Uri.ToString().ToConsoleString(ConsoleColor.Cyan);
                    operation.Details = "The downloaded file is located here: ".ToConsoleString() + tempFile.ToConsoleString(ConsoleColor.Cyan);
                    Process.Start(tempFile);
                }
            });
        }

        private string GetFileName(CloudBlob blob)
        {
            if(blob.Name.Contains("/") == false)
            {
                return blob.Name;
            }
            else
            {
                var start = blob.Name.LastIndexOf('/')+1;
                return blob.Name.Substring(start);
            }
        }

        private void UploadBlob()
        {
            Dialog.ShowTextInput("Choose file".ToConsoleString(), (f) =>
            {
                var operation = new ProgressOperation()
                {
                    Message = "Uploading file ".ToConsoleString()+f.ToConsoleString(),
                    State = OperationState.Scheduled
                };

                ProgressOperationManager.Operations.Add(operation);

                if (File.Exists(f.ToString()) == false)
                {
                    operation.State = OperationState.Failed;
                    operation.Message = "File not found - ".ToConsoleString()+f;
                }
                else
                {
                    Dialog.ShowTextInput("Enter blob prefix".ToConsoleString(), (pre) =>
                    {
                        var blobPath = System.IO.Path.Combine(pre.ToString(), System.IO.Path.GetFileName(f.ToString()));
                        var blob = container.GetBlockBlobReference(blobPath);
                        Application.MessagePump.QueueAsyncAction(blob.UploadFromFileAsync(f.ToString(), FileMode.Open), (t) =>
                        {
                            if(t.Exception != null)
                            {
                                operation.State = OperationState.Failed;
                                operation.Message = operation.Message = "Failed to upload file ".ToConsoleString() + f.ToConsoleString();
                                operation.Details = t.Exception.ToString().ToConsoleString();
                            }
                            else
                            {
                                operation.State = OperationState.Completed;
                                operation.Message = operation.Message = "Finished uploading file ".ToConsoleString() + f.ToConsoleString();

                                if(Application != null && PageStack.CurrentPage == this)
                                {
                                    PageStack.TryRefresh();
                                }
                            }
                        });
                    },
                    ()=>
                    {
                        operation.State = OperationState.CompletedWithWarnings;
                        operation.Message = "Cancelled uploading file ".ToConsoleString() + f.ToConsoleString();
                    });
                }
            });
        }

        private void DeleteSelectedBlob()
        {
            Dialog.ConfirmYesOrNo("Are you sure you want ot delete blob " + (Grid.SelectedItem as CloudBlob).Name + "?", () =>
            {
                var operation = new ProgressOperation()
                {
                    Message = "Deleting blob ".ToConsoleString() + (Grid.SelectedItem as CloudBlob).Name.ToConsoleString(ConsoleColor.Yellow),
                    State = OperationState.InProgress
                };

                Application.MessagePump.QueueAsyncAction((Grid.SelectedItem as CloudBlob).DeleteAsync(), (tp) =>
                {
                    if(tp.Exception != null)
                    {
                        operation.State = OperationState.Failed;
                        operation.Details = tp.Exception.ToString().ToConsoleString();
                        operation.Message = "Failed to delete blob ".ToConsoleString() + (Grid.SelectedItem as CloudBlob).Name.ToConsoleString(ConsoleColor.Yellow);

                    }
                    else
                    {
                        operation.State = OperationState.Completed;
                        operation.Message = "Finished deleting blob ".ToConsoleString() + (Grid.SelectedItem as CloudBlob).Name.ToConsoleString(ConsoleColor.Yellow);
                    }

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
            openButton.CanFocus = Grid.SelectedItem != null;
        }
    }
}
