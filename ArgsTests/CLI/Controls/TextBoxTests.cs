using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs.Cli;
using PowerArgs;
using System.Threading;

namespace ArgsTests.CLI.Controls
{
    [TestClass]
    public class TextBoxTests
    {
        [TestMethod]
        public void Basic()
        {
            var testCli = new CliUnitTestConsole(80,1);
            ConsoleProvider.Current = testCli;
            var app = new ConsoleApp(0, 0, 80, 1);
            app.LayoutRoot.Add(new TextBox()).Fill();
            var task = app.Start();

            testCli.Input.Enqueue(new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false));

            Thread.Sleep(1000);
            var result = testCli.Buffer.ToString();
            testCli.Input.Enqueue(new ConsoleKeyInfo('*', ConsoleKey.Escape, false, false, false));
            task.Wait();


            Assert.AreEqual(80, result.Length);
            Console.WriteLine(result);
        }
    }
}
