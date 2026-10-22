using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests.ShapeModels
{
    public class ActionBase
    {
        public int Result;
        [ArgActionMethod, ArgShortcut("go"), ArgDescription("Inherited action")]
        public virtual void Run([ArgRequired] int count) => Result = count;
        [ArgActionMethod] public Task Async(int count) { Result = count; return Task.CompletedTask; }
        [ArgActionMethod] public static void BaseStatic() { }
        [ArgActionMethod] protected void Protected() { }
    }
    public class ClassicInherited : ActionBase { [ArgActionMethod] public void Local() => Result = 8; }
    public partial class GeneratedInherited : ActionBase { [ArgActionMethod] public void Local() => Result = 8; }
    public partial class GeneratedWithProperty : ActionBase { public partial bool Verbose { get; set; } }
    public class ClassicOverride : ActionBase { [ArgShortcut("twice"), ArgDescription("Override")] public override void Run([ArgRequired] int count) => Result = count * 2; }
    public partial class GeneratedOverride : ActionBase { [ArgShortcut("twice"), ArgDescription("Override")] public override void Run([ArgRequired] int count) => Result = count * 2; }
    public class ClassicHidden : ActionBase { public new void Run(int count) => Result = -count; }
    public partial class GeneratedHidden : ActionBase { public new void Run(int count) => Result = -count; }

    public partial class ActionOnly
    {
        public int Result;
        [ArgActionMethod] public void Run(int count) => Result = count;
        [ArgActionMethod] public void Empty() => Result = 5;
        [ArgActionMethod] private void Private() { }
    }
    public class MainBase
    {
        public int Count { get; set; }
        public int Result;
        public virtual void Main() => Result = Count;
    }
    public class ClassicMain : MainBase { }
    public partial class GeneratedMain : MainBase { }
    public partial class GeneratedMainOverride : MainBase { public override void Main() => Result = Count * 2; }
    public partial class MainOnly { public bool Invoked; public void Main() => Invoked = true; }
    public partial class AsyncMainOnly { public bool Invoked; public async Task Main() { await Task.Yield(); Invoked = true; } }
    public partial class SplitAction { }
    public partial class SplitAction { [ArgActionMethod] public void Run() { } }
    public class EmptyPayload { public bool Initialized = true; }
    public partial class EmptyModelAction { public EmptyPayload? Received; [ArgActionMethod] public void Run(EmptyPayload args) => Received = args; }

    [ArgIgnoreCase(false), ArgDescription("Base commands")]
    public class CaseBase { public int Result; [ArgActionMethod] public void Run(CasePayload args) => Result = args.Count; }
    [ArgIgnoreCase(false)] public class CasePayload { [ArgPosition(1)] public int Count { get; set; } }
    public class ClassicCase : CaseBase { }
    public partial class GeneratedCase : CaseBase { }
    public partial class OrdinaryProperties { public int Count { get; set; } [ArgActionMethod] public void Run() { } }
    public partial class GenericAction<T> { [ArgActionMethod] public void Run() { } }
    public partial class Unrelated { public void Helper() { } }
    public partial class UnsupportedBase : ActionBase { [ArgActionMethod] public int Bad() => 1; }
    public partial class DerivedUnsupported : UnsupportedBase { public partial bool Flag { get; set; } }
    public partial class UnsupportedMainAction { [ArgActionMethod] public int Main() => 1; }

    public partial class Container
    {
        public partial class Commands { public int Result; [ArgActionMethod] public void Run() => Result = 11; }
        private partial class PrivateCommands { public void Main() { } }
        public static ArgumentPipelineKind PrivatePipeline => GeneratedArgumentModelRegistry.GetPipelineKind<PrivateCommands>();
    }
    public class PopulateHook : ArgHook
    {
        public override void AfterPopulateProperties(HookContext context) => ((HookedBase)context.Args).Result = 9;
    }
    [PopulateHook]
    public class HookedBase { public int Result; public void Main() => Result++; }
    public class ClassicHooked : HookedBase { }
    public partial class GeneratedHooked : HookedBase { }
}

namespace ArgsTests
{
    using ShapeModels;

    [TestClass]
    public class GeneratedShapeTests
    {
        [TestMethod]
        public void ActionOnlyScaffoldsNeedNoDummyProperty()
        {
            Assert.AreEqual(ArgumentPipelineKind.GeneratedDirectParser, GeneratedArgumentModelRegistry.GetPipelineKind<ActionOnly>());
            Assert.AreEqual(42, Args.InvokeAction<ActionOnly>("Run", "42").Args.Result);
            Assert.AreEqual(5, Args.InvokeAction<ActionOnly>("Empty").Args.Result);
            Assert.ThrowsExactly<MissingArgException>(() => Args.InvokeAction<ActionOnly>());
            Assert.ThrowsExactly<UnknownActionArgException>(() => Args.InvokeAction<ActionOnly>("missing"));
            Assert.ThrowsExactly<UnexpectedArgException>(() => Args.InvokeAction<ActionOnly>("Empty", "unexpected"));
            Assert.AreEqual(ArgumentPipelineKind.GeneratedDirectParser, GeneratedArgumentModelRegistry.GetPipelineKind<SplitAction>());
            Args.InvokeAction<SplitAction>("Run");
            Assert.AreEqual(2, new CommandLineArgumentsDefinition(typeof(ActionOnly)).Actions.Count);
            Assert.AreEqual(11, Args.InvokeAction<Container.Commands>("Run").Args.Result);
            Assert.AreEqual(ArgumentPipelineKind.GeneratedDirectParser, GeneratedArgumentModelRegistry.GetPipelineKind<Container.Commands>());
        }

        [TestMethod]
        public void InheritedActionsMatchClassicDefinitionsAndInvocation()
        {
            var classic = new CommandLineArgumentsDefinition(typeof(ClassicInherited));
            var generated = new CommandLineArgumentsDefinition(typeof(GeneratedInherited));
            CollectionAssert.AreEqual(classic.Actions.Select(a => a.DefaultAlias).ToArray(), generated.Actions.Select(a => a.DefaultAlias).ToArray());
            for (var i = 0; i < classic.Actions.Count; i++)
            {
                CollectionAssert.AreEqual(classic.Actions[i].Aliases.ToArray(), generated.Actions[i].Aliases.ToArray());
                Assert.AreEqual(classic.Actions[i].Description, generated.Actions[i].Description);
            }
            Assert.AreEqual(ArgumentPipelineKind.GeneratedDirectParser, GeneratedArgumentModelRegistry.GetPipelineKind<GeneratedInherited>());
            foreach (var input in new[] { new[] { "go", "7" }, new[] { "Async", "9" }, new[] { "Local" } })
                Assert.AreEqual(Args.InvokeAction<ClassicInherited>(input).Args.Result, Args.InvokeAction<GeneratedInherited>(input).Args.Result);
            Assert.AreEqual(4, Args.InvokeAction<GeneratedWithProperty>("go", "4", "-Verbose").Args.Result);
            Assert.ThrowsExactly<MissingArgException>(() => Args.InvokeAction<GeneratedInherited>("Run"));
            Assert.ThrowsExactly<UnknownActionArgException>(() => Args.InvokeAction<GeneratedInherited>("BaseStatic"));
            Assert.ThrowsExactly<UnknownActionArgException>(() => Args.InvokeAction<GeneratedInherited>("Protected"));
            StringAssert.Contains(ArgUsage.GenerateUsageFromTemplate<GeneratedInherited>().ToString(), "Inherited action");
        }

        [TestMethod]
        public void OverridesInheritMetadataAndHiddenMethodsRetainTheBaseAction()
        {
            Assert.AreEqual(Args.InvokeAction<ClassicOverride>("go", "6").Args.Result, Args.InvokeAction<GeneratedOverride>("go", "6").Args.Result);
            Assert.AreEqual(1, new CommandLineArgumentsDefinition(typeof(GeneratedOverride)).Actions.Count(a => a.DefaultAlias == "Run"));
            Assert.AreEqual(12, Args.InvokeAction<GeneratedOverride>("twice", "6").Args.Result);
            var expected = new CommandLineArgumentsDefinition(typeof(ClassicOverride)).FindMatchingAction("go");
            var actual = new CommandLineArgumentsDefinition(typeof(GeneratedOverride)).FindMatchingAction("go");
            CollectionAssert.AreEqual(expected.Aliases.ToArray(), actual.Aliases.ToArray());
            Assert.AreEqual(expected.Description, actual.Description);
            Assert.AreEqual(2, Args.InvokeAction<ClassicHidden>("Run", "2").Args.Result);
            Assert.AreEqual(2, Args.InvokeAction<GeneratedHidden>("Run", "2").Args.Result);
        }

        [TestMethod]
        public void InheritedCaseAndUsageMetadataMatchClassic()
        {
            Assert.AreEqual(Args.InvokeAction<ClassicCase>("Run", "-Count", "3").Args.Result, Args.InvokeAction<GeneratedCase>("Run", "-Count", "3").Args.Result);
            Assert.ThrowsExactly<UnknownActionArgException>(() => Args.InvokeAction<ClassicCase>("run", "3"));
            Assert.ThrowsExactly<UnknownActionArgException>(() => Args.InvokeAction<GeneratedCase>("run", "3"));
            Assert.ThrowsExactly<UnexpectedArgException>(() => Args.InvokeAction<GeneratedCase>("Run", "-count", "3"));
            Assert.AreEqual(new CommandLineArgumentsDefinition(typeof(ClassicCase)).Description, new CommandLineArgumentsDefinition(typeof(GeneratedCase)).Description);
        }

        [TestMethod]
        public void MainOnlyAndInheritedMainUseGeneratedInvocation()
        {
            Assert.IsTrue(Args.InvokeMain<MainOnly>().Args.Invoked);
            Assert.IsTrue(Args.InvokeMain<AsyncMainOnly>().Args.Invoked);
            Assert.AreEqual(ArgumentPipelineKind.GeneratedDirectParser, GeneratedArgumentModelRegistry.GetPipelineKind<GeneratedMain>());
            Assert.AreEqual(Args.InvokeMain<ClassicMain>("-Count", "4").Args.Result, Args.InvokeMain<GeneratedMain>("-Count", "4").Args.Result);
            Assert.AreEqual(8, Args.InvokeMain<GeneratedMainOverride>("-Count", "4").Args.Result);
        }

        [TestMethod]
        public void EmptyActionObjectsAreConstructed()
        {
            var parsed = Args.ParseAction<EmptyModelAction>("Run");
            Assert.IsNotNull(parsed.ActionArgs);
            Assert.IsTrue(((EmptyPayload)parsed.ActionArgs).Initialized);
            parsed.Invoke();
            Assert.AreSame(parsed.ActionArgs, parsed.Args.Received);
        }

        [TestMethod]
        public void IneligibleAndUnrelatedPartialClassesKeepReflection()
        {
            Assert.AreEqual(ArgumentPipelineKind.Reflection, GeneratedArgumentModelRegistry.GetPipelineKind<OrdinaryProperties>());
            Assert.AreEqual(ArgumentPipelineKind.Reflection, GeneratedArgumentModelRegistry.GetPipelineKind<GenericAction<int>>());
            Assert.AreEqual(ArgumentPipelineKind.Reflection, GeneratedArgumentModelRegistry.GetPipelineKind<Unrelated>());
            Assert.AreEqual(ArgumentPipelineKind.Reflection, GeneratedArgumentModelRegistry.GetPipelineKind<DerivedUnsupported>());
            Assert.AreEqual(ArgumentPipelineKind.Reflection, GeneratedArgumentModelRegistry.GetPipelineKind<UnsupportedMainAction>());
            Assert.AreEqual(ArgumentPipelineKind.Reflection, Container.PrivatePipeline);
        }

        [TestMethod]
        public void InheritedHooksSelectAndRunTheSharedLifecycle()
        {
            Assert.AreEqual(ArgumentPipelineKind.GeneratedSharedParser, GeneratedArgumentModelRegistry.GetPipelineKind<GeneratedHooked>());
            Assert.AreEqual(10, Args.InvokeMain<ClassicHooked>().Args.Result);
            Assert.AreEqual(10, Args.InvokeMain<GeneratedHooked>().Args.Result);
        }
    }
}
