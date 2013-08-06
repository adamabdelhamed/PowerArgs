using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Collections.Generic;
using System.Linq;

namespace ArgsTests
{
    [TestClass]
    public class LongFormTests
    {
        public class LongFormArgs
        {
            [ArgLongForm("--your-age")]
            public int Age { get; set; }

            public string NotUsed { get; set; }
        }

        public class LongFormArgs2
        {
            [ArgLongForm("--your-age")]
            [ArgLongForm("--your-real-age")]
            public int Age { get; set; }

            public string NotUsed { get; set; }
        }

        public class LongFormArgsMixed
        {
            [ArgShortcut("ag")]
            [ArgLongForm("--your-age")]
            [ArgLongForm("--your-real-age")]
            public int Age { get; set; }

            [ArgLongForm("--help")]
            [ArgLongForm("--help-please")]
            public bool Help { get; set; }
        }

        public class LongFormBad
        {
            [ArgLongForm(null)]
            public int Age { get; set; }
        }

        [TestMethod]
        public void TestLongFormAppearsInUsage()
        {
            string usage = ArgUsage.GetUsage<LongFormArgs>("test");
            Assert.IsTrue(usage.Contains("--your-age"));

            usage = ArgUsage.GetUsage<LongFormArgsMixed>("test");
            Assert.IsTrue(usage.Contains("--your-age"));
            Assert.IsTrue(usage.Contains("--your-real-age"));
        }

        [TestMethod]
        public void TestLongFormBasic()
        {
            var args = Args.Parse<LongFormArgs>("--your-age", "100");
            Assert.AreEqual(100, args.Age);

            var args2 = Args.Parse<LongFormArgs>("--your-age=100");
            Assert.AreEqual(100, args2.Age);

            var definition = new CommandLineArgumentsDefinition(typeof(LongFormArgs));

            var aliases = definition.Arguments.Where(a => a.DefaultAlias == "Age").Single().Aliases;
            Assert.AreEqual(2, aliases.Count);
            Assert.AreEqual("-your-age", aliases[1]);
        }

        [TestMethod]
        public void TestLongFormMultiple()
        {
            var args = Args.Parse<LongFormArgs2>("--your-age", "100");
            Assert.AreEqual(100, args.Age);

            var args2 = Args.Parse<LongFormArgs2>("--your-real-age", "200");
            Assert.AreEqual(200, args2.Age);
        }

        [TestMethod]
        public void TestLongFormMixed()
        {
            var args = Args.Parse<LongFormArgsMixed>("-ag", "100");
            Assert.AreEqual(100, args.Age);

            args = Args.Parse<LongFormArgsMixed>("-age", "101");
            Assert.AreEqual(101, args.Age);

            args = Args.Parse<LongFormArgsMixed>("--your-age", "102");
            Assert.AreEqual(102, args.Age);

            args = Args.Parse<LongFormArgsMixed>("--your-real-age", "103");
            Assert.AreEqual(103, args.Age);
        }

        [TestMethod]
        public void TestLongFormBad()
        {
            try
            {
                Args.Parse<LongFormBad>("-a", "100");
                Assert.Fail("An exception should have been thrown");
            }
            catch (UnexpectedArgException ex)
            {
                
            }
        }
    }
}
