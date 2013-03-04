using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Text.RegularExpressions;

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

        public class EnumArgs
        {
            [DefaultValue(BasicEnum.Option2)]
            public BasicEnum Option { get; set; }
        }

        public class Point
        {
            public int X { get; set; }
            public int Y { get; set; }
        }

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

            [ArgShortcut("li")]
            public List<int> List { get; set; }

            [ArgShortcut("bytes")]
            public byte[] ArrayOfBytes { get; set; }

            [ArgIgnore]
            public object SomeObjectToIgnore { get; set; }
        }

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
            var args = new string[] { "/string:stringValue", "/i:34", "/d:33.33", "/b" };

            var parsed = Args.Parse<BasicArgsSC>(args);

            Assert.AreEqual("stringValue", parsed.String);
            Assert.AreEqual(34, parsed.Int);
            Assert.AreEqual(33.33, parsed.Double);
            Assert.AreEqual(true, parsed.Bool);
        }

        [TestMethod]
        public void TestPowerShellStyle()
        {
            Guid g = Guid.NewGuid();
            DateTime d = DateTime.Today;

            var args = new string[] { "-String", "stringValue", "-i", "34", "-d", "33.33", "-b", "-byte", "255", "-g", g.ToString(), "-t", d.ToString(), "-l", long.MaxValue+"", "-li", "100,200,300", "-bytes", "10,20,30"  };

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
        public void TestExtraArgs()
        {
            var args = new string[] { "-bool", "-string", "string", "-extraArg", "extraValue" };

            try
            {
                var parsed = Args.Parse<BasicArgs>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (ArgException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("unexpected") && ex.Message.ToLower().Contains("extraarg"));
            }
        }

        // TODO - Fix known issue with extra args and then this test will pass
        // [TestMethod]
        public void TestExtraArgs2()
        {
            var args = new string[] { "-bool", "-string", "string", "extraValue" };

            try
            {
                var parsed = Args.Parse<BasicArgs>(args);
                Assert.Fail("An exception should have been thrown");
            }
            catch (ArgException ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("unexpected") && ex.Message.ToLower().Contains("extravalue"));
            }
        }

        [TestMethod]
        public void TestBasicUsage()
        {
            var basicUsage = ArgUsage.GetUsage<BasicArgs>("basic");
            ArgUsage.GetUsage<PointArgs>("basic");
            Console.WriteLine(basicUsage);
        }

        [TestMethod]
        public void TestBasicUsageWithPositioning()
        {
            var basicUsage = ArgUsage.GetUsage<PositionedArgs>( "basic");
            Console.WriteLine(basicUsage);
        }
    }
}
