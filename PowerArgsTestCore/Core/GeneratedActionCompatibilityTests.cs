using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ArgsTests.GeneratedActionCompatibilityModels
{
    public sealed class NamedPayload
    {
        [ArgRequired, ArgShortcut("n"), ArgDescription("The payload name")]
        public string Name { get; set; }

        [ArgRange(1, 10)]
        public int Count { get; set; }
    }

    public partial class ParseOnlyScaffold
    {
        public partial bool Global { get; set; }

        [ArgIgnore]
        public partial int Invocations { get; set; }

        [ArgIgnore]
        public partial NamedPayload Received { get; set; }

        [ArgActionMethod, ArgShortcut("run"), ArgDescription("Runs a named payload")]
        public void Execute(NamedPayload args)
        {
            Received = args;
            Invocations++;
        }
    }

    public partial class StaticAmbientScaffold
    {
        public partial bool Global { get; set; }
        public static bool ObservedGlobal;
        public static string ObservedName;

        [ArgActionMethod]
        public static void Run(NamedPayload args)
        {
            ObservedGlobal = Args.GetAmbientArgs<StaticAmbientScaffold>().Global;
            ObservedName = args.Name;
        }
    }

    public partial class PositionalScaffold
    {
        [ArgIgnore]
        public partial string Summary { get; set; }

        [ArgActionMethod]
        public void Copy([ArgRequired, ArgShortcut("-s")] string source, int count, GeneratedInvocationModels.CopyMode mode)
        {
            Summary = source + ":" + count + ":" + mode;
        }
    }

    public sealed class ScalarPayload
    {
        public Guid Id { get; set; }
        public DateTime When { get; set; }
        public Uri Endpoint { get; set; }
        public IPAddress Address { get; set; }
        public ConsoleString Label { get; set; }
        public byte Retries { get; set; }
        public long Size { get; set; }
        public float Ratio { get; set; }
        public double Weight { get; set; }
    }

    public partial class ScalarScaffold
    {
        [ArgIgnore]
        public partial ScalarPayload Received { get; set; }

        [ArgActionMethod]
        public void Run(ScalarPayload args) => Received = args;
    }

    public partial class AsyncModelScaffold
    {
        [ArgIgnore]
        public partial NamedPayload Received { get; set; }

        [ArgActionMethod]
        public async Task Run(NamedPayload args)
        {
            await Task.Yield();
            Received = args;
        }
    }

    public partial class BooleanScaffold
    {
        [ArgIgnore]
        public partial bool Observed { get; set; }

        [ArgActionMethod]
        public void Toggle(bool force)
        {
            Observed = force;
        }
    }

    public partial class ShortcutsOnlyScaffold
    {
        [ArgIgnore]
        public partial bool Called { get; set; }

        [ArgActionMethod, ArgShortcut(ArgShortcutPolicy.ShortcutsOnly), ArgShortcut("go")]
        public void Execute()
        {
            Called = true;
        }
    }

    public partial class ConflictingActionScaffold
    {
        [ArgIgnore]
        public partial bool Marker { get; set; }

        [ArgActionMethod, ArgShortcut("second")]
        public void First() { }

        [ArgActionMethod]
        public void Second() { }
    }

    [ArgEnforceCase]
    public sealed class CasePayload
    {
        [ArgRequired]
        public string Name { get; set; }
    }

    [ArgEnforceCase]
    public partial class CaseScaffold
    {
        [ArgIgnore]
        public partial string Received { get; set; }

        [ArgActionMethod]
        public void Run(CasePayload args)
        {
            Received = args.Name;
        }
    }

    public partial class MultipleActionScaffold
    {
        [ArgIgnore]
        public partial string Selected { get; set; }

        [ArgActionMethod]
        public void Add(NamedPayload args) => Selected = "add:" + args.Name;

        [ArgActionMethod]
        public void Remove(string id) => Selected = "remove:" + id;
    }

    public partial class ThrowingScaffold
    {
        [ArgIgnore]
        public partial bool Marker { get; set; }

        [ArgActionMethod]
        public void Fail(string message) => throw new InvalidOperationException(message);

        [ArgActionMethod]
        public async Task FailAsync(string message)
        {
            await Task.Yield();
            throw new InvalidOperationException(message);
        }
    }

    public partial class OptionalParameterFallbackScaffold
    {
        [ArgIgnore]
        public partial string Received { get; set; }

        [ArgActionMethod]
        public void Run(string value = "default") => Received = value;
    }

    public partial class MissingMainScaffold
    {
        [ArgRequired]
        public partial string Value { get; set; }
    }

    public partial class BadMainParametersScaffold
    {
        [ArgRequired]
        public partial string Value { get; set; }
        public void Main(string value) { }
    }

    public partial class BadMainReturnScaffold
    {
        [ArgRequired]
        public partial string Value { get; set; }
        public object Main() => Value;
    }
}

namespace ArgsTests
{
    [TestClass]
    [TestCategory(Categories.Core)]
    public class GeneratedActionCompatibilityTests
    {
        [TestMethod]
        public async Task GeneratedParseActionDoesNotInvokeUntilRequested()
        {
            var result = Args.ParseAction<GeneratedActionCompatibilityModels.ParseOnlyScaffold>("run", "-g", "-n", "Ada", "-Count", "2");

            Assert.IsTrue(result.Definition.UsesGeneratedPipeline);
            Assert.IsTrue(result.Args.Global);
            Assert.AreEqual(0, result.Args.Invocations);
            Assert.AreEqual("Ada", ((GeneratedActionCompatibilityModels.NamedPayload)result.ActionArgs).Name);

            result.Invoke();
            await result.InvokeAsync();
            Assert.AreEqual(2, result.Args.Invocations);
            Assert.AreSame(result.ActionArgs, result.Args.Received);
        }

        [TestMethod]
        public async Task GeneratedParseActionAsyncSupportsDirectParametersWithoutInvoking()
        {
            var result = await Args.ParseActionAsync<GeneratedActionCompatibilityModels.PositionalScaffold>("Copy", "file.txt", "3", "Safe");

            Assert.IsTrue(result.Definition.UsesGeneratedPipeline);
            Assert.IsNull(result.Args.Summary);
            CollectionAssert.AreEqual(new object[] { "file.txt", 3, GeneratedInvocationModels.CopyMode.Safe }, result.ActionParameters);
            result.Invoke();
            Assert.AreEqual("file.txt:3:Safe", result.Args.Summary);
        }

        [TestMethod]
        public void GeneratedStaticActionCanReadAmbientGlobalArguments()
        {
            GeneratedActionCompatibilityModels.StaticAmbientScaffold.ObservedGlobal = false;
            GeneratedActionCompatibilityModels.StaticAmbientScaffold.ObservedName = null;

            var result = Args.InvokeAction<GeneratedActionCompatibilityModels.StaticAmbientScaffold>("Run", "-g", "-n", "Grace", "-Count", "1");

            Assert.IsTrue(result.Definition.UsesGeneratedPipeline);
            Assert.IsTrue(GeneratedActionCompatibilityModels.StaticAmbientScaffold.ObservedGlobal);
            Assert.AreEqual("Grace", GeneratedActionCompatibilityModels.StaticAmbientScaffold.ObservedName);
        }

        [TestMethod]
        public void GeneratedWeakTypeApisReturnActionArgumentsAndInvoke()
        {
            var parsed = Args.ParseAction(typeof(GeneratedActionCompatibilityModels.ParseOnlyScaffold), "run", "-n", "Ada", "-Count", "2");
            Assert.IsInstanceOfType<GeneratedActionCompatibilityModels.ParseOnlyScaffold>(parsed.Value);
            Assert.IsInstanceOfType<GeneratedActionCompatibilityModels.NamedPayload>(parsed.ActionArgs);
            Assert.AreEqual(0, ((GeneratedActionCompatibilityModels.ParseOnlyScaffold)parsed.Value).Invocations);

            var invoked = Args.InvokeAction(typeof(GeneratedActionCompatibilityModels.ParseOnlyScaffold), "run", "-n", "Ada", "-Count", "2");
            Assert.AreEqual(1, ((GeneratedActionCompatibilityModels.ParseOnlyScaffold)invoked.Value).Invocations);
        }

        [TestMethod]
        public async Task GeneratedDefinitionOverloadsSupportParseAndAsyncInvoke()
        {
            var definition = new CommandLineArgumentsDefinition(typeof(GeneratedActionCompatibilityModels.ParseOnlyScaffold));
            var parsed = Args.ParseAction(definition, "run", "-n", "Ada", "-Count", "2");
            Assert.AreSame(definition, parsed.Definition);
            Assert.AreEqual(0, ((GeneratedActionCompatibilityModels.ParseOnlyScaffold)parsed.Value).Invocations);

            var invoked = await Args.InvokeActionAsync(definition, "run", "-n", "Grace", "-Count", "3");
            Assert.AreSame(definition, invoked.Definition);
            Assert.AreEqual(1, ((GeneratedActionCompatibilityModels.ParseOnlyScaffold)invoked.Value).Invocations);
            Assert.AreEqual("Grace", ((GeneratedActionCompatibilityModels.NamedPayload)invoked.ActionArgs).Name);
        }

        [TestMethod]
        public void GeneratedDirectParametersSupportPositionalAndNamedForms()
        {
            var positional = Args.InvokeAction<GeneratedActionCompatibilityModels.PositionalScaffold>("Copy", "source.txt", "4", "Fast");
            Assert.AreEqual("source.txt:4:Fast", positional.Args.Summary);

            var named = Args.InvokeAction<GeneratedActionCompatibilityModels.PositionalScaffold>("Copy", "-source", "other.txt", "-count", "5", "-mode", "Safe");
            Assert.AreEqual("other.txt:5:Safe", named.Args.Summary);

            var shortcut = Args.InvokeAction<GeneratedActionCompatibilityModels.PositionalScaffold>("Copy", "-s", "short.txt", "-count", "6", "-mode", "Fast");
            Assert.AreEqual("short.txt:6:Fast", shortcut.Args.Summary);

            var negative = Args.InvokeAction<GeneratedActionCompatibilityModels.PositionalScaffold>("Copy", "source.txt", "-4", "Safe");
            Assert.AreEqual("source.txt:-4:Safe", negative.Args.Summary);

            var longForm = Args.InvokeAction<GeneratedActionCompatibilityModels.PositionalScaffold>("Copy", "--source=long.txt", "--count=7", "--mode=Fast");
            Assert.IsTrue(longForm.Definition.UsesGeneratedPipeline);
            Assert.AreEqual("long.txt:7:Fast", longForm.Args.Summary);
        }

        [TestMethod]
        public void GeneratedActionArgumentModelsSupportEveryDirectScalarReviver()
        {
            var id = Guid.NewGuid();
            var result = Args.InvokeAction<GeneratedActionCompatibilityModels.ScalarScaffold>("Run", "-Id", id.ToString(), "-When", "2026-08-03", "-Endpoint", "https://example.com/path", "-Address", "127.0.0.1", "-Label", "hello", "-Retries", "7", "-Size", "9000000000", "-Ratio", "1.5", "-Weight", "2.25");
            var value = result.Args.Received;

            Assert.IsTrue(result.Definition.UsesGeneratedPipeline);
            Assert.AreEqual(id, value.Id);
            Assert.AreEqual(new DateTime(2026, 8, 3), value.When.Date);
            Assert.AreEqual("https://example.com/path", value.Endpoint.AbsoluteUri.TrimEnd('/'));
            Assert.AreEqual(IPAddress.Loopback, value.Address);
            Assert.AreEqual("hello", value.Label.ToString());
            Assert.AreEqual((byte)7, value.Retries);
            Assert.AreEqual(9000000000L, value.Size);
            Assert.AreEqual(1.5f, value.Ratio);
            Assert.AreEqual(2.25d, value.Weight);
        }

        [TestMethod]
        public async Task GeneratedTaskActionSupportsArgumentModels()
        {
            var result = await Args.InvokeActionAsync<GeneratedActionCompatibilityModels.AsyncModelScaffold>("Run", "-n", "Ada", "-Count", "4");
            Assert.IsTrue(result.Definition.UsesGeneratedPipeline);
            Assert.AreEqual("Ada", result.Args.Received.Name);
            Assert.AreEqual(4, result.Args.Received.Count);
        }

        [TestMethod]
        public void GeneratedBooleanActionParameterSupportsImplicitAndExplicitValues()
        {
            Assert.IsTrue(Args.InvokeAction<GeneratedActionCompatibilityModels.BooleanScaffold>("Toggle", "-force").Args.Observed);
            Assert.IsFalse(Args.InvokeAction<GeneratedActionCompatibilityModels.BooleanScaffold>("Toggle", "-force", "false").Args.Observed);
            Assert.IsTrue(Args.InvokeAction<GeneratedActionCompatibilityModels.BooleanScaffold>("Toggle", "/force:true").Args.Observed);
        }

        [TestMethod]
        public void GeneratedActionHonorsShortcutsOnlyPolicy()
        {
            Assert.IsTrue(Args.InvokeAction<GeneratedActionCompatibilityModels.ShortcutsOnlyScaffold>("go").Args.Called);
            Assert.ThrowsExactly<UnknownActionArgException>(() => Args.InvokeAction<GeneratedActionCompatibilityModels.ShortcutsOnlyScaffold>("Execute"));
        }

        [TestMethod]
        public void GeneratedActionsReportMissingAndUnknownActionsLikeLegacyPipeline()
        {
            var missing = Assert.ThrowsExactly<MissingArgException>(() => Args.InvokeAction<GeneratedActionCompatibilityModels.ParseOnlyScaffold>());
            Assert.IsTrue(missing.Message.Contains("No action was specified"));
            Assert.IsTrue(missing.Context.Definition.UsesGeneratedPipeline);

            var unknown = Assert.ThrowsExactly<UnknownActionArgException>(() => Args.InvokeAction<GeneratedActionCompatibilityModels.ParseOnlyScaffold>("bogus"));
            Assert.IsTrue(unknown.Message.Contains("bogus"));
            Assert.IsTrue(unknown.Context.Definition.UsesGeneratedPipeline);
        }

        [TestMethod]
        public void GeneratedDefinitionRejectsConflictingActionAliases()
        {
            Assert.ThrowsExactly<InvalidArgDefinitionException>(() => Args.InvokeAction<GeneratedActionCompatibilityModels.ConflictingActionScaffold>("First"));
            Assert.ThrowsExactly<InvalidArgDefinitionException>(() => Args.InvokeAction<GeneratedActionCompatibilityModels.ConflictingActionScaffold>("second"));
        }

        [TestMethod]
        public void GeneratedActionsHonorCaseForActionsAndNestedArguments()
        {
            var valid = Args.InvokeAction<GeneratedActionCompatibilityModels.CaseScaffold>("Run", "-Name", "Ada");
            Assert.AreEqual("Ada", valid.Args.Received);

            Assert.ThrowsExactly<UnknownActionArgException>(() => Args.InvokeAction<GeneratedActionCompatibilityModels.CaseScaffold>("run", "-Name", "Ada"));
            Assert.ThrowsExactly<MissingArgException>(() => Args.InvokeAction<GeneratedActionCompatibilityModels.CaseScaffold>("Run", "-name", "Ada"));
        }

        [TestMethod]
        public void GeneratedActionDuplicateAndUnexpectedArgumentsUseLegacyErrors()
        {
            var duplicate = Assert.ThrowsExactly<DuplicateArgException>(() => Args.InvokeAction<GeneratedActionCompatibilityModels.ParseOnlyScaffold>("run", "-n", "Ada", "-n", "Grace", "-Count", "2"));
            Assert.IsTrue(duplicate.Context.Definition.UsesGeneratedPipeline);
            var unexpected = Assert.ThrowsExactly<UnexpectedArgException>(() => Args.InvokeAction<GeneratedActionCompatibilityModels.ParseOnlyScaffold>("run", "-n", "Ada", "-Count", "2", "-bogus", "value"));
            Assert.IsTrue(unexpected.Context.Definition.UsesGeneratedPipeline);
        }

        [TestMethod]
        public void GeneratedValidationFailuresCarryContextAndDefinitionsAreCleanedBetweenCalls()
        {
            var first = Args.InvokeAction<GeneratedActionCompatibilityModels.ParseOnlyScaffold>("run", "-n", "Ada", "-Count", "1");
            Assert.AreEqual(1, first.Args.Received.Count);
            var last = Args.InvokeAction<GeneratedActionCompatibilityModels.ParseOnlyScaffold>("run", "-n", "Ada", "-Count", "10");
            Assert.AreEqual(10, last.Args.Received.Count);

            var range = Assert.ThrowsExactly<ValidationArgException>(() => Args.InvokeAction<GeneratedActionCompatibilityModels.ParseOnlyScaffold>("run", "-n", "Ada", "-Count", "11"));
            Assert.IsNotNull(range.Context);
            Assert.AreEqual("Execute", range.Context.SpecifiedAction.DefaultAlias);

            var missing = Assert.ThrowsExactly<MissingArgException>(() => Args.InvokeAction<GeneratedActionCompatibilityModels.ParseOnlyScaffold>("run", "-Count", "2"));
            Assert.IsNotNull(missing.Context);
        }

        [TestMethod]
        public void GeneratedEnumFailuresMatchValidationBehavior()
        {
            var ex = Assert.ThrowsExactly<ValidationArgException>(() => Args.InvokeAction<GeneratedActionCompatibilityModels.PositionalScaffold>("Copy", "file.txt", "2", "Impossible"));
            Assert.IsTrue(ex.Message.Contains("Impossible"));
            Assert.IsTrue(ex.Message.Contains("Fast"));
            Assert.IsNotNull(ex.Context);
        }

        [TestMethod]
        public void GeneratedMultipleActionsSelectOnlyTheRequestedMethod()
        {
            var add = Args.InvokeAction<GeneratedActionCompatibilityModels.MultipleActionScaffold>("Add", "-n", "one", "-Count", "1");
            Assert.AreEqual("add:one", add.Args.Selected);
            Assert.AreEqual("Add", add.Definition.SpecifiedAction.DefaultAlias);

            var remove = Args.InvokeAction<GeneratedActionCompatibilityModels.MultipleActionScaffold>("Remove", "-id", "two");
            Assert.AreEqual("remove:two", remove.Args.Selected);
            Assert.AreEqual("Remove", remove.Definition.SpecifiedAction.DefaultAlias);
        }

        [TestMethod]
        public async Task GeneratedUserExceptionsAreNotWrapped()
        {
            var sync = Assert.ThrowsExactly<InvalidOperationException>(() => Args.InvokeAction<GeneratedActionCompatibilityModels.ThrowingScaffold>("Fail", "-message", "sync failure"));
            Assert.AreEqual("sync failure", sync.Message);

            var asyncException = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => Args.InvokeActionAsync<GeneratedActionCompatibilityModels.ThrowingScaffold>("FailAsync", "-message", "async failure"));
            Assert.AreEqual("async failure", asyncException.Message);
        }

        [TestMethod]
        public void UnsupportedOptionalParameterActionUsesReflectionFallback()
        {
            var result = Args.InvokeAction<GeneratedActionCompatibilityModels.OptionalParameterFallbackScaffold>("Run");
            Assert.IsFalse(result.Definition.UsesGeneratedPipeline);
            Assert.IsNull(result.Args.Received);
        }

        [TestMethod]
        public void GeneratedActionMetadataFeedsUsageAndDefinitionApis()
        {
            var definition = new CommandLineArgumentsDefinition(typeof(GeneratedActionCompatibilityModels.ParseOnlyScaffold));
            var action = definition.FindMatchingAction("run");

            Assert.IsTrue(definition.UsesGeneratedPipeline);
            Assert.AreEqual("Runs a named payload", action.Description);
            Assert.AreEqual(2, action.Arguments.Count);
            Assert.AreEqual("The payload name", action.FindMatchingArgument("n").Description);
            Assert.IsTrue(action.FindMatchingArgument("n").IsRequired);
            Assert.IsTrue(ArgUsage.GenerateUsageFromTemplate(typeof(GeneratedActionCompatibilityModels.ParseOnlyScaffold)).Contains("The payload name"));
        }

        [TestMethod]
        public void GeneratedActionsAreReliableAcrossParallelInvocations()
        {
            var results = new string[64];
            Parallel.For(0, results.Length, i =>
            {
                var result = Args.InvokeAction<GeneratedActionCompatibilityModels.ParseOnlyScaffold>("run", "-n", "item" + i, "-Count", ((i % 10) + 1).ToString());
                results[i] = result.Args.Received.Name;
            });

            for (var i = 0; i < results.Length; i++) Assert.AreEqual("item" + i, results[i]);
        }

        [TestMethod]
        public void GeneratedMainInvalidShapesRetainLegacyValidationMessages()
        {
            var missing = Assert.ThrowsExactly<InvalidArgDefinitionException>(() => Args.InvokeMain<GeneratedActionCompatibilityModels.MissingMainScaffold>("-Value", "x"));
            Assert.IsTrue(missing.Message.Contains("no Main() method"));

            var parameters = Assert.ThrowsExactly<InvalidArgDefinitionException>(() => Args.InvokeMain<GeneratedActionCompatibilityModels.BadMainParametersScaffold>("-Value", "x"));
            Assert.IsTrue(parameters.Message.Contains("must not take any parameters"));

            var returnType = Assert.ThrowsExactly<InvalidArgDefinitionException>(() => Args.InvokeMain<GeneratedActionCompatibilityModels.BadMainReturnScaffold>("-Value", "x"));
            Assert.IsTrue(returnType.Message.Contains("must return void"));
        }
    }
}
