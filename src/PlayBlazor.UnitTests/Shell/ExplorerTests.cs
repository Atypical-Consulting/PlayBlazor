using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Shell;

public class ExplorerTests
{
    private BunitContext _context = null!;

    [SetUp]
    public void Setup()
    {
        _context = new BunitContext();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    private IRenderedComponent<PlaygroundExplorer> RenderExplorer(Action<PlayBlazorOptions>? configure = null)
    {
        _context.Services.AddPlayBlazor(configure);
        return _context.Render<PlaygroundExplorer>(ps => ps
            .Add(e => e.Assemblies, new[] { typeof(BasicFixture).Assembly }));
    }

    [Test]
    public void ListsDiscoveredComponents_AndSelectsFirst()
    {
        var cut = RenderExplorer();

        var items = cut.FindAll(".pb-explorer-item").Select(i => i.TextContent.Trim()).ToList();
        items.Should().Contain("BasicFixture").And.Contain("EventFixture");
        cut.FindAll(".pb-explorer-selected").Count.Should().Be(1);
        cut.Find(".pb-title").Should().NotBeNull();
    }

    [Test]
    public void Search_FiltersItems()
    {
        var cut = RenderExplorer();

        cut.Find(".pb-explorer-search").Input("Basic");

        var items = cut.FindAll(".pb-explorer-item").Select(i => i.TextContent.Trim()).ToList();
        items.Should().Contain("BasicFixture").And.NotContain("EventFixture");
    }

    [Test]
    public void PermalinkInUrl_PreselectsItsComponent()
    {
        _context.Services.AddPlayBlazor();
        var navigation = _context.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        navigation.NavigateTo($"{navigation.BaseUri}?pb-EventFixture=e30");

        var cut = _context.Render<PlaygroundExplorer>(ps => ps
            .Add(e => e.Assemblies, new[] { typeof(BasicFixture).Assembly }));

        cut.Find(".pb-explorer-selected").TextContent.Trim().Should().Be("EventFixture");
    }

    [Test]
    public void Groups_AreOrderedByCategoryName()
    {
        var cut = RenderExplorer();

        var groups = cut.FindAll(".pb-explorer-eyebrow").Select(e => e.TextContent.Trim()).ToList();
        groups.Should().BeInAscendingOrder(StringComparer.Ordinal);
    }

    [Test]
    public void ExcludedComponents_AreHiddenFromTheList()
    {
        var cut = RenderExplorer(options => options.Exclude<EventFixture>());

        cut.FindAll(".pb-explorer-item").Select(i => i.TextContent.Trim())
            .Should().Contain("BasicFixture").And.NotContain("EventFixture");
    }

    [Test]
    public void ClickingItem_SwapsHostedPlayground()
    {
        var cut = RenderExplorer();

        cut.FindAll(".pb-explorer-item").Single(i => i.TextContent.Contains("EventFixture")).Click();

        cut.Find(".pb-explorer-detail .pb-title").TextContent.Should().Be("EventFixture");
        cut.Find(".pb-explorer-detail .event-source").Should().NotBeNull();
    }
}
