using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace ArgsTests
{

    [TestClass]
    public class ComponentModelReviverTest
    {
        [TypeConverter(typeof(PointConverter))]
        public class Point2
        {
            public int X { get; set; }
            public int Y { get; set; }

        }

        public class PointConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return sourceType == typeof(string);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                return destinationType == typeof(Point2);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
            {
                var text = value.ToString();

                var match = Regex.Match(text, @"(\d*),(\d*)");
                if (match.Success == false)
                {
                    throw new ArgException("Not a valid point: " + text);
                }
                else
                {
                    Point2 ret = new Point2();
                    ret.X = int.Parse(match.Groups[1].Value);
                    ret.Y = int.Parse(match.Groups[2].Value);
                    return ret;
                }
            }

            public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
            {
                if(value is Point2 == false) return "?";
                
                Point2 val = value as Point2;
                return val.X+","+val.Y;
            }
        }

        public class ConvertArgs
        {
            public Point2 ThePoint { get; set; }
        }

        [TestMethod]
        public void TestTypeConverter()
        {
            var parsed = Args.Parse<ConvertArgs>(new string[] { "-t", "3,4" });
            Assert.AreEqual(3, parsed.ThePoint.X);
            Assert.AreEqual(4, parsed.ThePoint.Y);
        }

        [TestMethod]
        public void TestTypeConverterNegative()
        {
            Helpers.Run(() =>
            {
                var parsed = Args.Parse<ConvertArgs>(new string[] { "-t", "3,NOTANUMBER" });
            }, 
            Helpers.ExpectedArgException());
        }
    }
}
