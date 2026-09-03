using AwesomeAssertions;
using Bunit;
using NUnit.Framework;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Shell;

public class VariantTests
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

    private IRenderedComponent<PlaygroundView> RenderView()
    {
        _context.Services.AddPlayBlazor(options => options.For<BasicFixture>()
            .Variant("Dense demo", v => v.Set("Dense", true).Set("Label", "dense!"))
            .Variant("With content", v => v.Set("ChildContent", "from variant")));
        return _context.Render<PlaygroundView>(ps => ps.Add(v => v.Component, typeof(BasicFixture)));
    }

    [Test]
    public void Variants_RenderAsChips()
    {
        var cut = RenderView();

        cut.FindAll(".pb-variant").Select(b => b.TextContent.Trim())
            .Should().Equal("Dense demo", "With content");
    }

    [Test]
    public void ApplyingVariant_SeedsStateAndSnippet()
    {
        var cut = RenderView();

        cut.FindAll(".pb-variant")[0].Click();

        cut.Find(".basic-fixture").TextContent.Should().Contain("Dense=True").And.Contain("Label=dense!");
        cut.Find(".pb-code code").TextContent.Should().Be("""<BasicFixture Dense="true" Label="dense!" />""");
        cut.Find(".pb-variant-active").TextContent.Trim().Should().Be("Dense demo");
    }

    [Test]
    public void VariantWithSlotText_RendersChildContent()
    {
        var cut = RenderView();

        cut.FindAll(".pb-variant")[1].Click();

        cut.Find(".basic-fixture").TextContent.Should().Contain("from variant");
        cut.Find(".pb-code code").TextContent.Should().Be("<BasicFixture>from variant</BasicFixture>");
    }

    [Test]
    public void SwitchingVariants_ReplacesInsteadOfMerging()
    {
        var cut = RenderView();
        cut.FindAll(".pb-variant")[0].Click();

        cut.FindAll(".pb-variant")[1].Click();

        cut.Find(".pb-code code").TextContent.Should().Be("<BasicFixture>from variant</BasicFixture>");
    }

    [Test]
    public void ManualChange_ClearsActiveVariant()
    {
        var cut = RenderView();
        cut.FindAll(".pb-variant")[0].Click();

        cut.FindAll("input[type=checkbox]")[1].Change(true); // Outlined

        cut.FindAll(".pb-variant-active").Should().BeEmpty();
    }
}
