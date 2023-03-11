using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Collections.Generic;
using System.Reflection.Metadata;

namespace ArgsTests.Templating
{
    [TestClass]
    [TestCategory(Categories.RGB)]
    public class RGBTests
    {
        [TestMethod]
        public void TestRGBToConsoleColor()
        {
            foreach (var color in Enum.GetValues<ConsoleColor>())
            {
                var rgb = RGB.Convert(color);
                var back = rgb.ToConsoleColor();
                Assert.IsTrue(back.HasValue);
                Assert.AreEqual(color, back);
            }

            Assert.IsFalse(RGB.Orange.ToConsoleColor().HasValue);
        }
    }
     
}
