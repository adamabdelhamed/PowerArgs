using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs.Cli;
using PowerArgs;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using ArgsTests.CLI;

namespace ArgsTests.Data
{




    [TestClass]
    public class DataSourceTests
    {
        private CliUnitTestConsole console;

        [TestInitialize]
        public void Init()
        {
            console = new CliUnitTestConsole();
            ConsoleProvider.Current = console;
        }

        [TestMethod]
        public void LoadMoreBasic()
        {
            int expectedNumberOfItems = 95;
            CliMessagePump pump = new CliMessagePump(ConsoleProvider.Current, (k) => { });
            TestLoadMoreDataSource dataSource = new TestLoadMoreDataSource(pump, expectedNumberOfItems, TimeSpan.FromMilliseconds(50));

            var query = new CollectionQuery(0, 7, null);
            List<object> viewedData = new List<object>();
            pump.Start();
            
            // the first call to GetDataView should return an empty result that indicates that the view is incomplete
            // because it is still loading data
            var initialDataView = dataSource.GetDataView(query);
            Assert.IsFalse(initialDataView.IsViewComplete);
            Assert.IsFalse(initialDataView.IsViewEndOfData);
            Assert.AreEqual(0, initialDataView.Items.Count);

            // Since we're simulating the server call and we know the max amount of time it can take wait twice that long
            Thread.Sleep(dataSource.MaxDelay + dataSource.MaxDelay);

            // Now there should be a full page of data available to us
            var nextAttemptDataView = dataSource.GetDataView(query);
            Assert.AreEqual(query.Take, nextAttemptDataView.Items.Count);
            Assert.IsTrue(nextAttemptDataView.IsViewComplete);
            Assert.IsFalse(nextAttemptDataView.IsViewEndOfData);

            viewedData.AddRange(nextAttemptDataView.Items);
            query.Skip += query.Take;

            // now ask for data in a tight loop.  Sometimes we will get more data, sometimes we'll get an empty page
            // while more data is being loaded
            while(nextAttemptDataView.IsViewEndOfData == false)
            {
                nextAttemptDataView = dataSource.GetDataView(query);

                Console.WriteLine(nextAttemptDataView.Items.Count+" items, viewComplete == "+nextAttemptDataView.IsViewComplete);

                if (nextAttemptDataView.Items.Count < query.Take && nextAttemptDataView.IsViewEndOfData == false)
                {
                    Thread.Sleep(10);
                    Assert.IsFalse(nextAttemptDataView.IsViewComplete);
                }
                else if(nextAttemptDataView.Items.Count == query.Take)
                {
                    Assert.IsTrue(nextAttemptDataView.IsViewComplete);
                }
                else if(nextAttemptDataView.Items.Count > query.Take)
                {
                    Assert.Fail(nextAttemptDataView.Items.Count+" should never exceed "+ query.Take);
                }

                viewedData.AddRange(nextAttemptDataView.Items);
                query.Skip += nextAttemptDataView.Items.Count;
            }

            int nextExpectedId = 0;
            foreach(var item in viewedData)
            {
                Assert.IsNotNull(item);
                Console.WriteLine(item);
                Assert.AreEqual(nextExpectedId++, ((Item)item).Id);
            }

            Assert.AreEqual(expectedNumberOfItems, viewedData.Count);
            console.InputQueue.Enqueue(new ConsoleKeyInfo(' ', ConsoleKey.Escape, false, false, false));
        }
    }
}
