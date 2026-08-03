using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System.Threading.Tasks;

namespace ArgsTests.GeneratedInvocationModels
{
    public enum CopyMode
    {
        Fast,
        Safe,
    }

    public partial class MainArgs
    {
        [ArgRequired]
        public partial string StringVal { get; set; }

        [ArgIgnore]
        public partial bool MainWasCalled { get; set; }

        public void Main()
        {
            MainWasCalled = true;
        }
    }

    public partial class AsyncMainArgs
    {
        [ArgRequired]
        public partial string StringVal { get; set; }

        [ArgIgnore]
        public partial bool MainWasCalled { get; set; }

        public async Task Main()
        {
            await Task.Yield();
            MainWasCalled = true;
        }
    }

    public partial class ActionArgs
    {
        public partial bool GlobalFlag { get; set; }

        [ArgIgnore]
        public partial int InvocationCount { get; set; }

        [ArgActionMethod]
        [ArgShortcut("run")]
        [ArgDescription("Runs the generated action")]
        public void Execute()
        {
            InvocationCount++;
        }
    }

    [ArgEnforceCase]
    public partial class AsyncActionArgs
    {
        [ArgIgnore]
        public partial bool WasCalled { get; set; }

        [ArgActionMethod]
        public async Task Run()
        {
            await Task.Yield();
            WasCalled = true;
        }
    }

    public sealed class RunArgs
    {
        [ArgRequired]
        [ArgShortcut("n")]
        public string Name { get; set; }

        [ArgRange(1, 10)]
        public int Count { get; set; }

        [ArgPosition(1)]
        public string Tail { get; set; }
    }

    public partial class ParameterizedActionArgs
    {
        public partial bool GlobalFlag { get; set; }

        [ArgIgnore]
        public partial RunArgs Received { get; set; }

        [ArgActionMethod]
        [ArgShortcut("run")]
        public void Execute(RunArgs args)
        {
            Received = args;
        }
    }

    public partial class DirectParameterActionArgs
    {
        [ArgIgnore]
        public partial string Received { get; set; }

        [ArgIgnore]
        public partial int ReceivedCount { get; set; }

        [ArgIgnore]
        public partial CopyMode ReceivedMode { get; set; }

        [ArgActionMethod]
        public Task Copy([ArgRequired, ArgDescription("Source value")] string source, int count, CopyMode mode)
        {
            Received = source;
            ReceivedCount = count;
            ReceivedMode = mode;
            return Task.CompletedTask;
        }
    }
}

namespace ArgsTests
{
    [TestClass]
    [TestCategory(Categories.Core)]
    public class GeneratedInvocationTests
    {
        [TestMethod]
        public void GeneratedInvokeMainMatchesStrongAndWeakCoreTests()
        {
            var strong = Args.InvokeMain<GeneratedInvocationModels.MainArgs>("-s", "hello");
            Assert.IsTrue(strong.Args.MainWasCalled);
            Assert.AreEqual("hello", strong.Args.StringVal);
            Assert.IsTrue(strong.Definition.UsesGeneratedPipeline);

            var weak = Args.InvokeMain(typeof(GeneratedInvocationModels.MainArgs), "-s", "hello");
            Assert.IsTrue(((GeneratedInvocationModels.MainArgs)weak.Value).MainWasCalled);
            Assert.AreEqual("hello", ((GeneratedInvocationModels.MainArgs)weak.Value).StringVal);
        }

        [TestMethod]
        public async Task GeneratedInvokeMainSupportsTaskReturningMain()
        {
            var result = await Args.InvokeMainAsync<GeneratedInvocationModels.AsyncMainArgs>("-s", "hello");
            Assert.IsTrue(result.Args.MainWasCalled);
            Assert.AreEqual("hello", result.Args.StringVal);
        }

        [TestMethod]
        public async Task GeneratedInvokeActionMatchesActionShortcutCoreTest()
        {
            var result = Args.InvokeAction<GeneratedInvocationModels.ActionArgs>("run", "-g");

            Assert.IsTrue(result.Args.GlobalFlag);
            Assert.AreEqual(1, result.Args.InvocationCount);
            Assert.AreEqual("Runs the generated action", result.Definition.SpecifiedAction.Description);
            Assert.IsTrue(result.Definition.UsesGeneratedPipeline);

            result.Invoke();
            Assert.AreEqual(2, result.Args.InvocationCount);

            await result.InvokeAsync();
            Assert.AreEqual(3, result.Args.InvocationCount);
        }

        [TestMethod]
        public async Task GeneratedInvokeActionSupportsTaskReturningActions()
        {
            var result = await Args.InvokeActionAsync<GeneratedInvocationModels.AsyncActionArgs>("Run");
            Assert.IsTrue(result.Args.WasCalled);
            Assert.ThrowsExactly<UnknownActionArgException>(() => Args.InvokeAction<GeneratedInvocationModels.AsyncActionArgs>("run"));
        }

        [TestMethod]
        public async Task GeneratedInvokeActionSupportsActionArgumentModels()
        {
            var result = Args.InvokeAction<GeneratedInvocationModels.ParameterizedActionArgs>("run", "-g", "-n", "Ada", "-Count", "3");

            Assert.IsTrue(result.Definition.UsesGeneratedPipeline);
            Assert.IsTrue(result.Args.GlobalFlag);
            Assert.IsInstanceOfType<GeneratedInvocationModels.RunArgs>(result.ActionArgs);
            Assert.AreEqual("Ada", result.Args.Received.Name);
            Assert.AreEqual(3, result.Args.Received.Count);

            result.Args.Received = null;
            await result.InvokeAsync();
            Assert.AreSame(result.ActionArgs, result.Args.Received);
            Assert.ThrowsExactly<ValidationArgException>(() => Args.InvokeAction<GeneratedInvocationModels.ParameterizedActionArgs>("run", "-n", "Ada", "-Count", "11"));
        }

        [TestMethod]
        public void GeneratedInvokeActionSupportsDirectMethodParameters()
        {
            var result = Args.InvokeAction<GeneratedInvocationModels.DirectParameterActionArgs>("Copy", "-source", "input.txt", "-count", "4", "-mode", "Safe");

            Assert.IsTrue(result.Definition.UsesGeneratedPipeline);
            Assert.AreEqual("input.txt", result.Args.Received);
            Assert.AreEqual(4, result.Args.ReceivedCount);
            Assert.AreEqual(GeneratedInvocationModels.CopyMode.Safe, result.Args.ReceivedMode);
            Assert.AreEqual("Source value", result.Definition.SpecifiedAction.Arguments[0].Description);
            Assert.AreEqual(3, result.ActionParameters.Length);
            Assert.ThrowsExactly<MissingArgException>(() => Args.InvokeAction<GeneratedInvocationModels.DirectParameterActionArgs>("Copy", "-count", "4", "-mode", "Fast"));
        }

        [TestMethod]
        public void GeneratedInvokeActionHandlesLinuxStylePositionalPathsWithoutRuntimeFallback()
        {
            var result = Args.InvokeAction<GeneratedInvocationModels.ParameterizedActionArgs>("run", "/tmp", "-g", "-n", "Ada");

            Assert.IsTrue(result.Definition.UsesGeneratedPipeline);
            Assert.IsTrue(result.Args.GlobalFlag);
            Assert.AreEqual("Ada", result.Args.Received.Name);
            Assert.AreEqual("/tmp", result.Args.Received.Tail);
        }
    }
}
