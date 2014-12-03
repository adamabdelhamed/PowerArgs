using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.IO;

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

        [TestMethod]
        public void TestAwesomeTabCompletionKnowsWhichActionIAmPerforming()
        {
            ConsoleHelper.ConsoleImpl = new TestConsoleProvider("f\t \t \t"); // should expand to 'fruits -apples -bananas'
            var parsed = Args.InvokeAction<ConflictingArgumentsThatAwesomeTabCompletionMakesBetter>("$");
            
            Assert.IsTrue(parsed.Args.apples);
            Assert.IsTrue(parsed.Args.bananas);
            Assert.IsFalse(parsed.Args.asparagus);
            Assert.IsFalse(parsed.Args.beets);

            ConsoleHelper.ConsoleImpl = new TestConsoleProvider("v\t \t \t"); // should expand to 'vegetables -asparagus -beets'
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
            ConsoleHelper.ConsoleImpl = new TestConsoleProvider(input);
            var definition = new CommandLineArgumentsDefinition(typeof(ConflictingArgumentsThatAwesomeTabCompletionMakesBetter));

            var completed = string.Join(" ", ConsoleHelper.ReadLine(ref input, new List<string>(), definition));
            Assert.AreEqual("meats -animal Chicken", completed);
             
        }
    }
}
