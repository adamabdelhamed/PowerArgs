using PowerArgs;
using PowerArgs.Preview;
using System;
using System.IO;

namespace ExternalUnitTestPipelineStageExe
{
    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling), ArgPipeline]
    class Program
    {
        public class Customer
        {
            public string Name { get; set; }
            public string AccountNumber { get; set; }

            public bool HasBeenProcessed { get; set; }
        }

        static void Main(string[] args)
        {
            PowerLogger.LogFile = @"C:\temp\ExternalLog-"+DateTime.Now.Ticks+".log";

            foreach(var arg in args)
            {
                if(arg == "PretendToNotBePowerArgsEnabled")
                {
                    PowerLogger.LogLine("Pretended to not be PowerArgs enabled");
                    return;
                }
            }

            try
            {
                Args.InvokeAction<Program>(args);
                PowerLogger.LogLine("InvokeAction returned without error");
            }
            catch(Exception ex)
            {
                PowerLogger.LogLine("Unhandled top level exception in test program\n\n"+ex.ToString());
            }
        }

        [ArgActionMethod]
        public void Double([ArgPipelineTarget(PipelineOnly=false)]double number)
        {
            var output = 2 * number;
            PowerLogger.LogLine("Double of "+number+" is "+output);
            Console.Write(number+" - I am the doubler, ");
            ConsoleString.WriteLine("Muahahahaha!", ConsoleColor.Green);
            ArgPipeline.Push(output);
        }

        [ArgActionMethod]
        public void ProcessCustomer([ArgPipelineTarget(PipelineOnly = true)]Customer customer)
        {
            customer.HasBeenProcessed = true;
            ArgPipeline.Push(customer);
        }
    }
}
