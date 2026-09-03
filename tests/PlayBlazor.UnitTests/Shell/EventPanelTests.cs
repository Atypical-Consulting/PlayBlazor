using AwesomeAssertions;
using Bunit;
using NUnit.Framework;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Shell;

public class EventPanelTests
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
        => _context.Render<PlaygroundView>(ps => ps.Add(v => v.Component, typeof(EventFixture)));

    [Test]
    public void EmptyState_ListsInterceptedEvents()
    {
        RenderView().Find(".pb-events-empty").TextContent.Should().Contain("OnPing");
    }

    [Test]
    public void ComponentEvent_AppearsInLog()
    {
        var cut = RenderView();

        cut.Find(".event-source").Click();

        var events = cut.Find(".pb-events").TextContent;
        events.Should().Contain("OnPing").And.Contain("ping!");
    }

    [Test]
    public void Clear_EmptiesTheLog()
    {
        var cut = RenderView();
        cut.Find(".event-source").Click();

        cut.Find(".pb-events-clear").Click();

        cut.Find(".pb-events-empty").TextContent.Should().Contain("Listening");
    }
}
