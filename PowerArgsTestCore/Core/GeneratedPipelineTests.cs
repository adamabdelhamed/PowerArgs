using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests.GeneratedPipelineModels
{
    public partial class RequiredArgs
    {
        [ArgRequired]
        public partial string RequiredValue { get; set; }

        public partial int Count { get; set; }

        public partial double Double { get; set; }

        public partial bool Bool { get; set; }
    }

    public partial class UnsupportedMetadataArgs
    {
        [ArgRequired]
        [ArgDescription("This metadata is not generated in the first slice")]
        [ArgRegex(".+")]
        public partial string RequiredValue { get; set; }
    }

    public partial class ConditionalRequiredArgs
    {
        [ArgRequired(IfNot = nameof(SecondArgument))]
        public partial string FirstArgument { get; set; }

        public partial string SecondArgument { get; set; }
    }

    [ArgDescription("Generated feature-rich arguments")]
    public partial class FeatureRichArgs
    {
        [ArgRequired]
        [ArgPosition(0)]
        [ArgDescription("The input value")]
        public partial string Input { get; set; }

        [ArgShortcut("n")]
        [ArgRange(1, 10)]
        public partial int Count { get; set; }

        [ArgRange(0, 100, MaxIsExclusive = true)]
        public partial int ExclusiveMaximum { get; set; }

        [ArgShortcut(ArgShortcutPolicy.ShortcutsOnly)]
        [ArgShortcut("--verbose")]
        public partial bool Verbose { get; set; }

        [ArgIgnore]
        public partial string Ignored { get; set; }
    }

    [ArgEnforceCase]
    public partial class CaseSensitiveGeneratedArgs
    {
        [ArgRequired]
        [ArgShortcut("v")]
        public partial string Value { get; set; }
    }

    public partial class GeneratedShortcutArgs
    {
        [ArgShortcut("s")]
        public partial string SomeString { get; set; }

        [ArgShortcut(null)]
        public partial string SomeOtherString { get; set; }
    }

    public partial class GeneratedPositionedArgs
    {
        [ArgPosition(0)]
        public partial string First { get; set; }

        [ArgPosition(1)]
        public partial string Second { get; set; }
    }
}

namespace ArgsTests
{
    [TestClass]
    [TestCategory(Categories.Core)]
    public class GeneratedPipelineTests
    {
        [TestMethod]
        public void GeneratedPipelineParsesNamedArguments()
        {
            var parsed = Args.Parse<GeneratedPipelineModels.RequiredArgs>("-RequiredValue", "hello", "-Count", "42");

            Assert.AreEqual("hello", parsed.RequiredValue);
            Assert.AreEqual(42, parsed.Count);
            Assert.IsTrue(GeneratedArgumentModelRegistry.IsGenerated<GeneratedPipelineModels.RequiredArgs>());
            Assert.IsTrue(Args.GetAmbientDefinition().UsesGeneratedPipeline);
        }

        [TestMethod]
        public void GeneratedPipelineMatchesSlashColonCoreTest()
        {
            var parsed = Args.Parse<GeneratedPipelineModels.RequiredArgs>("/requiredvalue:stringValue", "/c:34", "/d:33.33", "/b");

            Assert.AreEqual("stringValue", parsed.RequiredValue);
            Assert.AreEqual(34, parsed.Count);
            Assert.AreEqual(33.33, parsed.Double);
            Assert.IsTrue(parsed.Bool);
        }

        [TestMethod]
        public void GeneratedArgRequiredValidatorThrowsOnMissingArg()
        {
            try
            {
                Args.Parse<GeneratedPipelineModels.RequiredArgs>();
                Assert.Fail("An exception should have been thrown");
            }
            catch (MissingArgException ex)
            {
                Assert.AreEqual("The argument 'RequiredValue' is required", ex.Message);
            }
        }

        [TestMethod]
        public void ConditionalArgRequiredUsesCompileTimeReflectionFallback()
        {
            var parsed = Args.Parse<GeneratedPipelineModels.ConditionalRequiredArgs>("-SecondArgument", "hello");
            Assert.IsNull(parsed.FirstArgument);
            Assert.AreEqual("hello", parsed.SecondArgument);

            Assert.ThrowsExactly<MissingArgException>(() => Args.Parse<GeneratedPipelineModels.ConditionalRequiredArgs>());
            Assert.IsFalse(GeneratedArgumentModelRegistry.IsGenerated<GeneratedPipelineModels.ConditionalRequiredArgs>());
            Assert.IsFalse(Args.GetAmbientDefinition().UsesGeneratedPipeline);
        }

        [TestMethod]
        public void UnsupportedMetadataFallsBackToReflectionPipeline()
        {
            var parsed = Args.Parse<GeneratedPipelineModels.UnsupportedMetadataArgs>("-RequiredValue", "hello");

            Assert.AreEqual("hello", parsed.RequiredValue);
            Assert.IsFalse(GeneratedArgumentModelRegistry.IsGenerated<GeneratedPipelineModels.UnsupportedMetadataArgs>());
            Assert.IsFalse(Args.GetAmbientDefinition().UsesGeneratedPipeline);
            Assert.AreEqual("This metadata is not generated in the first slice", Args.GetAmbientDefinition().Arguments[0].Description);
        }

        [TestMethod]
        public void GeneratedFeatureAttributesParseWithoutReflection()
        {
            var parsed = Args.Parse<GeneratedPipelineModels.FeatureRichArgs>("input.txt", "-n", "7", "--verbose");

            Assert.AreEqual("input.txt", parsed.Input);
            Assert.AreEqual(7, parsed.Count);
            Assert.IsTrue(parsed.Verbose);
            Assert.IsNull(parsed.Ignored);
            Assert.IsTrue(GeneratedArgumentModelRegistry.IsGenerated<GeneratedPipelineModels.FeatureRichArgs>());
            Assert.IsTrue(Args.GetAmbientDefinition().UsesGeneratedPipeline);
            Assert.AreEqual("Generated feature-rich arguments", Args.GetAmbientDefinition().Description);
            Assert.AreEqual("The input value", Args.GetAmbientDefinition().Arguments[0].Description);
            Assert.AreEqual(0, Args.GetAmbientDefinition().Arguments[0].Position);
            Assert.AreEqual(4, Args.GetAmbientDefinition().Arguments.Count);
        }

        [TestMethod]
        public void GeneratedArgRangeMatchesCoreValidatorBehavior()
        {
            var exception = Assert.ThrowsExactly<ValidationArgException>(() => Args.Parse<GeneratedPipelineModels.FeatureRichArgs>("input.txt", "-n", "11"));
            Assert.AreEqual("Count must be at least 1, but not greater than 10", exception.Message);

            exception = Assert.ThrowsExactly<ValidationArgException>(() => Args.Parse<GeneratedPipelineModels.FeatureRichArgs>("input.txt", "-ExclusiveMaximum", "100"));
            Assert.AreEqual("ExclusiveMaximum must be at least 0, and less than 100", exception.Message);
        }

        [TestMethod]
        public void GeneratedCaseSensitivityMatchesCoreBehavior()
        {
            var parsed = Args.Parse<GeneratedPipelineModels.CaseSensitiveGeneratedArgs>("-v", "correct");
            Assert.AreEqual("correct", parsed.Value);
            Assert.ThrowsExactly<MissingArgException>(() => Args.Parse<GeneratedPipelineModels.CaseSensitiveGeneratedArgs>("-V", "wrong"));
        }

        [TestMethod]
        public void GeneratedShortcutAttributesMatchCoreTests()
        {
            var parsed = Args.Parse<GeneratedPipelineModels.GeneratedShortcutArgs>("-s", "FirstString", "-SomeOtherString", "SecondString");

            Assert.AreEqual("FirstString", parsed.SomeString);
            Assert.AreEqual("SecondString", parsed.SomeOtherString);
        }

        [TestMethod]
        public void GeneratedPositionedArgumentsMatchCoreTest()
        {
            var parsed = Args.Parse<GeneratedPipelineModels.GeneratedPositionedArgs>("first", "second");

            Assert.AreEqual("first", parsed.First);
            Assert.AreEqual("second", parsed.Second);
        }

        [TestMethod]
        public void RuntimeInputNeverChangesARegisteredModelsPipeline()
        {
            var longForm = Args.Parse<GeneratedPipelineModels.RequiredArgs>("--RequiredValue=hello", "--Count=4");
            Assert.AreEqual("hello", longForm.RequiredValue);
            Assert.AreEqual(4, longForm.Count);
            Assert.IsTrue(Args.GetAmbientDefinition().UsesGeneratedPipeline);

            var path = Args.Parse<GeneratedPipelineModels.GeneratedPositionedArgs>("/tmp/input.txt", "-2");
            Assert.AreEqual("/tmp/input.txt", path.First);
            Assert.AreEqual("-2", path.Second);
            Assert.IsTrue(Args.GetAmbientDefinition().UsesGeneratedPipeline);

            var duplicate = Assert.ThrowsExactly<DuplicateArgException>(() => Args.Parse<GeneratedPipelineModels.RequiredArgs>("-RequiredValue", "first", "-RequiredValue", "second"));
            Assert.IsTrue(duplicate.Context.Definition.UsesGeneratedPipeline);

            var unexpected = Assert.ThrowsExactly<UnexpectedArgException>(() => Args.Parse<GeneratedPipelineModels.RequiredArgs>("-RequiredValue", "hello", "-bogus", "value"));
            Assert.IsTrue(unexpected.Context.Definition.UsesGeneratedPipeline);
        }
    }
}
