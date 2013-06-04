using System;
using PowerArgs;

namespace HelloWorld
{
    [TabCompletion]
    [ArgExample("HelloWorld -s SomeString -i 50 -sw", "Shows how to use the shortcut version of the switch parameter")]
    public class MyArgs
    {
        [ArgDescription("Description for a required string parameter")]
        [StickyArg] // The most recent value of this argument will be stored in AppData/Roaming/PowerArgs/HelloWorld.txt
        public string StringArg { get; set; }

        [ArgDescription("Description for an optional integer parameter")]
        [ArgLongForm("integer-long-form")]
        public int IntArg { get; set; }

        [ArgDescription("Description for an optional switch parameter")]
        public bool SwitchArg { get; set; }

        [ArgDescription("Shows the help documentation")]
        [ArgShortcut("-h")]
        [ArgShortcut("-?")]
        [ArgShortcut("--?")]
        public bool Help { get; set; }

        [ArgShortcut(ArgShortcutPolicy.NoShortcut)]
        public DateTime DateArg { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var cancellation = PowerArgs.EasterEggs.MatrixMode.Start();
            System.Threading.Thread.Sleep(2000);
            Console.WriteLine(new System.Net.WebClient().DownloadString("http://www.bing.com"));
            cancellation.Invoke();
            return;


            try
            {
                var parsed = Args.Parse<MyArgs>(args);
                if (parsed.Help)
                {
                    ArgUsage.GetStyledUsage<MyArgs>().Write();
                }
                else
                {
                    Console.WriteLine("You entered StringArg '{0}' and IntArg '{1}', switch was '{2}'", parsed.StringArg, parsed.IntArg, parsed.SwitchArg);
                }
            }
            catch (ArgException ex)
            {
                Console.WriteLine(ex.Message);
                ArgUsage.GetStyledUsage<MyArgs>().Write();
            }
        }
    }
}
