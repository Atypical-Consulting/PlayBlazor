using AwesomeAssertions;
using Bunit;
using NUnit.Framework;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Shell;

public class CodePanelTests
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

    private IRenderedComponent<PlaygroundView> RenderView()
        => _context.Render<PlaygroundView>(ps => ps.Add(v => v.Component, typeof(BasicFixture)));

    [Test]
    public void CodePanel_ShowsDefaultSnippet()
    {
        RenderView().Find(".pb-code code").TextContent.Should().Be("<BasicFixture />");
    }

    [Test]
    public void CodePanel_UpdatesLiveWithControls()
    {
        var cut = RenderView();

        cut.FindAll("input[type=checkbox]")[0].Change(true); // Dense

        cut.Find(".pb-code code").TextContent.Should().Be("""<BasicFixture Dense="true" />""");
    }

    [Test]
    public void CopyButton_WritesSnippetToClipboard()
    {
        _context.JSInterop.SetupVoid("navigator.clipboard.writeText", _ => true);
        var cut = RenderView();
        cut.FindAll("input[type=checkbox]")[0].Change(true);

        cut.Find(".pb-copy").Click();

        var invocation = _context.JSInterop.Invocations
            .Single(i => i.Identifier == "navigator.clipboard.writeText");
        invocation.Arguments[0].Should().Be("""<BasicFixture Dense="true" />""");
    }
}
