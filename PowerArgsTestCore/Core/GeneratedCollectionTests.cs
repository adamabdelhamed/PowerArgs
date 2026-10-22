using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests.GeneratedCollectionModels
{
    public class ClassicArgs
    {
        [ArgShortcut("--values"), ArgShortcut("v")] public int[] Values { get; set; }
        public List<string> Names { get; set; }
        public bool Flag { get; set; }
        public int Count { get; set; }
    }

    public partial class GeneratedArgs
    {
        [ArgShortcut("--values"), ArgShortcut("v")] public partial int[] Values { get; set; }
        public partial List<string> Names { get; set; }
        public partial bool Flag { get; set; }
        public partial int Count { get; set; }
    }

    public class ClassicPositioned
    {
        [ArgPosition(0), ArgRequired] public List<int> Values { get; set; }
        public bool Flag { get; set; }
    }

    public partial class GeneratedPositioned
    {
        [ArgPosition(0), ArgRequired] public partial List<int> Values { get; set; }
        public partial bool Flag { get; set; }
    }

    public class ClassicAction
    {
        public bool Global { get; set; }
        [ArgIgnore] public List<int> Received { get; set; }
        [ArgActionMethod] public void Run(List<int> values) => Received = values;
    }

    public partial class GeneratedAction
    {
        public partial bool Global { get; set; }
        [ArgIgnore] public partial List<int> Received { get; set; }
        [ArgActionMethod] public void Run(List<int> values) => Received = values;
    }

    public class Payload
    {
        [ArgPosition(1)] public int[] Values { get; set; }
    }

    public partial class ModelAction
    {
        [ArgIgnore] public partial Payload Received { get; set; }
        public partial List<string> Tags { get; set; }
        [ArgActionMethod] public void Run(Payload args) => Received = args;
    }

    public class ClassicModelAction
    {
        [ArgIgnore] public Payload Received { get; set; }
        public List<string> Tags { get; set; }
        [ArgActionMethod] public void Run(Payload args) => Received = args;
    }

    public class CollectionValidator : ArgValidator
    {
        public override void Validate(string name, ref string arg) { }
    }

    public partial class ValidatedCollection
    {
        [CollectionValidator] public partial List<int> Values { get; set; }
    }

    public partial class NestedCollection
    {
        public partial List<List<int>> Values { get; set; }
    }

    public partial class DefaultCollection
    {
        [DefaultValue("1,2")] public partial int[] Values { get; set; }
    }

    public partial class EnumCollection
    {
        public partial DayOfWeek?[] Days { get; set; }
    }
}

namespace ArgsTests
{
    using GeneratedCollectionModels;

    [TestClass]
    public class GeneratedCollectionTests
    {
        public static IEnumerable<object[]> ParseInputs()
        {
            yield return new object[] { Array.Empty<string>() };
            yield return new object[] { new[] { "-Values" } };
            yield return new object[] { new[] { "-Values", "" } };
            yield return new object[] { new[] { "-Values", "1,2,3" } };
            yield return new object[] { new[] { "-Values", "1", "-2", "3", "-Flag" } };
            yield return new object[] { new[] { "--Values=1,2,3" } };
            yield return new object[] { new[] { "--Values=1", "2", "3" } };
            yield return new object[] { new[] { "--Values", "1", "2", "3" } };
            yield return new object[] { new[] { "/Values:1,2" } };
            yield return new object[] { new[] { "-Names", "a,b", "c" } };
            yield return new object[] { new[] { "-Names", "a,,b" } };
            yield return new object[] { new[] { "-Names", "", "tail" } };
            yield return new object[] { new[] { "-Names", "first", "-" } };
            yield return new object[] { new[] { "-Names", "/tmp/input", "tail" } };
            yield return new object[] { new[] { "-Values", "1", "/Flag" } };
            yield return new object[] { new[] { "-Values", "1", "-Count", "7" } };
        }

        [TestMethod, DynamicData(nameof(ParseInputs))]
        public void NamedCollectionsMatchClassicSyntax(string[] input)
        {
            var expected = Args.Parse<ClassicArgs>(input);
            var actual = Args.Parse<GeneratedArgs>(input);
            Assert.AreEqual(ArgumentPipelineKind.GeneratedDirectParser, GeneratedArgumentModelRegistry.GetPipelineKind<GeneratedArgs>());
            CollectionAssert.AreEqual(expected.Values, actual.Values);
            CollectionAssert.AreEqual(expected.Names, actual.Names);
            Assert.AreEqual(expected.Flag, actual.Flag);
            Assert.AreEqual(expected.Count, actual.Count);
        }

        [TestMethod]
        public void CollectionFailuresMatchClassicAndDoNotPoisonNextParse()
        {
            foreach (var input in new[]
            {
                new[] { "-Values", "bad" }, new[] { "-Values", "1,2", "3" },
                new[] { "-Values", "1", "-Values", "2" }, new[] { "-Values", "1", "-v", "2" },
                new[] { "/Values:1", "2" }, new[] { "-Values", "1", "/not-an-option" },
                new[] { "--Names=a,b" },
            })
            {
                Exception expected = null;
                Exception actual = null;
                try { Args.Parse<ClassicArgs>(input); } catch (ArgException ex) { expected = ex; }
                try { Args.Parse<GeneratedArgs>(input); } catch (ArgException ex) { actual = ex; }
                Assert.IsNotNull(expected);
                Assert.IsNotNull(actual);
                Assert.AreEqual(expected.GetType(), actual.GetType(), string.Join(" ", input));
            }
            CollectionAssert.AreEqual(new[] { 9, 10 }, Args.Parse<GeneratedArgs>("-Values", "9", "10").Values);
            Assert.ThrowsExactly<DuplicateArgException>(() => Args.Parse<GeneratedArgs>("-Values", "1", "-v", "2"));
            Assert.IsNull(Args.Parse<GeneratedArgs>().Values);
        }

        [TestMethod]
        public void PositionedCollectionsConsumeTailThenAllowNamedOptions()
        {
            var input = new[] { "1", "-2", "3", "-Flag" };
            var expected = Args.Parse<ClassicPositioned>(input);
            var actual = Args.Parse<GeneratedPositioned>(input);
            CollectionAssert.AreEqual(expected.Values, actual.Values);
            Assert.AreEqual(expected.Flag, actual.Flag);
            Assert.ThrowsExactly<MissingArgException>(() => Args.Parse<GeneratedPositioned>());
        }

        [TestMethod]
        public void CollectionActionParametersUseGeneratedInvocation()
        {
            foreach (var input in new[] { new[] { "Run", "1", "2", "-Global" }, new[] { "Run", "-values", "1,2" } })
            {
                var expected = Args.InvokeAction<ClassicAction>(input);
                var actual = Args.InvokeAction<GeneratedAction>(input);
                CollectionAssert.AreEqual(expected.Args.Received, actual.Args.Received);
                Assert.AreEqual(expected.Args.Global, actual.Args.Global);
                Assert.AreEqual(ArgumentPipelineKind.GeneratedDirectParser, GeneratedArgumentModelRegistry.GetPipelineKind<GeneratedAction>());
            }
            var model = Args.InvokeAction<ModelAction>("Run", "1", "2", "-Tags", "red", "blue").Args;
            var classicModel = Args.InvokeAction<ClassicModelAction>("Run", "1", "2", "-Tags", "red", "blue").Args;
            CollectionAssert.AreEqual(classicModel.Received.Values, model.Received.Values);
            CollectionAssert.AreEqual(classicModel.Tags, model.Tags);
            Assert.AreEqual(ArgumentPipelineKind.GeneratedDirectParser, GeneratedArgumentModelRegistry.GetPipelineKind<ModelAction>());
        }

        [TestMethod]
        public void CollectionElementErrorsRetainClassicMessages()
        {
            var expected = Assert.ThrowsExactly<ArgException>(() => Args.Parse<ClassicArgs>("-Values", "1", "bad"));
            var actual = Assert.ThrowsExactly<ArgException>(() => Args.Parse<GeneratedArgs>("-Values", "1", "bad"));
            Assert.AreEqual(expected.Message, actual.Message);
            expected = Assert.ThrowsExactly<ArgException>(() => Args.InvokeAction<ClassicAction>("Run", "1", "bad"));
            actual = Assert.ThrowsExactly<ArgException>(() => Args.InvokeAction<GeneratedAction>("Run", "1", "bad"));
            Assert.AreEqual(expected.Message, actual.Message);
            Assert.ThrowsExactly<DuplicateArgException>(() => Args.InvokeAction<GeneratedAction>("Run", "1", "-values", "2"));
            CollectionAssert.AreEqual(new[] { 3, 4 }, Args.InvokeAction<GeneratedAction>("Run", "3", "4").Args.Received);
        }

        [TestMethod]
        public void DefaultsEnumsAndCustomElementReviversRetainClassicConversion()
        {
            CollectionAssert.AreEqual(new[] { 1, 2 }, Args.Parse<DefaultCollection>().Values);
            CollectionAssert.AreEqual(new DayOfWeek?[] { DayOfWeek.Monday, DayOfWeek.Friday }, Args.Parse<EnumCollection>("-Days", "Monday", "friday").Days);
            var original = ArgRevivers.GetReviver(typeof(int));
            try
            {
                ArgRevivers.SetReviver<int>((name, value) => name == "Values_element" ? int.Parse(value) + 100 : -1);
                var expected = Args.Parse<ClassicArgs>("-Values", "1", "2");
                var actual = Args.Parse<GeneratedArgs>("-Values", "1", "2");
                CollectionAssert.AreEqual(expected.Values, actual.Values);
            }
            finally { ArgRevivers.SetReviver(typeof(int), original); }
        }

        [TestMethod]
        public void ComplexCollectionsKeepTheSharedParser()
        {
            Assert.AreEqual(ArgumentPipelineKind.GeneratedSharedParser, GeneratedArgumentModelRegistry.GetPipelineKind<ValidatedCollection>());
            Assert.AreEqual(ArgumentPipelineKind.GeneratedSharedParser, GeneratedArgumentModelRegistry.GetPipelineKind<NestedCollection>());
            CollectionAssert.AreEqual(new[] { 1, 2 }, Args.Parse<ValidatedCollection>("-Values", "1", "2").Values);
        }
    }
}
