using System;
using PowerArgs;

namespace HelloWorld
{
    public class BaseArgs
    {
        [ArgDescription("Run a sync cycle")]
        [ArgActionMethod]
        public static void Sync(SyncArgs syncArgs)
        {
            if (!syncArgs.Quiet)
                Console.WriteLine("Sync called");
            // [...] do work...
        }
    }

    public class SyncArgs
    {
        [ArgShortcut("q")]
        [ArgDescription("Suppress console output")]
        public bool Quiet { get; set; }
    }


    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Args.InvokeAction<BaseArgs>(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
