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
    [TestCategory(Categories.Games)]
    public class PowerArgsIntroTests
    {
        [TestMethod]
        public async Task TestPowerArgsIntroCompletesOnItsOwn()
        {
            var app = new ConsoleApp(80, 30);
            var appTask = app.Start();

            PowerArgsGamesIntro intro = null;
            await app.InvokeNextCycle(() => intro = app.LayoutRoot.Add(new PowerArgsGamesIntro()).CenterVertically());
            Assert.IsNotNull(intro);
       
            await app.InvokeNextCycle(async () =>
            {
                await intro.Play();
                app.Stop();
            });

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
                    var appTask = app.Start();
                    var playTask = intro.Play();

                    await Task.Delay(10);
                    app.InvokeNextCycle(() => intro.Cleanup());

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
