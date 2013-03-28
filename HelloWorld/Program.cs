using System;
using PowerArgs;

namespace HelloWorld
{
    [TabCompletion]
    [ArgExample("HelloWorld -s SomeString -i 50 -sw", "Shows how to use the shortcut version of the switch parameter")]
    public class MyArgs
    {
        [ArgRequired(PromptIfMissing=true)]
        [ArgPosition(0)]
        [ArgDescription("Description for a required string parameter")]
        public string StringArg { get; set; }

        [ArgDescription("Description for an optional integer parameter")]
        public int IntArg { get; set; }

        [ArgDescription("Description for an optional switch parameter")]
        public bool SwitchArg { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var parsed = Args.Parse<MyArgs>(args);
                Console.WriteLine("You entered StringArg '{0}' and IntArg '{1}', switch was '{2}'", parsed.StringArg, parsed.IntArg, parsed.SwitchArg);
            }
            catch (ArgException ex)
            {
                Console.WriteLine(ex.Message);
                ArgUsage.GetStyledUsage<MyArgs>().Write();
            }
        }
    }
}
