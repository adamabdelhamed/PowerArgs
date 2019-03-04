using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using PowerArgs.Cli;
using System;
using System.Collections.Generic;

namespace ArgsTests.CLI.Controls
{
    [TestClass]
    public class GridLayoutTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void GridLayoutEndToEnd()
        {
            var app = new CliTestHarness(this.TestContext, 80, 20, true);

            var gridLayout = app.LayoutRoot.Add(new GridLayout(new GridLayoutOptions()
            {
                Columns = new List<GridColumnDefinition>()
                {
                    new GridColumnDefinition(){ Width = 5, Type = GridValueType.Pixels },
                    new GridColumnDefinition(){ Width= 2, Type = GridValueType.RemainderValue },
                    new GridColumnDefinition(){ Width = 2, Type = GridValueType.RemainderValue },
                },
                Rows = new List<GridRowDefinition>()
                {
                     new GridRowDefinition(){ Height = 1, Type = GridValueType.Pixels },
                     new GridRowDefinition(){ Height= 2, Type = GridValueType.RemainderValue },
                     new GridRowDefinition(){ Height = 1, Type = GridValueType.Pixels },
                }
            })).Fill();

            var colorWheel = new List<ConsoleColor>()
            {
                ConsoleColor.Red, ConsoleColor.DarkRed, ConsoleColor.Red,
                ConsoleColor.Black, ConsoleColor.White, ConsoleColor.Black,
                ConsoleColor.Green, ConsoleColor.DarkGreen, ConsoleColor.Green
            };
            var colorIndex = 0;
            for (var y = 0; y < gridLayout.NumRows; y++)
            {
                for (var x = 0; x < gridLayout.NumColumns; x++)
                {
                    gridLayout.Add(new ConsoleControl()
                    {
                        Background = colorWheel[colorIndex],
                    },x,y);
                    colorIndex = colorIndex == colorWheel.Count - 1 ? 0 : colorIndex + 1;
                }
            }

            app.QueueAction(async () =>
            {
                await app.Paint().AsAwaitable();
                app.RecordKeyFrame();
                app.Stop();
            });

            app.Start().Wait();
            app.AssertThisTestMatchesLKG();
        }
    }
}
