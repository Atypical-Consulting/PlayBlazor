using AwesomeAssertions;
using Bunit;
using NUnit.Framework;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Shell;

public class ErrorHintTests
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

    [Test]
    public void MissingParentException_ShowsHint()
    {
        var cut = _context.Render<PlaygroundView>(ps => ps.Add(v => v.Component, typeof(RequiresParentFixture)));

        cut.Find(".pb-error-hint").TextContent.Should().Contain("parent");
    }

    [Test]
    public void OrdinaryException_ShowsNoHint()
    {
        var cut = _context.Render<PlaygroundView>(ps => ps.Add(v => v.Component, typeof(ThrowingRenderFixture)));

        cut.FindAll(".pb-error-hint").Should().BeEmpty();
        cut.Find(".pb-error").TextContent.Should().Contain("render boom");
    }

    [Test]
    public void SwitchingComponent_RecoversFromPreviousError()
    {
        var cut = _context.Render<PlaygroundView>(ps => ps.Add(v => v.Component, typeof(ThrowingRenderFixture)));
        cut.Find(".pb-error").Should().NotBeNull();

        cut.Render(ps => ps.Add(v => v.Component, typeof(BasicFixture)));

        cut.FindAll(".pb-error").Should().BeEmpty();
        cut.Find(".basic-fixture").Should().NotBeNull();
    }
}
