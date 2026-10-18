using System.Reflection;
using AwesomeAssertions;
using Bunit;
using NUnit.Framework;
using PlayBlazor.Demo.Shared;
using PlayBlazor.Discovery;
using PlayBlazor.Shell.Workspace;
using PlayBlazor.UnitTests.Diagnostics;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Shell;

/// <summary>
/// <see cref="PlayBlazorOptions.CategoryResolver"/> is the one grouping hook: the explorer rail and
/// the demo home wall must both group by it, never render an empty header, and file components
/// without a category under one trailing "Other".
/// </summary>
public class CategoryTests
{
    private BunitContext _context = null!;

    private static string? Family(Type type) => type.Name switch
    {
        nameof(BasicFixture) or nameof(SlottedFixture) => "Basics",
        nameof(EventFixture) or nameof(DetailEventFixture) => "Events",
        _ => null,
    };

    [SetUp]
    public void Setup()
    {
        _context = new BunitContext();
        var module = _context.JSInterop.SetupModule("./_content/PlayBlazor/playground-workspace.js");
        module.Setup<string?>("init", _ => true).SetResult(null);
        module.SetupVoid("saveLayout", _ => true).SetVoidResult();
        module.SetupVoid("dispose").SetVoidResult();
        _context.Services.AddPlayBlazor(o => o.CategoryResolver = Family);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    [Test]
    public void Rail_GroupsByTheHostCategory_OtherLast_NoEmptyHeader()
    {
        var cut = _context.Render<PlaygroundWorkspace>(ps =>
            ps.Add(w => w.Assemblies, new[] { typeof(BasicFixture).Assembly }));

        var headers = cut.FindAll(".pbw-rail-group");
        headers.Select(h => h.TextContent).Should().Equal("Basics", "Events", "Other");
        headers.Should().AllSatisfy(h => h.NextElementSibling!.ClassList.Should().Contain("pbw-rail-item"));
        cut.FindAll(".pbw-picker optgroup").Select(g => g.GetAttribute("label")).Should().StartWith("Basics · 2");

        // A filter that empties a group drops its header too.
        cut.Find(".pbw-rail-filter").Input(nameof(EventFixture));
        cut.FindAll(".pbw-rail-group").Select(h => h.TextContent).Should().Equal("Events");
    }

    [Test]
    public void Wall_GroupsByTheSameCategory_AndTheFinderStillFilters()
    {
        var cut = _context.Render<DemoLanding>(ps => ps
            .Add(c => c.Assembly, typeof(BasicFixture).Assembly)
            .Add(c => c.LibraryName, "Fixtures")
            .Add(c => c.DocsUrl, "https://example.test/")
            .Add(c => c.Current, "mud"));

        cut.Find(".demo-tiles").ClassList.Should().Contain("demo-tiles-families");
        cut.FindAll(".demo-letter").Select(h => h.FirstChild!.TextContent).Should().Equal("Basics", "Events", "Other");

        cut.Find("input.demo-filter").Input("Event");
        cut.FindAll(".demo-letter").Select(h => h.FirstChild!.TextContent).Should().Equal("Events");
        cut.FindAll("a.demo-tile").Should().HaveCount(2);
    }

    [Test]
    public void Wall_WithoutAResolver_KeepsTheAToZIndex()
    {
        using var context = new BunitContext();
        context.Services.AddPlayBlazor();
        var cut = context.Render<DemoLanding>(ps => ps
            .Add(c => c.Assembly, typeof(BasicFixture).Assembly)
            .Add(c => c.LibraryName, "Fixtures")
            .Add(c => c.DocsUrl, "https://example.test/")
            .Add(c => c.Current, "mud"));

        cut.Find(".demo-tiles").ClassList.Should().NotContain("demo-tiles-families");
        cut.FindAll(".demo-letter").Should().AllSatisfy(h => h.TextContent.Should().HaveLength(1));
    }

    /// <summary>A showcase's family map drifts silently when the curated surface grows: pin it.</summary>
    [Test]
    [TestCaseSource(typeof(ExploredLibraries), nameof(ExploredLibraries.All))]
    public void EveryListedComponent_HasAFamily(Assembly assembly, Action<PlayBlazorOptions> configure)
    {
        var options = new PlayBlazorOptions();
        configure(options);
        var listed = CuratedSurface.Of(assembly, options, new ReflectionCatalogProvider(options: options));

        options.CategoryResolver.Should().NotBeNull();
        // AddTag is a Fluent utility with no family of its own; it lists under "Other" on purpose.
        listed.Where(c => options.CategoryOf(c.Type) == PlayBlazorOptions.OtherCategory)
            .Select(c => c.DisplayName).Should().BeSubsetOf(["AddTag"]);
    }
}
