using Hammer.AzureStorage;
using PowerArgs;

namespace DoxygenPublisher
{
    public class CommandLineStorageConfigProvider : ISharedStorageAccountConfigProvider
    {
        public static string CommandLineAccountKey { get; set; }
        public static string CommandLineAccountName { get; set; }

        public string AccountKey
        {
            get { return CommandLineAccountKey; }
        }

        public string AccountName
        {
            get { return CommandLineAccountName; }
        }

        public bool EnableFiddler
        {
            get { return false; }
        }

        public bool UseDevStorage
        {
            get { return false; }
        }

        public bool UseHttps
        {
            get { return false; }
        }
    }

    public class StorageResetHook : ArgHook
    {
        public override void BeforeInvoke(ArgHook.HookContext context)
        {
            var action = context.SpecifiedAction;
            var storageAccountNameArgument = action.FindMatchingArgument("StorageAccountName");
            var storageAccountKeyArgument = action.FindMatchingArgument("StorageAccountKey");

            if (storageAccountNameArgument != null && storageAccountKeyArgument != null)
            {
                CommandLineStorageConfigProvider.CommandLineAccountName = "" + storageAccountNameArgument.RevivedValue;
                CommandLineStorageConfigProvider.CommandLineAccountKey = "" + storageAccountKeyArgument.RevivedValue;
                SharedStorageAccount.Reset(new CommandLineStorageConfigProvider());
            }
        }
    }
}
