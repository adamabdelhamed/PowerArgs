using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.IO;

namespace ArgsTests
{
    [TestClass]
    public class TabCompletionTests
    {
        const int MaxHistory = 10;

        [TabCompletion(typeof(MyCompletionSource), "$", ExeName = "TestSuiteTestArgs.exe", HistoryToSave = MaxHistory)]
        public class TestArgs
        {
            public string SomeParam { get; set; }
            public int AnotherParam { get; set; }
        }

        public class MyCompletionSource : SimpleTabCompletionSource
        {
            public MyCompletionSource() : base(MyCompletionSource.GetWords()) {}
            private static IEnumerable<string> GetWords()
            {
                return "Adam|Abdelhamed".Split('|');
            }
        }

        [TestMethod]
        public void TestPassThrough()
        {
            ConsoleHelper.ConsoleImpl = new TestConsoleProvider("-s Adam -a 100");
            var parsed = Args.Parse<TestArgs>("$");
            Assert.AreEqual("Adam", parsed.SomeParam);
            Assert.AreEqual(100, parsed.AnotherParam);
        }

        [TestMethod]
        public void TestQuotesWithinArgs()
        {
            ConsoleHelper.ConsoleImpl = new TestConsoleProvider("-s \\\"Adam\\\" -a 100");
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
        public void TestMultiTabs()
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
        public void TestShiftTab()
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

    public class TestConsoleProvider : PowerArgs.ConsoleHelper.IConsoleProvider
    {
        public static TestConsoleProvider SimulateConsoleInput(string input)
        {
            var simulator = new TestConsoleProvider(input);
            ConsoleHelper.ConsoleImpl = simulator;
            return simulator; 
        }

        string input;
        int i;
        public TestConsoleProvider(string input)
        {
            this.input = input;
            i = 0;
        }

        public void Append(string text)
        {
            input = input + text;
        }

        public int CursorLeft { get; set; }

        bool shift = false;
        public ConsoleKeyInfo ReadKey()
        {
            if (i == input.Length) return new ConsoleKeyInfo((char)0, ConsoleKey.Enter, false, false, false);
            var c = input[i++];
            ConsoleKey key = ConsoleKey.NoName;

            if (c == '\b') key = ConsoleKey.Backspace;
            else if (c == '\t') key = ConsoleKey.Tab;
            else if (c == '{' && ReadAheadLookFor("delete}")) key = ConsoleKey.Delete;
            else if (c == '{' && ReadAheadLookFor("home}")) key = ConsoleKey.Home;
            else if (c == '{' && ReadAheadLookFor("end}")) key = ConsoleKey.End;
            else if (c == '{' && ReadAheadLookFor("left}")) key = ConsoleKey.LeftArrow;
            else if (c == '{' && ReadAheadLookFor("right}")) key = ConsoleKey.RightArrow;
            else if (c == '{' && ReadAheadLookFor("up}")) key = ConsoleKey.UpArrow;
            else if (c == '{' && ReadAheadLookFor("down}")) key = ConsoleKey.DownArrow;
            else if (c == '{' && ReadAheadLookFor("shift}"))
            {
                shift = true;
                var ret = ReadKey();
                shift = false;
                return ret;
            }

            return new ConsoleKeyInfo(c, key, shift, false, false);
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
            var str = output.ToString();
            CursorLeft += str.Length;
        }
        public void WriteLine(object output) { }
        public void WriteLine() { }
    }
}
