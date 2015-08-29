using System;
using System.Globalization;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Text.RegularExpressions;
using System.Net.Mail;
using System.Net;

namespace ArgsTests
{

    [TestClass]
    public class BasicTests
    {
        public enum BasicEnum
        {
            Option1,
            Option2,
            Option3
        }

        public enum EnumWithFlags
        {
            Zero = 0,
            One = 1,
            Two = 2,
            Four = 4,
            Eight = 8,
            Sixteen = 16,
        }

        [UsageAutomation]
        public class EnumArgs
        {
            [DefaultValue(BasicEnum.Option2)]
            public BasicEnum Option { get; set; }

            [ArgIgnoreCase]
            public BasicEnum Option2 { get; set; }
        }

        public class EnumArgs2
        {
            [ArgDefaultValue(BasicEnum.Option2)]
            public BasicEnum Option { get; set; }
        }

        [UsageAutomation]
        public class EnumArgsWithFlags
        {
            public EnumWithFlags Option { get; set; }
        }

        [UsageAutomation]
        public class EnumArgsExplicitIgnoreCase
        {
            [DefaultValue(BasicEnum.Option2)]
            [ArgIgnoreCase]
            public BasicEnum Option { get; set; }

        }

        [UsageAutomation]
        public class ArgsThatNeedManualReviverRegistration
        {
            public MailAddress Address { get; set; }

            [ArgReviver]
            public static MailAddress Revive(string key, string value)
            {
                return new MailAddress(value);
            }
        }

        public class Point
        {
            public int X { get; set; }
            public int Y { get; set; }
        }
        [UsageAutomation]
        [ArgExample("sometool -point 100,50", "Creates a new point with x = 100 and y = 50")]
        public class PointArgs
        {
            public Point Point { get; set; }

            [ArgReviver]
            public static Point Revive(string key, string val)
            {
                var match = Regex.Match(val, @"(\d*),(\d*)");
                if (match.Success == false)
                {
                    throw new ArgException("Not a valid point: " + val);
                }
                else
                {
                    Point ret = new Point();
                    ret.X = int.Parse(match.Groups[1].Value);
                    ret.Y = int.Parse(match.Groups[2].Value);
                    return ret;
                }
            }
        }

        public class BasicHook : ArgHook
        {
            public static bool WasRun { get; set; }

            public override void BeforeParse(ArgHook.HookContext context)
            {
                context.SetProperty("Year", 2013);
                context.SetProperty("Year", 2013);
                context.SetProperty("Name", "Adam");
                context.SetProperty("Name", "Adam");
            }

            public override void AfterPopulateProperties(ArgHook.HookContext context)
            {
                Assert.IsTrue(context.HasProperty("Year"));
                Assert.IsTrue(context.HasProperty("Name"));

                var year = context.GetProperty<int>("Year");
                var name = context.GetProperty<string>("Name");

                Assert.AreEqual(2013, year);
                Assert.AreEqual("Adam", name);

                context.ClearProperty("Year");
                context.SetProperty<string>("Name", null);

                Assert.IsFalse(context.HasProperty("Year"));
                Assert.IsFalse(context.HasProperty("Name"));

                Assert.IsNull(context.GetProperty<string>("Name"));

                try
                {
                    context.GetProperty<int>("Year");
                    Assert.Fail("An exception should have been thrown");
                }
                catch (KeyNotFoundException)
                {
                    // Throw for value types, return null for reference types
                }

                WasRun = true;
            }
        }

        [UsageAutomation]
        [BasicHook]
        public class BasicArgs
        {
            public string String { get; set; }
            public int Int { get; set; }
            public double Double { get; set; }
            public bool Bool { get; set; }

            public Guid Guid { get; set; }
            public DateTime Time { get; set; }
            public long Long { get; set; }
            [ArgShortcut(null)]
            public byte Byte { get; set; }

            public Uri Uri { get; set; }

            public IPAddress IPAddress { get; set; }

            [ArgShortcut("li")]
            public List<int> List { get; set; }

            [ArgShortcut("bytes")]
            public byte[] ArrayOfBytes { get; set; }

            [ArgIgnore]
            public object SomeObjectToIgnore { get; set; }
        }

        [UsageAutomation]
        public class BasicArgsSC
        {
            public string String { get; set; }
            public int Int { get; set; }
            public double Double { get; set; }
            public bool Bool { get; set; }

            public Guid Guid { get; set; }
            public DateTime Time { get; set; }
            public long Long { get; set; }
            [ArgShortcut(null)]
            public byte Byte { get; set; }

            [ArgShortcut("li")]
            public List<int> List { get; set; }

            [ArgShortcut("bytes")]
            public byte[] ArrayOfBytes { get; set; }

            [ArgIgnore]
            public object SomeObjectToIgnore { get; set; }
        }

        [UsageAutomation]
        public class PositionedArgs
        {
            [ArgPosition(0)]
            public string First { get; set; }

            [ArgPosition(1)]
            public string Second { get; set; }
        }

        [TestMethod]
        public void TestSlashColonStyle()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            var args = new string[] { "/string:stringValue", "/i:34", "/d:33.33", "/b" };

            var parsed = Args.Parse<BasicArgsSC>(args);

            Assert.AreEqual("stringValue", parsed.String);
            Assert.AreEqual(34, parsed.Int);
            Assert.AreEqual(33.33, parsed.Double);
            Assert.AreEqual(true, parsed.Bool);
        }

        // TODO - More tests around lists and arrays


        [TestMethod]
        public void TestManualReviverRegistration()
        {
            // A little bit of reflection magic to ensure that no other test has registered this reviver
            var reviverType = (from a in typeof(Args).Assembly.GetTypes() where a.Name == "ArgRevivers" select a).Single();
            var prop = reviverType.GetProperty("Revivers", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var reviverDictionary = prop.GetValue(null,null) as Dictionary<Type, Func<string, string, object>>;
            reviverDictionary.Remove(typeof(MailAddress));

            try
            {
                var parsed = Args.Parse<ArgsThatNeedManualReviverRegistration>("-a", "someone@somewhere.com");
                Assert.Fail("The first parse atttempt should have failed.");
            }
            catch (InvalidArgDefinitionException)
            {
                Args.SearchAssemblyForRevivers();
                var parsed = Args.Parse<ArgsThatNeedManualReviverRegistration>("-a", "someone@somewhere.com");
                Assert.AreEqual("someone@somewhere.com", parsed.Address.Address);
            }
        }

        [TestMethod]
        public void TestOverrideDefaultReviver()
        {
            var intReviver = ArgRevivers.GetReviver(typeof(int));
            try
            {
                ArgRevivers.SetReviver<int>((k, v) => { return -1; });

                var def = new CommandLineArgumentsDefinition();
                def.Arguments.Add(new CommandLineArgument(typeof(int), "theInt"));
                Args.Parse(def, "-theInt", "100");
                Assert.AreEqual(-1, def.FindMatchingArgument("theInt").RevivedValue);
            }
            finally
            {
                ArgRevivers.SetReviver(typeof(int), intReviver);
            }

            // make sure the old reviver is working again
            var newDef = new CommandLineArgumentsDefinition();
            newDef.Arguments.Add(new CommandLineArgument(typeof(int), "theInt"));
            Args.Parse(newDef, "-theInt", "100");
            Assert.AreEqual(100, newDef.FindMatchingArgument("theInt").RevivedValue);
        }

        [TestMethod]
        public void TestPowerShellStyle()
        {
            BasicHook.WasRun = false;
            Guid g = Guid.NewGuid();
            DateTime d = DateTime.Today;
            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            var args = new string[] { "-String", "stringValue", "-i", "34", "-d", "33.33", "-b", "-byte", "255", "-g", g.ToString(), "-t", d.ToString(), "-l", long.MaxValue+"", "-li", "100,200,300", "-bytes", "10,20,30", "-uri", "http://www.bing.com", "-ipaddress", IPAddress.Loopback.ToString() };
            BasicArgs parsed = Args.Parse<BasicArgs>(args);
            Assert.AreEqual("stringValue", parsed.String);
            Assert.AreEqual(34, parsed.Int);
            Assert.AreEqual(33.33, parsed.Double);
            Assert.AreEqual(true, parsed.Bool);
            Assert.AreEqual(255, parsed.Byte);
            Assert.AreEqual(g, parsed.Guid);
            Assert.AreEqual(d, parsed.Time);
            Assert.AreEqual(long.MaxValue, parsed.Long);
            Assert.AreEqual(3, parsed.List.Count);
            Assert.AreEqual(100, parsed.List[0]);
            Assert.AreEqual(200, parsed.List[1]);
            Assert.AreEqual(300, parsed.List[2]);
            Assert.AreEqual(3, parsed.ArrayOfBytes.Length);
            Assert.AreEqual(10, parsed.ArrayOfBytes[0]);
            Assert.AreEqual(20, parsed.ArrayOfBytes[1]);
            Assert.AreEqual(30, parsed.ArrayOfBytes[2]);
            Assert.AreEqual(new Uri("http://www.bing.com"), parsed.Uri);
            Assert.AreEqual(IPAddress.Loopback, parsed.IPAddress);
            Assert.IsTrue(BasicHook.WasRun);
        }

        [TestMethod]
        public void TestPowerShellStyleWeak()
        {
            BasicHook.WasRun = false;
            Guid g = Guid.NewGuid();
            DateTime d = DateTime.Today;
            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            var args = new string[] { "-String", "stringValue", "-i", "34", "-d", "33.33", "-b", "-byte", "255", "-g", g.ToString(), "-t", d.ToString(), "-l", long.MaxValue + "", "-li", "100,200,300", "-bytes", "10,20,30", "-uri", "http://www.bing.com", "-ipaddress", IPAddress.Loopback.ToString() };

            BasicArgs parsed = (BasicArgs)Args.Parse(typeof(BasicArgs), args);

            Assert.AreEqual("stringValue", parsed.String);
            Assert.AreEqual(34, parsed.Int);
            Assert.AreEqual(33.33, parsed.Double);
            Assert.AreEqual(true, parsed.Bool);
            Assert.AreEqual(255, parsed.Byte);
            Assert.AreEqual(g, parsed.Guid);
            Assert.AreEqual(d, parsed.Time);
            Assert.AreEqual(long.MaxValue, parsed.Long);
            Assert.AreEqual(3, parsed.List.Count);
            Assert.AreEqual(100, parsed.List[0]);
            Assert.AreEqual(200, parsed.List[1]);
            Assert.AreEqual(300, parsed.List[2]);
            Assert.AreEqual(3, parsed.ArrayOfBytes.Length);
            Assert.AreEqual(10, parsed.ArrayOfBytes[0]);
            Assert.AreEqual(20, parsed.ArrayOfBytes[1]);
            Assert.AreEqual(30, parsed.ArrayOfBytes[2]);
            Assert.AreEqual(new Uri("http://www.bing.com"), parsed.Uri);
            Assert.AreEqual(IPAddress.Loopback, parsed.IPAddress);
            Assert.IsTrue(BasicHook.WasRun);
        }

        [TestMethod]
        public void TestSingleElementInArray()
        {
            var args = new string[] { "-bytes", "10" };

            BasicArgs parsed = Args.Parse<BasicArgs>(args);
            Assert.AreEqual(1, parsed.ArrayOfBytes.Length);
            Assert.AreEqual(10, parsed.ArrayOfBytes[0]);
        }

        [TestMethod]
        public void TestSingleElementInList()
        {
            var args = new string[] { "-li", "10" };

            BasicArgs parsed = Args.Parse<BasicArgs>(args);
            Assert.AreEqual(1, parsed.List.Count);
            Assert.AreEqual(10, parsed.List[0]);
        }

        [TestMethod]
        public void TestPositionArgsPS()
        {
            var args = new string[] { "value1", "value2" };

            PositionedArgs parsed = Args.Parse<PositionedArgs>(args);

            Assert.AreEqual("value1", parsed.First);
            Assert.AreEqual("value2", parsed.Second);

        }

        [TestMethod]
        public void TestPositionArgsSC()
        {
            var args = new string[] { "value1", "value2" };

            PositionedArgs parsed = Args.Parse<PositionedArgs>(args);

            Assert.AreEqual("value1", parsed.First);
            Assert.AreEqual("value2", parsed.Second);
        }

        [TestMethod]
        public void TestPositionArgsMixed()
        {
            var args = new string[] { "value1", "-Second", "value2" };

            PositionedArgs parsed = Args.Parse<PositionedArgs>(args);

            Assert.AreEqual("value1", parsed.First);
            Assert.AreEqual("value2", parsed.Second);

        }

        [TestMethod]
        public void TestCustomReviver()
        {
            var args = new string[] { "-point", "50,25" };

            var parsed = Args.Parse<PointArgs>(args);

            Assert.AreEqual(50, parsed.Point.X);
            Assert.AreEqual(25, parsed.Point.Y);

        }

        [TestMethod]
        public void TestEnum()
        {
            var args = new string[] { "-option", "Option3" };

            var parsed = Args.Parse<EnumArgs>(args);
            Assert.AreEqual(BasicEnum.Option3, parsed.Option);

            parsed = Args.Parse<EnumArgs>(new string[] { }); // Test the default value
            Assert.AreEqual(BasicEnum.Option2, parsed.Option);
        }

        [TestMethod]
        public void TestArgDefaultValue()
        {
            var args = new string[] { };

            var parsed = Args.Parse<EnumArgs2>(args);
            Assert.AreEqual(BasicEnum.Option2, parsed.Option);
        }


        [TestMethod]
        public void TestEnumWithFlags()
        {
            var args = new string[] { "-o", "Zero,One,Two" };

            var parsed = Args.Parse<EnumArgsWithFlags>(args);
            Assert.AreEqual(EnumWithFlags.Zero | EnumWithFlags.One | EnumWithFlags.Two, parsed.Option);
        }

        [TestMethod]
        public void TestEnumWithFlagValues()
        {
            var args = new string[] { "-o", "0,1,2" };

            var parsed = Args.Parse<EnumArgsWithFlags>(args);
            Assert.AreEqual(EnumWithFlags.Zero | EnumWithFlags.One | EnumWithFlags.Two, parsed.Option);
        }

        [TestMethod]
        public void TestEnumWithValues()
        {
            var args = new string[] { "-o", "2" };

            var parsed = Args.Parse<EnumArgsWithFlags>(args);
            Assert.AreEqual(EnumWithFlags.Two, parsed.Option);
        }

        [TestMethod]
        public void TestEnumWithValuesPreAggregated()
        {
            var args = new string[] { "-o", "3" };

            var parsed = Args.Parse<EnumArgsWithFlags>(args);
            Assert.AreEqual(EnumWithFlags.One | EnumWithFlags.Two, parsed.Option);
        }


        [TestMethod]
        public void ArgReviversReviveEnumThrowsOnInvalidValue()
        {
            var args = new string[] { "-option", "NonExistentOption" };

            try
            {
                Args.Parse<EnumArgs>(args);
                Assert.Fail("Should have thrown an exception");
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(ValidationArgException));
                Assert.AreEqual("NonExistentOption is not a valid value for type BasicEnum, options are Option1, Option2, Option3", ex.Message);
            }
        }

        [TestMethod]
        public void ArgReviversReviveEnumThrowsOnInvalidValueInFlagList()
        {
            var args = new string[] { "-o", "One,NonExistentOption, Two" };

            try
            {
                Args.Parse<EnumArgsWithFlags>(args);
                Assert.Fail("Should have thrown an exception");
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(ValidationArgException));
                Assert.AreEqual("NonExistentOption is not a valid value for type EnumWithFlags, options are Zero, One, Two, Four, Eight, Sixteen", ex.Message);
            }
        }

        [TestMethod]
        public void TestEnumCaseSensitivity()
        {
            var args = new string[] { "-o", "option3" };

            var parsed = Args.Parse<EnumArgs>(args);
            Assert.AreEqual(BasicEnum.Option3, parsed.Option);
            Args.Parse<EnumArgsExplicitIgnoreCase>(args);
            Assert.AreEqual(BasicEnum.Option3, parsed.Option);
        }

        [TestMethod]
        public void TestSwitch()
        {
            var args = new string[] { "-bool", "-string", "string" };

            var parsed = Args.Parse<BasicArgs>(args);
            Assert.AreEqual(true, parsed.Bool);
            Assert.AreEqual("string", parsed.String);
        }

        [TestMethod]
        public void TestSwitchWithFalseValueSpecified()
        {
            var args = new string[] { "-bool", "false", "-string", "string" };

            var parsed = Args.Parse<BasicArgs>(args);
            Assert.AreEqual(false, parsed.Bool);
            Assert.AreEqual("string", parsed.String);
        }

        [TestMethod]
        public void TestSwitchWithZeroValueSpecified()
        {
            var args = new string[] { "-bool", "0", "-string", "string" };

            var parsed = Args.Parse<BasicArgs>(args);
            Assert.AreEqual(false, parsed.Bool);
            Assert.AreEqual("string", parsed.String);
        }

        [TestMethod]
        public void TestSwitchWithTrueValueSpecified()
        {
            var args = new string[] { "-bool", "true", "-string", "string" };

            var parsed = Args.Parse<BasicArgs>(args);
            Assert.AreEqual(true, parsed.Bool);
            Assert.AreEqual("string", parsed.String);
        }

        [TestMethod]
        public void TestRevivalFailure()
        {
            var args = new string[] { "-int", "notAnInt" };

            try
            {
                var parsed = Args.Parse<BasicArgs>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (ArgException ex)
            {
            }
        }

        [TestMethod]
        public void ArgsThrowsOnUnexpectedNamedArgument()
        {
            var args = new string[] { "-bool", "-string", "string", "-extraArg", "extraValue" };

            try
            {
                var parsed = Args.Parse<BasicArgs>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (Exception ex)
            {                
                Assert.IsInstanceOfType(ex, typeof(UnexpectedArgException));
                Assert.AreEqual("Unexpected named argument: extraArg", ex.Message);
            }
        }


        [TestMethod]
        public void ArgParserThrowsOnUnexpectedArgument()
        {
            var args = new string[] { "-bool", "-string", "string", "extraValue" };

            try
            {
                var parsed = Args.Parse<BasicArgs>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(UnexpectedArgException));
                Assert.AreEqual("Unexpected argument: extraValue", ex.Message);
            }
        }

        [TestMethod]
        public void ArgsThrowsOnUnexpectedPositionalArgument()
        {
            var args = new string[] { "A", "B", "extraarg", };

            try
            {
                var parsed = Args.Parse<PositionedArgs>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(UnexpectedArgException));
                Assert.AreEqual("Unexpected unnamed argument: extraarg", ex.Message);
            }
        }

        [TestMethod]
        public void ArgParserThrowsOnDuplicateNamedArguments()
        {
            var args = new string[] { "-string", "string", "-string", "specifiedTwice" };

            try
            {
                var parsed = Args.Parse<BasicArgs>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(DuplicateArgException));
                Assert.AreEqual("Argument specified more than once: string", ex.Message);
            }
        }

        [TestMethod]
        public void ArgsThrowsOnDuplicateMixedCaseNamedArguments()
        {
            var args = new string[] { "-string", "string", "-String", "specifiedTwice" };

            try
            {
                var parsed = Args.Parse<BasicArgs>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(DuplicateArgException));
                Assert.AreEqual("Argument specified more than once: String", ex.Message);
            }
        }

        [TestMethod]
        public void ArgParserThrowsOnDuplicateArgumentUsingSlashColonFormat()
        {
            var args = new string[] { "/string:string", "/string:specifiedTwice" };

            try
            {
                var parsed = Args.Parse<BasicArgs>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(DuplicateArgException));
                Assert.AreEqual("Argument specified more than once: string", ex.Message);
            }
        }

        [TestMethod]
        public void TestBasicUsage()
        {
            var basicUsage = ArgUsage.GetUsage<BasicArgs>("basic");
            ArgUsage.GetUsage<PointArgs>("basic");
            ArgUsage.GetStyledUsage<PointArgs>("basic").Write();
            Console.WriteLine(basicUsage);
        }

        [TestMethod]
        public void TestBasicUsageWithPositioning()
        {
            var basicUsage = ArgUsage.GetUsage<PositionedArgs>( "basic");
            ArgUsage.GetStyledUsage<PositionedArgs>("basic").Write();
            Console.WriteLine(basicUsage);
        }

        [TestMethod]
        public void TestBasicUsageWithNoExeNameThrows()
        {
            try 
            {
                var basicUsage = ArgUsage.GetUsage<BasicArgs>();
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(InvalidOperationException));
                Assert.IsTrue(ex.Message.ToLower().Contains("could not determine the name of your executable automatically"));
            }
        }

        [TestMethod]
        public void UnexpectedArgumentExceptionInheritsFromArgException()
        {
            var uae = new UnexpectedArgException("test");
            Assert.IsInstanceOfType(uae, typeof(ArgException));
        }

        [TestMethod]
        public void TestArgWithValueThatStartsWithIndicator()
        {
            var parsed = Args.Parse<BasicArgs>("-String", "/Root");
            Assert.AreEqual("/Root", parsed.String);
        }

        [TestMethod]
        public void TestConvert1()
        {
            AssertConversion("");
        }

        [TestMethod]
        public void TestConvert2()
        {
            AssertConversion("John", "John");
        }

        [TestMethod]
        public void TestConvert3()
        {
            AssertConversion("John Doe", "John", "Doe");
        }

        [TestMethod]
        public void TestConvert4()
        {
            AssertConversion("John A Doe", "John", "A", "Doe");
        }

        [TestMethod]
        public void TestConvert5()
        {
            AssertConversion("\"John A Doe\"", "John A Doe");
        }

        [TestMethod]
        public void TestConvert6()
        {
            AssertConversion("\"John A Doe\" -foo bar", "John A Doe", "-foo", "bar");
        }

        [TestMethod]
        public void TestConvert7()
        {
            AssertConversion("Foo \"John A Doe\" -foo bar","Foo", "John A Doe", "-foo", "bar");
        }

        [TestMethod]
        public void TestConvert8()
        {
            AssertConversion("\"1\" \"\" Foo \"John A Doe\" -foo         bar","1", "", "Foo", "John A Doe", "-foo", "bar");
        }

        [TestMethod]
        public void TestInitializeDefaults()
        {
            var args = new EnumArgs();
            Assert.AreNotEqual(BasicEnum.Option2, args.Option);
            Assert.AreEqual(default(BasicEnum), args.Option2);
            Args.InitializeDefaults(args);
            Assert.AreEqual(BasicEnum.Option2, args.Option);
            Assert.AreEqual(default(BasicEnum), args.Option2);
        }

        private void AssertConversion(string commandLine, params string[] expectedResult)
        {
            var result = Args.Convert(commandLine);

            Assert.AreEqual(expectedResult.Length, result.Length);

            for (int i = 0; i < expectedResult.Length; i++)
            {
                Assert.AreEqual(expectedResult[i], result[i]);
            }
        }
    }
}
