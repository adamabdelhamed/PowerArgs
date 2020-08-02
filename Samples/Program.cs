using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Threading.Tasks;

namespace Samples
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            int count = 0;
            while (true)
            {
                count++;
                var app = new ConsoleApp();


                app.Invoke(async () =>
                {
                    var panel = app.LayoutRoot.Add(new ConsolePanel()).Fill();
                    var label = panel.Add(new Label() { Text = $"{count}".ToWhite() }).CenterBoth();
                    var stp = panel.Add(new SpaceTimePanel(10, 5)).CenterHorizontally().DockToBottom();
                    stp.SpaceTime.Start();
                    await Task.Delay(10);
                    stp.SpaceTime.Stop();
                    app.Stop();

                });
                app.Start().Wait();
                GC.Collect();
            }
        }
    }
}
