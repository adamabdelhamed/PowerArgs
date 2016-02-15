using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using PowerArgs.Cli;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HelloWorld.Samples
{
    public class TableEntityDataSource : LoadMoreDataSource
    {
        CloudTable table;
        public TableEntityDataSource(CloudTable table, CliMessagePump pump) : base(pump)
        {
            this.table = table;
        }

        protected override async Task<LoadMoreResult> LoadMoreAsync(CollectionQuery query, object continuationToken)
        {
            var tableQuery = new TableQuery();
            tableQuery.FilterString = query.Filter;
            try
            {
                var next = await table.ExecuteQuerySegmentedAsync(tableQuery, continuationToken as TableContinuationToken);
                var result = new LoadMoreResult(next.Results.Select(r => r as object).ToList(), next.ContinuationToken);
                return result;
            }
            catch (StorageException ex)
            {
                var result = new LoadMoreResult(new List<object>(), null);
                return result;
            }
        }
    }
}
