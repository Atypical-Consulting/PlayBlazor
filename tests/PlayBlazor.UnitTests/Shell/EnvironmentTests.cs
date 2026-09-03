using AwesomeAssertions;
using Bunit;
using NUnit.Framework;
using PlayBlazor.Rendering;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Shell;

public class EnvironmentTests
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

    private IRenderedComponent<PlaygroundView> RenderView(Action<PlayBlazorOptions>? configure = null, Type? component = null)
    {
        _context.Services.AddPlayBlazor(configure);
        return _context.Render<PlaygroundView>(ps => ps.Add(v => v.Component, component ?? typeof(BasicFixture)));
    }

    [Test]
    public void DarkToggle_FlipsStageClass()
    {
        var cut = RenderView();

        cut.Find(".pb-env-dark").Click();

        cut.Find(".pb-preview").ClassList.Should().Contain("pb-stage-dark");
        cut.Find(".pb-env-dark").ClassList.Should().Contain("pb-env-on");
    }

    [Test]
    public void RtlToggle_SetsDirOnSpecimen()
    {
        var cut = RenderView();

        cut.Find(".pb-env-rtl").Click();

        cut.Find(".pb-specimen").GetAttribute("dir").Should().Be("rtl");
    }

    [Test]
    public void CheckerToggle_FlipsStageClass()
    {
        var cut = RenderView();

        cut.Find(".pb-env-checker").Click();

        cut.Find(".pb-preview").ClassList.Should().Contain("pb-stage-checker");
    }

    [Test]
    public void ViewportSelect_ConstrainsSpecimenWidth()
    {
        var cut = RenderView();

        cut.Find(".pb-env-width").Change("360");

        var specimen = cut.Find(".pb-specimen");
        specimen.GetAttribute("style").Should().Contain("width:360px");
        specimen.ClassList.Should().Contain("pb-specimen-constrained");
    }

    [Test]
    public void ThemeWrapper_WrapsSpecimen()
    {
        var cut = RenderView(configure: options => options.ThemeWrapper = context => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "wrapper-marker");
            builder.AddContent(2, context.Content);
            builder.CloseElement();
        });

        cut.Find(".pb-specimen .wrapper-marker .basic-fixture").Should().NotBeNull();
    }

    [Test]
    public void Environment_IsCascadedToSpecimen()
    {
        var cut = RenderView(component: typeof(EnvFixture));
        cut.Find(".env-fixture").TextContent.Should().Contain("Dark=False");

        cut.Find(".pb-env-dark").Click();

        cut.Find(".env-fixture").TextContent.Should().Contain("Dark=True");
    }
}
