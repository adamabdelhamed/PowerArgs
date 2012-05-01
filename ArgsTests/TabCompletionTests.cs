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

        [TabCompletion("$")]
        public class TestArgs
        {
            public string SomeParam { get; set; }
            public int AnotherParam { get; set; }
        }

        public class TestConsoleProvider : PowerArgs.ConsoleHelper.IConsoleProvider
        {
            string input;
            int i;
            public TestConsoleProvider(string input)
            {
                this.input = input;
                i = 0;
            }

            public int CursorLeft { get; set; }

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
                return new ConsoleKeyInfo(c, key, false, false, false);
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
            public void WriteLine(object output) {}
            public void WriteLine() {}
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
        public void TestBasicCompletion()
        {
            ConsoleHelper.ConsoleImpl = new TestConsoleProvider("-som\t Adam -an\t 100");
            var parsed = Args.Parse<TestArgs>("$");
            Assert.AreEqual("Adam", parsed.SomeParam);
            Assert.AreEqual(100, parsed.AnotherParam);
        }

        [TestMethod]
        public void TestBackspace()
        {
            ConsoleHelper.ConsoleImpl = new TestConsoleProvider("\b\t-sot\bmeparam Adam");
            var parsed = Args.Parse<TestArgs>("$");
            Assert.AreEqual("Adam", parsed.SomeParam);
        }

        [TestMethod]
        public void TestArrowAndDelete()
        {
            ConsoleHelper.ConsoleImpl = new TestConsoleProvider("-sot{left}{right}{left}{delete}meparam Adam");
            var parsed = Args.Parse<TestArgs>("$");
            Assert.AreEqual("Adam", parsed.SomeParam);
        }

        [TestMethod]
        public void TestHomeAndEnd()
        {
            ConsoleHelper.ConsoleImpl = new TestConsoleProvider("someparam Adam{home}-{end} -a 100");
            var parsed = Args.Parse<TestArgs>("$");
            Assert.AreEqual("Adam", parsed.SomeParam);
            Assert.AreEqual(100, parsed.AnotherParam);
        }

        private string Keys(string s, int num)
        {
            string ret = "";
            for (int i = 0; i < num; i++)
            {
                ret += s;
            }
            return ret;
        }
    }
}
