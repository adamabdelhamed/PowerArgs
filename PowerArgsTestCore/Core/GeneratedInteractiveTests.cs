using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests.InteractiveModels
{
    public interface IRootPrompt { string? Name { get; } }
    public class ClassicRoot : IRootPrompt
    {
        public bool Enabled { get; set; }
        [ArgRequired(If = "Enabled", PromptIfMissing = true), ArgRegex("[a-z]+", "letters only")] public string? Name { get; set; }
    }
    public partial class GeneratedRoot : IRootPrompt
    {
        public partial bool Enabled { get; set; }
        [ArgRequired(If = "Enabled", PromptIfMissing = true), ArgRegex("[a-z]+", "letters only")] public partial string? Name { get; set; }
    }
    public class PromptPayload
    {
        public bool Enabled { get; set; }
        [ArgRequired(If = "Enabled", PromptIfMissing = true), ArgRegex("[a-z]+", "letters only")] public string? Name { get; set; }
    }
    public class ActionState { public string? Received; }
    public class ClassicAction : ActionState { [ArgActionMethod] public void Run(PromptPayload args) => Received = args.Name; }
    public partial class GeneratedAction : ActionState { [ArgActionMethod] public void Run(PromptPayload args) => Received = args.Name; }
    public class ClassicParameters : ActionState { [ArgActionMethod] public void Run(bool enabled, [ArgRequired(If = "enabled", PromptIfMissing = true)] string? name) => Received = name; }
    public partial class GeneratedParameters : ActionState { [ArgActionMethod] public void Run(bool enabled, [ArgRequired(If = "enabled", PromptIfMissing = true)] string? name) => Received = name; }
    public interface ICount { int Count { get; } }
    public class ClassicCount : ICount { [ArgRequired(PromptIfMissing = true), ArgRange(1, 9)] public int Count { get; set; } }
    public partial class GeneratedCount : ICount { [ArgRequired(PromptIfMissing = true), ArgRange(1, 9)] public partial int Count { get; set; } }
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class AppendValue(string suffix) : ArgValidator
    {
        public override void Validate(string name, ref string arg) => arg += suffix;
    }
    public class ClassicOrdered : IRootPrompt { [AppendValue("a", Priority = 0), ArgRequired(PromptIfMissing = true), AppendValue("b", Priority = 200)] public string? Name { get; set; } }
    public partial class GeneratedOrdered : IRootPrompt { [AppendValue("a", Priority = 0), ArgRequired(PromptIfMissing = true), AppendValue("b", Priority = 200)] public partial string? Name { get; set; } }
    public class ClassicEmpty : IRootPrompt { [PromptIfEmpty(KeepAsking = true)] public string? Name { get; set; } }
    public partial class GeneratedEmpty : IRootPrompt { [PromptIfEmpty(KeepAsking = true)] public partial string? Name { get; set; } }
    public class PromptPersistence : IStickyArgPersistenceProvider
    {
        public static Dictionary<string, string> Values = new();
        public Dictionary<string, string> Load(string pathInfo) => new(Values);
        public void Save(Dictionary<string, string> values, string pathInfo) => Values = new(values);
    }
    [StickyArgPersistence(typeof(PromptPersistence))]
    public class ClassicSticky : IRootPrompt
    {
        public bool Enabled { get; set; }
        [ArgRequired(If = "Enabled", PromptIfMissing = true), StickyArg, ArgRegex("[a-z]+", "letters only")] public string? Name { get; set; }
    }
    [StickyArgPersistence(typeof(PromptPersistence))]
    public partial class GeneratedSticky : IRootPrompt
    {
        public partial bool Enabled { get; set; }
        [ArgRequired(If = "Enabled", PromptIfMissing = true), StickyArg, ArgRegex("[a-z]+", "letters only")] public partial string? Name { get; set; }
    }
    public class ReplState { public static readonly List<string?> Invoked = new(); }
    [TabCompletion("$", REPL = true, HistoryToSave = 0, EnableFileSystemTabCompletion = false)]
    public class ClassicRepl : ReplState, IRootPrompt
    {
        public bool Enabled { get; set; }
        [ArgRequired(If = "Enabled", PromptIfMissing = true)] public string? Name { get; set; }
        public void Main() => Invoked.Add(Name);
    }
    [TabCompletion("$", REPL = true, HistoryToSave = 0, EnableFileSystemTabCompletion = false)]
    public partial class GeneratedRepl : ReplState, IRootPrompt
    {
        public partial bool Enabled { get; set; }
        [ArgRequired(If = "Enabled", PromptIfMissing = true)] public partial string? Name { get; set; }
        public void Main() => Invoked.Add(Name);
    }
    public class UpdateState { public readonly List<string?> Values = new(); }
    public class ReplaceLate(string? value) : ArgHook
    {
        public override void AfterPopulateProperties(HookContext context) => context.PopulateCurrentArgument(value);
        public override void AfterPopulateProperty(HookContext context) => ((UpdateState)context.Args).Values.Add(context.RevivedProperty?.ToString());
    }
    public class ClassicUpdate : UpdateState
    {
        [ReplaceLate("after")] public string? Name { get; set; }
        [ReplaceLate(null)] public int Count { get; set; }
        [ReplaceLate(null)] public string? Cleared { get; set; }
    }
    public partial class GeneratedUpdate : UpdateState
    {
        [ReplaceLate("after")] public partial string? Name { get; set; }
        [ReplaceLate(null)] public partial int Count { get; set; }
        [ReplaceLate(null)] public partial string? Cleared { get; set; }
    }
}

namespace ArgsTests
{
    using InteractiveModels;

    // Reimplements ReadKey so a prompt regression fails instead of spinning on synthetic Enter.
    internal sealed class BoundedInteractiveConsole(string input, int readLimit = 256) : TestConsoleProvider(input), IConsoleProvider
    {
        internal int Reads { get; private set; }
        public new ConsoleKeyInfo ReadKey()
        {
            if (Reads++ >= readLimit) throw new InvalidOperationException("Interactive test exceeded its input budget");
            return base.ReadKey();
        }
        public new ConsoleKeyInfo ReadKey(bool intercept) => ReadKey();
    }

    [TestClass, DoNotParallelize]
    public class GeneratedInteractiveTests
    {
        private static void WithConsole(string input, Action<BoundedInteractiveConsole> exercise, int readLimit = 256)
        {
            var previous = ConsoleProvider.Current;
            var console = new BoundedInteractiveConsole(input, readLimit);
            ConsoleProvider.Current = console;
            try { exercise(console); }
            finally { ConsoleProvider.Current = previous; }
        }

        [TestMethod]
        public void ConditionalPromptUpdatesRootScaffold()
        {
            var previous = ConsoleProvider.Current;
            try
            {
                foreach (var type in new[] { typeof(ClassicRoot), typeof(GeneratedRoot) })
                {
                    ConsoleProvider.Current = new BoundedInteractiveConsole("alice");
                    Assert.AreEqual("alice", ((IRootPrompt)Args.Parse(type, "-Enabled")).Name, type.Name);
                }
            }
            finally { ConsoleProvider.Current = previous; }
        }

        [TestMethod]
        public void ConditionalPromptUpdatesActionPayloadInsteadOfParent()
        {
            var previous = ConsoleProvider.Current;
            try
            {
                foreach (var type in new[] { typeof(ClassicAction), typeof(GeneratedAction) })
                {
                    ConsoleProvider.Current = new BoundedInteractiveConsole("alice");
                    Assert.AreEqual("alice", ((ActionState)Args.InvokeAction(type, "Run", "-Enabled").Value).Received, type.Name);
                }
            }
            finally { ConsoleProvider.Current = previous; }
        }

        [TestMethod, DataRow("-Enabled", "alice", "alice", true), DataRow("-Enabled false", "alice", "alice", true), DataRow("-Enabled -Name bob", "", "bob", false), DataRow("", "", null, false)]
        public void RootPromptPresenceAndExplicitValuesMatchClassic(string args, string input, string? expected, bool shouldRead)
        {
            PipelineParity.AssertEquivalent<ClassicRoot, GeneratedRoot>(ArgumentPipelineKind.GeneratedSharedParser, type =>
            {
                string? value = null;
                WithConsole(input, console =>
                {
                    value = ((IRootPrompt)Args.Parse(type, Args.Convert(args))).Name;
                    Assert.AreEqual(expected, value);
                    Assert.AreEqual(shouldRead, console.Reads > 0);
                    Assert.AreEqual(value, Args.GetAmbientDefinition().Arguments.Single(a => a.DefaultAlias == "Name").RevivedValue);
                });
                return value ?? "<null>";
            });
        }

        [TestMethod, DataRow("123", false), DataRow("{escape}", true)]
        public void PromptErrorsRemainUserErrors(string input, bool escape)
        {
            PipelineParity.AssertEquivalent<ClassicRoot, GeneratedRoot>(ArgumentPipelineKind.GeneratedSharedParser, type =>
            {
                WithConsole(input, _ =>
                {
                    if (escape) Assert.ThrowsExactly<MissingArgException>(() => Args.Parse(type, "-Enabled"));
                    else Assert.ThrowsExactly<ValidationArgException>(() => Args.Parse(type, "-Enabled"));
                });
                // Capture a fresh attempt so the common harness also compares error details/context.
                WithConsole(input, _ => Args.Parse(type, "-Enabled"));
                return "unreachable";
            });
        }

        [TestMethod]
        public void PromptedActionParametersAndPayloadsCanBeParsedBeforeInvocation()
        {
            PipelineParity.AssertEquivalent<ClassicParameters, GeneratedParameters>(ArgumentPipelineKind.GeneratedSharedParser, type =>
            {
                WithConsole("alice", _ =>
                {
                    var action = Args.ParseAction(type, "Run", "-enabled");
                    Assert.IsNull(((ActionState)action.Value).Received);
                    Assert.AreEqual("alice", action.ActionParameters[1]);
                    action.Invoke();
                    Assert.AreEqual("alice", ((ActionState)action.Value).Received);
                });
                return "alice";
            });
            PipelineParity.AssertEquivalent<ClassicAction, GeneratedAction>(ArgumentPipelineKind.GeneratedSharedParser, type =>
            {
                WithConsole("alice", _ =>
                {
                    var action = Args.ParseAction(type, "Run", "-Enabled");
                    Assert.IsNull(((ActionState)action.Value).Received);
                    Assert.AreEqual("alice", ((PromptPayload)action.ActionArgs).Name);
                    action.Invoke();
                    Assert.AreEqual("alice", ((ActionState)action.Value).Received);
                });
                return "alice";
            });
        }

        [TestMethod]
        public void TypedPromptsAndPromptIfEmptyKeepTheirValidationLifecycle()
        {
            PipelineParity.AssertEquivalent<ClassicCount, GeneratedCount>(ArgumentPipelineKind.GeneratedSharedParser, type =>
            {
                WithConsole("5", _ => Assert.AreEqual(5, ((ICount)Args.Parse(type)).Count));
                WithConsole("20", _ => Assert.ThrowsExactly<ValidationArgException>(() => Args.Parse(type)));
                return "5";
            });
            PipelineParity.AssertEquivalent<ClassicEmpty, GeneratedEmpty>(ArgumentPipelineKind.GeneratedSharedParser, type =>
            {
                WithConsole("{enter}alice", _ => Assert.AreEqual("alice", ((IRootPrompt)Args.Parse(type, "-Name")).Name));
                return "alice";
            });
        }

        [TestMethod]
        public void PromptedValuesRunSkippedValidatorsOnceAndPreservePriority()
        {
            PipelineParity.AssertEquivalent<ClassicOrdered, GeneratedOrdered>(ArgumentPipelineKind.GeneratedSharedParser, type =>
            {
                WithConsole("v", _ => Assert.AreEqual("vab", ((IRootPrompt)Args.Parse(type)).Name));
                WithConsole("", _ => Assert.AreEqual("vab", ((IRootPrompt)Args.Parse(type, "-Name", "v")).Name), readLimit: 0);
                return "vab";
            });
        }

        [TestMethod]
        public void NonInteractiveDefinitionsNeverReadPromptInput()
        {
            PipelineParity.AssertEquivalent<ClassicRoot, GeneratedRoot>(ArgumentPipelineKind.GeneratedSharedParser, type =>
            {
                var definition = new CommandLineArgumentsDefinition(type) { IsNonInteractive = true };
                WithConsole("", _ => Assert.ThrowsExactly<MissingArgException>(() => Args.Parse(definition, "-Enabled")), readLimit: 0);
                definition.IsNonInteractive = false;
                WithConsole("alice", _ => Assert.AreEqual("alice", ((IRootPrompt)Args.Parse(definition, "-Enabled").Value).Name));
                definition.IsNonInteractive = true;
                WithConsole("", _ => Assert.IsNull(((IRootPrompt)Args.Parse(definition).Value).Name), readLimit: 0);
                return "reused";
            });
            PipelineParity.AssertEquivalent<ClassicEmpty, GeneratedEmpty>(ArgumentPipelineKind.GeneratedSharedParser, type =>
            {
                WithConsole("", _ => Assert.IsNull(((IRootPrompt)Args.Parse(new CommandLineArgumentsDefinition(type) { IsNonInteractive = true }).Value).Name), readLimit: 0);
                return "no input";
            });
        }

        [TestMethod]
        public void PromptedStickyValuesPersistAndFailedValidationDoesNotOverwriteThem()
        {
            try
            {
                PipelineParity.AssertEquivalent<ClassicSticky, GeneratedSticky>(ArgumentPipelineKind.GeneratedSharedParser, type =>
                {
                    PromptPersistence.Values.Clear();
                    WithConsole("alice", _ => Assert.AreEqual("alice", ((IRootPrompt)Args.Parse(type, "-Enabled")).Name));
                    Assert.AreEqual("alice", PromptPersistence.Values["Name"]);
                    WithConsole("", _ => Assert.AreEqual("alice", ((IRootPrompt)Args.Parse(type, "-Enabled")).Name), readLimit: 0);
                    WithConsole("", _ => Assert.ThrowsExactly<ValidationArgException>(() => Args.Parse(type, "-Name", "123")), readLimit: 0);
                    Assert.AreEqual("alice", PromptPersistence.Values["Name"]);
                    return "alice";
                });
            }
            finally { PromptPersistence.Values.Clear(); }
        }

        [TestMethod]
        public void ReplEscapeResumesAndGeneratedTabCompletionInvokesOnlyTheSuccessfulCommand()
        {
            try
            {
                PipelineParity.AssertEquivalent<ClassicRepl, GeneratedRepl>(ArgumentPipelineKind.GeneratedSharedParser, type =>
                {
                    ReplState.Invoked.Clear();
                    WithConsole("-Enabled{enter}{escape}-Na\t alice -En\t{enter}quit", _ =>
                    {
                        var result = Args.InvokeMain(type, "$");
                        Assert.AreEqual("alice", ((IRootPrompt)result.Value).Name);
                        CollectionAssert.AreEqual(new[] { "alice" }, ReplState.Invoked);
                    });
                    return "alice";
                });
            }
            finally { ReplState.Invoked.Clear(); }
        }

        [TestMethod]
        public void LatePopulationReplacesAndClearsValuesAndExposesRevivedValuesToHooks()
        {
            PipelineParity.AssertEquivalent<ClassicUpdate, GeneratedUpdate>(ArgumentPipelineKind.GeneratedSharedParser, type =>
            {
                var result = Args.Parse(type, "-Name", "before", "-Count", "7", "-Cleared", "before");
                Assert.AreEqual("after", type.GetProperty("Name")!.GetValue(result));
                Assert.AreEqual(0, type.GetProperty("Count")!.GetValue(result));
                Assert.IsNull(type.GetProperty("Cleared")!.GetValue(result));
                CollectionAssert.AreEqual(new string?[] { "before", "7", "before", "after", null, null }, ((UpdateState)result).Values);
                return "after,0,null";
            });
        }

        [TestMethod]
        public void LatePopulationRejectsMissingOrForeignArgumentContexts()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() => new ArgHook.HookContext().PopulateCurrentArgument("value"));
            var foreign = new CommandLineArgument(typeof(string), "foreign");
            var context = new ArgHook.HookContext { Definition = new CommandLineArgumentsDefinition(typeof(GeneratedRoot)), CurrentArgument = foreign };
            Assert.ThrowsExactly<InvalidOperationException>(() => context.PopulateCurrentArgument("value"));
            Assert.IsNull(foreign.RevivedValue);
        }
    }
}
