using PowerArgs;
using System;

namespace HelloWorld.Samples
{
    [TabCompletion] // This is useful for the sample, but you don't need it in your program (unless you want it).
    public class HelloWorldParseArgs
    {
        [ArgRequired, ArgDescription("The Uri to ping"), DefaultValue("http://www.bing.com")]
        public Uri UriArg { get; set; }
    }

    public class HelloWorldParse
    {
        public static void _Main(string[] args)
        {
            try
            {
                var parsed = Args.Parse<HelloWorldParseArgs>(args);
                Console.WriteLine("You entered UriArg '{0}'", parsed.UriArg);
            }
            catch (ArgException ex)
            {
                Console.WriteLine(ex.Message);
                ArgUsage.GetStyledUsage<HelloWorldParseArgs>().Write();
            }
        }
    }
}
