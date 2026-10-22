using System;
using System.Linq;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests.GeneratedExpansionModels
{
    public class ClassicOptional
    {
        public int? Count { get; set; }
        public bool? Enabled { get; set; }
        public DayOfWeek? Day { get; set; }
        public decimal Price { get; set; }
        public char? Letter { get; set; }
    }

    public partial class GeneratedOptional
    {
        public partial int? Count { get; set; }
        public partial bool? Enabled { get; set; }
        public partial DayOfWeek? Day { get; set; }
        public partial decimal Price { get; set; }
        public partial char? Letter { get; set; }
    }

    public class ClassicNumbers
    {
        public short Small { get; set; }
        public ushort UnsignedSmall { get; set; }
        public uint Unsigned { get; set; }
        public ulong Large { get; set; }
        public sbyte Tiny { get; set; }
    }

    public partial class GeneratedNumbers
    {
        public partial short Small { get; set; }
        public partial ushort UnsignedSmall { get; set; }
        public partial uint Unsigned { get; set; }
        public partial ulong Large { get; set; }
        public partial sbyte Tiny { get; set; }
    }

    public partial class HelperScaffold
    {
        public partial int Count { get; set; }
        [ArgIgnore] public partial int Result { get; set; }
        public void Main() => Result = Double(Count);
        private int Double(int value) => value * 2;
        public string Describe() => Count.ToString();
    }

    public partial class RenamedScaffold
    {
        [ArgShortcut(ArgShortcutPolicy.ShortcutsOnly), ArgShortcut("amount")]
        public partial int Count { get; set; }
    }

    public class PayloadBase
    {
        [ArgRequired, ArgShortcut("n")]
        public string Name { get; set; }
    }

    public class Payload : PayloadBase
    {
        public int Count { get; set; }
    }

    public class ClassicAction
    {
        [ArgIgnore] public Payload Received { get; set; }
        [ArgActionMethod] public void Run(Payload value) => Received = value;
    }

    public partial class GeneratedAction
    {
        [ArgIgnore] public partial Payload Received { get; set; }
        [ArgActionMethod] public void Run(Payload value) => Received = value;
    }

    public partial class OptionalAction
    {
        [ArgIgnore] public partial decimal? Received { get; set; }
        [ArgActionMethod] public void Run(decimal? amount) => Received = amount;
    }

    public class OverrideBase
    {
        public virtual int Count { get; set; }
    }

    public class OverridePayload : OverrideBase
    {
        [ArgRange(1, 10)]
        public override int Count { get; set; }
    }

    public partial class OverrideAction
    {
        [ArgIgnore] public partial int Received { get; set; }
        [ArgActionMethod] public void Run(OverridePayload args) => Received = args.Count;
    }
}

namespace ArgsTests
{
    using GeneratedExpansionModels;

    [TestClass]
    public class GeneratedExpansionTests
    {
        [TestMethod]
        public void OptionalScalarsMatchClassicValuesAndResetBetweenCalls()
        {
            var input = new[] { "-Count", "42", "-Enabled", "false", "-Day", "Friday", "-Price", "12", "-Letter", "x" };
            var classic = Args.Parse<ClassicOptional>(input);
            var generated = Args.Parse<GeneratedOptional>(input);
            Assert.AreEqual(classic.Count, generated.Count);
            Assert.AreEqual(classic.Enabled, generated.Enabled);
            Assert.AreEqual(classic.Day, generated.Day);
            Assert.AreEqual(classic.Price, generated.Price);
            Assert.AreEqual(classic.Letter, generated.Letter);
            Assert.IsTrue(GeneratedArgumentModelRegistry.IsGenerated<GeneratedOptional>());
            Assert.AreEqual(ArgumentPipelineKind.GeneratedDirectParser, GeneratedArgumentModelRegistry.GetPipelineKind<GeneratedOptional>());
            generated = Args.Parse<GeneratedOptional>();
            Assert.IsNull(generated.Count);
            Assert.IsNull(generated.Enabled);
            Assert.IsNull(generated.Day);
            Assert.AreEqual(0m, generated.Price);
        }

        [TestMethod]
        [DataRow("-Count", "MaxValue")]
        [DataRow("-Enabled", "")]
        [DataRow("-Day", "friday")]
        [DataRow("-Letter", "")]
        public void OptionalScalarsPreserveClassicSpecialValues(string key, string value)
        {
            var classic = Args.Parse<ClassicOptional>(key, value);
            var generated = Args.Parse<GeneratedOptional>(key, value);
            Assert.AreEqual(classic.Count, generated.Count);
            Assert.AreEqual(classic.Enabled, generated.Enabled);
            Assert.AreEqual(classic.Day, generated.Day);
            Assert.AreEqual(classic.Letter, generated.Letter);
        }

        [TestMethod]
        [DataRow("-Count", "invalid")]
        [DataRow("-Day", "not-a-day")]
        [DataRow("-Price", "invalid")]
        public void OptionalScalarsPreserveClassicErrors(string key, string value)
        {
            var classic = Assert.ThrowsExactly<ArgException>(() => Args.Parse<ClassicOptional>(key, value));
            var generated = Assert.ThrowsExactly<ArgException>(() => Args.Parse<GeneratedOptional>(key, value));
            Assert.AreEqual(classic.Message, generated.Message);
        }

        [TestMethod]
        public void HelperMethodsDoNotDisableGeneratedMain()
        {
            Assert.IsTrue(GeneratedArgumentModelRegistry.IsGenerated<HelperScaffold>());
            Assert.AreEqual(ArgumentPipelineKind.GeneratedDirectParser, GeneratedArgumentModelRegistry.GetPipelineKind<HelperScaffold>());
            var result = Args.InvokeMain<HelperScaffold>("-Count", "7");
            Assert.AreEqual(14, result.Args.Result);
        }

        [TestMethod]
        public void InheritedActionPropertiesMatchClassicParsingAndHelp()
        {
            var input = new[] { "Run", "-n", "Ada", "-Count", "3" };
            var classic = Args.InvokeAction<ClassicAction>(input);
            var generated = Args.InvokeAction<GeneratedAction>(input);
            Assert.IsTrue(GeneratedArgumentModelRegistry.IsGenerated<GeneratedAction>());
            Assert.AreEqual(classic.Args.Received.Name, generated.Args.Received.Name);
            Assert.AreEqual(classic.Args.Received.Count, generated.Args.Received.Count);
            Assert.IsTrue(ArgUsage.GenerateUsageFromTemplate<GeneratedAction>().ToString().Contains("Name"));
            var expected = new CommandLineArgumentsDefinition(typeof(ClassicAction)).Actions[0].Arguments;
            var actual = new CommandLineArgumentsDefinition(typeof(GeneratedAction)).Actions[0].Arguments;
            CollectionAssert.AreEqual(expected.Select(a => a.DefaultAlias).ToArray(), actual.Select(a => a.DefaultAlias).ToArray());
            Assert.ThrowsExactly<MissingArgException>(() => Args.InvokeAction<GeneratedAction>("Run"));
        }

        [TestMethod]
        public void PipelineKindDistinguishesSharedParserAndReflection()
        {
            Assert.AreEqual(ArgumentPipelineKind.Reflection, GeneratedArgumentModelRegistry.GetPipelineKind<ClassicOptional>());
            Assert.AreEqual(ArgumentPipelineKind.GeneratedSharedParser, GeneratedArgumentModelRegistry.GetPipelineKind<GeneratedReadmeModels.CollectionArgs>());
        }

        [TestMethod]
        public void OptionalActionParameterUsesDirectInvocation()
        {
            Assert.AreEqual(ArgumentPipelineKind.GeneratedDirectParser, GeneratedArgumentModelRegistry.GetPipelineKind<OptionalAction>());
            Assert.AreEqual(12m, Args.InvokeAction<OptionalAction>("Run", "12").Args.Received);
            Assert.IsNull(Args.InvokeAction<OptionalAction>("Run").Args.Received);
        }

        [TestMethod]
        public void OverriddenActionPropertyIsEmittedOnceWithItsValidation()
        {
            var definition = new CommandLineArgumentsDefinition(typeof(OverrideAction));
            Assert.AreEqual(1, definition.Actions[0].Arguments.Count);
            Assert.AreEqual(4, Args.InvokeAction<OverrideAction>("Run", "-Count", "4").Args.Received);
            Assert.ThrowsExactly<ValidationArgException>(() => Args.InvokeAction<OverrideAction>("Run", "-Count", "11"));
        }

        [TestMethod]
        public void AdditionalNumericTypesUseDirectParserWithClassicConversions()
        {
            var input = new[] { "-Small", "-12", "-UnsignedSmall", "65535", "-Unsigned", "4294967295", "-Large", "18446744073709551615", "-Tiny", "-128" };
            var classic = Args.Parse<ClassicNumbers>(input);
            var generated = Args.Parse<GeneratedNumbers>(input);
            Assert.AreEqual(ArgumentPipelineKind.GeneratedDirectParser, GeneratedArgumentModelRegistry.GetPipelineKind<GeneratedNumbers>());
            Assert.AreEqual(classic.Small, generated.Small);
            Assert.AreEqual(classic.UnsignedSmall, generated.UnsignedSmall);
            Assert.AreEqual(classic.Unsigned, generated.Unsigned);
            Assert.AreEqual(classic.Large, generated.Large);
            Assert.AreEqual(classic.Tiny, generated.Tiny);
            var expected = Assert.ThrowsExactly<ArgException>(() => Args.Parse<ClassicNumbers>("-Tiny", "128"));
            var actual = Assert.ThrowsExactly<ArgException>(() => Args.Parse<GeneratedNumbers>("-Tiny", "128"));
            Assert.AreEqual(expected.Message, actual.Message);
        }

        [TestMethod]
        public void CustomScalarReviversWinEvenAfterGeneratedParserHasBeenUsed()
        {
            Assert.AreEqual(7, Args.Parse<HelperScaffold>("-Count", "7").Count);
            var original = ArgRevivers.GetReviver(typeof(int));
            try
            {
                ArgRevivers.SetReviver<int>((name, value) => int.Parse(value) + 100);
                Assert.AreEqual(107, Args.Parse<HelperScaffold>("-Count", "7").Count);
                Assert.AreEqual(Args.Parse<ClassicOptional>("-Count", "7").Count, Args.Parse<GeneratedOptional>("-Count", "7").Count);
                ArgRevivers.SetReviver<int>((name, value) => name == "amount" ? 123 : -1);
                Assert.AreEqual(123, Args.Parse<RenamedScaffold>("-amount", "7").Count);
                ArgRevivers.SetReviver<int>((name, value) => throw new ValidationArgException("custom failure"));
                var error = Assert.ThrowsExactly<ValidationArgException>(() => Args.Parse<HelperScaffold>("-Count", "7"));
                Assert.AreEqual("custom failure", error.Message);
            }
            finally
            {
                ArgRevivers.SetReviver(typeof(int), original);
            }
            Assert.AreEqual(7, Args.Parse<HelperScaffold>("-Count", "7").Count);
        }

        [TestMethod]
        [DataRow("en-US", "12.5")]
        [DataRow("fr-FR", "12,5")]
        public void DecimalConversionPreservesCurrentCulture(string culture, string value)
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                Assert.AreEqual(Args.Parse<ClassicOptional>("-Price", value).Price, Args.Parse<GeneratedOptional>("-Price", value).Price);
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }
    }
}
