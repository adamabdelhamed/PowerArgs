using PowerArgs;
using System;

namespace HelloWorld.Samples
{
     [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling, ShowTypeColumn = false), TabCompletion /* [TabCompletion] is useful for the sample, but you don't need it in your program (unless you want it).*/ ]
    public class HelloWorldInvokeArgs
    {
        public string StringArg { get; set; }
        public int    IntArg    { get; set; }
        public bool   SwitchArg { get; set; }
  
        public void Main()
        {
            Console.WriteLine("You entered StringArg '{0}' and IntArg '{1}', switch was '{2}'", this.StringArg, this.IntArg, this.SwitchArg);
        }
    }

    public class HelloWorldInvoke
    {
        public static void _Main(string[] args)
        {
            var parsed = Args.InvokeMain<HelloWorldInvokeArgs>(args);
        }
    }
}
