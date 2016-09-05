using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs.Cli;
using PowerArgs;
using System.Threading;
using System.Diagnostics;

namespace ArgsTests.CLI.Apps
{
    [TestClass]
    public class BasicXmlAppTests
    {
        [TestMethod]
        [Timeout(30000)]
        public void TestBasicFormSubmit()
        {
            var testCli = new CliUnitTestConsole(80,4);

            ConsoleProvider.Current = testCli;

            var viewModel = new BasicXmlAppViewModel();
            var app = ConsoleApp.FromMvVm(Resources.BasicXmlApp, viewModel);
            app.Stopping.SubscribeForLifetime(() => 
            {
                Console.WriteLine(testCli.Buffer.ToString());
            }, app.LifetimeManager);

            var task = app.Start();

            Thread.Sleep(100);
            testCli.Input.Enqueue("Adam");
            testCli.Input.Enqueue(ConsoleKey.Tab);
            testCli.Input.Enqueue(ConsoleKey.Enter);

            task.Wait();
            Assert.AreEqual(new ConsoleString("Adam"), viewModel.Name);
        }

        [TestMethod]
        [Timeout(1000)]
        public void TestConsoleWipesOnStopped()
        {
            var testCli = new CliUnitTestConsole(80, 4);
            ConsoleProvider.Current = testCli;

            var viewModel = new BasicXmlAppViewModel();
            var app = ConsoleApp.FromMvVm(Resources.BasicXmlApp, viewModel);

            bool appDrewProperly = false;
            bool appWipedAfterStoppedEvent = false;
            bool appWipedAfterTask = false;

            app.Stopping.SubscribeForLifetime(() =>
            {
                appDrewProperly = testCli.Buffer.ToString().Trim().Length > 0;

                Console.WriteLine("STOPPING");
                Console.WriteLine(testCli.Buffer.ToString());
                Console.WriteLine("STOPPING");
            }, app.LifetimeManager);

            app.Stopped.SubscribeForLifetime(() =>
            {
                appWipedAfterStoppedEvent = testCli.Buffer.ToString().Trim().Length == 0;
                Console.WriteLine("STOPPED");
                Console.WriteLine(testCli.Buffer.ToString());
                Console.WriteLine("STOPPED");
            }
            , app.LifetimeManager);

            var task = app.Start();
            Thread.Sleep(100);
            testCli.Input.Enqueue("Adam");
            testCli.Input.Enqueue(ConsoleKey.Tab);
            testCli.Input.Enqueue(ConsoleKey.Enter);

            task.Wait();
            appWipedAfterTask = testCli.Buffer.ToString().Trim().Length == 0;

            Console.WriteLine("END");
            Console.WriteLine(testCli.Buffer.ToString());
            Console.WriteLine("END");

            Assert.IsTrue(appDrewProperly, "Assert app drew properly");
            Assert.IsTrue(appWipedAfterStoppedEvent, "Assert app wiped properly during event");
            Assert.IsTrue(appWipedAfterTask, "Assert app wiped properly afer task");
        }
    }
}
