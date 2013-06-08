using PowerArgs;
using System;

namespace HelloWorld.Samples
{
    [TabCompletion] // This is useful for the sample, but you don't need it in your program (unless you want it).
    public class HelloWorldParseArgs
    {
        public string StringArg { get; set; }
        public int    IntArg    { get; set; }
        public bool   SwitchArg { get; set; }
    }

    public class HelloWorldParse
    {
        public static void _Main(string[] args)
        {
            try
            {
                var parsed = Args.Parse<HelloWorldParseArgs>(args);
                Console.WriteLine("You entered StringArg '{0}' and IntArg '{1}', switch was '{2}'", parsed.StringArg, parsed.IntArg, parsed.SwitchArg);
            }
            catch (ArgException ex)
            {
                Console.WriteLine(ex.Message);
                ArgUsage.GetStyledUsage<HelloWorldParseArgs>().Write();
            }
        }
    }
}
