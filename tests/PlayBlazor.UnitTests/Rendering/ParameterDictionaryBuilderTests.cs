using AwesomeAssertions;
using NUnit.Framework;
using PlayBlazor.Discovery;
using PlayBlazor.Rendering;
using PlayBlazor.State;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Rendering;

public class ParameterDictionaryBuilderTests
{
    [Test]
    public void Build_EmptyState_ReturnsEmptyDictionary()
    {
        var descriptor = new ReflectionCatalogProvider().Describe(typeof(BasicFixture));

        ParameterDictionaryBuilder.Build(descriptor, new PlaygroundState()).Should().BeEmpty();
    }

    [Test]
    public void Build_IncludesOnlyModifiedDrivableParameters()
    {
        var descriptor = new ReflectionCatalogProvider().Describe(typeof(BasicFixture));
        var state = new PlaygroundState();
        state.Set("Dense", true);
        state.Set("Label", "hello");
        state.Set("OnValueChanged", "ignored"); // Event — must be skipped
        state.Set("Endpoint", "ignored");       // Unsupported — must be skipped

        var parameters = ParameterDictionaryBuilder.Build(descriptor, state);

        parameters.Should().BeEquivalentTo(new Dictionary<string, object>
        {
            ["Dense"] = true,
            ["Label"] = "hello",
        });
    }

    [Test]
    public void Build_SkipsNullModifiedValues()
    {
        var descriptor = new ReflectionCatalogProvider().Describe(typeof(BasicFixture));
        var state = new PlaygroundState();
        state.Set("Label", null);

        ParameterDictionaryBuilder.Build(descriptor, state).Should().BeEmpty();
    }
}
