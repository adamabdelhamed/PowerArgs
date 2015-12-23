using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ArgsTests.Data
{
    public class Item
    {
        public int Id { get; set; }
        public string Value { get; set; }
        public Item(int id, string value)
        {
            this.Id = id;
            this.Value = value;
        }

        public override string ToString()
        {
            return Id + " - " + Value;
        }
    }

    public class TestLoadMoreDataSource : LoadMoreDataSource
    {
        public const int LoadBatchSize = 10;
        private List<Item> serverData;
        private Random rand = new Random();

        public TimeSpan MaxDelay { get; private set; }

        public TestLoadMoreDataSource(CliMessagePump pump, int numberOfObjectsToSimulate, TimeSpan maxDelay) : base(pump)
        {
            serverData = new List<Item>();
            this.MaxDelay = maxDelay;
            for (int i = 0; i < numberOfObjectsToSimulate; i++)
            {
                serverData.Add(new Item(i, "StringValue-" + i));
            }
        }

        protected override Task<LoadMoreResult> LoadMoreAsync(CollectionQuery query, object continuationToken)
        {
            return Task.Factory.StartNew(() =>
            {
                int index = continuationToken == null ? 0 : int.Parse(continuationToken as string);
                // sleep to simulate a delay in the async code
                Thread.Sleep(rand.Next((int)MaxDelay.TotalMilliseconds / 2, (int)MaxDelay.TotalMilliseconds));
                List<object> batch = new List<object>();
                while (batch.Count < LoadBatchSize && index < serverData.Count)
                {
                    if (query.Filter == null || serverData[index].Value.IndexOf(query.Filter, StringComparison.InvariantCultureIgnoreCase) >= 0)
                    {
                        batch.Add(serverData[index]);
                    }
                    index++;
                }

                foreach (var item in batch)
                {
                    Assert.IsNotNull(item);
                }

                var ret = new LoadMoreResult(batch, batch.Count == 0 ? null : index < serverData.Count ? index + "" : null);
                return ret;
            });
        }
    }
}
