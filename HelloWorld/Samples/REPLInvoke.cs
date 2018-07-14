using PowerArgs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HelloWorld.Samples
{
    /*
     * This sample shows how you can easily provide a REPL (Read Eval Print Loop) experience for
     * your command line app.  All you do is provide REPL = true to the [TabCompletion] attribute
     * and then make sure you call either Args.InvokeAction (like this sample does) or Args.InvokeMain.
     * 
     * PowerArgs will manage the REPL for you.  You can even pass hints to [TabCompletion] that let you customize the
     * terminating string and the welcome message.
     * 
     * The HistoryToSave parameter to [TabCompletion] indicates that the users' command line history should be saved
     * so that they can use the up and down arrows to cycle through recent commands.  By default this is saved in <UserFolder>/AppData/Roaming/PowerArgs/<ExeName>.TabCompletionHistory.txt
     * 
     * Users would get automatic tab completion/cycling for action names, property names, and enum values.  This sample
     * extends tab completion to include the strings that are in the Items list that the sample operates on.
     * 
     * PowerArgs also implements a 'cls' command that users can use to clear the console while in the REPL.
     * 
     * Finally, the sample shows how to use things like the [ArgDescription] attribute to provide better auto generated
     * usage documentation.
     */

    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling), TabCompletion(typeof(ItemNameCompletion), REPL = true, HistoryToSave = 10)]
    [ArgExample("add a,b,c", "Adds three items (\"a\", \"b\", and \"c\") to the list"), ArgExample("clear", "Clears the list")]
    public class REPLInvokeArgs
    {
        // The REPL exposes Add, Remove, List, and Clear commands that operate on this list 
        public static List<string> Items = new List<string>();

        public enum OutputMode
        {
            [ArgDescription("Don't output anything (except for the list command)")] Off     = 0,
            [ArgDescription("Output minimal info")]                                 Minimal = 1,
            [ArgDescription("Output every single detail")]                          Verbose = 2,
        }

        [DefaultValue(OutputMode.Minimal), ArgDescription("Determines the verbosity of the output")]
        public OutputMode Output { get; set; }

        [ArgActionMethod, OmitFromUsageDocs, ArgDescription("Displays the help")]
        public void Help()
        {
            ArgUsage.GenerateUsageFromTemplate(typeof(REPLInvokeArgs)).WriteLine();
        }

        [ArgActionMethod, ArgDescription("Adds a new item to the list")]
        public void Add(Items toAdd)
        {
            foreach (var item in toAdd.Values)
            {
                Items.Add(item);
                if (Output == OutputMode.Verbose)
                {
                    Console.WriteLine("Added item '{0}'", item);
                }
            }
        }

        [ArgActionMethod, ArgDescription("Removes the first item found that matches the name")]
        public void Remove(SingleItemArgs item)
        {
            bool removed = Items.Remove(item.Value);

            if     (removed && Output > OutputMode.Off)  Console.WriteLine("Removed item '{0}'", item.Value);
            else if(!removed && Output > OutputMode.Off) Console.WriteLine("Item '{0}' not found", item.Value);
        }

        [ArgActionMethod, ArgDescription("Displays the list of items")]
        public void List()
        {
            if (Items.Count == 0 && Output != OutputMode.Off) Console.WriteLine("The list is empty");
            for (int i = 0; i < Items.Count; i++) Console.WriteLine(i + ": " + Items[i]);
        }

        [ArgActionMethod, ArgDescription("Clears the list of items")]
        public void Clear()
        {
            int count = Items.Count;
            Items.Clear();

            if (Output != OutputMode.Off) Console.WriteLine(count == 1 ? "Removed 1 item" : string.Format("Removed {0} items", count));
        }
    }

    public class SingleItemArgs
    {
        [ArgPosition(1), ArgRequired, ArgDescription("The textal value of the item")]
        public string Value { get; set; }
    }

    public class Items
    {
        [ArgPosition(1), ArgRequired, ArgDescription("Comma separated names of the items to operate on")]
        public List<string> Values { get; set; }
    }

    public class ItemNameCompletion : SimpleTabCompletionSource
    {
        // The lambda that is sent to the base constructor will ensure that we get tab completion
        // on the REPL command line for items that are in the list at the time of execution.
        public ItemNameCompletion() : base(() => { return REPLInvokeArgs.Items; }) 
        {
            MinCharsBeforeCyclingBegins = 0;
        }
    }

    public class REPLInvoke
    {
        // This is the code you would put in your Main method.  It's called _Main here since you can only have 1 called Main
        // in the assembly.
        public static void _Main(string[] args) => Args.InvokeAction<REPLInvokeArgs>(args);
    }
}
