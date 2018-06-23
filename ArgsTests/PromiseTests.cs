using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests
{
    [TestClass]
    public class PromiseTests
    {
        [TestMethod]
        public async Task TestAsAwaitable()
        {
            Deferred d = Deferred.Create();

            var promise = d.Promise;
            bool done = false;
            Task.Factory.StartNew(()=> { Thread.Sleep(50); done = true; d.Resolve(); });
            Assert.IsFalse(done);
            await promise.AsAwaitable();
            Assert.IsTrue(done);
        }

        [TestMethod]
        public async Task TestWhenAll()
        {
            var promises = new List<Promise>();
            var completionCount = 0;
            for(var i = 0; i < 5; i++)
            {
                var myI = i;
                Deferred d = Deferred.Create();
                promises.Add(d.Promise);
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(100 * (1+ myI));
                    Interlocked.Increment(ref completionCount);
                    d.Resolve();
                });
            }
            Assert.AreEqual(0, completionCount);
            await Promise.WhenAll(promises).AsAwaitable();
            Assert.AreEqual(5, completionCount);
        }

        [TestMethod]
        public async Task TestWhenAllWithExceptions()
        {
            var promises = new List<Promise>();
            for (var i = 0; i < 10; i++)
            {
                var myI = i;
                Deferred d = Deferred.Create();
                promises.Add(d.Promise);
                Task.Factory.StartNew(() =>
                {
                    if (myI % 2 == 0)
                    {
                        d.Resolve();
                    }
                    else
                    {
                        d.Reject(new Exception("Failed"));
                    }
                });
            }
            try
            {
                await Promise.WhenAll(promises).AsAwaitable();
                Assert.Fail("An exception should have been thrown");
            }
            catch(PromiseWaitException ex)
            {
                Assert.AreEqual(5, ex.InnerExceptions.Count);
            }
        }
    }
}
