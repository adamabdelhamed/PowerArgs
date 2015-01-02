using PowerArgs;
using PowerArgs.Preview;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HelloWorld.Samples
{


    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling), TabCompletion(REPL = true, HistoryToSave = 10), ArgPipeline]
    public class PipingArgs
    {
        [ArgActionMethod]
        public void Send(int numberOfNumbers)
        {
            for(int i = 0; i < numberOfNumbers; i++)
            {
                ArgPipeline.Push(i);
            }
        }

        [ArgActionMethod]
        public void GetRecords(int numberOfRecords)
        {
            for (int i = 0; i < numberOfRecords; i++)
            {
                ArgPipeline.Push(new { Index = i, Name = "Adam", Country = "United States of America" });
            }
        }
    }

    public class Piping
    {
        // This is the code you would put in your Main method.  It's called _Main here since you can only have 1 called Main
        // in the assembly.
        public static void _Main(string[] args)
        {
            Args.InvokeAction<PipingArgs>(args);
        }
    }
}
