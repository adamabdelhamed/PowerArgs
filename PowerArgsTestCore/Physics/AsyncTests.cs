using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs.Cli;
using PowerArgs;
using System.Threading;
using PowerArgs.Cli.Physics;
using System.Threading.Tasks;
using PowerArgs.Games;
using System.Linq;
namespace ArgsTests.CLI.Physics
{
    [TestClass]
    [TestCategory(Categories.Physics)]
    public class AsyncTests
    {
        public TestContext TestContext { get; set; }


        [TestMethod]
        public async Task TestSpaceTimeSynchronousException()
        {
            var message = "Plain old Exception";
            var st = new SpaceTime(80, 40);
            st.Invoke(() => throw new Exception(message));
            bool handled = false;
            st.UnhandledException.SubscribeOnce((data) =>
            {
                var ex = PromiseWaitException.Clean(data.Exception).Single();
                Assert.AreEqual(message, ex.Message);
                data.Handling = EventLoop.EventLoopExceptionHandling.Swallow;
                handled = true;
                st.Stop();
            });

            await st.Start().AsAwaitable();
            Assert.IsTrue(handled);
        }

        [TestMethod]
        public async Task TestSpaceTimeAsynchronousExceptionQueueAction()
        {
            using (var lt = new Lifetime())
            {
                var message = "Plain old Exception";
                var st = new SpaceTime(80, 40);
                st.InvokeNextCycle(async () =>
                {
                    await Task.Yield();
                    throw new Exception(message);
                });
                bool handled = false;
                st.UnhandledException.SubscribeForLifetime((data) =>
                {
                    var ex = PromiseWaitException.Clean(data.Exception).Single();
                    Assert.AreEqual(message, ex.Message);
                    data.Handling = EventLoop.EventLoopExceptionHandling.Swallow;
                    handled = true;
                    st.Stop();
                }, lt);

                await st.Start().AsAwaitable();
                Assert.IsTrue(handled);
            }
        }


        [TestMethod]
        public async Task TestSpaceTimeAsynchronousExceptionDoASAP()
        {
            var message = "Plain old Exception";
            var st = new SpaceTime(80, 40);
            st.Invoke( async () =>
            {
                st.Invoke(async () =>
                {
                    await TaskEx.WhenAny(Task.Delay(100), Task.Delay(10));
                    throw new Exception(message);
                });
            });
            bool handled = false;
            st.UnhandledException.SubscribeOnce((data) =>
            {
                var ex = PromiseWaitException.Clean(data.Exception).Single();
                Assert.AreEqual(message, ex.Message);
                data.Handling = EventLoop.EventLoopExceptionHandling.Swallow;
                handled = true;
                st.Stop();
            });

            await st.Start().AsAwaitable();
            Assert.IsTrue(handled);
        }

        [TestMethod]
        public async Task TestSpaceTimeAsynchronousExceptionDoASAPInFinally()
        {
            var message = "Plain old Exception";
            var st = new SpaceTime(80, 40);
            st.Invoke(async () =>
            {
                st.Invoke( async () =>
                {
                    try
                    {
                        await TaskEx.WhenAny(Task.Delay(100), Task.Delay(10));
                    }
                    finally
                    {
                        throw new Exception(message);
                    }
                });
            });
            bool handled = false;
            st.UnhandledException.SubscribeOnce((data) =>
            {
                var ex = PromiseWaitException.Clean(data.Exception).Single();
                Assert.AreEqual(message, ex.Message);
                data.Handling = EventLoop.EventLoopExceptionHandling.Swallow;
                handled = true;
                st.Stop();
            });

            await st.Start().AsAwaitable();
            Assert.IsTrue(handled);
        }
    }
}
