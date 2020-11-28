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

            try
            {
                await st.Start();
            }
            catch(Exception exc)
            {
                var ex = exc.Clean().Single();
                Assert.AreEqual(message, ex.Message);
                handled = true;
            }
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

                try
                {
                    await st.Start();
                }catch(Exception exc)
                {
                    var ex = exc.Clean().Single();
                    Assert.AreEqual(message, ex.Message);
                     handled = true;
                }
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
                await Task.Delay(100);
                throw new Exception(message);
            });
            bool handled = false;

            try
            {
                await st.Start();
            }
            catch(Exception exc)
            {
                var ex = exc.Clean().Single();
                Assert.AreEqual(message, ex.Message);
                handled = true;
            }
            Assert.IsTrue(handled);
        }

        [TestMethod]
        public async Task TestSpaceTimeAsynchronousExceptionDoASAPInFinally()
        {
            var message = "Plain old Exception";
            var st = new SpaceTime(80, 40);
            st.Invoke(async () =>
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
            bool handled = false;

            try
            {
                await st.Start();
            }catch(Exception exc)
            {
                var ex = exc.Clean().Single();
                Assert.AreEqual(message, ex.Message);
                handled = true;
            }
            Assert.IsTrue(handled);
        }
    }
}
