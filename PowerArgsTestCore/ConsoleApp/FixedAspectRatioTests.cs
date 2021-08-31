using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using PowerArgs.Cli;

namespace ArgsTests.CLI.Controls
{
    [TestClass]
    [TestCategory(Categories.ConsoleApp)]
    public class FixedAspectRatioTests
    {
        public TestContext TestContext { get; set; }
        
        [TestMethod]
        public void TestFixedAspectRatio()
        {
            var app = new CliTestHarness(this.TestContext, 130, 40, true);
            app.SecondsBetweenKeyframes = .05f;
            app.InvokeNextCycle(async () =>
            {
                var fixedPanel = app.LayoutRoot.Add(new FixedAspectRatioPanel(2, new ConsolePanel() { Background = RGB.Green })).CenterBoth();
                app.LayoutRoot.Background = RGB.DarkYellow;
                fixedPanel.Background = RGB.Red;
                fixedPanel.Width = app.LayoutRoot.Width;
                fixedPanel.Height = app.LayoutRoot.Height;
                await app.PaintAndRecordKeyFrameAsync();

                while(fixedPanel.Width > 2)
                {
                    fixedPanel.Width -= 2;
                    await app.PaintAndRecordKeyFrameAsync();
                }

                app.Stop();
            });
            app.Start().Wait();
            app.AssertThisTestMatchesLKG();
        }
    }
}
