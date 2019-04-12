using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs.Cli;
using PowerArgs;
using System.Threading.Tasks;

namespace ArgsTests.CLI.Apps
{
    [TestClass]
    [TestCategory(Categories.ConsoleApp)]
    public class AppLifecycle
    {
        [TestMethod]
        public void PumpFailurePreservesStack()
        {
            var testCli = new CliUnitTestConsole(80, 4);
            ConsoleProvider.Current = testCli;
            ConsoleApp app = new ConsoleApp();
            var promise = app.Start();

            app.QueueAction(() =>
            {
                throw new FormatException("Some fake exception");
            });

            try
            {
                promise.Wait();
                Assert.Fail("An exception should have been thrown");
            }
            catch (PromiseWaitException ex)
            {
                Assert.AreEqual(typeof(FormatException), ex.InnerException.GetType());
                Assert.AreEqual("Some fake exception", ex.InnerException.Message);
            }
        }

        [TestMethod]
        public async Task TestQueueActionAsync()
        {
            var app = new ConsoleApp(80, 30);
            var appTask = app.Start().AsAwaitable();

            var hello = await app.QueueActionAsync<string>(async () =>
            {
                ConsoleApp.AssertAppThread(app);
                await Task.Delay(100);
                ConsoleApp.AssertAppThread(app);
                app.Stop();
                return "Hello";
            });

            Assert.AreEqual("Hello", hello);

            await appTask;
        }

        [TestMethod]
        public async Task TestQueueActionAsyncExceptions()
        {
            try
            {
                var app = new ConsoleApp(80, 30);
                var appTask = app.Start().AsAwaitable();

                var hello = await app.QueueActionAsync<string>(async () =>
                {
                    ConsoleApp.AssertAppThread(app);
                    await Task.Delay(10);
                    ConsoleApp.AssertAppThread(app);
                    throw new FormatException("Some random format exception");
                });

                Assert.AreEqual("Hello", hello);

                await appTask;
                Assert.Fail("A format exception should have been thrown");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.InnerException is FormatException);
                Assert.IsTrue(ex.InnerException.Message == "Some random format exception");
            }
        }
    }
}
