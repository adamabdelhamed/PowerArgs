using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PowerArgs
{
    internal class REPLExitException : Exception {}
    internal class REPLContinueException : Exception { }
    
    /// <summary>
    /// An attribute that can be placed on an argument property that adds argument aware tab completion for users who press the tab key while
    /// in the context of the targeted argument.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter,AllowMultiple=true)]
    public class ArgumentAwareTabCompletionAttribute : Attribute, ICommandLineArgumentMetadata
    {
        /// <summary>
        /// The tab completion source type that will be used to implement tab completion
        /// </summary>
        public Type CompletionSourceType { get; private set; }

        /// <summary>
        /// Creates a new ArgumentAwareTabCompletionAttribute given a completion source type
        /// </summary>
        /// <param name="completionSourceType"></param>
        public ArgumentAwareTabCompletionAttribute(Type completionSourceType)
        {
            this.CompletionSourceType = completionSourceType;
        }

        internal object CreateTabCompletionSource(CommandLineArgumentsDefinition definition, CommandLineArgument argument)
        {
            ITabCompletionSource ret;
            if(this.CompletionSourceType.GetInterfaces().Contains(typeof(ISmartTabCompletionSource)))
            {
                var source = ObjectFactory.CreateInstance(CompletionSourceType) as ISmartTabCompletionSource;
                return new ArgumentAwareWrapperSmartTabCompletionSource(definition, argument, source);
            }
            else if (this.CompletionSourceType.IsSubclassOf(typeof(ArgumentAwareTabCompletionSource)))
            {
                ret = (ITabCompletionSource)Activator.CreateInstance(this.CompletionSourceType, definition, argument);
            }
            else if (this.CompletionSourceType.GetInterfaces().Contains(typeof(ITabCompletionSource)))
            {
                var toWrap = (ITabCompletionSource)ObjectFactory.CreateInstance(this.CompletionSourceType);
                ret = new ArgumentAwareWrapperTabCompletionSource(definition, argument, toWrap);
            }
            else
            {
                throw new InvalidArgDefinitionException("The type " + this.CompletionSourceType.FullName + " does not implement ITabCompletionSource or ITabCompletionSource.  The target argument was " + argument.DefaultAlias);
            }

            return ret;
        }
    }

    /// <summary>
    /// A hook that takes over the command line and provides tab completion for known strings when the user presses
    /// the tab key.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TabCompletion : ArgHook, ICommandLineArgumentsDefinitionMetadata
    {
        /// <summary>
        /// Gets or sets the type to be used for global tab completion.  The type should implement ITabCompletionSource or ISmartTabCompletionSource
        /// </summary>
        public Type CompletionSourceType { get; set; }

        /// <summary>
        /// Gets or sets the type to be used to dynamically configure syntax highlighting.  The type must implement IHighlighterConfigurator.
        /// </summary>
        public Type HighlighterConfiguratorType { get; set; }

        /// <summary>
        /// When this indicator is the only argument the user specifies that triggers the hook to enhance the command prompt.  By default, the indicator is the empty string.
        /// </summary>
        public string Indicator { get; set; }

        /// <summary>
        /// If this is > 0 then PowerArgs will save this many previous executions of the command line to your application data folder.
        /// Users can then access the history by pressing arrow up or down from the enhanced command prompt.
        /// </summary>
        public int HistoryToSave { get; set; }

        /// <summary>
        /// The location of the history file name (AppData/PowerArgs/EXE_NAME.TabCompletionHistory.txt
        /// </summary>
        public string HistoryFileName { get; set; }

        /// <summary>
        /// The name of your program (leave null and PowerArgs will try to detect it automatically)
        /// </summary>
        public string ExeName { get; set; }

        /// <summary>
        /// If true, then you must use Args.InvokeAction or Args.InvokeMain instead of Args.Parse.  Your user
        /// will get an interactive prompt that loops until they specify the REPLExitIndicator.
        /// </summary>
        public bool REPL { get; set; }

        /// <summary>
        /// The string users can specify in order to exit the REPL (defaults to string.Empty)
        /// </summary>
        public string REPLExitIndicator { get; set; }

        /// <summary>
        /// The message to display to the user when the REPL starts.  The default is Type a command or '{{Indicator}}' to exit.
        /// You can customize this message and use {{Indicator}} for the placeholder for your exit indicator.
        /// </summary>
        public string REPLWelcomeMessage { get; set; }

        internal bool ShowREPLWelcome { get; set; }

        private string HistoryFileNameInternal
        {
            get
            {
                var exeName = ExeName ?? Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().Location);
                return HistoryFileName ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PowerArgs", exeName + ".TabCompletionHistory.txt");
            }
        }

        /// <summary>
        /// Creates a new tab completion hook.
        /// </summary>
        /// <param name="indicator">When this indicator is the only argument the user specifies that triggers the hook to enhance the command prompt.  By default, the indicator is the empty string.</param>
        public TabCompletion(string indicator = "")
        {
            this.Indicator = indicator;
            BeforeParsePriority = 100;
            HistoryToSave = 0;
            REPLExitIndicator = "quit";
            REPLWelcomeMessage = "Type a command or '{{Indicator}}' to exit.";
            ShowREPLWelcome = true;
        }

        /// <summary>
        /// Creates a new tab completion hook given a custom tab completion implementation.
        /// </summary>
        /// <param name="completionSource">A type that implements ITabCompletionSource such as SimpleTabCompletionSource</param>
        /// <param name="indicator">When this indicator is the only argument the user specifies that triggers the hook to enhance the command prompt.  By default, the indicator is the empty string.</param>
        public TabCompletion(Type completionSource, string indicator = "") : this(indicator)
        {
            this.CompletionSourceType = completionSource;
        }

        /// <summary>
        /// Before PowerArgs parses the args, this hook inspects the command line for the indicator and if found 
        /// takes over the command line and provides tab completion.
        /// </summary>
        /// <param name="context">The context used to inspect the command line arguments.</param>
        public override void BeforeParse(ArgHook.HookContext context)
        {
          
            if (CompletionSourceType != null && 
                CompletionSourceType.GetInterfaces().Contains(typeof(ITabCompletionSource)) == false &&
                CompletionSourceType.GetInterfaces().Contains(typeof(ISmartTabCompletionSource)) == false)
            {
                throw new InvalidArgDefinitionException("Type does not implement ITabCompletionSource or ISmartTabCompletionSource: " + CompletionSourceType.FullName);
            }

            if (context.Definition.IsNonInteractive)
            {
                this.REPL = false;
                return;
            }
            if (Indicator == "" && context.CmdLineArgs.Length != 0)
            {
                this.REPL = false;
                return;
            }
            if (Indicator != "" && (context.CmdLineArgs.Length != 1 || context.CmdLineArgs[0] != Indicator))
            {
                this.REPL = false;
                return;
            }

            if (REPL && ShowREPLWelcome)
            {
                ConsoleString.Empty.WriteLine();
                var message = REPLWelcomeMessage.Replace("{{Indicator}}", REPLExitIndicator);
                ConsoleString.WriteLine(message, ConsoleColor.Cyan);
                ConsoleString.Empty.WriteLine();
                ConsoleString.Write(Indicator + "> ", ConsoleColor.Cyan);
                ShowREPLWelcome = false;
            }
            else if (REPL)
            {
                ConsoleString.Write(Indicator + "> ", ConsoleColor.Cyan);
            }
            else
            {
                // This is a little hacky, but I could not find a better way to make the tab completion start on the same lime
                // as the command line input
                try
                {
                    var lastLine = StdConsoleProvider.ReadALineOfConsoleOutput(Console.CursorTop - 1);
                    Console.CursorTop--;
                    Console.WriteLine(lastLine);
                    Console.CursorTop--;
                    Console.CursorLeft = lastLine.Length + 1;
                }
                catch (Exception)
                {
                    Console.WriteLine();
                    Console.Write(Indicator + "> ");
                }
            }

            PowerArgsRichCommandLineReader reader = new PowerArgsRichCommandLineReader(context.Definition, LoadHistory());

            IHighlighterConfigurator customConfigurator;
            if(HighlighterConfiguratorType.TryCreate<IHighlighterConfigurator>(out customConfigurator))
            {
                customConfigurator.Configure(reader.Highlighter);
            }

            var newCommandLineString = reader.ReadLine().ToString();
            var newCommandLineArray = Args.Convert(newCommandLineString);

            if (REPL && newCommandLineArray.Length == 1 && string.Equals(newCommandLineArray[0], REPLExitIndicator, StringComparison.OrdinalIgnoreCase))
            {
                throw new REPLExitException();
            }

            if (REPL && newCommandLineArray.Length == 1 && newCommandLineArray[0] == "cls")
            {
                ConsoleProvider.Current.Clear();
                throw new REPLContinueException();
            }

            else if (REPL && newCommandLineArray.Length == 0 && string.IsNullOrWhiteSpace(REPLExitIndicator) == false)
            {
                throw new REPLContinueException();
            }

            context.CmdLineArgs = newCommandLineArray;
            AddToHistory(newCommandLineString);
        }

        /// <summary>
        /// Clears all history saved on disk
        /// </summary>
        public void ClearHistory()
        {
            if (File.Exists(HistoryFileNameInternal))
            {
                File.WriteAllText(HistoryFileNameInternal, "");
            }
        }

        private void AddToHistory(string item)
        {
            if (HistoryToSave == 0) return;

            if(File.Exists(HistoryFileNameInternal) == false)
            {
                File.WriteAllLines(HistoryFileNameInternal, new string[0]);
            }

            List<string> history = File.ReadAllLines(HistoryFileNameInternal).ToList();
            history.Insert(0, item);
            history = history.Distinct().ToList();
            while (history.Count > HistoryToSave) history.RemoveAt(history.Count - 1);
            File.WriteAllLines(HistoryFileNameInternal, history.ToArray());
        }

        private List<ConsoleString> LoadHistory()
        {
            if (HistoryToSave == 0) return new List<ConsoleString>();

            if (Directory.Exists(Path.GetDirectoryName(HistoryFileNameInternal)) == false)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(HistoryFileNameInternal));
                return new List<ConsoleString>();
            }
            else if (File.Exists(HistoryFileNameInternal) == false)
            {
                File.WriteAllLines(HistoryFileNameInternal, new string[0]);
                return new List<ConsoleString>();
            }
            else
            {
                var lines = File.ReadAllLines(HistoryFileNameInternal).ToList();
                List<ConsoleString> ret = new List<ConsoleString>();
                foreach(var line in lines)
                {
                    ret.Add(new ConsoleString(line));
                }
                return ret;
            }
        } 
    }
}
