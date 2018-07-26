using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using PowerArgs.Games;
using System.Linq;

namespace ArgsTests.CLI.Games
{
    [TestClass]
    public class PowerArgsIntroTests
    {
        [TestMethod]
        public async Task TestPowerArgsIntroCompletesOnItsOwn()
        {
            var app = new ConsoleApp(80, 30);
            var appTask = app.Start().AsAwaitable();

            PowerArgsGamesIntro intro = null;
            await app.QueueAction(() => intro = app.LayoutRoot.Add(new PowerArgsGamesIntro()).CenterVertically()).AsAwaitable();
            Assert.IsNotNull(intro);
       
            await app.QueueAction(async () =>
            {
                await intro.Play().AsAwaitable();
                app.Stop();
            }).AsAwaitable();

            await appTask;
        }

       

        [TestMethod]
        public async Task TestPowerArgsIntroInterrupts()
        {
            int i = 0;
            try
            {
                for (i = 0; i < 100; i++)
                {
                    var app = new ConsoleApp(80, 30);
                    var intro = app.LayoutRoot.Add(new PowerArgsGamesIntro()).CenterVertically();
                    var appTask = app.Start().AsAwaitable();
                    var playTask = intro.Play().AsAwaitable();

                    await Task.Delay(10);
                    app.QueueAction(() => intro.Cleanup());

                    app.Stop();
                    await appTask;
                    await playTask;
                }
            }
            catch (Exception ex)
            {
                if (ex.InnerException is InvalidOperationException && ex.InnerException.Message.ToLower().Contains("collection was modified"))
                {
                    Assert.Fail("Collection modified bug repro at i == "+i);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
