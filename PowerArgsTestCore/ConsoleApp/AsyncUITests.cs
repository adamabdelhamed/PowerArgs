using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs.Cli;
using System;
using System.Threading;
using System.Threading.Tasks;
using PowerArgs;
namespace ArgsTests.CLI
{
    [TestClass]
    [TestCategory(Categories.ConsoleApp)]
    public class AsyncUITests
    {
        [TestMethod]
        public void TestAsyncAwaitWithinApp()
        {
            var app = new ConsoleApp(1, 1);
            var appPromise = app.Start();

            app.QueueAction(async () =>
            {
                ConsoleApp.AssertAppThread(app);
                foreach (TaskCreationOptions option in Enum.GetValues(typeof(TaskCreationOptions)))
                {
                    Console.WriteLine(option);
                    await Task.Factory.StartNew(() => { Thread.Sleep(50); }, option);
                    ConsoleApp.AssertAppThread(app);
                    Console.WriteLine("App thread confirmed");
                }
                app.Stop();
            });

            appPromise.Wait();
        }

        [TestMethod]
        public void TestAsyncAwaitWithinAppExceptionPath()
        {
            var app = new ConsoleApp(1, 1);
            var appPromise = app.Start();

            app.QueueAction(async () =>
            {
                ConsoleApp.AssertAppThread(app);
                foreach (TaskCreationOptions option in Enum.GetValues(typeof(TaskCreationOptions)))
                {
                    Console.WriteLine(option);
                    try
                    {
                        await Task.Factory.StartNew(() => { throw new NotImplementedException("OOPS"); }, option);
                        Assert.Fail("An exception should have been thrown");
                    }
                    catch (NotImplementedException) { }

                    ConsoleApp.AssertAppThread(app);
                }
                app.Stop();
            });

            appPromise.Wait();
        }

        [TestMethod]
        public async Task TestTaskTimeout()
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1)).TimeoutAfter(TimeSpan.FromSeconds(.5));
                Assert.Fail("An exception should have been thrown");
            }
            catch(TimeoutException)
            {
                Console.WriteLine("Expected timeout fired");
            }
        }
    }
}
