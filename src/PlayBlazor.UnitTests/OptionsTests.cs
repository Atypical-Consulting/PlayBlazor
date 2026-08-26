using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests;

public class OptionsTests
{
    private static readonly RenderFragment Sample = builder => builder.AddContent(0, "preset!");

    [Test]
    public void SlotPreset_StoredAndRetrieved()
    {
        var options = new PlayBlazorOptions();
        options.For<BasicFixture>().Slot(nameof(BasicFixture.ChildContent), Sample);

        options.TryGetSlotPreset(typeof(BasicFixture), "ChildContent", out var fragment).Should().BeTrue();
        fragment.Should().BeSameAs(Sample);
    }

    [Test]
    public void SlotPreset_UnknownComponent_ReturnsFalse()
    {
        var options = new PlayBlazorOptions();

        options.TryGetSlotPreset(typeof(BasicFixture), "ChildContent", out _).Should().BeFalse();
    }

    [Test]
    public void SlotPreset_OpenGenericRegistration_MatchesOtherClosings()
    {
        var options = new PlayBlazorOptions();
        options.For<GenericFixture<string>>().Slot("ChildContent", Sample);

        options.TryGetSlotPreset(typeof(GenericFixture<int>), "ChildContent", out var fragment).Should().BeTrue();
        fragment.Should().BeSameAs(Sample);
    }

    [Test]
    public void AddPlayBlazor_WithConfigure_RegistersConfiguredOptions()
    {
        var services = new ServiceCollection()
            .AddPlayBlazor(o => o.For<BasicFixture>().Slot("ChildContent", Sample))
            .BuildServiceProvider();

        var options = services.GetRequiredService<PlayBlazorOptions>();

        options.TryGetSlotPreset(typeof(BasicFixture), "ChildContent", out _).Should().BeTrue();
    }
}
