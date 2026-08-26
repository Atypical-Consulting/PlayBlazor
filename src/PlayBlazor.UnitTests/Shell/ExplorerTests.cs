using AwesomeAssertions;
using Bunit;
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
        _context.Services.AddPlayBlazor();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    private IRenderedComponent<PlaygroundExplorer> RenderExplorer()
        => _context.Render<PlaygroundExplorer>(ps => ps
            .Add(e => e.Assemblies, new[] { typeof(BasicFixture).Assembly }));

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
    public void ClickingItem_SwapsHostedPlayground()
    {
        var cut = RenderExplorer();

        cut.FindAll(".pb-explorer-item").Single(i => i.TextContent.Contains("EventFixture")).Click();

        cut.Find(".pb-explorer-detail .pb-title").TextContent.Should().Be("EventFixture");
        cut.Find(".pb-explorer-detail .event-source").Should().NotBeNull();
    }
}
