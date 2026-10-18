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
    public void TemplatePreset_MatchesItsExactClosingOnly()
    {
        // A RenderFragment<int> served to GenericFixture<string> would be an InvalidCastException.
        RenderFragment<int> template = value => builder => builder.AddContent(0, value);
        var options = new PlayBlazorOptions();
        options.For<GenericFixture<int>>().Slot("RowTemplate", template, "@context");

        options.TryGetTemplatePreset(typeof(GenericFixture<int>), "RowTemplate", out var found).Should().BeTrue();
        found.Should().BeSameAs(template);
        options.TryGetTemplatePreset(typeof(GenericFixture<string>), "RowTemplate", out _).Should().BeFalse();
        options.TryGetSlotSource(typeof(GenericFixture<int>), "RowTemplate", out var source).Should().BeTrue();
        source.Should().Be("@context");
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
