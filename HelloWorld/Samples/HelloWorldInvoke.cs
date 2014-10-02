using PowerArgs;
using System;

namespace HelloWorld.Samples
{
    public enum Environment
    {
        Dev,
        Test,
        Production
    }

    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling, ShowTypeColumn = true), TabCompletion(HistoryToSave=10,REPL=true) /* [TabCompletion] is useful for the sample, but you don't need it in your program (unless you want it).*/ ]
    [ArgDescription("A simple command line application that accepts a few optional arguments.")]
    [ArgExample("HelloWorld -s HelloWorld","A very simple example that results in the 'StringArg' property being set to 'HelloWorld'",Title = "Your very first example")]
    public class HelloWorldInvokeArgs
    {
        [DefaultValue("Hello!")]
        [ArgumentAwareTabCompletion(typeof(MetalTabCompletionSource))]
        [ArgDescription("An optional string arg")]
        public string StringArg { get; set; }
        [ArgDescription("An optional int arg")]
        public int?    IntArg    { get; set; }
        [ArgDescription("An optional switch arg")]
        public bool   SwitchArg { get; set; }

        [HelpHook]
        public bool Help { get; set; }

        [DefaultValue(Environment.Dev), ArgDescription("The target environment for this operation")]
        public Environment Environment { get; set; }
  
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

    public class MetalTabCompletionSource : SimpleTabCompletionSource
    {
        public MetalTabCompletionSource() : base(new string[] { "Gold", "Silver", "Iron" })
        {
            this.MinCharsBeforeCyclingBegins = 0;
        }
    }
}
