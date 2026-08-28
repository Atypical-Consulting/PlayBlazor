using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
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
    public void HostConfiguredGenericClosing_ReplacesTheDiscoveredPlaceholder()
    {
        // Discovery closes GenericFixture<TItem> with string; the host configured double.
        var cut = RenderWorkspace(options =>
            options.For<GenericFixture<double>>().Parameter(nameof(GenericFixture<double>.Value), 1.5));

        cut.Find(".pbw-picker").Change(nameof(GenericFixture<double>));

        cut.Find(".pbw-stage-name").TextContent.Should().Contain("GenericFixture");
        cut.Find(".pbw-specimen-zone div").TextContent.Should().Contain("1.5",
            "the Person-style host preset only binds on the host's own closing");
        cut.Find("[data-panel=graph] .pbw-tnode-sel").TextContent
            .Should().Contain("TItem=\"double\"", "the graph shows the real generic closing");
    }

    [Test]
    public void TheAddressBar_AlwaysCarriesTheBench()
    {
        var cut = RenderWorkspace();
        var navigation = _context.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        navigation.Uri.Should().Contain("pb-BasicFixture=", "selecting a component makes the URL shareable");

        cut.Find(".pbw-picker").Change(nameof(EventFixture));
        navigation.Uri.Should().Contain("pb-EventFixture=").And.NotContain("pb-BasicFixture=");

        cut.Find(".pbw-picker").Change(nameof(BasicFixture));
        var pristine = navigation.Uri;
        cut.FindAll("[data-panel=parameters] input[type=checkbox]")[0].Change(true); // Dense
        navigation.Uri.Should().NotBe(pristine, "modified state re-encodes into the URL");
        navigation.Uri.Should().Contain("pb-BasicFixture=");
    }

    [Test]
    public void EnvironmentToggles_ReSyncTheUrl()
    {
        var cut = RenderWorkspace();
        var navigation = _context.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        var before = navigation.Uri;

        cut.Find(".pbw-env-btn").Click(); // dark

        navigation.Uri.Should().NotBe(before, "the environment is part of the permalink");
    }

    [Test]
    public void NullableEnum_OffersANullOption()
    {
        _context.Services.AddPlayBlazor();
        var cut = _context.Render<PlayBlazor.Shell.ControlHost>(ps => ps
            .Add(c => c.Parameter, new PlayBlazor.Model.ParameterDescriptor(
                "Size", typeof(PlayBlazor.UnitTests.Fixtures.FixtureSize?),
                PlayBlazor.Model.ControlKind.Enum, IsNullable: true,
                DefaultValue: null, HasDefault: true, Summary: null)));

        cut.FindAll("option")[0].TextContent.Should().Be("(null)");
        cut.Find("select").GetAttribute("value").Should().BeNullOrEmpty();
    }

    [Test]
    public void Snippet_ShowsWhatTheHostContributes()
    {
        var cut = RenderWorkspace(options => options.For<GenericFixture<double>>()
            .Parameter(nameof(GenericFixture<double>.Value), 2.5));
        cut.Find(".pbw-picker").Change(nameof(GenericFixture<double>));

        var code = cut.Find("[data-panel=razor] .pbw-code").TextContent;
        code.Should().Contain("TItem=\"double\"", "the generic closing renders");
        code.Should().Contain("Value=\"2.5\"", "drivable presets render as literals");
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
