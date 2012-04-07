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

            BasicArgs parsed = Args.Parse<BasicArgs>(args, ArgStyle.SlashColon);

            Assert.AreEqual("stringValue", parsed.String);
            Assert.AreEqual(34, parsed.Int);
            Assert.AreEqual(33.33, parsed.Double);
            Assert.AreEqual(true, parsed.Bool);
        }

        [TestMethod]
        public void TestPowerShellStyle()
        {
            var args = new string[] { "-String", "stringValue", "-i", "34", "-d", "33.33", "-b" };

            BasicArgs parsed = Args.Parse<BasicArgs>(args);

            Assert.AreEqual("stringValue", parsed.String);
            Assert.AreEqual(34, parsed.Int);
            Assert.AreEqual(33.33, parsed.Double);
            Assert.AreEqual(true, parsed.Bool);
        }


        [TestMethod]
        public void TestPositionArgsPS()
        {
            var args = new string[] { "value1", "value2" };

            PositionedArgs parsed = Args.Parse<PositionedArgs>(args, ArgStyle.PowerShell);

            Assert.AreEqual("value1", parsed.First);
            Assert.AreEqual("value2", parsed.Second);

        }

        [TestMethod]
        public void TestPositionArgsSC()
        {
            var args = new string[] { "value1", "value2" };

            PositionedArgs parsed = Args.Parse<PositionedArgs>(args, ArgStyle.SlashColon);

            Assert.AreEqual("value1", parsed.First);
            Assert.AreEqual("value2", parsed.Second);
        }

        [TestMethod]
        public void TestPositionArgsMixed()
        {
            var args = new string[] { "value1", "-Second", "value2" };

            PositionedArgs parsed = Args.Parse<PositionedArgs>(args, ArgStyle.PowerShell);

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
        public void TestBasicUsage()
        {
            var basicUsage = ArgUsage.GetUsage<BasicArgs>(ArgStyle.PowerShell, "basic");
            ArgUsage.GetUsage<PointArgs>(ArgStyle.PowerShell, "basic");
            Console.WriteLine(basicUsage);
        }

        [TestMethod]
        public void TestBasicUsageWithPositioning()
        {
            var basicUsage = ArgUsage.GetUsage<PositionedArgs>(ArgStyle.PowerShell, "basic");
            Console.WriteLine(basicUsage);
        }
    }
}
