using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Collections.Generic;

namespace ArgsTests.GeneratedReadmeModels
{
    public sealed class EvenNumberAttribute : ArgValidator
    {
        public override void Validate(string name, ref string arg)
        {
            if (int.Parse(arg) % 2 != 0) throw new ValidationArgException(name + " must be even");
        }
    }

    public sealed class ReadmePoint
    {
        public int X { get; }
        public int Y { get; }

        public ReadmePoint(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    [ArgReviverType]
    public static class ReadmeRevivers
    {
        [ArgReviver]
        public static ReadmePoint RevivePoint(string key, string value)
        {
            var parts = value.Split(',');
            if (parts.Length != 2) throw new ArgException("Not a valid point: " + value);
            return new ReadmePoint(int.Parse(parts[0]), int.Parse(parts[1]));
        }
    }

    public partial class CustomReviverArgs
    {
        public partial ReadmePoint Point { get; set; }
    }

    public partial class CollectionArgs
    {
        public partial List<int> Values { get; set; }
    }

    public partial class SecureArgs
    {
        public partial SecureStringArgument Password { get; set; }
    }

    public class InheritedArgsBase
    {
        public string BaseValue { get; set; }
    }

    public partial class InheritedArgs : InheritedArgsBase
    {
        [ArgIgnore]
        public partial bool Marker { get; set; }
    }

    [AllowUnexpectedArgs]
    public partial class UnexpectedArgs
    {
        public partial string Known { get; set; }
    }

    public partial class CustomValidatorArgs
    {
        [EvenNumber]
        public partial int Number { get; set; }
    }

    public partial class HelpArgs
    {
        [HelpHook(WriteHelp = false)]
        public partial bool Help { get; set; }
    }

    public partial class PromptRequiredArgs
    {
        [ArgRequired(PromptIfMissing = true)]
        public partial string Value { get; set; }
    }

    [TabCompletion("$")]
    public partial class TabCompletionArgs
    {
        public partial string Value { get; set; }
    }

    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
    public partial class MainArgs
    {
        public partial string Value { get; set; }

        [ArgIgnore]
        public partial bool Called { get; set; }

        public void Main() => Called = true;
    }

    public sealed class TwoOperandArgs
    {
        [ArgRequired, ArgPosition(1)]
        public double Value1 { get; set; }

        [ArgRequired, ArgPosition(2)]
        public double Value2 { get; set; }
    }

    [ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
    public partial class CalculatorArgs
    {
        [HelpHook(WriteHelp = false), ArgShortcut("-?")]
        public partial bool Help { get; set; }

        [ArgIgnore]
        public partial double Result { get; set; }

        [ArgActionMethod, ArgDescription("Adds the two operands")]
        public void Add(TwoOperandArgs args) => Result = args.Value1 + args.Value2;
    }

    public sealed class MemoryStickyPersistence : IStickyArgPersistenceProvider
    {
        public static Dictionary<string, string> Values { get; } = new Dictionary<string, string>();

        public Dictionary<string, string> Load(string pathInfo) => new Dictionary<string, string>(Values);

        public void Save(Dictionary<string, string> stickyArgs, string pathInfo)
        {
            Values.Clear();
            foreach (var pair in stickyArgs) Values.Add(pair.Key, pair.Value);
        }
    }

    [StickyArgPersistence(typeof(MemoryStickyPersistence))]
    public partial class StickyArgs
    {
        [StickyArg]
        public partial string Name { get; set; }
    }
}

namespace ArgsTests
{
    [TestClass]
    [TestCategory(Categories.Core)]
    public class GeneratedReadmeFeatureTests
    {
        [TestMethod]
        public void GeneratedSharedParserSupportsCustomReviversCollectionsAndSecureArguments()
        {
            var point = Args.Parse<GeneratedReadmeModels.CustomReviverArgs>("-Point", "3,4");
            Assert.AreEqual(3, point.Point.X);
            Assert.AreEqual(4, point.Point.Y);
            Assert.IsTrue(Args.GetAmbientDefinition().UsesGeneratedPipeline);

            var collection = Args.Parse<GeneratedReadmeModels.CollectionArgs>("-Values", "1,2,3");
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, collection.Values);
            Assert.IsTrue(Args.GetAmbientDefinition().UsesGeneratedPipeline);

            var secure = Args.Parse<GeneratedReadmeModels.SecureArgs>();
            Assert.IsNotNull(secure.Password);
            Assert.IsTrue(Args.GetAmbientDefinition().UsesGeneratedPipeline);
        }

        [TestMethod]
        public void GeneratedSharedParserSupportsInheritanceAndUnexpectedArguments()
        {
            var inherited = Args.Parse<GeneratedReadmeModels.InheritedArgs>("-BaseValue", "base");
            Assert.AreEqual("base", inherited.BaseValue);
            Assert.IsTrue(Args.GetAmbientDefinition().UsesGeneratedPipeline);

            var unexpected = Args.Parse<GeneratedReadmeModels.UnexpectedArgs>("-Known", "known", "-dynamic", "value");
            Assert.AreEqual("known", unexpected.Known);
            Assert.AreEqual("value", Args.GetAmbientDefinition().UnexpectedExplicitArguments["dynamic"]);
            Assert.IsTrue(Args.GetAmbientDefinition().UsesGeneratedPipeline);
        }

        [TestMethod]
        public void GeneratedMetadataSupportsCustomValidatorsHelpTabCompletionAndStickyValues()
        {
            Assert.AreEqual(4, Args.Parse<GeneratedReadmeModels.CustomValidatorArgs>("-Number", "4").Number);
            Assert.ThrowsExactly<ValidationArgException>(() => Args.Parse<GeneratedReadmeModels.CustomValidatorArgs>("-Number", "3"));
            Assert.IsTrue(GeneratedArgumentModelRegistry.IsGenerated<GeneratedReadmeModels.CustomValidatorArgs>());

            Assert.IsNull(Args.Parse<GeneratedReadmeModels.HelpArgs>("-Help"));
            Assert.IsTrue(GeneratedArgumentModelRegistry.IsGenerated<GeneratedReadmeModels.HelpArgs>());

            Assert.AreEqual("normal", Args.Parse<GeneratedReadmeModels.TabCompletionArgs>("-Value", "normal").Value);
            Assert.IsTrue(Args.GetAmbientDefinition().UsesGeneratedPipeline);

            GeneratedReadmeModels.MemoryStickyPersistence.Values.Clear();
            Assert.AreEqual("Ada", Args.Parse<GeneratedReadmeModels.StickyArgs>("-Name", "Ada").Name);
            Assert.AreEqual("Ada", Args.Parse<GeneratedReadmeModels.StickyArgs>().Name);
            Assert.IsTrue(Args.GetAmbientDefinition().UsesGeneratedPipeline);
        }

        [TestMethod]
        public void PromptingRequirementsRemainGeneratedAndRespectNonInteractiveMode()
        {
            var definition = new CommandLineArgumentsDefinition(typeof(GeneratedReadmeModels.PromptRequiredArgs)) { IsNonInteractive = true };

            var exception = Assert.ThrowsExactly<MissingArgException>(() => Args.Parse(definition));
            Assert.IsTrue(exception.Context.Definition.UsesGeneratedPipeline);
            Assert.IsTrue(GeneratedArgumentModelRegistry.IsGenerated<GeneratedReadmeModels.PromptRequiredArgs>());
        }

        [TestMethod]
        public void GeneratedSharedParserUsesGeneratedMainInvokerAndTypeMetadata()
        {
            var result = Args.InvokeMain<GeneratedReadmeModels.MainArgs>("-Value", "hello");

            Assert.AreEqual("hello", result.Args.Value);
            Assert.IsTrue(result.Args.Called);
            Assert.AreEqual(ArgExceptionPolicy.StandardExceptionHandling, result.Definition.ExceptionBehavior.Policy);
            Assert.IsTrue(result.Definition.UsesGeneratedPipeline);
        }

        [TestMethod]
        public void ReadmeStyleActionsAndHelpHookStayGenerated()
        {
            var result = Args.InvokeAction<GeneratedReadmeModels.CalculatorArgs>("Add", "2", "5");
            Assert.AreEqual(7d, result.Args.Result);
            Assert.IsTrue(result.Definition.UsesGeneratedPipeline);
            Assert.AreEqual(ArgExceptionPolicy.StandardExceptionHandling, result.Definition.ExceptionBehavior.Policy);

            var help = Args.InvokeAction<GeneratedReadmeModels.CalculatorArgs>("Add", "-?");
            Assert.IsTrue(help.Cancelled);
            Assert.IsTrue(help.Definition.UsesGeneratedPipeline);
        }
    }
}
