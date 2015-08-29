using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.IO;
using PowerArgs.Cli;

namespace ArgsTests
{
    public class AnimalCompletionSource : ISmartTabCompletionSource
    {
        private SimpleTabCompletionSource source = new SimpleTabCompletionSource(new string[] { "Chicken", "Cow" }) { MinCharsBeforeCyclingBegins = 0 };
        public bool TryComplete(TabCompletionContext context, out string completion)
        {
            return source.TryComplete(context, out completion);
        }
    }

    [TestClass]
    public class NewAwesomeTabCompletionTests
    {
        [TabCompletion("$", ExeName = "TestSuiteTestArgs.exe")]
        public class ConflictingArgumentsThatAwesomeTabCompletionMakesBetter
        {
            // this is just here to make sure that the tab after 'fruits' is smart enough to prefer '-apples' to '-applesglobal'
            public string ApplesGlobal { get; set; }

            public bool apples;
            public bool bananas;

            public bool asparagus;
            public bool beets;

            public string animal;

            [ArgActionMethod]
            public void Fruits(bool apples, bool bananas)
            {
                this.apples = apples;
                this.bananas = bananas;
            }

            [ArgActionMethod]
            public void Vegetables(bool asparagus, bool beets)
            {
                this.asparagus = asparagus;
                this.beets = beets;
            }

            [ArgActionMethod]
            public void Meats([ArgumentAwareTabCompletionAttribute(typeof(AnimalCompletionSource))]string animal)
            {
                this.animal = animal;
            }
        }

        [TabCompletion(typeof(AnimalCompletionSource), "$")]
        public class TopLevelSmartTabCompletionArgs
        {
            [ArgPosition(0)]
            public string Animal { get; set; }
        }

        [TabCompletion(typeof(string), "$")] // string is not a valid completion source type
        public class GarbageTabCompletionTypeArgs
        {
            public string Animal { get; set; }
        }

        [TestMethod]
        public void TestTopLevelSmartTabCompletionSource()
        {
            ConsoleProvider.Current = new TestConsoleProvider("ch\t");
            var parsed = Args.Parse<TopLevelSmartTabCompletionArgs>("$");
            Assert.AreEqual("Chicken", parsed.Animal);
        }

        [TestMethod]
        public void TestGarbageTopLevelSmartTabCompletionSource()
        {
            try
            {
                ConsoleProvider.Current = new TestConsoleProvider("ch\t");
                Args.Parse<GarbageTabCompletionTypeArgs>("$");
                Assert.Fail("An exception should have been thrown");
            }
            catch(InvalidArgDefinitionException ex)
            {
            }
        }

        [TestMethod]
        public void TestAwesomeTabCompletionKnowsWhichActionIAmPerforming()
        {
            ConsoleProvider.Current = new TestConsoleProvider("f\t \t \t"); // should expand to 'fruits -apples -bananas'
            var parsed = Args.InvokeAction<ConflictingArgumentsThatAwesomeTabCompletionMakesBetter>("$");
            
            Assert.IsTrue(parsed.Args.apples);
            Assert.IsTrue(parsed.Args.bananas);
            Assert.IsFalse(parsed.Args.asparagus);
            Assert.IsFalse(parsed.Args.beets);

            ConsoleProvider.Current = new TestConsoleProvider("v\t \t \t"); // should expand to 'vegetables -asparagus -beets'
            parsed = Args.InvokeAction<ConflictingArgumentsThatAwesomeTabCompletionMakesBetter>("$");
            Assert.IsFalse(parsed.Args.apples);
            Assert.IsFalse(parsed.Args.bananas);
            Assert.IsTrue(parsed.Args.asparagus);
            Assert.IsTrue(parsed.Args.beets);
        }

        [TestMethod]
        public void TestArgumentAwareSmartTabCompletion()
        {
            var input = "m\t -a\t \t"; // should expand to 'meats -animal Chicken;
            ConsoleProvider.Current = new TestConsoleProvider(input);
            var definition = new CommandLineArgumentsDefinition(typeof(ConflictingArgumentsThatAwesomeTabCompletionMakesBetter));

            PowerArgsRichCommandLineReader reader = new PowerArgsRichCommandLineReader(definition, new List<ConsoleString>());
            reader.ThrowOnSyntaxHighlightException = true;
            reader.TabHandler.ThrowOnTabCompletionHandlerException = true;
            var completed = string.Join(" ", reader.ReadCommandLine());
            Assert.AreEqual("meats -animal Chicken", completed);
             
        }
    }
}
