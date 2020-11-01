using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using PowerArgs;
namespace ArgsTests.CLI.Physics
{
    [TestClass]
    [TestCategory(Categories.Physics)]
    public class E2EPhysicsTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void E2ESimplestApp()
        {
            var app = new ConsoleApp(1,1);
            var panel = app.LayoutRoot.Add(new SpaceTimePanel(1, 1));
            SpacialElement element;
            TimeSpan lastAge = TimeSpan.Zero;
            panel.SpaceTime.InvokeNextCycle(() =>
            {
                element = new SpacialElement();
                panel.SpaceTime.Add(element);

                panel.SpaceTime.Invoke(async () => 
                {
                    lastAge = element.CalculateAge();
                    if(Time.CurrentTime.Now == TimeSpan.FromSeconds(.5))
                    {
                        Time.CurrentTime.Stop();
                    }
                    await Time.CurrentTime.YieldAsync();
                });
             });

            panel.SpaceTime.Start().Then(()=> app.Stop());
            app.Start().Wait();
            Assert.AreEqual(.5, lastAge.TotalSeconds);
        }
    }
}
