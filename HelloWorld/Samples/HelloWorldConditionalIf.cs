using PowerArgs;
using System;

namespace HelloWorld.Samples
{
    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling, ShowTypeColumn = false), TabCompletion(REPL=true) /* [TabCompletion] is useful for the sample, but you don't need it in your program (unless you want it).*/ ]
    public class HelloWorldConditionalIfArgs
    {
        [HelpHook, ArgShortcut("-?")]
        public bool Help { get; set; }

        [ArgRequired(IfNot = "Anonymous"), ArgCantBeCombinedWith("Anonymous")]
        public string Name { get; set; }

        [ArgRequired(If = "Name")]
        public string Address { get; set; }

        [ArgRequired(IfNot = "Name")]
        public bool Anonymous { get; set; }
  
        public void Main()
        {
            if(Anonymous)
            {
                Console.WriteLine("You are anonymous");
            }
            else
            {
                // Your code can assume that both Name and Address have been provided (and validated if you added validators)
                Console.WriteLine("Your name is '{0}' and your address is '{1}'", Name, Address);
            }
        }
    }

     public class HelloWorldConditionalIInvoke
    {
        public static void _Main(string[] args)
        {
            var parsed = Args.InvokeMain<HelloWorldConditionalIfArgs>(args);
        }
    }
}
