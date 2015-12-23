using Microsoft.WindowsAzure.Storage.Table;
using PowerArgs.Cli;
using System.Linq;
using System.Threading.Tasks;

namespace HelloWorld.Samples
{
    public class TableListDataSource : LoadMoreDataSource
    {
        CloudTableClient client;
        public TableListDataSource(CloudTableClient client, CliMessagePump pump) : base(pump)
        {
            this.client = client;
        }

        protected override async Task<LoadMoreResult> LoadMoreAsync(CollectionQuery query, object continuationToken)
        {
            var next = await client.ListTablesSegmentedAsync(query.Filter, continuationToken as TableContinuationToken);
            var result = new LoadMoreResult(next.Results.Select(r => r as object).ToList(), next.ContinuationToken);
            return result;
        }
    }
}
