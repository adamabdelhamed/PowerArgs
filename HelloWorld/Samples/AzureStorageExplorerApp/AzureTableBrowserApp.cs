using PowerArgs.Cli;

namespace HelloWorld.Samples
{
    public class AzureTableBrowserApp : ConsolePageApp
    {
        public AzureTableBrowserApp()
        {
            PageStack.RegisterDefaultRoute("accounts", () => new StorageAccountsPage());
            PageStack.RegisterRoute("accounts/{account}", () => new ServicesPage());
            PageStack.RegisterRoute("accounts/{account}/tables", () => new TablesPage());
            PageStack.RegisterRoute("accounts/{account}/containers", () => new ContainersPage());
            PageStack.RegisterRoute("accounts/{account}/tables/{table}", () => new TablePage());
            PageStack.Navigate("accounts");
        }
    }  
}
