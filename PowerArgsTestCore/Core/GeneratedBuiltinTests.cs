using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests.GeneratedBuiltinModels
{
    public class ClassicChoice
    {
        [ArgRequired(IfNot = "Second | Third"), ArgCantBeCombinedWith("Second | Third")] public string? First { get; set; }
        [ArgShortcut("second-alias")] public string? Second { get; set; }
        public string? Third { get; set; }
    }
    public partial class GeneratedChoice
    {
        [ArgRequired(IfNot = "Second | Third"), ArgCantBeCombinedWith("Second | Third")] public partial string? First { get; set; }
        [ArgShortcut("second-alias")] public partial string? Second { get; set; }
        public partial string? Third { get; set; }
    }
    public class ClassicPresence
    {
        public bool Enabled { get; set; }
        [DefaultValue("default")] public string? Mode { get; set; }
        [ArgRequired(If = "(Enabled & Mode) | !Mode")] public int? Count { get; set; }
    }
    public partial class GeneratedPresence
    {
        public partial bool Enabled { get; set; }
        [DefaultValue("default")] public partial string? Mode { get; set; }
        [ArgRequired(If = "(Enabled & Mode) | !Mode")] public partial int? Count { get; set; }
    }
    public class ClassicCollections
    {
        [ArgRequired(IfNot = "Other"), ArgRange(1, 10), ArgRegex("[0-9]+", "digits only", Priority = -1)] public int[]? Values { get; set; }
        public string? Other { get; set; }
    }
    public partial class GeneratedCollections
    {
        [ArgRequired(IfNot = "Other"), ArgRange(1, 10), ArgRegex("[0-9]+", "digits only", Priority = -1)] public partial int[]? Values { get; set; }
        public partial string? Other { get; set; }
    }
    public class ClassicInvalid { public bool Enabled { get; set; } [ArgRequired(If = "Enabled | Unknown")] public string? Value { get; set; } }
    public partial class GeneratedInvalid { public partial bool Enabled { get; set; } [ArgRequired(If = "Enabled | Unknown")] public partial string? Value { get; set; } }
    public class ClassicMalformed { [ArgRequired(If = "Value || Value")] public string? Value { get; set; } }
    public partial class GeneratedMalformed { [ArgRequired(If = "Value || Value")] public partial string? Value { get; set; } }
    public class ClassicBoth { [ArgRequired(If = "Value", IfNot = "Value")] public string? Value { get; set; } }
    public partial class GeneratedBoth { [ArgRequired(If = "Value", IfNot = "Value")] public partial string? Value { get; set; } }
    public class ClassicInvalidExclusion { [ArgCantBeCombinedWith("Unknown")] public string? Value { get; set; } }
    public partial class GeneratedInvalidExclusion { [ArgCantBeCombinedWith("Unknown")] public partial string? Value { get; set; } }
    public class ClassicCase { [ArgEnforceCase, ArgShortcut("exact")] public string? Value { get; set; } [ArgRequired(If = "EXACT")] public string? Dependent { get; set; } }
    public partial class GeneratedCase { [ArgEnforceCase, ArgShortcut("exact")] public partial string? Value { get; set; } [ArgRequired(If = "EXACT")] public partial string? Dependent { get; set; } }
    public class ClassicMultiple { public bool Flag { get; set; } [ArgCantBeCombinedWith("Flag", AfterPopulatePropertiesPriority = 10), ArgCantBeCombinedWith("Missing", AfterPopulatePropertiesPriority = -10)] public string? Value { get; set; } }
    public partial class GeneratedMultiple { public partial bool Flag { get; set; } [ArgCantBeCombinedWith("Flag", AfterPopulatePropertiesPriority = 10), ArgCantBeCombinedWith("Missing", AfterPopulatePropertiesPriority = -10)] public partial string? Value { get; set; } }
    public class ClassicPaths { [ArgExistingFile] public string? File { get; set; } [ArgExistingDirectory] public string? Directory { get; set; } }
    public partial class GeneratedPaths { [ArgExistingFile] public partial string? File { get; set; } [ArgExistingDirectory] public partial string? Directory { get; set; } }
    public class ClassicHelp { [HelpHook(WriteHelp = false)] public bool Help { get; set; } [ArgRequired(IfNot = "Help")] public string? Name { get; set; } }
    public partial class GeneratedHelp { [HelpHook(WriteHelp = false)] public partial bool Help { get; set; } [ArgRequired(IfNot = "Help")] public partial string? Name { get; set; } }
    public class ClassicInvalidHelp { [HelpHook(WriteHelp = false)] public string? Help { get; set; } }
    public partial class GeneratedInvalidHelp { [HelpHook(WriteHelp = false)] public partial string? Help { get; set; } }
    public class EmptyUsage : IUsageTemplateProvider { public string GetTemplate() => ""; }
    public class ClassicHelpActions
    {
        [HelpHook(UsageTemplateProviderType = typeof(EmptyUsage))] public bool First { get; set; }
        [HelpHook(UsageTemplateProviderType = typeof(EmptyUsage))] public bool Second { get; set; }
        [ArgActionMethod] public void Run() { }
    }
    public partial class GeneratedHelpActions
    {
        [HelpHook(UsageTemplateProviderType = typeof(EmptyUsage))] public partial bool First { get; set; }
        [HelpHook(UsageTemplateProviderType = typeof(EmptyUsage))] public partial bool Second { get; set; }
        [ArgActionMethod] public void Run() { }
    }
    public class ClassicActions
    {
        public bool Enabled { get; set; }
        public string? Result;
        [ArgActionMethod] public void Run([ArgRequired(If = "Enabled"), ArgRegex("[a-z]+", "letters only")] string? name, [ArgCantBeCombinedWith("name")] string? other) => Result = name ?? other;
        [ArgActionMethod] public void Unused([ArgRequired(If = "Missing")] string? value) { }
    }
    public partial class GeneratedActions
    {
        public partial bool Enabled { get; set; }
        public string? Result;
        [ArgActionMethod] public void Run([ArgRequired(If = "Enabled"), ArgRegex("[a-z]+", "letters only")] string? name, [ArgCantBeCombinedWith("name")] string? other) => Result = name ?? other;
        [ArgActionMethod] public void Unused([ArgRequired(If = "Missing")] string? value) { }
    }
    public partial class Prompting { [ArgRequired(If = "Enabled", PromptIfMissing = true)] public partial string? Value { get; set; } public partial bool Enabled { get; set; } }
    public class CustomRange : ArgRange { public CustomRange() : base(1, 10) { } public override void Validate(string name, ref string arg) => arg = "7"; }
    public partial class CustomCollection { [CustomRange] public partial int[]? Values { get; set; } }
}

namespace ArgsTests
{
    using GeneratedBuiltinModels;

    [TestClass]
    public class GeneratedBuiltinTests
    {
        private static void Parity<TClassic, TGenerated>(string[] input) where TClassic : class where TGenerated : class
        {
            Assert.AreEqual(ArgumentPipelineKind.GeneratedDirectParser, GeneratedArgumentModelRegistry.GetPipelineKind<TGenerated>());
            object? classic = null;
            Exception? expected = null;
            try { classic = Args.Parse<TClassic>(input); } catch (Exception ex) { expected = ex; }
            if (expected != null)
            {
                var actual = Assert.Throws<Exception>(() => Args.Parse<TGenerated>(input));
                Assert.AreEqual(expected.GetType(), actual.GetType());
                Assert.AreEqual(expected.Message, actual.Message);
                Assert.AreEqual(expected.InnerException?.GetType(), actual.InnerException?.GetType());
                if (actual is ArgException argError) Assert.IsTrue(argError.Context.Definition.UsesGeneratedPipeline);
            }
            else
            {
                var generated = Args.Parse<TGenerated>(input);
                Assert.AreEqual(JsonSerializer.Serialize(classic, typeof(TClassic)), JsonSerializer.Serialize(generated));
            }
        }

        [TestMethod, DataRow(""), DataRow("-First a"), DataRow("-second-alias b"), DataRow("-Third c"), DataRow("-First a -Second b"), DataRow("-Second b -Third c")]
        public void ExclusiveChoiceMatchesClassic(string input) => Parity<ClassicChoice, GeneratedChoice>(Args.Convert(input));

        [TestMethod, DataRow(""), DataRow("-Enabled"), DataRow("-Enabled false"), DataRow("-Enabled false -Count 0"), DataRow("-Count 5"), DataRow("-Enabled -Count 12")]
        public void ConditionsUseRevivedPresenceIncludingFalseAndDefaults(string input) => Parity<ClassicPresence, GeneratedPresence>(Args.Convert(input));

        [TestMethod, DataRow(""), DataRow("-Other yes"), DataRow("-Values 4 5 99"), DataRow("-Values 11 5"), DataRow("-Values x"), DataRow("-Values 4,5"), DataRow("-Values")]
        public void BuiltinCollectionValidatorsPreserveRawFirstValueContract(string input) => Parity<ClassicCollections, GeneratedCollections>(Args.Convert(input));

        [TestMethod]
        public void InvalidExpressionsAndShortCircuitsMatchClassic()
        {
            Parity<ClassicInvalid, GeneratedInvalid>([]);
            Parity<ClassicInvalid, GeneratedInvalid>(["-Enabled", "-Value", "ok"]);
            Parity<ClassicInvalid, GeneratedInvalid>(["-Enabled"]);
            Parity<ClassicMalformed, GeneratedMalformed>([]);
            Parity<ClassicBoth, GeneratedBoth>([]);
            Parity<ClassicInvalidExclusion, GeneratedInvalidExclusion>([]);
            Parity<ClassicCase, GeneratedCase>(["-exact", "ok"]);
            Parity<ClassicMultiple, GeneratedMultiple>(["-Flag", "-Value", "ok"]);
            Parity<ClassicMultiple, GeneratedMultiple>([]);
        }

        [TestMethod]
        public void FileAndDirectoryValidatorsRetainNormalizationAndErrors()
        {
            var file = Path.GetTempFileName();
            try
            {
                Parity<ClassicPaths, GeneratedPaths>(["-File", Path.GetRelativePath(Environment.CurrentDirectory, file), "-Directory", "."]);
                Parity<ClassicPaths, GeneratedPaths>(["-File", file + ".missing"]);
                Parity<ClassicPaths, GeneratedPaths>(["-Directory", file + ".missing"]);
            }
            finally { File.Delete(file); }
        }

        [TestMethod]
        public void RepeatedHelpCancellationAndFailuresDoNotLeakState()
        {
            for (var i = 0; i < 3; i++)
            {
                Parity<ClassicHelp, GeneratedHelp>(["-Help"]);
                Parity<ClassicHelp, GeneratedHelp>([]);
                Parity<ClassicHelp, GeneratedHelp>(["-Help", "false"]);
                Parity<ClassicHelp, GeneratedHelp>(["-Name", "ok"]);
            }
            Parity<ClassicInvalidHelp, GeneratedInvalidHelp>([]);
            Parity<ClassicInvalidHelp, GeneratedInvalidHelp>(["-Help", "yes"]);
        }

        [TestMethod]
        public void OnlyTheCurrentHelpHookWritesUsageWhenDefinitionsAreReused()
        {
            foreach (var type in new[] { typeof(ClassicHelpActions), typeof(GeneratedHelpActions) })
            {
                var definition = new CommandLineArgumentsDefinition(type);
                var writes = new int[2];
                definition.Arguments[0].Metadata.OfType<HelpHook>().Single().UsageWritten += _ => writes[0]++;
                definition.Arguments[1].Metadata.OfType<HelpHook>().Single().UsageWritten += _ => writes[1]++;
                Assert.IsTrue(Args.ParseAction(definition, "Run", "-First").Cancelled);
                Assert.IsTrue(Args.ParseAction(definition, "Run", "-Second").Cancelled);
                Assert.IsFalse(Args.ParseAction(definition, "Run").Cancelled);
                CollectionAssert.AreEqual(new[] { 1, 1 }, writes);
            }
        }

        [TestMethod, DataRow("Run"), DataRow("Run -Enabled"), DataRow("Run -Enabled -name ok"), DataRow("Run -name 123"), DataRow("Run -name ok -other no")]
        public void ActionParametersUseGlobalConditionsAndIgnoreUnselectedActions(string input)
        {
            Assert.AreEqual(ArgumentPipelineKind.GeneratedDirectParser, GeneratedArgumentModelRegistry.GetPipelineKind<GeneratedActions>());
            var args = Args.Convert(input);
            ArgException? expected = null;
            string? value = null;
            try { value = Args.InvokeAction<ClassicActions>(args).Args.Result; } catch (ArgException ex) { expected = ex; }
            if (expected == null) Assert.AreEqual(value, Args.InvokeAction<GeneratedActions>(args).Args.Result);
            else
            {
                var actual = Assert.Throws<ArgException>(() => Args.InvokeAction<GeneratedActions>(args));
                Assert.AreEqual(expected.GetType(), actual.GetType());
                Assert.AreEqual(expected.Message, actual.Message);
            }
        }

        [TestMethod]
        public void SuppliedDefinitionEditsRefreshPreparedValidation()
        {
            var definition = new CommandLineArgumentsDefinition(typeof(GeneratedActions));
            var argument = definition.Actions.Single(a => a.DefaultAlias == "Run").Arguments[0];
            Args.InvokeAction(definition, "Run", "-Enabled", "-name", "ok");
            var required = argument.Metadata.OfType<ArgRequired>().Single();
            required.If = null;
            required.IfNot = "Enabled";
            Assert.ThrowsExactly<MissingArgException>(() => Args.InvokeAction(definition, "Run"));
            Args.InvokeAction(definition, "Run", "-Enabled");
            argument.Metadata.RemoveAll(m => m is ArgRegex);
            argument.Metadata.Add(new ArgRegex("[0-9]+", "numbers only"));
            Args.InvokeAction(definition, "Run", "-name", "123");
            Assert.ThrowsExactly<ValidationArgException>(() => Args.InvokeAction(definition, "Run", "-name", "ok"));
            var range = new ArgRange(1, 10) { Priority = -10 };
            argument.Metadata.Add(range);
            Assert.AreEqual("Expected a number for arg: name", Assert.ThrowsExactly<ValidationArgException>(() => Args.InvokeAction(definition, "Run", "-name", "bad")).Message);
            range.Priority = 10;
            Assert.AreEqual("numbers only: bad", Assert.ThrowsExactly<ValidationArgException>(() => Args.InvokeAction(definition, "Run", "-name", "bad")).Message);
        }

        [TestMethod]
        public void PreparedConditionsAreIsolatedAcrossThreadsAndRepeatedFailures()
        {
            Parallel.For(0, 8, _ =>
            {
                for (var i = 0; i < 20; i++)
                {
                    Assert.AreEqual("ok", Args.Parse<GeneratedChoice>("-First", "ok").First);
                    Assert.ThrowsExactly<MissingArgException>(() => Args.Parse<GeneratedChoice>());
                    Assert.IsNull(Args.Parse<GeneratedChoice>("-Second", "ok").First);
                    Assert.ThrowsExactly<UnexpectedArgException>(() => Args.Parse<GeneratedChoice>("-First", "a", "-Second", "b"));
                }
            });
        }

        [TestMethod]
        public void DisabledValidationSkipsValidatorsButStillRunsExclusionHooks()
        {
            foreach (var type in new[] { typeof(ClassicActions), typeof(GeneratedActions) })
            {
                var definition = new CommandLineArgumentsDefinition(type) { ValidationEnabled = false };
                Args.InvokeAction(definition, "Run", "-Enabled");
                Args.InvokeAction(definition, "Run", "-name", "123");
                Assert.ThrowsExactly<UnexpectedArgException>(() => Args.InvokeAction(definition, "Run", "-name", "123", "-other", "no"));
            }
        }

        [TestMethod]
        public void InteractiveRequirementsAndDerivedCollectionValidatorsKeepSharedLifecycle()
        {
            Assert.AreEqual(ArgumentPipelineKind.GeneratedSharedParser, GeneratedArgumentModelRegistry.GetPipelineKind<Prompting>());
            Assert.AreEqual(ArgumentPipelineKind.GeneratedSharedParser, GeneratedArgumentModelRegistry.GetPipelineKind<CustomCollection>());
            CollectionAssert.AreEqual(new[] { 7 }, Args.Parse<CustomCollection>("-Values", "1").Values);
        }
    }
}
