using Microsoft.WindowsAzure.Storage.Blob;
using PowerArgs.Cli;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HelloWorld.Samples
{
    public class ContainerListDataSource : LoadMoreDataSource
    {
        CloudBlobClient client;
        public ContainerListDataSource(CloudBlobClient client, CliMessagePump pump) : base(pump)
        {
            this.client = client;
        }

        protected override async Task<LoadMoreResult> LoadMoreAsync(CollectionQuery query, object continuationToken)
        {
            var next = await client.ListContainersSegmentedAsync(continuationToken as BlobContinuationToken);
            var result = new LoadMoreResult(next.Results.Where
                (r => 
                    query.Filter == null || 
                    (r.Name.IndexOf(query.Filter,StringComparison.InvariantCultureIgnoreCase)  >= 0)
                ).Select(r => new ContainerRecord(r) as object).ToList(), next.ContinuationToken);
            return result;
        }
    }
}
