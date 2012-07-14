using System;
using PowerArgs;

namespace HelloWorld
{
    [TabCompletion]
    public class MyArgs
    {
        [ArgRequired(PromptIfMissing=true)]
        public string StringArg { get; set; }

        public int IntArg { get; set; }

        [ArgShortcut("sw")]
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
                Console.WriteLine(ArgUsage.GetUsage<MyArgs>());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
