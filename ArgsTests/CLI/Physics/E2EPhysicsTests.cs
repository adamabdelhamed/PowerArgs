using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;

namespace ArgsTests.CLI.Physics
{
    [TestClass]
    public class E2EPhysicsTests
    {
        [TestMethod]
        public void E2ESimplestApp()
        {
            var app = new ConsoleApp(1,1);
            var panel = app.LayoutRoot.Add(new SpacetimePanel(1, 1));
            SpacialElement element;
            TimeSpan lastAge = TimeSpan.Zero;
            panel.SpaceTime.QueueAction(() =>
            {
                element = new SpacialElement();
                panel.SpaceTime.Add(element);

                panel.SpaceTime.Add(TimeFunction.Create(() => 
                {
                    lastAge = element.CalculateAge();
                    if(Time.CurrentTime.Now == TimeSpan.FromSeconds(.5))
                    {
                        Time.CurrentTime.Stop();
                        app.Stop();
                    }
                }));
            });

            panel.SpaceTime.Start();
            app.Start().Wait();
            Assert.AreEqual(.5, lastAge.TotalSeconds);
        }

        [TestMethod]
        public void TestSeeking()
        {
            SpaceTime st = new SpaceTime(100, 20, TimeSpan.FromSeconds(.1));
            SpacialElement seeker = null;
            SpacialElement target = null;
            st.QueueAction(() =>
            {
                seeker = SpaceTime.CurrentSpaceTime.Add(new SpacialElement(1, 1, 0, 0));
                target = SpaceTime.CurrentSpaceTime.Add(new SpacialElement(1, 1, 99, 0));
                var seekerV = new SpeedTracker(seeker);
                var seekerFunc = new Seeker(seeker, target, seekerV, 1) { RemoveWhenReached = true };
                seekerFunc.Lifetime.LifetimeManager.Manage(() => { st.Stop(); });
            });

            st.Start().Wait();
            Assert.AreEqual(seeker.CenterX, seeker.CenterX);
            Assert.AreEqual(seeker.CenterY, seeker.CenterY);
        }
    }
}
