using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PowerArgs.Cli;

namespace ArgsTests.CLI
{
    [TestClass]
    public class ContextAssistTests
    {
        [TestMethod]
        public void TestBasicAssistStartOfLine()
        {
            ConsoleProvider.Current = new TestConsoleProvider("{control} {w}{down}{down}{enter}");
            CliHelper cli = new CliHelper();
            var picker = new ContextAssistPicker();
            picker.Options.Add(ContextAssistSearchResult.FromString("Option 1"));
            picker.Options.Add(ContextAssistSearchResult.FromString("Option 2"));
            picker.Options.Add(ContextAssistSearchResult.FromString("Option 3"));
            cli.Reader.ContextAssistProvider = picker;
            
            var line = cli.Reader.ReadLine();
            Assert.AreEqual("Option 3", line.ToString());
        }

        [TestMethod]
        public void TestEscapingFromPicker()
        {
            ConsoleProvider.Current = new TestConsoleProvider("{control} {w}{down}{down}{escape}");
            CliHelper cli = new CliHelper();
            var picker = new ContextAssistPicker();
            picker.Options.Add(ContextAssistSearchResult.FromString("Option 1"));
            picker.Options.Add(ContextAssistSearchResult.FromString("Option 2"));
            picker.Options.Add(ContextAssistSearchResult.FromString("Option 3"));
            cli.Reader.ContextAssistProvider = picker;

            var line = cli.Reader.ReadLine();
            Assert.AreEqual("", line.ToString());
        }

        [TestMethod]
        public void TestCyclingDownThroughOptions()
        {
            ConsoleProvider.Current = new TestConsoleProvider("{control} {w}{down}{down}{down}{down}{down}{enter}");
            CliHelper cli = new CliHelper();
            var picker = new ContextAssistPicker();
            picker.Options.Add(ContextAssistSearchResult.FromString("Option 1"));
            picker.Options.Add(ContextAssistSearchResult.FromString("Option 2"));
            picker.Options.Add(ContextAssistSearchResult.FromString("Option 3"));
            cli.Reader.ContextAssistProvider = picker;

            var line = cli.Reader.ReadLine();
            Assert.AreEqual("Option 3", line.ToString());
        }

        [TestMethod]
        public void TestCyclingUpThroughOptions()
        {
            ConsoleProvider.Current = new TestConsoleProvider("{control} {w}{up}{enter}");
            CliHelper cli = new CliHelper();
            var picker = new ContextAssistPicker();
            picker.Options.Add(ContextAssistSearchResult.FromString("Option 1"));
            picker.Options.Add(ContextAssistSearchResult.FromString("Option 2"));
            picker.Options.Add(ContextAssistSearchResult.FromString("Option 3"));
            cli.Reader.ContextAssistProvider = picker;

            var line = cli.Reader.ReadLine();
            Assert.AreEqual("Option 3", line.ToString());
        }

        [TestMethod]
        public void TestBasicAssistEndOfLineAfterASpace()
        {
            ConsoleProvider.Current = new TestConsoleProvider("choice: {control} {w}{down}{down}{enter}");
            CliHelper cli = new CliHelper();
            var picker = new ContextAssistPicker();
            picker.Options.Add(ContextAssistSearchResult.FromString("Option 1"));
            picker.Options.Add(ContextAssistSearchResult.FromString("Option 2"));
            picker.Options.Add(ContextAssistSearchResult.FromString("Option 3"));
            cli.Reader.ContextAssistProvider = picker;

            var line = cli.Reader.ReadLine();
            Assert.AreEqual("choice: Option 3", line.ToString());
        }

        [TestMethod]
        public void TestBasicAssistEndOfLineReplacingCurrentToken()
        {
            ConsoleProvider.Current = new TestConsoleProvider("choice: asdasdasdasdasd{control} {w}{down}{down}{enter}");
            CliHelper cli = new CliHelper();
            var picker = new ContextAssistPicker();
            picker.Options.Add(ContextAssistSearchResult.FromString("Option 1"));
            picker.Options.Add(ContextAssistSearchResult.FromString("Option 2"));
            picker.Options.Add(ContextAssistSearchResult.FromString("Option 3"));
            cli.Reader.ContextAssistProvider = picker;

            var line = cli.Reader.ReadLine();
            Assert.AreEqual("choice: Option 3", line.ToString());
        }

        [TestMethod]
        public void TestBasicAssistMiddleOfLineReplacingCurrentToken()
        {
            ConsoleProvider.Current = new TestConsoleProvider("choice: abc after{left}{left}{left}{left}{left}{left}{control} {w}{down}{down}{enter}");
            CliHelper cli = new CliHelper();
            var picker = new ContextAssistPicker();
            picker.Options.Add(ContextAssistSearchResult.FromString("Option 1"));
            picker.Options.Add(ContextAssistSearchResult.FromString("Option 2"));
            picker.Options.Add(ContextAssistSearchResult.FromString("Option 3"));
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

        [TestMethod]
        public void TestObjectPickerBasic()
        {
            ConsoleProvider.Current = new TestConsoleProvider("1 Mic{w}");
            PeoplePicker picker = new PeoplePicker();
            var result = picker.Search();
            Assert.IsInstanceOfType(result.ResultValue, typeof(PeoplePicker.Person));
            var person = result.ResultValue as PeoplePicker.Person;
            Assert.AreEqual("Adam", person.Name);
        }

        [TestMethod]
        public void TestObjectPickerCancel()
        {
            ConsoleProvider.Current = new TestConsoleProvider("1 Mic{escape}");
            PeoplePicker picker = new PeoplePicker();
            var result = picker.Search();
            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestObjectPickerCantCancel()
        {
            ConsoleProvider.Current = new TestConsoleProvider("1 Mic{escape}{w}");
            PeoplePicker picker = new PeoplePicker();
            var result = picker.Search(allowCancel: false);
            Assert.IsInstanceOfType(result.ResultValue, typeof(PeoplePicker.Person));
            var person = result.ResultValue as PeoplePicker.Person;
            Assert.AreEqual("Adam", person.Name);
        }

        [TestMethod]
        public void TestBasePickerWithCustomConsole()
        {
            var picker = new ContextAssistPicker();
            picker.Options.Add(ContextAssistSearchResult.FromString("Pizza"));
            picker.Options.Add(ContextAssistSearchResult.FromString("Pasta"));
            picker.Options.Add(ContextAssistSearchResult.FromString("Steak"));

            var choice = picker.Pick(new TestConsoleProvider("s{w}"));
            Assert.AreEqual("Steak", choice.DisplayText);
            Assert.AreEqual("Steak", choice.ResultValue);
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

        protected override System.Collections.Generic.List<ContextAssistSearchResult> GetResults(string searchString)
        {
            return states.Where(r => r.StartsWith(searchString, StringComparison.InvariantCultureIgnoreCase))
                .Select(s => ContextAssistSearchResult.FromString(s))
                .ToList();
        }

        public override bool SupportsAsync
        {
            get { return false; }
        }

        protected override System.Threading.Tasks.Task<List<ContextAssistSearchResult>> GetResultsAsync(string searchString)
        {
            throw new NotImplementedException();
        }
    }

    public class PeoplePicker : ContextAssistSearch
    {
        public class Person
        {
            public string Name { get; set; }
            public string Address { get; set; }
        }

        public static List<Person> people = new List<Person>()
        {
            new Person()
            {
                Name = "Adam",
                Address = "1 Microsoft Way"
            },
            new Person()
            {
                Name = "Joe",
                Address = "Joe's place"
            },
            new Person()
            {
                Name = "Mike",
                Address = "Main street"
            }
        };


        public override bool SupportsAsync
        {
            get { return false; }
        }

        protected override List<ContextAssistSearchResult> GetResults(string searchString)
        {
            return people.Where(p => p.Name.StartsWith(searchString, StringComparison.InvariantCultureIgnoreCase) || p.Address.StartsWith(searchString, StringComparison.InvariantCultureIgnoreCase))
                         .Select(p => ContextAssistSearchResult.FromObject(p))
                         .ToList();
        }

        protected override Task<List<ContextAssistSearchResult>> GetResultsAsync(string searchString)
        {
            throw new NotImplementedException();
        }
    }

    public class StatePickerAssistantAsync : ContextAssistSearch
    {
        public static readonly IEnumerable<string> states = new List<string>
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
        }.AsReadOnly();

        static Random rand = new Random();

        protected override System.Collections.Generic.List<ContextAssistSearchResult> GetResults(string searchString)
        {
            throw new NotImplementedException();
        }

        public override bool SupportsAsync
        {
            get { return true; }
        }

        protected override System.Threading.Tasks.Task<List<ContextAssistSearchResult>> GetResultsAsync(string searchString)
        {
            return Task.Factory.StartNew<List<ContextAssistSearchResult>>(() => 
            {
                return states.Where(r => r.StartsWith(searchString, StringComparison.InvariantCultureIgnoreCase))
                    .Select(s => ContextAssistSearchResult.FromString(s))
                    .ToList();
            });
        }
    }
}
