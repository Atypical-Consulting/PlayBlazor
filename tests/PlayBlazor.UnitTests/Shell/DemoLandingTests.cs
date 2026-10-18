using System.Reflection;
using AwesomeAssertions;
using Bunit;
using NUnit.Framework;
using PlayBlazor.Demo.Shared;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Shell;

public class DemoLandingTests
{
    private BunitContext _context = null!;

    [SetUp]
    public void Setup()
    {
        _context = new BunitContext();
        _context.Services.AddPlayBlazor();
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    [Test]
    public void Landing_TilesEveryDiscoveredComponent()
    {
        var cut = _context.Render<DemoLanding>(ps => ps
            .Add(c => c.Assembly, typeof(BasicFixture).Assembly)
            .Add(c => c.LibraryName, "Fixtures")
            .Add(c => c.DocsUrl, "https://example.test/")
            .Add(c => c.Current, "mud"));

        cut.FindAll("a.demo-tile").Count.Should().BeGreaterThan(0);
        cut.Markup.Should().Contain("Fixtures");
    }

    [Test]
    public void Switcher_MarksTheCurrentLibraryAndLinksTheOthers()
    {
        var cut = _context.Render<LibrarySwitcher>(ps => ps.Add(c => c.Current, "fluent"));

        cut.FindAll("a.demo-switch").Count.Should().Be(2);
        cut.FindAll("span.demo-switch-current").Count.Should().Be(1);
        cut.Markup.Should().Contain("../mud/").And.Contain("../daisy/");
    }

    [Test]
    public void Landing_PassesItsCurrentParameterThroughToTheSwitcher()
    {
        var cut = _context.Render<DemoLanding>(ps => ps
            .Add(c => c.Assembly, typeof(BasicFixture).Assembly)
            .Add(c => c.LibraryName, "MudBlazor")
            .Add(c => c.DocsUrl, "https://example.test/")
            .Add(c => c.Current, "mud"));

        // One switcher in the hero, one in the footer: both mark the same current library.
        var current = cut.FindAll("span.demo-switch-current");
        current.Count.Should().Be(2);
        current.Should().AllSatisfy(c => c.TextContent.Should().Be("MudBlazor"));
        cut.FindAll(".demo-hero a.demo-switch").Count.Should().Be(2);
    }

    [Test]
    public void Landing_NamesTheLibraryInItsOnlyH1_AndOwnsTheOnlyFilter()
    {
        var cut = _context.Render<DemoLanding>(ps => ps
            .Add(c => c.Assembly, typeof(BasicFixture).Assembly)
            .Add(c => c.LibraryName, "Fixtures")
            .Add(c => c.DocsUrl, "https://example.test/")
            .Add(c => c.Current, "mud"));

        cut.FindAll("h1").Should().ContainSingle().Which.TextContent.Should().Contain("playgrounds").And.Contain("Fixtures");
        cut.FindAll("input[type=search]").Count.Should().Be(1);

        cut.Find("input.demo-filter").Input("zz-no-such-component");
        cut.FindAll("a.demo-tile").Should().BeEmpty();
        cut.Find(".demo-empty-reset").Click();
        cut.FindAll("a.demo-tile").Count.Should().BeGreaterThan(0);
    }
}
