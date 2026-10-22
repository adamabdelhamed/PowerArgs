using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;

namespace ArgsTests;

// Keep one comparison contract as the classic suite gains generated counterparts.
// Scenario callbacks can include console/persistence setup and explicit expected-value checks.
internal static class PipelineParity
{
    internal static void AssertEquivalent<TClassic, TGenerated>(ArgumentPipelineKind expectedTier, Func<Type, string> exercise)
    {
        Assert.AreEqual(ArgumentPipelineKind.Reflection, GeneratedArgumentModelRegistry.GetPipelineKind(typeof(TClassic)));
        Assert.AreEqual(expectedTier, GeneratedArgumentModelRegistry.GetPipelineKind(typeof(TGenerated)));
        var classic = Capture(typeof(TClassic), exercise);
        var generated = Capture(typeof(TGenerated), exercise);
        Assert.AreEqual(classic, generated);
    }

    private static Outcome Capture(Type type, Func<Type, string> exercise)
    {
        try { return new Outcome(exercise(type), null, null, null); }
        catch (Exception error) when (error is ArgException || error is InvalidArgDefinitionException)
        {
            if (error is ArgException argumentError)
            {
                Assert.IsNotNull(argumentError.Context);
                Assert.AreEqual(GeneratedArgumentModelRegistry.IsGenerated(type), argumentError.Context.Definition.UsesGeneratedPipeline);
            }
            return new Outcome(null, error.GetType(), error.Message, error.InnerException?.GetType());
        }
    }

    private sealed record Outcome(string? Value, Type? ErrorType, string? Message, Type? InnerType);
}
