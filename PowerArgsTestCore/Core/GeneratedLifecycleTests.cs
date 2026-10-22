using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests.LifecycleModels
{
    public class State { public readonly List<string> Events = new(); public Payload? Received; public int Number; }
    public class TraceHook : ArgHook
    {
        private int parses;
        public override void BeforeParse(HookContext context)
        {
            ((State)context.Args).Events.Add("parse");
            ((State)context.Args).Number = ++parses;
            if (context.CmdLineArgs[0] == "cancel") throw new ArgCancelProcessingException();
            if (context.CmdLineArgs[0] == "alias") context.CmdLineArgs = new[] { "Run" }.Concat(context.CmdLineArgs.Skip(1)).ToArray();
        }
        public override void BeforePopulateProperties(HookContext context) => ((State)context.Args).Events.Add("before populate");
        public override void AfterPopulateProperties(HookContext context) => ((State)context.Args).Events.Add("after populate");
        public override void BeforeInvoke(HookContext context) => ((State)context.Args).Events.Add("before invoke");
        public override void AfterInvoke(HookContext context) => ((State)context.Args).Events.Add("after invoke");
    }
    public class FirstValueValidator : ArgValidator
    {
        public override void Validate(string name, ref string arg)
        {
            if (arg == "pair") arg = "3,4";
            if (arg == "bad") throw new ValidationArgException("Invalid first value");
        }
    }
    public class Payload
    {
        [ArgPosition(1), FirstValueValidator] public int[] Values { get; set; } = new[] { 99 };
        [DefaultValue(7)] public int Count { get; set; }
        public string? Label { get; set; } = "initializer";
    }
    [TraceHook] public class ClassicAction : State { [ArgActionMethod] public void Run(Payload args) { Received = args; Events.Add("body"); } }
    [TraceHook] public partial class GeneratedAction : State { [ArgActionMethod] public void Run(Payload args) { Received = args; Events.Add("body"); } }
    [TraceHook] public partial class ParameterAction : State
    {
        [ArgActionMethod] public void Run([FirstValueValidator] int[] values, int count) { Number = count; Events.Add(string.Join(",", values)); }
        [ArgActionMethod] public void Empty() { Events.Add("body"); }
        [ArgActionMethod] public void Maybe(Payload? args) => Received = args;
    }
    [TraceHook] public class ClassicParameters : State { [ArgActionMethod] public void Run([FirstValueValidator] int[] values, int count) => Number = count; }
    public class ConditionalPayload
    {
        public bool Enabled { get; set; }
        [ArgRequired(If = "Enabled")] public string? Name { get; set; }
    }
    public class ClassicConditional { public ConditionalPayload? Received; [ArgActionMethod] public void Run(ConditionalPayload args) => Received = args; }
    public partial class GeneratedConditional { public ConditionalPayload? Received; [ArgActionMethod] public void Run(ConditionalPayload args) => Received = args; }

    public class TransformHook : ArgHook
    {
        public override void BeforePopulateProperty(HookContext context) { if (context.ArgumentValue == "alias") context.ArgumentValue = "changed"; }
    }
    public class ClassicGlobals { [TransformHook] public string? Label { get; set; } public string? Result; [ArgActionMethod] public void Run() => Result = Label; }
    public partial class GeneratedGlobals { [TransformHook] public partial string? Label { get; set; } public string? Result; [ArgActionMethod] public void Run() => Result = Label; }

    public partial class AsyncAction
    {
        public static TaskCompletionSource<bool>? Gate;
        public bool Finished;
        [ArgActionMethod] public async Task Run() { await Gate!.Task; Finished = true; }
        [ArgActionMethod] public void Fail() => throw new ArgException("action failure");
        [ArgActionMethod] public async Task FailAsync() { await Task.Yield(); throw new ArgException("async failure"); }
    }
    [TraceHook] public partial class SharedFailure : State { [ArgActionMethod] public void Fail() => throw new ArgException("failure"); }
    public partial class NullableReferences { public partial string? Name { get; set; } public partial List<string?>? Values { get; set; } }
    public class ExternalActions { [ArgActionMethod] public static void External() { } }
    [ArgActionType(typeof(ExternalActions))] public partial class ExternalScaffold { public partial bool Verbose { get; set; } [ArgActionMethod] public void Local() { } }
    public partial class LegacyScaffold { [ArgPosition(0), ArgRequired] public partial string Action { get; set; } [ArgActionMethod] public void Run() { } }
    public partial class CachedValidatorAction { public Payload? Received; [ArgActionMethod] public void Run(Payload args) => Received = args; }
}

namespace ArgsTests
{
    using LifecycleModels;
    [TestClass]
    public class GeneratedLifecycleTests
    {
        [TestMethod]
        public void SharedActionsPreserveHookOrderCollectionValidationAndDefaults()
        {
            Assert.AreEqual(ArgumentPipelineKind.GeneratedSharedParser, GeneratedArgumentModelRegistry.GetPipelineKind<GeneratedAction>());
            foreach (var input in new[] { new[] { "alias", "pair" }, new[] { "Run", "1", "2", "-Label", "name" }, new[] { "Run" } })
            {
                var expected = Args.InvokeAction<ClassicAction>(input).Args;
                var actual = Args.InvokeAction<GeneratedAction>(input).Args;
                CollectionAssert.AreEqual(expected.Events, actual.Events);
                CollectionAssert.AreEqual(expected.Received!.Values, actual.Received!.Values);
                Assert.AreEqual(expected.Received.Label, actual.Received.Label);
                Assert.AreEqual(expected.Received.Count, actual.Received.Count);
                Assert.AreEqual(1, actual.Number);
            }
            Assert.ThrowsExactly<ValidationArgException>(() => Args.InvokeAction<GeneratedAction>("Run", "bad"));
            Assert.AreEqual(7, Args.InvokeAction<GeneratedAction>("Run", "1").Args.Received!.Count);
            Assert.AreEqual(Args.InvokeAction<ClassicAction>("cancel").Cancelled, Args.InvokeAction<GeneratedAction>("cancel").Cancelled);
        }

        [TestMethod]
        public void ParseThenInvokeAndSuppliedDefinitionsKeepGeneratedActions()
        {
            var definition = new CommandLineArgumentsDefinition(typeof(GeneratedAction));
            var count = definition.Actions[0].Arguments.First(a => a.DefaultAlias == "Count");
            count.Metadata.Remove(count.Metadata.OfType<DefaultValueAttribute>().Single());
            count.Metadata.Add(new DefaultValueAttribute(12));
            var parsed = Args.ParseAction(definition, "Run", "1");
            Assert.IsTrue(parsed.Definition.UsesGeneratedPipeline);
            Assert.IsNotNull(parsed.ActionArgs);
            Assert.AreEqual(12, ((Payload)parsed.ActionArgs).Count);
            Assert.IsNull(((GeneratedAction)parsed.Value).Received);
            parsed.Invoke();
            Assert.AreSame(parsed.ActionArgs, ((GeneratedAction)parsed.Value).Received);
            var invoked = Args.InvokeAction(definition, "Run", "2");
            Assert.AreSame(definition, invoked.Definition);
            Assert.IsTrue(invoked.Definition.UsesGeneratedPipeline);
            Assert.AreEqual(12, ((GeneratedAction)invoked.Value).Received!.Count);
        }

        [TestMethod]
        public void SharedParametersAndParameterlessActionsUseGeneratedInvokers()
        {
            var result = Args.ParseAction<ParameterAction>("Run", "-values", "pair", "-count", "8");
            Assert.AreEqual(8, result.ActionParameters[1]);
            CollectionAssert.AreEqual(new[] { 3, 4 }, (int[])result.ActionParameters[0]);
            result.Invoke();
            Assert.AreEqual(8, result.Args.Number);
            var expected = Args.ParseAction<ClassicParameters>("Run", "pair");
            var omitted = Args.ParseAction<ParameterAction>("Run", "pair");
            Assert.AreEqual(expected.ActionParameters[1], omitted.ActionParameters[1]);
            omitted.Invoke();
            expected.Invoke();
            Assert.AreEqual(expected.Args.Number, omitted.Args.Number);
            Assert.IsNotNull(Args.InvokeAction<ParameterAction>("Maybe", "pair").Args.Received);
            CollectionAssert.AreEqual(new[] { "parse", "before populate", "after populate", "before invoke", "body", "after invoke" }, Args.InvokeAction<ParameterAction>("Empty").Args.Events);
        }

        [TestMethod]
        public void ConditionalActionRequirementsKeepSharedValidation()
        {
            Assert.AreEqual(ArgumentPipelineKind.GeneratedSharedParser, GeneratedArgumentModelRegistry.GetPipelineKind<GeneratedConditional>());
            Assert.ThrowsExactly<MissingArgException>(() => Args.InvokeAction<ClassicConditional>("Run", "-Enabled"));
            Assert.ThrowsExactly<MissingArgException>(() => Args.InvokeAction<GeneratedConditional>("Run", "-Enabled"));
            Assert.AreEqual("ok", Args.InvokeAction<GeneratedConditional>("Run", "-Enabled", "-Name", "ok").Args.Received!.Name);
            Assert.IsNull(Args.InvokeAction<GeneratedConditional>("Run").Args.Received!.Name);
        }

        [TestMethod]
        public void GlobalPropertyHooksRemainActiveWithActions()
        {
            Assert.AreEqual(ArgumentPipelineKind.GeneratedSharedParser, GeneratedArgumentModelRegistry.GetPipelineKind<GeneratedGlobals>());
            Assert.AreEqual(Args.InvokeAction<ClassicGlobals>("Run", "-Label", "alias").Args.Result, Args.InvokeAction<GeneratedGlobals>("Run", "-Label", "alias").Args.Result);
        }

        [TestMethod, DataRow(false), DataRow(true)]
        public async Task GeneratedAsyncInvocationReturnsBeforeTheActionCompletes(bool useDefinitionApi)
        {
            AsyncAction.Gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            // A dedicated thread prevents a regression from hanging the test runner; the gate is always released.
            var returned = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);
            var parsed = Args.ParseAction<AsyncAction>("Run");
            var thread = new Thread(() => { try { returned.SetResult(useDefinitionApi ? Args.InvokeActionAsync(new CommandLineArgumentsDefinition(typeof(AsyncAction)), "Run") : parsed.InvokeAsync()); } catch (Exception ex) { returned.SetException(ex); } }) { IsBackground = true };
            thread.Start();
            try
            {
                var pending = await returned.Task.WaitAsync(TimeSpan.FromSeconds(5));
                Assert.IsFalse(pending.IsCompleted);
                Assert.IsFalse(parsed.Args.Finished);
                AsyncAction.Gate.SetResult(true);
                await pending;
                Assert.IsTrue(useDefinitionApi ? ((AsyncAction)((Task<ArgAction>)pending).Result.Value).Finished : parsed.Args.Finished);
            }
            finally { AsyncAction.Gate.TrySetResult(true); thread.Join(TimeSpan.FromSeconds(5)); }
        }

        [TestMethod]
        public async Task GeneratedActionExceptionsCarryTheParseContext()
        {
            var parsed = Args.ParseAction<AsyncAction>("Fail");
            var error = Assert.ThrowsExactly<ArgException>(() => parsed.Invoke());
            Assert.AreSame(parsed.Definition, error.Context.Definition);
            var asyncParsed = Args.ParseAction<AsyncAction>("FailAsync");
            error = await Assert.ThrowsExactlyAsync<ArgException>(() => asyncParsed.InvokeAsync());
            Assert.AreSame(asyncParsed.Definition, error.Context.Definition);
            var shared = Args.ParseAction<SharedFailure>("Fail");
            error = Assert.ThrowsExactly<ArgException>(() => shared.Invoke());
            Assert.AreSame(shared.Definition, error.Context.Definition);
            Assert.IsFalse(shared.Args.Events.Contains("after invoke"));
        }

        [TestMethod]
        public void NullableReferenceAnnotationsCompileInGeneratedTypeMetadata()
        {
            Assert.AreEqual(ArgumentPipelineKind.GeneratedDirectParser, GeneratedArgumentModelRegistry.GetPipelineKind<NullableReferences>());
            Assert.IsNull(Args.Parse<NullableReferences>().Name);
            var result = Args.Parse<NullableReferences>("-Name", "hello", "-Values", "a", "b");
            Assert.AreEqual("hello", result.Name);
            CollectionAssert.AreEqual(new[] { "a", "b" }, result.Values);
        }

        [TestMethod]
        public void ReusedSharedDefinitionsResetArgumentsAfterSuccessAndFailure()
        {
            Assert.AreEqual(ArgumentPipelineKind.GeneratedSharedParser, GeneratedArgumentModelRegistry.GetPipelineKind<CachedValidatorAction>());
            CollectionAssert.AreEqual(new[] { 3, 4 }, Args.InvokeAction<CachedValidatorAction>("Run", "pair").Args.Received!.Values);
            Assert.ThrowsExactly<ValidationArgException>(() => Args.InvokeAction<CachedValidatorAction>("Run", "bad"));
            var empty = Args.InvokeAction<CachedValidatorAction>("Run").Args.Received!;
            Assert.IsNull(empty.Values);
            Assert.IsNull(empty.Label);
            Assert.AreEqual(7, empty.Count);
        }

        [TestMethod]
        public void ExternalAndLegacyActionDiscoveryKeepReflection()
        {
            Assert.AreEqual(ArgumentPipelineKind.Reflection, GeneratedArgumentModelRegistry.GetPipelineKind<ExternalScaffold>());
            Args.InvokeAction<ExternalScaffold>("External");
            Assert.AreEqual(ArgumentPipelineKind.Reflection, GeneratedArgumentModelRegistry.GetPipelineKind<LegacyScaffold>());
            Assert.AreEqual("Run", Args.InvokeAction<LegacyScaffold>("Run").Args.Action);
        }
    }
}
