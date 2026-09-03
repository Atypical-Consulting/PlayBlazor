using AwesomeAssertions;
using Bunit;
using NUnit.Framework;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Shell;

public class PlaygroundViewTests
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

    private IRenderedComponent<PlaygroundView> RenderView(Type componentType)
        => _context.Render<PlaygroundView>(ps => ps.Add(v => v.Component, componentType));

    [Test]
    public void RendersPreviewWithComponentDefaults()
    {
        var cut = RenderView(typeof(BasicFixture));

        cut.Find(".pb-preview .basic-fixture").TextContent
            .Should().Contain("Size=Medium").And.Contain("Count=3");
    }

    [Test]
    public void RendersOneControlPerDrivableParameter()
    {
        var cut = RenderView(typeof(BasicFixture));

        // Dense, Outlined (bool) + Size (enum) + Label (text) + Count, Ratio, MaxItems (number) + ChildContent (text slot) = 8
        cut.FindAll(".pb-control").Count.Should().Be(8);
    }

    [Test]
    public void TogglingControl_UpdatesPreview()
    {
        var cut = RenderView(typeof(BasicFixture));

        cut.FindAll("input[type=checkbox]")[0].Change(true); // Dense

        cut.Find(".basic-fixture").TextContent.Should().Contain("Dense=True");
    }

    [Test]
    public void RowReset_RestoresDefault_OnFreshInstance()
    {
        var cut = RenderView(typeof(BasicFixture));
        cut.FindAll("input[type=checkbox]")[0].Change(true);
        cut.Find(".basic-fixture").TextContent.Should().Contain("Dense=True");

        cut.Find(".pb-row-reset").Click();

        cut.Find(".basic-fixture").TextContent.Should().Contain("Dense=False");
    }

    [Test]
    public void ListsUncontrolledParameters()
    {
        var cut = RenderView(typeof(BasicFixture));

        var uncontrolled = cut.Find(".pb-uncontrolled").TextContent;
        uncontrolled.Should().Contain("Endpoint").And.NotContain("ChildContent").And.NotContain("OnValueChanged");
    }

    [Test]
    public void ThrowingComponent_ShowsErrorInsteadOfCrashing()
    {
        var cut = RenderView(typeof(ThrowingRenderFixture));

        cut.Find(".pb-error").TextContent.Should().Contain("render boom");
    }

    [Test]
    public void WarningBadge_ShownForUninstantiableComponent()
    {
        var cut = RenderView(typeof(ThrowingCtorFixture));

        cut.FindAll(".pb-warning").Count.Should().Be(1);
    }
}
