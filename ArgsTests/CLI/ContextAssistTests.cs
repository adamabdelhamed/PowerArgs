using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArgsTests.CLI
{
    [TestClass]
    public class ContextAssistTests
    {
        [TestMethod]
        public void TestBasicAssistStartOfLine()
        {
            ConsoleProvider.Current = new TestConsoleProvider("{control} {w}{down}{down}{enter}");
            Cli cli = new Cli();
            var picker = new ContextAssistPicker();
            picker.Options.Add("Option 1");
            picker.Options.Add("Option 2");
            picker.Options.Add("Option 3");
            cli.Reader.ContextAssistProvider = picker;
            
            var line = cli.Reader.ReadLine();
            Assert.AreEqual("Option 3", line.ToString());
        }

        [TestMethod]
        public void TestEscapingFromPicker()
        {
            ConsoleProvider.Current = new TestConsoleProvider("{control} {w}{down}{down}{escape}");
            Cli cli = new Cli();
            var picker = new ContextAssistPicker();
            picker.Options.Add("Option 1");
            picker.Options.Add("Option 2");
            picker.Options.Add("Option 3");
            cli.Reader.ContextAssistProvider = picker;

            var line = cli.Reader.ReadLine();
            Assert.AreEqual("", line.ToString());
        }

        [TestMethod]
        public void TestCyclingDownThroughOptions()
        {
            ConsoleProvider.Current = new TestConsoleProvider("{control} {w}{down}{down}{down}{down}{down}{enter}");
            Cli cli = new Cli();
            var picker = new ContextAssistPicker();
            picker.Options.Add("Option 1");
            picker.Options.Add("Option 2");
            picker.Options.Add("Option 3");
            cli.Reader.ContextAssistProvider = picker;

            var line = cli.Reader.ReadLine();
            Assert.AreEqual("Option 3", line.ToString());
        }

        [TestMethod]
        public void TestCyclingUpThroughOptions()
        {
            ConsoleProvider.Current = new TestConsoleProvider("{control} {w}{up}{enter}");
            Cli cli = new Cli();
            var picker = new ContextAssistPicker();
            picker.Options.Add("Option 1");
            picker.Options.Add("Option 2");
            picker.Options.Add("Option 3");
            cli.Reader.ContextAssistProvider = picker;

            var line = cli.Reader.ReadLine();
            Assert.AreEqual("Option 3", line.ToString());
        }

        [TestMethod]
        public void TestBasicAssistEndOfLineAfterASpace()
        {
            ConsoleProvider.Current = new TestConsoleProvider("choice: {control} {w}{down}{down}{enter}");
            Cli cli = new Cli();
            var picker = new ContextAssistPicker();
            picker.Options.Add("Option 1");
            picker.Options.Add("Option 2");
            picker.Options.Add("Option 3");
            cli.Reader.ContextAssistProvider = picker;

            var line = cli.Reader.ReadLine();
            Assert.AreEqual("choice: Option 3", line.ToString());
        }

        [TestMethod]
        public void TestBasicAssistEndOfLineReplacingCurrentToken()
        {
            ConsoleProvider.Current = new TestConsoleProvider("choice: asdasdasdasdasd{control} {w}{down}{down}{enter}");
            Cli cli = new Cli();
            var picker = new ContextAssistPicker();
            picker.Options.Add("Option 1");
            picker.Options.Add("Option 2");
            picker.Options.Add("Option 3");
            cli.Reader.ContextAssistProvider = picker;

            var line = cli.Reader.ReadLine();
            Assert.AreEqual("choice: Option 3", line.ToString());
        }

        [TestMethod]
        public void TestBasicAssistMiddleOfLineReplacingCurrentToken()
        {
            ConsoleProvider.Current = new TestConsoleProvider("choice: abc after{left}{left}{left}{left}{left}{left}{control} {w}{down}{down}{enter}");
            Cli cli = new Cli();
            var picker = new ContextAssistPicker();
            picker.Options.Add("Option 1");
            picker.Options.Add("Option 2");
            picker.Options.Add("Option 3");
            cli.Reader.ContextAssistProvider = picker;

            var line = cli.Reader.ReadLine();
            Assert.AreEqual("choice: Option 3 after", line.ToString());
        }

        [TestMethod]
        public void TestSyncSearchDoesntBreakConsole()
        {
            PowerLogger.LogFile = @"C:\temp\TestSearchAsync.txt";
            var input = "-s\t {control} ";

            for (int i = 0; i < 10000; i++ )
            {
                input+="{enter}{control} ";
            }

            input+="{wait}";

            ConsoleProvider.Current = new TestConsoleProvider(input);
            var parsed = Args.Parse<TestArgsForAssist>("$");
            Assert.AreEqual("Alabama", parsed.State);
        }

        [TestMethod]
        public void TestAsyncSearchDoesntBreakConsole()
        {
            PowerLogger.LogFile = @"C:\temp\TestSearchAsync.txt";
            var input = "-s\t {control} ";

            for (int i = 0; i < 10000; i++)
            {
                input += "{enter}{control} ";
            }

            input += "{wait}";

            ConsoleProvider.Current = new TestConsoleProvider(input);
            var parsed = Args.Parse<TestArgsForAssistAsync>("$");
            Assert.AreEqual("Alabama", parsed.State);
        }
    }

    [TabCompletion("$")]
    public class TestArgsForAssist
    {
        [ArgContextualAssistant(typeof(StatePickerAssistant))]
        public string State { get; set; }
    }

    [TabCompletion("$")]
    public class TestArgsForAssistAsync
    {
        [ArgContextualAssistant(typeof(StatePickerAssistantAsync))]
        public string State { get; set; }
    }

    
    public class StatePickerAssistant : ContextAssistSearch
    {
        List<string> states = new List<string>
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
            "Wyoming ",
        };

        static Random rand = new Random();

        protected override System.Collections.Generic.List<string> GetResults(string searchString)
        {
            return states.Where(r => r.StartsWith(searchString, StringComparison.InvariantCultureIgnoreCase)).ToList();
        }

        public override bool SupportsAsync
        {
            get { return false; }
        }

        protected override System.Threading.Tasks.Task<List<string>> GetResultsAsync(string searchString)
        {
            throw new NotImplementedException();
        }
    }

    public class StatePickerAssistantAsync : ContextAssistSearch
    {
        List<string> states = new List<string>
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
            "Wyoming ",
        };

        static Random rand = new Random();

        protected override System.Collections.Generic.List<string> GetResults(string searchString)
        {
            throw new NotImplementedException();
        }

        public override bool SupportsAsync
        {
            get { return true; }
        }

        protected override System.Threading.Tasks.Task<List<string>> GetResultsAsync(string searchString)
        {
            return Task.Factory.StartNew<List<string>>(() => 
            {
                return states.Where(r => r.StartsWith(searchString, StringComparison.InvariantCultureIgnoreCase)).ToList();
            });
        }
    }
}
