using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ArgsTests
{
    [TestClass]
    public class TabCompletionTests
    {
        const int MaxHistory = 10;

        public enum EnumWithShortcuts
        {
            [ArgShortcut("NumberOne")]
            One,
            Two,
            Three,
        }

        [TabCompletion(typeof(MyCompletionSource), "$", ExeName = "TestSuiteTestArgs.exe", HistoryToSave = MaxHistory)]
        public class TestArgs
        {
            public string SomeParam { get; set; }
            public int AnotherParam { get; set; }
            public bool BoolParam { get; set; }
        }

        [TabCompletion(typeof(MyCompletionSource), "$", ExeName = "TestSuiteTestArgs.exe", HistoryToSave = MaxHistory)]
        public class TestArgsWithSecureStringArgument
        {
            public SecureStringArgument Password { get; set; }
        }

        [TabCompletion(typeof(MyCompletionSource), "$", REPL = true, ExeName = "TestSuiteTestArgs.exe", HistoryToSave = MaxHistory)]
        public class TestArgsWithREPL
        {
            public string SomeParam { get; set; }
            public int AnotherParam { get; set; }
            public bool BoolParam { get; set; }

            public static List<string> SomeParamValues { get; private set; }

            public void Main()
            {
                SomeParamValues = SomeParamValues ?? new List<string>();
                SomeParamValues.Add(SomeParam);
            }
        }

        [TabCompletion("$")]
        public class TestArgsWithEnum
        {
            public EnumWithShortcuts Enum { get; set; }
        }

        [TabCompletion("$")]
        public class ArgsWithActionsAndEnums
        {
            [ArgActionMethod]
            public void Do(DayOfWeek week) { }
        }

        public class MyCompletionSource : SimpleTabCompletionSource
        {
            public MyCompletionSource() : base(MyCompletionSource.GetWords()) {}
            private static IEnumerable<string> GetWords()
            {
                return "Adam|Abdelhamed".Split('|');
            }
        }

        public class NameSource : SimpleTabCompletionSource
        {
            public NameSource() : base(new string[] { "Adam", "Joe" }) { }
        }

        public class CustomLegacySourceThatWillOnlyWorkWithATargetArgument : ITabCompletionSource
        {
            NameSource wrapped = new NameSource();

  
            public bool TryComplete(bool shift, string soFar, out string completion)
            {
                return wrapped.TryComplete(shift, soFar, out completion);
            }
        }

        [TabCompletion("$", ExeName = "TestSuiteTestArgs.exe", HistoryToSave = MaxHistory)]
        public class ArgAwareCompletionArgsWithLegacySource
        {
            [ArgumentAwareTabCompletionAttribute(typeof(CustomLegacySourceThatWillOnlyWorkWithATargetArgument))]
            public string Name { get; set; }
            public string Address { get; set; }
        }

        [TabCompletion(typeof(MyCompletionSource), "$", ExeName = "TestSuiteTestArgs.exe", HistoryToSave = MaxHistory)]
        public class ArgAwareCompletionArgs
        {
            [ArgumentAwareTabCompletionAttribute(typeof(NameSource))]
            public string Name { get; set; }
            public string Address { get; set; }
        }

        [TestMethod]
        public void EnsureSecureStringsAreNotTabCompletable()
        {
            ConsoleProvider.Current = new TestConsoleProvider("-pa\t");
            try
            {
                var parsed = Args.Parse<TestArgsWithSecureStringArgument>("$");
            }
            catch(UnexpectedArgException ex)
            {
                // if we see 'pa' as the argument then we know that the tab completion system did not expand 'pa' to 'password'
                Assert.AreEqual("Unexpected named argument: pa", ex.Message);
            }
        }

        [TestMethod]
        public void LegacyTestWithArgSpecificTabCompletion()
        {
            
            ConsoleProvider.Current = new TestConsoleProvider("-N A\t");
            var parsed = Args.Parse<ArgAwareCompletionArgsWithLegacySource>("$");
            Assert.AreEqual("Adam", parsed.Name);
        }

        [TestMethod]
        public void TestActionEnumArgCompletion()
        {
            ConsoleProvider.Current = new TestConsoleProvider("Do -week Mo\t");
            var parsed = Args.ParseAction<ArgsWithActionsAndEnums>("$");
            Assert.AreEqual(DayOfWeek.Monday, parsed.Definition.SpecifiedAction.Arguments[0].RevivedValue);
        }

        [TestMethod]
        public void TestPassThrough()
        {
            ConsoleProvider.Current = new TestConsoleProvider("-s Adam -a 100");
            var parsed = Args.Parse<TestArgs>("$");
            Assert.AreEqual("Adam", parsed.SomeParam);
            Assert.AreEqual(100, parsed.AnotherParam);
        }

        [TestMethod]
        public void TestQuotesWithinArgs()
        {
            ConsoleProvider.Current = new TestConsoleProvider("-s \\\"Adam\\\" -a 100");
            var parsed = Args.Parse<TestArgs>("$");
            Assert.AreEqual("\"Adam\"", parsed.SomeParam);
            Assert.AreEqual(100, parsed.AnotherParam);
        }

        [TestMethod]
        public void TestProperParsingOfQuotedArgs()
        {
            TestConsoleProvider.SimulateConsoleInput("-s \"Adam Abdelhamed\" -a 100");
            var parsed = Args.Parse<TestArgs>("$");
            Assert.AreEqual("Adam Abdelhamed", parsed.SomeParam);
            Assert.AreEqual(100, parsed.AnotherParam);
        }

        [TestMethod]
        public void TestBasicCompletion()
        {
            TestConsoleProvider.SimulateConsoleInput("-som\t Adam -an\t 100");
            var parsed = Args.Parse<TestArgs>("$");
            Assert.AreEqual("Adam", parsed.SomeParam);
            Assert.AreEqual(100, parsed.AnotherParam);
        }

        [TestMethod]
        public void TestCustomCompletion()
        {
            TestConsoleProvider.SimulateConsoleInput("-som\t Abd\t");
            var parsed = Args.Parse<TestArgs>("$");
            Assert.AreEqual("Abdelhamed", parsed.SomeParam);
        }

        [TestMethod]
        public void TestBackspace()
        {
            TestConsoleProvider.SimulateConsoleInput("\b-sot\bmeparam Adam");
            var parsed = Args.Parse<TestArgs>("$");
            Assert.AreEqual("Adam", parsed.SomeParam);
        }

        [TestMethod]
        public void TestArrowAndDelete()
        {
            TestConsoleProvider.SimulateConsoleInput("-sot{left}{right}{left}{delete}meparam Adam");
            var parsed = Args.Parse<TestArgs>("$");
            Assert.AreEqual("Adam", parsed.SomeParam);
        }

        [TestMethod]
        public void TestHomeAndEnd()
        {
            TestConsoleProvider.SimulateConsoleInput("someparam Adam{home}-{end} -a 100");
            var parsed = Args.Parse<TestArgs>("$");
            Assert.AreEqual("Adam", parsed.SomeParam);
            Assert.AreEqual(100, parsed.AnotherParam);
        }

        [TestMethod]
        public void TestMultiTabCycling()
        {
            // First tab goes to 'anotherParam', second tab advances since the first tab was completed by empty string
            TestConsoleProvider.SimulateConsoleInput("\t 50 \t\t stringval");
            var parsed = Args.Parse<TestArgs>("$");
            Assert.AreEqual("stringval", parsed.SomeParam);
            Assert.AreEqual(50, parsed.AnotherParam);
        }

        [TestMethod]
        public void TestMultiTabCyclingBackwards()
        {
            TestConsoleProvider.SimulateConsoleInput("\t\t{shift}\t 50 \t\t stringval \t\t");
            var parsed = Args.Parse<TestArgs>("$");
            Assert.AreEqual("stringval", parsed.SomeParam);
            Assert.AreEqual(50, parsed.AnotherParam);
        }

        [TestMethod]
        public void TestMultiTabsFileSystem()
        {
            using (var temps = new TempFiles())
            {
                temps.CreateDirectory("DummyFolder", "1", "2", "3", "4");

                TestConsoleProvider.SimulateConsoleInput("-som\t Dumm\t\\\t\t\t");
                var parsed = Args.Parse<TestArgs>("$");
                Assert.AreEqual( Path.Combine(Environment.CurrentDirectory, "DummyFolder\\3"), parsed.SomeParam);
            }
        }

        [TestMethod]
        public void TestShiftTabFileSystem()
        {
            using (var temps = new TempFiles())
            {
                temps.CreateDirectory("DummyFolder", "1", "2", "3", "4");

                TestConsoleProvider.SimulateConsoleInput("-som\t Dumm\t\\\t\t\t{shift}\t{shift}\t");
                var parsed = Args.Parse<TestArgs>("$");
                Assert.AreEqual(Path.Combine(Environment.CurrentDirectory, "DummyFolder\\1"), parsed.SomeParam);
            }
        }

        [TestMethod]
        public void TestHistoryBasic()
        {
            ClearHistory();
            TestConsoleProvider.SimulateConsoleInput("-s historytest");
            var parsed = Args.Parse<TestArgs>("$");
            TestConsoleProvider.SimulateConsoleInput("{up}");
            parsed = Args.Parse<TestArgs>("$");
            Assert.AreEqual("historytest", parsed.SomeParam);
            ClearHistory();
        }

        [TestMethod]
        public void TestHistoryUpDown()
        {
            ClearHistory();
            TestConsoleProvider.SimulateConsoleInput("-s historytest1");
            var parsed = Args.Parse<TestArgs>("$");
            TestConsoleProvider.SimulateConsoleInput("-s historytest2");
            parsed = Args.Parse<TestArgs>("$");
            TestConsoleProvider.SimulateConsoleInput("{up}{up}{down}");
            parsed = Args.Parse<TestArgs>("$");
            Assert.AreEqual("historytest2", parsed.SomeParam);
            ClearHistory();
        }

        [TestMethod]
        public void TestHistoryCleanup()
        {
            ClearHistory();
            for (int i = 0; i < MaxHistory + 1; i++)
            {
                TestConsoleProvider.SimulateConsoleInput("-s historytest"+i);
                var parsed = Args.Parse<TestArgs>("$");
            }

            TestConsoleProvider.SimulateConsoleInput(Repeat("{up}", MaxHistory+1));
            var parsedAgain = Args.Parse<TestArgs>("$");
            Assert.AreEqual("historytest10", parsedAgain.SomeParam);
            ClearHistory();
        }

        [TestMethod]
        public void TestEnumCompletion()
        {
            TestConsoleProvider.SimulateConsoleInput("-e\t thr\t");
            var parsed = Args.Parse<TestArgsWithEnum>("$");
            Assert.AreEqual(EnumWithShortcuts.Three, parsed.Enum);
        }

        [TestMethod]
        public void TestEnumShortcutsCompletion()
        {
            TestConsoleProvider.SimulateConsoleInput("-e\t numb\t");
            var parsed = Args.Parse<TestArgsWithEnum>("$");
            Assert.AreEqual(EnumWithShortcuts.One, parsed.Enum);
        }

        [TabCompletion(typeof(MyLongCompletionSource), "$", ExeName = "TestSuiteTestArgs.exe", HistoryToSave = MaxHistory)]
        public class LongTestArgs
        {
            public string SomeParam { get; set; }
            public int AnotherParam { get; set; }
            public bool BoolParam { get; set; }
        }

        public class MyLongCompletionSource : SimpleTabCompletionSource
        {
            public MyLongCompletionSource() : base(MyLongCompletionSource.GetWords()) { }
            private static IEnumerable<string> GetWords()
            {
                return "AVeryLongWordSoThatTheBufferWidthOfTheConsoleWillBeExceededAndAnExceptionWouldBeRisedIfNoMeasureIsTakendddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd|Abdelhamed".Split('|');
            }
        }

        [TestMethod]
        public void TestREPL()
        {
            var provider = TestConsoleProvider.SimulateConsoleInput("-som\t Adam{enter}cls{enter}-some\t Abdelhamed{enter}quit");

            int clearCount = 0;
            provider.ClearHappened += () => { clearCount++; };

            Args.InvokeMain<TestArgsWithREPL>("$");
            Assert.AreEqual(2, TestArgsWithREPL.SomeParamValues.Count);
            Assert.AreEqual("Adam", TestArgsWithREPL.SomeParamValues[0]);
            Assert.AreEqual("Abdelhamed", TestArgsWithREPL.SomeParamValues[1]);
            Assert.AreEqual(1, clearCount);
        }

        [TestMethod]
        public void TestREPLExitsWhenIndicatorIsNotPresent()
        {

            var result = Args.InvokeMain<TestArgsWithREPL>("-someparam", "somevalue");
            Assert.AreEqual("somevalue", result.Args.SomeParam);
        }

        [TestMethod]
        public void TestModeledActionREPL()
        {
            int invokeCount = 0;

            CommandLineArgumentsDefinition definition = new CommandLineArgumentsDefinition();
            definition.Metadata.Add(new TabCompletion() { REPL = true, Indicator = "$" });

            var action = new CommandLineAction((d) =>
            {
                Assert.AreEqual("go", d.SpecifiedAction.DefaultAlias);
                if (invokeCount == 0)
                {
                    Assert.AreEqual("Hawaii", d.SpecifiedAction.Arguments[0].RevivedValue);
                }
                else if (invokeCount == 1)
                {
                    Assert.AreEqual("Mexico", d.SpecifiedAction.Arguments[0].RevivedValue);
                }
                invokeCount++;
            });

            action.Aliases.Add("go");
            action.Description = "A simple action";
 

            definition.Actions.Add(action);

            var destination = new CommandLineArgument(typeof(string), "destination");
            destination.Metadata.Add(new ArgRequired());
            destination.Description = "The place to go to";

            action.Arguments.Add(destination);

            var provider = TestConsoleProvider.SimulateConsoleInput("g\t -dest\t Hawaii{enter}go -dest\t Mexico{enter}quit");
            Args.InvokeAction(definition, "$");
            Assert.AreEqual(2, invokeCount);
        }


        [TestMethod]
        public void TestArgAwareCompletionPositive()
        {
            TestConsoleProvider.SimulateConsoleInput("-name a\t");
            var parsed = Args.Parse<ArgAwareCompletionArgs>("$");
            Assert.AreEqual("Adam", parsed.Name);
        }

        [TestMethod]
        public void TestArgAwareCompletionNegative()
        {
            TestConsoleProvider.SimulateConsoleInput("-address a\t");
            var parsed = Args.Parse<ArgAwareCompletionArgs>("$");
            Assert.AreNotEqual("Adam", parsed.Address);
        }

        [TabCompletion(REPL = true, Indicator="$")]
        public class ArgsWithRequiredAndREPL
        {
            [ArgRequired(PromptIfMissing = true)]
            public string SomeArg { get; set; }

            public bool ExpectMainToBeCalled { get; set; }

            public void Main()
            {
                if (ExpectMainToBeCalled == false)
                {
                    throw new Exception("Main called!");
                }
                else
                {
                    Console.WriteLine("Main called!");
                }
            }
        }

        public class ArgsWithRequiredAndNoREPL
        {
            [ArgRequired(PromptIfMissing = true)]
            public string SomeArg { get; set; }

            public bool ExpectMainToBeCalled { get; set; }

            public void Main()
            {
                if (ExpectMainToBeCalled == false)
                {
                    throw new Exception("Main called!");
                }
                else
                {
                    Console.WriteLine("Main called!");
                }
            }
        }

        [TabCompletion(REPL = true, Indicator = "$")]
        public class ArgsWithRequiredAndREPLAndConditional
        {
            [ArgRequired(PromptIfMissing = true, If = "ExpectMainToBeCalled")]
            public string SomeArg { get; set; }

            public bool ExpectMainToBeCalled { get; set; }

            public void Main()
            {
                if (ExpectMainToBeCalled == false)
                {
                    throw new Exception("Main called!");
                }
                else
                {
                    Console.WriteLine("Main called!");
                }
            }
        }

        public class ArgsWithRequiredAndNoREPLAndConditional
        {
            [ArgRequired(PromptIfMissing = true, If = "ExpectMainToBeCalled")]
            public string SomeArg { get; set; }

            public bool ExpectMainToBeCalled { get; set; }

            public void Main()
            {
                if (ExpectMainToBeCalled == false)
                {
                    throw new Exception("Main called!");
                }
                else
                {
                    Console.WriteLine("Main called!");
                }
            }
        }

        [TestMethod]
        public void TestArgRequiredCancelBehaviorWithREPL()
        {
            TestConsoleProvider.SimulateConsoleInput("-e{enter}{escape}-s\t thearg -e{enter}quit");
            var action = Args.InvokeMain<ArgsWithRequiredAndREPL>("$");
            Assert.AreEqual("thearg", action.Args.SomeArg);
        }

        [TestMethod]
        public void TestArgRequiredCancelBehaviorWithNoREPL()
        {
            try
            {
                TestConsoleProvider.SimulateConsoleInput("{escape}");
                var action = Args.InvokeMain<ArgsWithRequiredAndREPL>("");
                Assert.Fail("An exception should have been thrown");
            }
            catch(MissingArgException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("somearg"));
                Console.WriteLine("Yay!");
            }
        }

        [TestMethod]
        public void TestArgRequiredCancelBehaviorWithREPLAndConditional()
        {
            TestConsoleProvider.SimulateConsoleInput("-e{enter}{escape}-s\t thearg -e{enter}quit");
            var action = Args.InvokeMain<ArgsWithRequiredAndREPLAndConditional>("$");
            Assert.AreEqual("thearg", action.Args.SomeArg);
        }

        [TestMethod]
        public void TestArgRequiredCancelBehaviorWithNoREPLAndConditional()
        {
            try
            {
                TestConsoleProvider.SimulateConsoleInput("{escape}");
                var action = Args.InvokeMain<ArgsWithRequiredAndNoREPLAndConditional>("-e");
                Assert.Fail("An exception should have been thrown");
            }
            catch (MissingArgException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("somearg"));
                Console.WriteLine("Yay!");
            }
        }

        private string Repeat(string s, int num)
        {
            string ret = "";
            for (int i = 0; i < num; i++)
            {
                ret += s;
            }
            return ret;
        }

        private void ClearHistory()
        {
            (typeof(TestArgs).GetCustomAttributes(typeof(TabCompletion), true)[0] as TabCompletion).ClearHistory();
        }
    }

    public class TestConsoleProvider : PowerArgs.IConsoleProvider
    {
        public event Action<string> WriteHappened;
        public event Action ClearHappened;

        public ConsoleColor ForegroundColor { get; set; }

        public ConsoleColor BackgroundColor { get; set; }

        public static TestConsoleProvider SimulateConsoleInput(string input)
        {
            var simulator = new TestConsoleProvider(input);
            ConsoleProvider.Current = simulator;
            return simulator; 
        }

        string input;
        int i;
        public TestConsoleProvider(string input = "")
        {
            this.input = input;
            i = 0;
            BufferWidth = 80;
        }

        public void Append(string text)
        {
            input = input + text;
        }

        public void Clear()
        {
            if (ClearHappened != null) ClearHappened();
        }

        public int CursorLeft { get; set; }
        public int CursorTop { get; set; }
        public int BufferWidth { get; set; }

        bool shift = false;
        bool control = false;
        public ConsoleKeyInfo ReadKey()
        {
            if (i == input.Length) return new ConsoleKeyInfo((char)0, ConsoleKey.Enter, false, false, false);
            var c = input[i++];
            ConsoleKey key = ConsoleKey.NoName;

            if (c == '\b') key = ConsoleKey.Backspace;
            else if (c == ' ') key = ConsoleKey.Spacebar;
            else if (c == '\t') key = ConsoleKey.Tab;
            else if (c == '{' && ReadAheadLookFor("delete}")) key = ConsoleKey.Delete;
            else if (c == '{' && ReadAheadLookFor("home}")) key = ConsoleKey.Home;
            else if (c == '{' && ReadAheadLookFor("end}")) key = ConsoleKey.End;
            else if (c == '{' && ReadAheadLookFor("escape}")) key = ConsoleKey.Escape;
            else if (c == '{' && ReadAheadLookFor("left}")) key = ConsoleKey.LeftArrow;
            else if (c == '{' && ReadAheadLookFor("right}")) key = ConsoleKey.RightArrow;
            else if (c == '{' && ReadAheadLookFor("up}")) key = ConsoleKey.UpArrow;
            else if (c == '{' && ReadAheadLookFor("down}")) key = ConsoleKey.DownArrow;
            else if (c == '{' && ReadAheadLookFor("enter}")) key = ConsoleKey.Enter;
            else if (c == '{' && ReadAheadLookFor("wait}"))
            {
                Thread.Sleep(1000);
                return ReadKey();
            }
            else if (c == '{' && ReadAheadLookFor("w}"))
            {
                Thread.Sleep(100);
                return ReadKey();
            }
            else if (c == '{' && ReadAheadLookFor("shift}"))
            {
                shift = true;
                var ret = ReadKey();
                shift = false;
                return ret;
            }
            else if (c == '{' && ReadAheadLookFor("control}"))
            {
                control = true;
                var ret = ReadKey();
                control = false;
                return ret;
            }

            return new ConsoleKeyInfo(c, key, shift, false, control);
        }

        private bool ReadAheadLookFor(string toFind)
        {
            int k = 0;
            for (int j = i; j < i + toFind.Length; j++)
            {
                if (input[j] != toFind[k++])
                {
                    return false;
                }
            }
            i += toFind.Length;
            return true;
        }

        public void Write(object output)
        {
            string text = output == null ? "" : output.ToString();
            CursorLeft += text.Length;

            if (WriteHappened != null)
            {
                WriteHappened(text);
            }
        }
        public void WriteLine(object output) 
        {
            if(WriteHappened != null)
            {
                string text = output == null ? "" : output.ToString();
                WriteHappened(text);
            }

            CursorLeft = 0;
            CursorTop++;
        }
        public void WriteLine() 
        {
            if (WriteHappened != null)
            {
                WriteHappened(Environment.NewLine);
            }

            CursorLeft = 0;
            CursorTop++;
        }


        public int Read()
        {
            throw new NotImplementedException();
        }

        public ConsoleKeyInfo ReadKey(bool intercept)
        {
            return ReadKey();
        }

        public string ReadLine()
        {
            throw new NotImplementedException();
        }


        public void Write(ConsoleString consoleString)
        {
            Write(consoleString.ToString());
        }

        public void Write(ConsoleCharacter consoleCharacter)
        {
            Write(consoleCharacter.ToString());
        }

        public void WriteLine(ConsoleString consoleString)
        {
            WriteLine(consoleString.ToString());
        }
    }
}
