using AwesomeAssertions;
using Bunit;
using NUnit.Framework;
using PlayBlazor.Shell.Workspace;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Shell;

public class WorkspaceTests
{
    private BunitContext _context = null!;

    [SetUp]
    public void Setup()
    {
        _context = new BunitContext();
        // SetVoidResult completes the interop task so code after the await (toasts) runs.
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
    public void RendersFourPanels_InTheirDefaultZones()
    {
        var cut = RenderWorkspace();

        cut.FindAll(".pbw-zone-right [data-panel]").Select(e => e.GetAttribute("data-panel"))
            .Should().ContainInOrder("graph", "parameters");
        cut.FindAll(".pbw-zone-bottom [data-panel]").Select(e => e.GetAttribute("data-panel"))
            .Should().ContainInOrder("razor", "signals");
    }

    [Test]
    public void Picker_ListsComponents_AndSwitchingResetsTheBench()
    {
        var cut = RenderWorkspace();
        cut.Find(".pbw-picker").Change(nameof(EventFixture));

        cut.Find(".pbw-stage-name").TextContent.Should().Contain("EventFixture");
        cut.Find(".event-source").Click();
        cut.FindAll(".pbw-signal").Should().HaveCount(1);

        cut.Find(".pbw-picker").Change(nameof(BasicFixture));

        cut.FindAll(".pbw-signal").Should().BeEmpty("switching components clears the signal log");
    }

    [Test]
    public void Pill_HidesAndRestoresAPanel()
    {
        var cut = RenderWorkspace();

        cut.FindAll(".pbw-pill").First(p => p.TextContent.Contains("Razor")).Click();
        cut.FindAll("[data-panel=razor]").Should().BeEmpty();

        cut.FindAll(".pbw-pill").First(p => p.TextContent.Contains("Razor")).Click();
        cut.FindAll("[data-panel=razor]").Should().HaveCount(1);
    }

    [Test]
    public void CloseButton_HidesThePanel()
    {
        var cut = RenderWorkspace();

        cut.Find("[data-panel=signals] .pbw-ph-close").Click();

        cut.FindAll("[data-panel=signals]").Should().BeEmpty();
    }

    [Test]
    public void CopyRazor_WritesTheSnippetAndToasts()
    {
        var cut = RenderWorkspace();

        cut.Find("[data-panel=razor] .pbw-ph-copy").Click();

        _context.JSInterop.Invocations["navigator.clipboard.writeText"].Single()
            .Arguments[0].Should().Be("<BasicFixture />");
        cut.Find(".pbw-toast").TextContent.Should().Contain("Razor copied");
    }

    [Test]
    public void PresentMode_HidesZones_AndEscapesBack()
    {
        var cut = RenderWorkspace();

        cut.FindAll(".pbw-mode").First(m => m.TextContent == "Present").Click();

        cut.Find(".pbw").ClassList.Should().Contain("pbw-present");

        cut.FindAll(".pbw-mode").First(m => m.TextContent == "Play").Click();
        cut.Find(".pbw").ClassList.Should().NotContain("pbw-present");
    }

    [Test]
    public void ResetLayout_RestoresHiddenPanels()
    {
        var cut = RenderWorkspace();
        cut.Find("[data-panel=signals] .pbw-ph-close").Click();

        cut.Find(".pbw-reset-layout").Click();

        cut.FindAll("[data-panel=signals]").Should().HaveCount(1);
    }
}
