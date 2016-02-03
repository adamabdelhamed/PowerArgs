using Microsoft.WindowsAzure.Storage.Blob;
using PowerArgs.Cli;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HelloWorld.Samples
{
    public class BlobDataSource : LoadMoreDataSource
    {
        CloudBlobContainer container;
        public BlobDataSource(CloudBlobContainer container, CliMessagePump pump) : base(pump)
        {
            this.container = container;
        }

        protected override async Task<LoadMoreResult> LoadMoreAsync(CollectionQuery query, object continuationToken)
        {
            BlobResultSegment next;

            next = await container.ListBlobsSegmentedAsync(query.Filter, true, BlobListingDetails.All, null, (continuationToken as BlobContinuationToken), new BlobRequestOptions(), new Microsoft.WindowsAzure.Storage.OperationContext());
     

            var result = new LoadMoreResult(next.Results.Select(r => r as object).ToList(), next.ContinuationToken );
            return result;
        }
    }
}
