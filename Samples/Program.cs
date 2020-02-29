using PowerArgs;
using System;
using System.Threading;

namespace Samples
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            return;
            new PerfTest(Args.Parse<PerfTestArgs>(args)).Start().Wait();

   

            //CalculatorProgramSample._Main(args); // a simple 4 function calculator
            //Samples.REPLInvoke._Main(args); //  Sample that shows how to implement a REPL (Read Evaluate Print Loop)
            //Samples.SearchSample.Run();
            //new Samples.ResourceMonitor().Start().Wait();
        }
    }
}
