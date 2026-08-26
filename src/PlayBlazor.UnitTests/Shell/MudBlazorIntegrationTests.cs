using AwesomeAssertions;
using Bunit;
using MudBlazor;
using NUnit.Framework;

namespace PlayBlazor.UnitTests.Shell;

/// <summary>
/// Renders PlaygroundView against real MudBlazor components — the same integration
/// the DemoHost exercises visually. Guards the DynamicComponent + auto-generated
/// controls path on a real component library.
/// </summary>
public class MudBlazorIntegrationTests
{
    private BunitContext _context = null!;

    [SetUp]
    public void Setup()
    {
        _context = new BunitContext();
        _context.Services.AddPlayBlazor();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public void MudButton_RendersInsidePlayground()
    {
        var cut = _context.Render<PlaygroundView>(ps => ps.Add(v => v.Component, typeof(MudButton)));

        cut.Find(".pb-preview .mud-button-root").Should().NotBeNull();
        cut.FindAll(".pb-control").Count.Should().BeGreaterThan(5);
    }

    [Test]
    public void MudButton_TogglingVariantControl_ChangesRenderedClasses()
    {
        var cut = _context.Render<PlaygroundView>(ps => ps.Add(v => v.Component, typeof(MudButton)));
        var variantSelect = cut.FindAll("select")
            .Single(s => s.PreviousElementSibling?.TextContent == nameof(MudButton.Variant));

        variantSelect.Change(nameof(Variant.Filled));

        cut.Find(".pb-preview .mud-button-root").ClassList.Should().Contain("mud-button-filled");
    }

    [Test]
    public void MudProgressCircular_RendersInsidePlayground()
    {
        var cut = _context.Render<PlaygroundView>(ps => ps.Add(v => v.Component, typeof(MudProgressCircular)));

        cut.Find(".pb-preview .mud-progress-circular").Should().NotBeNull();
    }
}
