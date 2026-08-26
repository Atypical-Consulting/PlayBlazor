using AwesomeAssertions;
using Bunit;
using MudBlazor;
using MudBlazor.Services;
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

    public sealed record TestPerson(string Name, int Age)
    {
        public static readonly IReadOnlyList<TestPerson> Samples =
        [
            new("Ada Lovelace", 36),
            new("Grace Hopper", 85),
        ];
    }

    [Test]
    public async Task PropertyColumn_IsPlayableInsideScaffoldedGrid()
    {
        await using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddMudServices();
        context.Services.AddPlayBlazor(options => options
            .For<PropertyColumn<TestPerson, string>>()
            .Parameter(nameof(PropertyColumn<TestPerson, string>.Property),
                (System.Linq.Expressions.Expression<Func<TestPerson, string>>)(p => p.Name))
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<MudDataGrid<TestPerson>>(0);
                builder.AddComponentParameter(1, nameof(MudDataGrid<TestPerson>.Items), TestPerson.Samples);
                builder.AddComponentParameter(2, nameof(MudDataGrid<TestPerson>.Columns),
                    (Microsoft.AspNetCore.Components.RenderFragment)(columns => columns.AddContent(0, specimen)));
                builder.CloseComponent();
            }));

        var cut = context.Render<PlaygroundView>(ps => ps
            .Add(v => v.Component, typeof(PropertyColumn<TestPerson, string>)));

        cut.FindAll(".pb-error").Should().BeEmpty();
        cut.Find(".pb-specimen").TextContent.Should().Contain("Ada Lovelace").And.Contain("Grace Hopper");
    }
}
