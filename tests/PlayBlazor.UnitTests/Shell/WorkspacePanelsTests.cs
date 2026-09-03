using AwesomeAssertions;
using Bunit;
using NUnit.Framework;
using PlayBlazor.Shell.Workspace;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Shell;

public class WorkspacePanelsTests
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
    public void ParameterRows_CarryTheSignatureTooltip()
    {
        var cut = RenderWorkspace();

        cut.FindAll("[data-panel=parameters] .pbw-prow")
            .First(r => r.TextContent.Contains("Dense"))
            .GetAttribute("title")
            .Should().Be("[Parameter] public bool Dense { get; set; } = false;");
    }

    [Test]
    public void Filter_NarrowsParameterRows()
    {
        var cut = RenderWorkspace();

        cut.Find(".pbw-pfilter").Input("dense");

        var rows = cut.FindAll("[data-panel=parameters] .pbw-prow");
        rows.Should().HaveCount(1);
        rows[0].TextContent.Should().Contain("Dense");
    }

    [Test]
    public void ModifiedChip_CountsAndFilters()
    {
        var cut = RenderWorkspace();
        cut.Find(".pbw-modchip").TextContent.Should().Contain("0/");

        cut.FindAll("[data-panel=parameters] input[type=checkbox]")[0].Change(true); // Dense
        cut.Find(".pbw-modchip").TextContent.Should().Contain("1/");

        cut.Find(".pbw-modchip").Click();
        cut.FindAll("[data-panel=parameters] .pbw-prow").Should().HaveCount(1);
    }

    [Test]
    public void NodeReset_RestoresDefaultsAndToasts()
    {
        var cut = RenderWorkspace();
        cut.FindAll("[data-panel=parameters] input[type=checkbox]")[0].Change(true);

        cut.Find(".pbw-preset-node").Click();

        cut.Find(".pbw-modchip").TextContent.Should().Contain("0/");
        cut.Find(".pbw-toast").TextContent.Should().Contain("reset");
    }

    [Test]
    public void FoldAll_CollapsesEveryGroup()
    {
        var cut = RenderWorkspace();
        cut.FindAll("[data-panel=parameters] details").Should().OnlyContain(d => d.HasAttribute("open"));

        cut.Find(".pbw-foldall").Click();

        cut.FindAll("[data-panel=parameters] details").Should().OnlyContain(d => !d.HasAttribute("open"));
    }

    [Test]
    public void Signals_UnfoldOnClick_AndClear()
    {
        var cut = RenderWorkspace();
        cut.Find(".pbw-picker").Change(nameof(DetailEventFixture));
        cut.Find(".detail-event-source").Click();

        cut.FindAll(".pbw-signal-detail").Should().BeEmpty();
        cut.Find(".pbw-signal").Click();
        cut.Find(".pbw-signal-detail").TextContent.Should().Contain("Detail = 3").And.Contain("Button = 1");

        cut.Find("[data-panel=signals] .pbw-ph-clear").Click();
        cut.FindAll(".pbw-signal").Should().BeEmpty();
    }

    [Test]
    public void Signals_OnAnEventlessComponent_SayThereAreNoEvents()
    {
        var cut = RenderWorkspace();
        cut.Find(".pbw-picker").Change(nameof(RichTypesFixture));

        cut.Find(".pbw-signals-empty").TextContent.Should().Contain("no events");
    }

    [Test]
    public void Signals_WithPlainPayload_HaveNoFold()
    {
        var cut = RenderWorkspace();
        cut.Find(".pbw-picker").Change(nameof(EventFixture));
        cut.Find(".event-source").Click();

        cut.Find(".pbw-signal").ClassList.Should().Contain("pbw-signal-flat");
        cut.Find(".pbw-signal").Click();
        cut.FindAll(".pbw-signal-detail").Should().BeEmpty();
    }

    [Test]
    public void Graph_ShowsThePlayedNode_AndRelatedNavigation()
    {
        var cut = RenderWorkspace(options =>
            options.For<BasicFixture>()
                .Related<EventFixture>()
                .Slot(nameof(BasicFixture.ChildContent), builder => { }));

        var nodes = cut.FindAll("[data-panel=graph] .pbw-tnode");
        nodes[0].ClassList.Should().Contain("pbw-tnode-sel");
        // A filled slot is real structure — it appears as a child of the played node.
        nodes.Should().Contain(n => n.ClassList.Contains("pbw-tnode-slot")
                                    && n.TextContent.Contains("ChildContent"));
        // Related components are typed links in their own section, not fake tree children.
        cut.Find("[data-panel=graph] .pbw-tree-eyebrow").TextContent.Should().Be("Related");

        cut.FindAll("[data-panel=graph] .pbw-tnode-link")
            .First(n => n.TextContent.Contains("EventFixture")).Click();

        cut.Find(".pbw-stage-name").TextContent.Should().Contain("EventFixture");
    }

    [Test]
    public void RichTypes_GetRealControls_AndDriveTheSpecimen()
    {
        var cut = RenderWorkspace();
        cut.Find(".pbw-picker").Change(nameof(RichTypesFixture));

        cut.Find("[data-panel=parameters] input[type=datetime-local]").Change("2026-08-28T09:30:00");
        cut.Find("[data-panel=parameters] input[type=time]").Change("00:00:30");
        var texts = cut.FindAll("[data-panel=parameters] .pb-control-text input");
        texts.First(t => t.Closest("label")!.TextContent.Contains("Tags")).Change("alpha, beta");

        var specimen = cut.Find(".rich-fixture").TextContent;
        specimen.Should().Contain("When=2026-08-28 09:30");
        specimen.Should().Contain("Every=00:00:30");
        specimen.Should().Contain("Tags=alpha|beta");
    }

    [Test]
    public void SpecimenInteraction_FlowsBackIntoTheBench()
    {
        var cut = RenderWorkspace();
        cut.Find(".pbw-picker").Change(nameof(TwoWayFixture));

        cut.Find(".two-way-source").Click();
        cut.Find(".two-way-source").Click();

        // The two-way bound value sticks (no snap-back), the bench marks it modified,
        // and the generated razor follows.
        cut.Find(".two-way-source").TextContent.Should().Contain("Value=2");
        cut.Find(".pbw-modchip").TextContent.Should().Contain("1/");
        cut.Find("[data-panel=razor]").TextContent.Should().Contain("Value=\"2\"");
    }

    [Test]
    public void FilterWithoutMatch_ShowsAnEmptyState()
    {
        var cut = RenderWorkspace();

        cut.Find(".pbw-pfilter").Input("zzzz");

        cut.Find(".pbw-params-empty").TextContent.Should().Contain("No parameter matches");
    }

    [Test]
    public void Variants_AppearAsExampleChips_AndSeedState()
    {
        var cut = RenderWorkspace(options =>
            options.For<BasicFixture>().Variant("Dense demo", v => v.Set("Dense", true)));

        cut.Find(".pbw-chip").Click();

        cut.Find(".basic-fixture").TextContent.Should().Contain("Dense=True");
        cut.Find(".pbw-chip").ClassList.Should().Contain("pbw-chip-active");
    }
}
