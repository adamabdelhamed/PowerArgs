using System;
using PowerArgs;

namespace HelloWorld
{
    public class MyArgs
    {
        [ArgRequired]
        public string StringArg { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            //TODO - Comment out/delete this line later
            args = new string[] { "-s", "AnArgumentStringHere" };

            try
            {
                var parsed = Args.Parse<MyArgs>(args);
                Console.WriteLine("You entered StringArg '{0}'", parsed.StringArg);
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
