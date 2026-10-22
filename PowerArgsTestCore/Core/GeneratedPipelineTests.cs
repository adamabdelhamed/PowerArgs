using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.IO;

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

    public partial class RegexArgs
    {
        [ArgRequired]
        [ArgDescription("A generated regular expression argument")]
        [ArgRegex(@"\d{3}-\d{2}-\d{4}", "Invalid social security number")]
        public partial string RequiredValue { get; set; }
    }

    public partial class UnsupportedMetadataArgs
    {
        [ArgCantBeCombinedWith(nameof(SecondValue))]
        public partial string FirstValue { get; set; }

        public partial string SecondValue { get; set; }
    }

    public enum GeneratedMode
    {
        Fast,
        Safe,
    }

    public partial class DefaultValueArgs
    {
        [DefaultValue(GeneratedMode.Safe)]
        public partial GeneratedMode Mode { get; set; }

        [ArgDefaultValue(7)]
        [ArgRange(1, 10)]
        public partial int Count { get; set; }
    }

    public partial class ExistingPathArgs
    {
        [ArgExistingFile]
        public partial string File { get; set; }

        [ArgExistingDirectory]
        public partial string Directory { get; set; }
    }

    public partial class PrimitiveFallbackArgs
    {
        public partial int Number { get; set; }
    }

    [ArgDescription("Generated metadata model")]
    [ArgProductName("PowerArgs Generated Tests")]
    [ArgProductVersion("1.2.3")]
    [ArgCopyright("Copyright generated")]
    [ArgExample("tool -visible value", "Shows generated metadata", Title = "Generated example")]
    public partial class UsageMetadataArgs
    {
        public partial string Visible { get; set; }

        [OmitFromUsageDocs]
        public partial string Secret { get; set; }
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
        public void ConditionalArgRequiredUsesGeneratedDirectParser()
        {
            var parsed = Args.Parse<GeneratedPipelineModels.ConditionalRequiredArgs>("-SecondArgument", "hello");
            Assert.IsNull(parsed.FirstArgument);
            Assert.AreEqual("hello", parsed.SecondArgument);

            Assert.ThrowsExactly<MissingArgException>(() => Args.Parse<GeneratedPipelineModels.ConditionalRequiredArgs>());
            Assert.IsTrue(GeneratedArgumentModelRegistry.IsGenerated<GeneratedPipelineModels.ConditionalRequiredArgs>());
            Assert.IsTrue(Args.GetAmbientDefinition().UsesGeneratedPipeline);
        }

        [TestMethod]
        public void GeneratedArgRegexMatchesCoreValidatorBehavior()
        {
            var parsed = Args.Parse<GeneratedPipelineModels.RegexArgs>("-RequiredValue", "111-22-3333");

            Assert.AreEqual("111-22-3333", parsed.RequiredValue);
            Assert.IsTrue(GeneratedArgumentModelRegistry.IsGenerated<GeneratedPipelineModels.RegexArgs>());
            Assert.IsTrue(Args.GetAmbientDefinition().UsesGeneratedPipeline);

            var exception = Assert.ThrowsExactly<ValidationArgException>(() => Args.Parse<GeneratedPipelineModels.RegexArgs>("-RequiredValue", "invalid"));
            Assert.AreEqual("Invalid social security number: invalid", exception.Message);
        }

        [TestMethod]
        public void CrossArgumentMetadataUsesGeneratedDirectParser()
        {
            var parsed = Args.Parse<GeneratedPipelineModels.UnsupportedMetadataArgs>("-FirstValue", "hello");

            Assert.AreEqual("hello", parsed.FirstValue);
            Assert.IsTrue(GeneratedArgumentModelRegistry.IsGenerated<GeneratedPipelineModels.UnsupportedMetadataArgs>());
            Assert.IsTrue(Args.GetAmbientDefinition().UsesGeneratedPipeline);
            Assert.ThrowsExactly<UnexpectedArgException>(() => Args.Parse<GeneratedPipelineModels.UnsupportedMetadataArgs>("-FirstValue", "hello", "-SecondValue", "world"));
        }

        [TestMethod]
        public void GeneratedDefaultsMatchCoreBehavior()
        {
            var parsed = Args.Parse<GeneratedPipelineModels.DefaultValueArgs>();

            Assert.AreEqual(GeneratedPipelineModels.GeneratedMode.Safe, parsed.Mode);
            Assert.AreEqual(7, parsed.Count);
            Assert.AreEqual(GeneratedPipelineModels.GeneratedMode.Safe, Args.GetAmbientDefinition().Arguments[0].DefaultValue);
            Assert.AreEqual(7, Args.GetAmbientDefinition().Arguments[1].DefaultValue);
            Assert.IsTrue(Args.GetAmbientDefinition().UsesGeneratedPipeline);

            parsed = Args.Parse<GeneratedPipelineModels.DefaultValueArgs>("-Mode", "Fast", "-Count", "4");
            Assert.AreEqual(GeneratedPipelineModels.GeneratedMode.Fast, parsed.Mode);
            Assert.AreEqual(4, parsed.Count);
        }

        [TestMethod]
        public void GeneratedPrimitiveFastPathRetainsConstantAndErrorFallbacks()
        {
            var maximum = Args.Parse<GeneratedPipelineModels.PrimitiveFallbackArgs>("-Number", nameof(int.MaxValue));
            var minimum = Args.Parse<GeneratedPipelineModels.PrimitiveFallbackArgs>("-Number", nameof(int.MinValue));

            Assert.AreEqual(int.MaxValue, maximum.Number);
            Assert.AreEqual(int.MinValue, minimum.Number);
            var exception = Assert.ThrowsExactly<ArgException>(() => Args.Parse<GeneratedPipelineModels.PrimitiveFallbackArgs>("-Number", "not-an-integer"));
            Assert.IsTrue(exception.Message.Contains("value must be an integer"));
            Assert.IsTrue(exception.Context.Definition.UsesGeneratedPipeline);
        }

        [TestMethod]
        public void GeneratedExistingPathValidatorsNormalizeAndRejectPaths()
        {
            var file = typeof(GeneratedPipelineTests).Assembly.Location;
            var directory = Path.GetDirectoryName(file);
            var parsed = Args.Parse<GeneratedPipelineModels.ExistingPathArgs>("-File", file, "-Directory", directory);

            Assert.AreEqual(Path.GetFullPath(file), parsed.File);
            Assert.AreEqual(Path.GetFullPath(directory), parsed.Directory);
            Assert.IsTrue(Args.GetAmbientDefinition().UsesGeneratedPipeline);

            var fileException = Assert.ThrowsExactly<ValidationArgException>(() => Args.Parse<GeneratedPipelineModels.ExistingPathArgs>("-File", "not-a-real-file"));
            Assert.AreEqual("File not found - not-a-real-file", fileException.Message);
            Assert.IsInstanceOfType<FileNotFoundException>(fileException.InnerException);

            var directoryException = Assert.ThrowsExactly<ValidationArgException>(() => Args.Parse<GeneratedPipelineModels.ExistingPathArgs>("-Directory", "not-a-real-directory"));
            Assert.AreEqual("Directory not found: 'not-a-real-directory'", directoryException.Message);
            Assert.IsInstanceOfType<DirectoryNotFoundException>(directoryException.InnerException);
        }

        [TestMethod]
        public void GeneratedUsageMetadataMatchesReflectedDefinitions()
        {
            Args.Parse<GeneratedPipelineModels.UsageMetadataArgs>();
            var definition = Args.GetAmbientDefinition();

            Assert.AreEqual("Generated metadata model", definition.Description);
            Assert.AreEqual("PowerArgs Generated Tests", definition.ProductName);
            Assert.AreEqual("1.2.3", definition.ProductVersion);
            Assert.AreEqual("Copyright generated", definition.Copyright);
            Assert.AreEqual(1, definition.Examples.Count);
            Assert.AreEqual("Generated example", definition.Examples[0].Title);
            Assert.IsFalse(definition.Arguments[1].IncludeInUsage);
            Assert.IsTrue(definition.UsesGeneratedPipeline);
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
