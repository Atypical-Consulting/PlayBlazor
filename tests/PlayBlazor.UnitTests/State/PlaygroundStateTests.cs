using AwesomeAssertions;
using NUnit.Framework;
using PlayBlazor.Model;
using PlayBlazor.State;

namespace PlayBlazor.UnitTests.State;

public class PlaygroundStateTests
{
    private static readonly ParameterDescriptor Dense = new(
        "Dense", typeof(bool), ControlKind.Bool, IsNullable: false,
        DefaultValue: false, HasDefault: true, Summary: null);

    [Test]
    public void GetValue_Unmodified_ReturnsDefault()
    {
        new PlaygroundState().GetValue(Dense).Should().Be(false);
    }

    [Test]
    public void Set_ThenGetValue_ReturnsModifiedValue()
    {
        var state = new PlaygroundState();

        state.Set("Dense", true);

        state.GetValue(Dense).Should().Be(true);
        state.IsModified("Dense").Should().BeTrue();
        state.ModifiedValues.Should().ContainKey("Dense");
    }

    [Test]
    public void Set_RaisesChanged_ButKeepsInstanceKey()
    {
        var state = new PlaygroundState();
        var raised = 0;
        state.Changed += () => raised++;
        var keyBefore = state.InstanceKey;

        state.Set("Dense", true);

        raised.Should().Be(1);
        state.InstanceKey.Should().Be(keyBefore);
    }

    [Test]
    public void Reset_RemovesValue_AndBumpsInstanceKey()
    {
        var state = new PlaygroundState();
        state.Set("Dense", true);
        var keyBefore = state.InstanceKey;

        state.Reset("Dense");

        state.IsModified("Dense").Should().BeFalse();
        state.GetValue(Dense).Should().Be(false);
        state.InstanceKey.Should().BeGreaterThan(keyBefore);
    }

    [Test]
    public void Reset_UnknownName_DoesNothing()
    {
        var state = new PlaygroundState();
        var raised = 0;
        state.Changed += () => raised++;
        var keyBefore = state.InstanceKey;

        state.Reset("Nope");

        raised.Should().Be(0);
        state.InstanceKey.Should().Be(keyBefore);
    }

    [Test]
    public void ResetAll_ClearsEverything_AndBumpsInstanceKey()
    {
        var state = new PlaygroundState();
        state.Set("Dense", true);
        state.Set("Label", "x");
        var keyBefore = state.InstanceKey;

        state.ResetAll();

        state.ModifiedValues.Should().BeEmpty();
        state.InstanceKey.Should().BeGreaterThan(keyBefore);
    }
}
