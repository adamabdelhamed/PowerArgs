using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;

namespace ArgsTests.CLI.Physics
{
    [TestClass]
    public class E2EPhysicsTests
    {
        public TestContext TestContext { get; set; }

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
            var w = 10;
            var h = 3;
            ConsoleApp app = new ConsoleApp(w,h);
            SpaceTime st = new SpaceTime(w, h, TimeSpan.FromSeconds(.1));
            var panel = app.LayoutRoot.Add(new SpacetimePanel(w, h, time: st)).Fill();
            app.Recorder = TestRecorder.CreateTestRecorder("Seeker", TestContext);


            SpacialElement seeker = null;
            SpacialElement target = null;
            st.QueueAction(() =>
            {
                seeker = SpaceTime.CurrentSpaceTime.Add(new SpacialElement(1, 1, 0, 1));
                seeker.Renderer = new SpacialElementRenderer() { Background = ConsoleColor.Green };
                target = SpaceTime.CurrentSpaceTime.Add(new SpacialElement(1, 1, w-2, 1));
                var seekerV = new SpeedTracker(seeker);
                var seekerFunc = new Seeker(seeker, target, seekerV, 1) { RemoveWhenReached = true };
                seekerFunc.Lifetime.LifetimeManager.Manage(() => 
                {
                    st.Stop(); 
                });
            });

            app.Start();
            st.Start().Wait();
            app.Stop();
            Assert.AreEqual(seeker.CenterX, target.CenterX);
            Assert.AreEqual(seeker.CenterY, target.CenterY);
        }
    }
}
