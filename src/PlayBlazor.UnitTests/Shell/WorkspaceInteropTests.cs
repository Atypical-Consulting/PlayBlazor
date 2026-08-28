using AwesomeAssertions;
using Bunit;
using NUnit.Framework;
using PlayBlazor.Shell.Workspace;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Shell;

/// <summary>
/// The JS module reports gestures through [JSInvokable] callbacks; these tests drive the
/// callbacks directly and assert the rendered layout follows.
/// </summary>
public class WorkspaceInteropTests
{
    private BunitContext _context = null!;

    [SetUp]
    public void Setup()
    {
        _context = new BunitContext();
        _context.JSInterop.SetupVoid("navigator.clipboard.writeText", _ => true).SetVoidResult();
        var module = _context.JSInterop.SetupModule("./_content/PlayBlazor/playground-workspace.js");
        module.Setup<string?>("init", _ => true).SetResult(null);
        module.SetupVoid("saveLayout", _ => true).SetVoidResult();
        module.SetupVoid("dispose").SetVoidResult();
    }

    [TearDown]
    public void TearDown()
        => _context.Dispose();

    private IRenderedComponent<PlaygroundWorkspace> RenderWorkspace(Action<PlayBlazorOptions>? configure = null)
    {
        _context.Services.AddPlayBlazor(configure);
        return _context.Render<PlaygroundWorkspace>(ps =>
            ps.Add(w => w.Assemblies, new[] { typeof(BasicFixture).Assembly }));
    }

    [Test]
    public async Task DroppingAPanelIntoAZone_ReordersIt()
    {
        var cut = RenderWorkspace();

        await cut.Instance.OnPanelDropped("razor", "right", 1, 0, 0);

        cut.FindAll(".pbw-zone-right [data-panel]").Select(e => e.GetAttribute("data-panel"))
            .Should().ContainInOrder("graph", "razor", "parameters");
    }

    [Test]
    public async Task DroppingAPanelOnTheCanvas_FloatsIt()
    {
        var cut = RenderWorkspace();

        await cut.Instance.OnPanelDropped("signals", null, 0, 120, 260);

        var panel = cut.Find("[data-panel=signals]");
        panel.ClassList.Should().Contain("pbw-panel-float");
        panel.GetAttribute("style").Should().Contain("left:120px").And.Contain("top:260px");
        cut.FindAll(".pbw-zone-bottom [data-panel]").Select(e => e.GetAttribute("data-panel"))
            .Should().ContainSingle().Which.Should().Be("razor");
    }

    [Test]
    public async Task ResizingAFloat_PersistsItsSize()
    {
        var cut = RenderWorkspace();
        await cut.Instance.OnPanelDropped("signals", null, 0, 10, 60);

        await cut.Instance.OnFloatResized("signals", 480, 320);

        cut.Find("[data-panel=signals]").GetAttribute("style")
            .Should().Contain("width:480px").And.Contain("height:320px");
    }

    [Test]
    public async Task ZoneResize_UpdatesTheGridVariables()
    {
        var cut = RenderWorkspace();

        await cut.Instance.OnZoneResized("right", 480);

        cut.Find(".pbw").GetAttribute("style").Should().Contain("--pbw-right:480px");
    }

    [Test]
    public async Task NumberKeys_TogglePanels()
    {
        var cut = RenderWorkspace();

        await cut.Instance.OnKey("3");
        cut.FindAll("[data-panel=razor]").Should().BeEmpty();

        await cut.Instance.OnKey("3");
        cut.FindAll("[data-panel=razor]").Should().HaveCount(1);
    }

    [Test]
    public async Task SlashKey_RevealsAHiddenParametersPanel()
    {
        var cut = RenderWorkspace();
        await cut.Instance.OnKey("2");
        cut.FindAll("[data-panel=parameters]").Should().BeEmpty();

        await cut.Instance.OnKey("/");

        cut.FindAll("[data-panel=parameters]").Should().HaveCount(1);
    }

    [Test]
    public async Task Escape_ExitsPresentMode()
    {
        var cut = RenderWorkspace();
        cut.FindAll(".pbw-mode").First(m => m.TextContent == "Present").Click();

        await cut.Instance.OnKey("Escape");

        cut.Find(".pbw").ClassList.Should().NotContain("pbw-present");
    }

    [Test]
    public async Task ArrowKeys_WalkTheVariantsInPresent()
    {
        var cut = RenderWorkspace(options => options.For<BasicFixture>()
            .Variant("One", v => v.Set("Dense", true))
            .Variant("Two", v => v.Set("Outlined", false)));
        cut.FindAll(".pbw-mode").First(m => m.TextContent == "Present").Click();

        await cut.Instance.OnKey("ArrowRight");

        cut.FindAll(".pbw-film-chip").First(c => c.TextContent == "Two")
            .ClassList.Should().Contain("pbw-film-active");
    }

    [Test]
    public async Task DoubleClickOnAFloatHeader_RedocksIt()
    {
        var cut = RenderWorkspace();
        await cut.Instance.OnPanelDropped("graph", null, 0, 40, 80);
        cut.Find("[data-panel=graph]").ClassList.Should().Contain("pbw-panel-float");

        cut.Find("[data-panel=graph] .pbw-panel-head").DoubleClick();

        cut.Find("[data-panel=graph]").ClassList.Should().NotContain("pbw-panel-float");
        cut.FindAll(".pbw-zone-right [data-panel]").Select(e => e.GetAttribute("data-panel"))
            .Should().ContainInOrder("parameters", "graph");
    }
}
