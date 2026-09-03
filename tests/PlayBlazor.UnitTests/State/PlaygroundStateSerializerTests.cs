using AwesomeAssertions;
using NUnit.Framework;
using PlayBlazor.Discovery;
using PlayBlazor.Model;
using PlayBlazor.Rendering;
using PlayBlazor.State;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.State;

public class PlaygroundStateSerializerTests
{
    private static ComponentDescriptor Describe()
        => new ReflectionCatalogProvider().Describe(typeof(BasicFixture));

    [Test]
    public void EncodeDecode_RoundTripsValuesAndEnvironment()
    {
        var descriptor = Describe();
        var state = new PlaygroundState();
        state.Set("Dense", true);
        state.Set("Size", FixtureSize.Large);
        state.Set("Count", 7);
        state.Set("Ratio", 2.75);
        state.Set("Label", "hello world");
        state.Set("ChildContent", "slot text");
        var environment = new PlaygroundEnvironment { Dark = true, ViewportWidth = 360 };

        var encoded = PlaygroundStateSerializer.Encode(descriptor, state, environment);
        var restoredState = new PlaygroundState();
        var restoredEnvironment = new PlaygroundEnvironment();
        PlaygroundStateSerializer.Decode(encoded, descriptor, restoredState, restoredEnvironment);

        restoredState.GetValue(descriptor.Parameters.Single(p => p.Name == "Dense")).Should().Be(true);
        restoredState.GetValue(descriptor.Parameters.Single(p => p.Name == "Size")).Should().Be(FixtureSize.Large);
        restoredState.GetValue(descriptor.Parameters.Single(p => p.Name == "Count")).Should().Be(7);
        restoredState.GetValue(descriptor.Parameters.Single(p => p.Name == "Ratio")).Should().Be(2.75);
        restoredState.GetValue(descriptor.Parameters.Single(p => p.Name == "Label")).Should().Be("hello world");
        restoredState.GetValue(descriptor.Parameters.Single(p => p.Name == "ChildContent")).Should().Be("slot text");
        restoredEnvironment.Dark.Should().BeTrue();
        restoredEnvironment.ViewportWidth.Should().Be(360);
    }

    [Test]
    public void Encode_IsUrlSafe()
    {
        var state = new PlaygroundState();
        state.Set("Label", "a?b&c=d/e+f");

        var encoded = PlaygroundStateSerializer.Encode(Describe(), state, new PlaygroundEnvironment());

        encoded.Should().MatchRegex("^[A-Za-z0-9_-]+$");
    }

    [Test]
    public void Decode_Garbage_ChangesNothing()
    {
        var state = new PlaygroundState();
        var environment = new PlaygroundEnvironment();

        PlaygroundStateSerializer.Decode("not base64 at all!!!", Describe(), state, environment);

        state.ModifiedValues.Should().BeEmpty();
        environment.Dark.Should().BeFalse();
    }

    [Test]
    public void Decode_UnknownOrMismatchedNames_AreSkipped()
    {
        var descriptor = Describe();
        var state = new PlaygroundState();
        state.Set("Count", 7);
        var encoded = PlaygroundStateSerializer.Encode(descriptor, state, new PlaygroundEnvironment());

        // Decode against a descriptor where "Count" does not exist.
        var otherDescriptor = new ReflectionCatalogProvider().Describe(typeof(EventFixture));
        var restored = new PlaygroundState();
        PlaygroundStateSerializer.Decode(encoded, otherDescriptor, restored, new PlaygroundEnvironment());

        restored.ModifiedValues.Should().BeEmpty();
    }
}
