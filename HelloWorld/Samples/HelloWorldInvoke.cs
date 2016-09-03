using PowerArgs;
using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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
        [ArgDescription("An optional int arg dddddddddddddddddddddddddddddddddddddddddddddddddddd d  d dddddddddddddddddddddd dddddddddddddddddddddd d ddddddddddddddddddddddddddddddddddd")]
        public int?    IntArg    { get; set; }
        [ArgDescription("An optional switch arg")]
        public bool   SwitchArg { get; set; }

        [ArgContextualAssistant(typeof(StatePickerAssistant))]
        public string State { get; set; }

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

    public class StatePickerAssistant : ContextAssistPicker
    {
        public static List<string> States = new List<string>
        {
            "Alabama",
            "Alaska", 
            "Arizona", 
            "Arkansas", 
            "California", 
            "Colorado", 
            "Connecticut", 
            "Delaware", 
            "Florida", 
            "Georgia", 
            "Hawaii", 
            "Idaho", 
            "Illinois", 
            "Indiana", 
            "Iowa", 
            "Kansas", 
            "Kentucky", 
            "Louisiana", 
            "Maine", 
            "Maryland", 
            "Massachusetts", 
            "Michigan", 
            "Minnesota", 
            "Mississippi", 
            "Missouri", 
            "Montana", 
            "Nebraska", 
            "Nevada", 
            "\"New Hampshire\"", 
            "\"New Jersey\"", 
            "\"New Mexico\"", 
            "\"New York\"", 
            "\"North Carolina\"", 
            "\"North Dakota\"",
            "Ohio",
            "Oklahoma",
            "Oregon",
            "Pennsylvania\"",
            "\"Rhode Island\"",
            "\"South Carolina\"",
            "\"South Dakota\"", 
            "Tennessee", 
            "Texas",
            "Utah",
            "Vermont",
            "Virginia",
            "Washington", 
            "\"West Virginia\"",
            "Wisconsin", 
            "Wyoming",
        };

        public StatePickerAssistant()
        {
            Options.AddRange(States.Select(s => ContextAssistSearchResult.FromString(s)));
        }
    }
}
