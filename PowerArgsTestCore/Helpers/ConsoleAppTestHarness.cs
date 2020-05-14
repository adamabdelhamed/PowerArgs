using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Runtime.CompilerServices;

namespace ArgsTests.CLI.Physics
{
    class ConsoleAppTestHarness
    {
        public static void Run(TestContext context, Action<ConsoleApp> testCode, [CallerMemberName]string testName = null, int w = 80, int h = 30)
        {
            ConsoleApp app = new ConsoleApp(w, h);
            app.Recorder = TestRecorder.CreateTestRecorder(testName, context);
            app.InvokeNextCycle(() => { testCode(app); });
            app.Start().Wait();
        }

        public static void Run(TestContext context, Action<ConsoleApp, SpaceTimePanel> testCode, [CallerMemberName]string testName = null, int w = 80, int h = 30)
        {
            Promise spaceTimePromise = null;
            Run(context, (app) =>
            {
                var panel = app.LayoutRoot.Add(new SpaceTimePanel(w, h));
                panel.SpaceTime.InvokeNextCycle(() =>
                {
                    testCode(app, panel);
                });
                spaceTimePromise = panel.SpaceTime.Start();
                spaceTimePromise.Finally((p) => app.Stop());

            }, testName, w, h);

            Assert.IsNotNull(spaceTimePromise);
            spaceTimePromise.Wait();
        }
    }
}
