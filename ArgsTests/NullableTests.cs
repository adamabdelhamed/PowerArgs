using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArgsTests
{
    [TestClass]
    public class NullableTests
    {
        class NullableArgs
        {
            public Nullable<int> OptionalNumber{get;set;}
            public Nullable<char> OptionalCharacter{get;set;}
            public Nullable<Guid> OptionalGuid { get; set; }
            public Nullable<DayOfWeek> OptionalDayOfWeek { get; set; }
        }

        [TestCleanup]
        public void Cleanup()
        {
            var reviverType = (from a in typeof(Args).Assembly.GetTypes() where a.Name == "ArgRevivers" select a).Single();
            var prop = reviverType.GetProperty("Revivers", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var reviverDictionary = prop.GetValue(null, null) as Dictionary<Type, Func<string, string, object>>;
            reviverDictionary.Remove(typeof(Nullable<Guid>));
        }

        [TestMethod]
        public void TestNullablesBasic()
        {
            NullableArgs parsed;

            parsed = Args.Parse<NullableArgs>("-OptionalNumber", "100");
            Assert.IsTrue(parsed.OptionalNumber.HasValue);
            Assert.IsFalse(parsed.OptionalCharacter.HasValue);
            Assert.IsFalse(parsed.OptionalGuid.HasValue);
            Assert.AreEqual(100, parsed.OptionalNumber.Value);

            parsed = Args.Parse<NullableArgs>();
            Assert.IsFalse(parsed.OptionalNumber.HasValue);
            Assert.IsFalse(parsed.OptionalCharacter.HasValue);
            Assert.IsFalse(parsed.OptionalGuid.HasValue);

            parsed = Args.Parse<NullableArgs>("-OptionalGuid", "E4BA2178-3367-4AA0-89B2-E08168378FE0");

            Assert.IsFalse(parsed.OptionalNumber.HasValue);
            Assert.IsFalse(parsed.OptionalCharacter.HasValue);
            Assert.IsTrue(parsed.OptionalGuid.HasValue);
            Assert.AreEqual(Guid.Parse("E4BA2178-3367-4AA0-89B2-E08168378FE0"), parsed.OptionalGuid.Value);

            try
            {
                Args.SearchAssemblyForRevivers();
                parsed = Args.Parse<NullableArgs>("-OptionalGuid", "A bad Guid");
                Assert.Fail("An exception should have been thrown");
            }
            catch(ValidationArgException ex)
            {
                Assert.IsTrue(ex.Message.Contains("CUSTOM MESSAGE"));
            }
        }

        [TestMethod]
        public void TestNullableIsEnum()
        {
            var def = new CommandLineArgumentsDefinition(typeof(NullableArgs));
            Assert.IsTrue(def.FindMatchingArgument("OptionalDayOfWeek").IsEnum);
        }

        [TestMethod]
        public void TestNullablesInTableExpression()
        {
            var def = new CommandLineArgumentsDefinition(typeof(NullableArgs));
            var usage = ArgUsage.GenerateUsageFromTemplate(def, PowerArgs.Resources.DefaultConsoleUsageTemplate, "dynamic template").ToString();
            Assert.IsTrue(usage.Contains("Monday"));
            Assert.IsTrue(usage.Contains("Tuesday"));
            Assert.IsTrue(usage.Contains("Wednesday"));
            Assert.IsTrue(usage.Contains("Thursday"));
            Assert.IsTrue(usage.Contains("Friday"));
            Assert.IsTrue(usage.Contains("Saturday"));
            Assert.IsTrue(usage.Contains("Sunday"));
        }
    }

    public static class NullableGuidReviver
    {
        // Make sure a custom NullableReviver works
        [ArgReviver]
        public static Nullable<Guid> Revive(string key, string value)
        {
            Guid ret;
            if(Guid.TryParse(value,out ret) == false)
            {
                throw new ValidationArgException("Bad GUID: CUSTOM MESSAGE");
            }
            return ret;
        }
    }
}
