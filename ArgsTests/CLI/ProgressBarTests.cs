using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArgsTests.CLI
{
    [TestClass]
    public class ProgressBarTests
    {
        [TestMethod]
        public void TestProgressBarDeterminateSanity()
        {
            var bar = new CliProgressBar("", 10);
            bar.Progress = .5;
            bar.Render();
            Assert.AreEqual(6, bar.renderedMessage.Length);
            
            for(int i = 0; i < bar.renderedMessage.Length; i++)
            {
                if(i < 3)
                {
                    // the first three characters should be filled since the progress is .5
                    Assert.AreEqual(bar.renderedMessage[i].ForegroundColor, bar.MessageFillColor);
                    Assert.AreEqual(bar.renderedMessage[i].BackgroundColor, bar.FillColor);
                }
                else
                {
                    // the final three characters should NOT BE FILLED since the progress is .5
                    Assert.AreEqual(bar.renderedMessage[i].ForegroundColor, ConsoleString.DefaultForegroundColor);
                    Assert.AreEqual(bar.renderedMessage[i].BackgroundColor, ConsoleString.DefaultBackgroundColor);
                }
            }
        }
    }
}
