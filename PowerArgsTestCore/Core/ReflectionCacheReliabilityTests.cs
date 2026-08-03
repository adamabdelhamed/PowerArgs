using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Collections.Generic;

namespace ArgsTests.ReflectionCacheModels
{
    public class StatefulHookAttribute : ArgHook
    {
    }

    public class DerivedDefaultAttribute : DefaultValueAttribute
    {
        public DerivedDefaultAttribute() : base(6)
        {
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Parameter, AllowMultiple = true)]
    public class RecordingValidatorAttribute : ArgValidator
    {
        public static readonly List<string> Calls = new List<string>();
        private readonly string name;

        public RecordingValidatorAttribute(string name, int priority)
        {
            this.name = name;
            Priority = priority;
        }

        public override void Validate(string argumentName, ref string arg) => Calls.Add(name);
    }

    [StatefulHook]
    public class CustomHookArgs
    {
        public string Value { get; set; }
    }

    public class DerivedDefaultArgs
    {
        [DerivedDefault]
        public int Count { get; set; }
    }

    public class OrderedValidatorArgs
    {
        [RecordingValidator("higher", 20)]
        [RecordingValidator("lower", 10)]
        public string Value { get; set; }
    }

    public class WholeInputRegexArgs
    {
        [ArgRegex(@"\d+")]
        public string Value { get; set; }
    }
}

namespace ArgsTests
{
    [TestClass]
    [TestCategory(Categories.Core)]
    public class ReflectionCacheReliabilityTests
    {
        public class ReusableArgs
        {
            public string Value { get; set; }
            public int Count { get; set; }
        }

        public class ReusableDefaultArgs
        {
            [DefaultValue(5)]
            public int Count { get; set; }
        }

        [AllowUnexpectedArgs]
        public class ReusableUnexpectedArgs
        {
            public string Known { get; set; }
        }

        [TestMethod]
        public void HookFreeReflectionDefinitionsAreReusedAndCleaned()
        {
            var first = Args.Parse<ReusableArgs>("-Value", "first", "-Count", "1");
            var firstDefinition = Args.GetAmbientDefinition();
            Assert.AreEqual("first", first.Value);

            var second = Args.Parse<ReusableArgs>("-Count", "2");
            var secondDefinition = Args.GetAmbientDefinition();
            Assert.AreSame(firstDefinition, secondDefinition);
            Assert.IsNull(second.Value);
            Assert.AreEqual(2, second.Count);
            Assert.IsNull(secondDefinition.Arguments[0].RevivedValue);
        }

        [TestMethod]
        public void ReusedDefinitionsDoNotRetainUnexpectedArguments()
        {
            Args.Parse<ReusableUnexpectedArgs>("-dynamic", "first");
            Assert.AreEqual("first", Args.GetAmbientDefinition().UnexpectedExplicitArguments["dynamic"]);

            Args.Parse<ReusableUnexpectedArgs>("-Known", "second");
            Assert.AreEqual(0, Args.GetAmbientDefinition().UnexpectedExplicitArguments.Count);
            Assert.AreEqual(0, Args.GetAmbientDefinition().UnexpectedImplicitArguments.Count);
        }

        [TestMethod]
        public void BuiltInDefaultHooksRemainCorrectOnReusedDefinitions()
        {
            var first = Args.Parse<ReusableDefaultArgs>();
            var firstDefinition = Args.GetAmbientDefinition();
            var second = Args.Parse<ReusableDefaultArgs>("-Count", "8");

            Assert.AreEqual(5, first.Count);
            Assert.AreEqual(8, second.Count);
            Assert.AreSame(firstDefinition, Args.GetAmbientDefinition());
        }

        [TestMethod]
        public void CustomHooksAndDerivedDefaultHooksReceiveFreshDefinitions()
        {
            Args.Parse<ReflectionCacheModels.CustomHookArgs>("-Value", "first");
            var firstCustomHookDefinition = Args.GetAmbientDefinition();
            Args.Parse<ReflectionCacheModels.CustomHookArgs>("-Value", "second");
            Assert.AreNotSame(firstCustomHookDefinition, Args.GetAmbientDefinition());

            var first = Args.Parse<ReflectionCacheModels.DerivedDefaultArgs>();
            var firstDerivedDefaultDefinition = Args.GetAmbientDefinition();
            var second = Args.Parse<ReflectionCacheModels.DerivedDefaultArgs>();
            Assert.AreEqual(6, first.Count);
            Assert.AreEqual(6, second.Count);
            Assert.AreNotSame(firstDerivedDefaultDefinition, Args.GetAmbientDefinition());
        }

        [TestMethod]
        public void AllocationFreeValidatorOrderingPreservesLegacyPriorityOrder()
        {
            ReflectionCacheModels.RecordingValidatorAttribute.Calls.Clear();
            Args.Parse<ReflectionCacheModels.OrderedValidatorArgs>("-Value", "test");

            CollectionAssert.AreEqual(new[] { "lower", "higher" }, ReflectionCacheModels.RecordingValidatorAttribute.Calls);
        }

        [TestMethod]
        public void AliasFastPathsPreserveCountContainmentAndDefaultAlias()
        {
            var argument = new CommandLineArgument(typeof(string), "LongName");
            argument.Metadata.Add(new ArgShortcut("n"));

            Assert.AreEqual(2, argument.Aliases.Count);
            Assert.IsTrue(argument.Aliases.Contains("longname"));
            Assert.IsTrue(argument.Aliases.Contains("N"));
            Assert.AreEqual("LongName", argument.DefaultAlias);
            Assert.AreEqual("LongName", argument.Aliases[0]);
            Assert.AreEqual("n", argument.Aliases[1]);
        }

        [TestMethod]
        public void RegexOptimizationStillRequiresAWholeInputMatch()
        {
            Assert.AreEqual("123", Args.Parse<ReflectionCacheModels.WholeInputRegexArgs>("-Value", "123").Value);
            Assert.ThrowsExactly<ValidationArgException>(() => Args.Parse<ReflectionCacheModels.WholeInputRegexArgs>("-Value", "prefix123"));
            Assert.ThrowsExactly<ValidationArgException>(() => Args.Parse<ReflectionCacheModels.WholeInputRegexArgs>("-Value", "123suffix"));
        }
    }
}
