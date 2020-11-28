using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs.Cli;
using PowerArgs.Games;
using System;
using System.Threading.Tasks;

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

            app.Invoke(async () =>
            {
                var intro = app.LayoutRoot.Add(new PowerArgsGamesIntro()).CenterVertically();
                await intro.Play();
                app.Stop();
            });

            await app.Start();
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
