using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests
{
    [TestClass]
    public class ConfigurationToolTests
    {
        public class ConfigurationToolArgs
        {
            [ArgRequired]
            [ArgPosition(0)]
            public string Action { get; set; }

            [ArgExample("thetool config mykey myvalue", "Sets the value of 'mykey' to 'myvalue'")]
            public ConfigArgs ConfigArgs { get; set; }

            public static int InvokeCount { get; set; }
            
            public static void Config(ConfigArgs args)
            {
                InvokeCount++;
            }
        }

        public class ConfigArgs
        {
            [ArgPosition(1)]
            [ArgShortcut("n")]
            [ArgDescription("The name of the configuration element to update")]
            public string ConfigName { get; set; }

            [ArgPosition(2)]
            [ArgShortcut("v")]
            public string ConfigValue { get; set; }
        }

        [TestMethod]
        public void TestConfigToolArgs()
        {
            var args = new string[] { "config", "background", "green" };

            var parsed = Args.ParseAction<ConfigurationToolArgs>(args);
            Assert.AreEqual("background", parsed.Args.ConfigArgs.ConfigName);
            Assert.AreEqual("green", parsed.Args.ConfigArgs.ConfigValue);
        }

        [TestMethod]
        public void TestConfigToolUsage()
        {
            var usage = ArgUsage.GetUsage<ConfigurationToolArgs>("mytool");
            Console.WriteLine(usage);
        }
    }
}
