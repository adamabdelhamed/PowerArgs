using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PowerArgs.Cli;

namespace ArgsTests.CLI
{
    [TestClass]
    public class GridTests
    {
        private CliUnitTestConsole console;
        private ConsoleApp app;
        [TestInitialize]
        public void Init()
        {
            console = new CliUnitTestConsole();
            ConsoleProvider.Current = console;
            app = new ConsoleApp(0, 0, 80, 80);
        }

        [TestMethod]
        public void TestGridBasic()
        {
            Grid grid = new Grid();
            MemoryDataSource data = new MemoryDataSource();
            grid.DataSource = data;

            for(int i = 0; i < 5; i++)
            {
                data.Items.Add(new { Id = i+1, StringValue = "Some value here" });
            }

            grid.VisibleColumns.Add(new ColumnViewModel("Id".ToConsoleString()));
            grid.VisibleColumns.Add(new ColumnViewModel("StringValue".ToConsoleString()));

            grid.Width = app.LayoutRoot.Width;
            grid.Height = app.LayoutRoot.Height;

            app.LayoutRoot.Controls.Add(grid);
            Task doneTask = app.Start();
            // give the UI thread a few milliseconds to paint before checking it
            // todo - maybe implement an after paint event on console app
            Thread.Sleep(100);

            // grab the state of the UI to check for the grid
            var consoleState = console.Buffer.ToString();

            // exit the app by simulating an escape key press
            console.InputQueue.Enqueue(new ConsoleKeyInfo(' ', ConsoleKey.Escape,false,  false, false));

            // wait for the app to exit normally
            doneTask.Wait();

            // verify that the console state matches our expected state
            Assert.AreEqual(Resources.BasicGridExpectedOutput, consoleState);
        }
    }
}
