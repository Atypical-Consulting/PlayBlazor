using AwesomeAssertions;
using NUnit.Framework;
using PlayBlazor.State;

namespace PlayBlazor.UnitTests.State;

public class WorkspaceLayoutTests
{
    [Test]
    public void Defaults_MatchTheHandoff()
    {
        var layout = new WorkspaceLayout();

        layout.Zone(WorkspaceLayout.RightZone).Should().ContainInOrder("graph", "parameters");
        layout.Zone(WorkspaceLayout.BottomZone).Should().ContainInOrder("razor", "signals");
        layout.RightWidth.Should().Be(330);
        layout.BottomHeight.Should().Be(235);
        layout.IsHidden("graph").Should().BeFalse();
        layout.Float("graph").Should().BeNull();
    }

    [Test]
    public void Dock_InsertsAtIndex_RemovingFromPreviousHome()
    {
        var layout = new WorkspaceLayout();

        layout.Dock("razor", WorkspaceLayout.RightZone, 1);

        layout.Zone(WorkspaceLayout.RightZone).Should().ContainInOrder("graph", "razor", "parameters");
        layout.Zone(WorkspaceLayout.BottomZone).Should().ContainSingle().Which.Should().Be("signals");
    }

    [Test]
    public void Dock_ClampsOutOfRangeIndex()
    {
        var layout = new WorkspaceLayout();

        layout.Dock("signals", WorkspaceLayout.RightZone, 99);

        layout.Zone(WorkspaceLayout.RightZone).Should().ContainInOrder("graph", "parameters", "signals");
    }

    [Test]
    public void SetFloat_RemovesFromZones_AndKeepsSizeAcrossMoves()
    {
        var layout = new WorkspaceLayout();

        layout.SetFloat("signals", 100, 200);
        layout.SetFloatSize("signals", 400, 300);
        layout.SetFloat("signals", 50, 60);

        layout.Zone(WorkspaceLayout.BottomZone).Should().ContainSingle().Which.Should().Be("razor");
        layout.Float("signals").Should().Be(new WorkspaceLayout.FloatInfo(50, 60, 400, 300));
    }

    [Test]
    public void Redock_ReturnsAFloatToItsDefaultZone()
    {
        var layout = new WorkspaceLayout();
        layout.SetFloat("parameters", 10, 20);

        layout.Redock("parameters");

        layout.Float("parameters").Should().BeNull();
        layout.Zone(WorkspaceLayout.RightZone).Should().ContainInOrder("graph", "parameters");
    }

    [Test]
    public void ToggleHidden_RoundTrips()
    {
        var layout = new WorkspaceLayout();

        layout.ToggleHidden("razor");
        layout.IsHidden("razor").Should().BeTrue();

        layout.ToggleHidden("razor");
        layout.IsHidden("razor").Should().BeFalse();
    }

    [Test]
    public void Resize_ClampsToTheHandoffBounds()
    {
        var layout = new WorkspaceLayout();

        layout.Resize(WorkspaceLayout.RightZone, 9000);
        layout.Resize(WorkspaceLayout.BottomZone, 10);

        layout.RightWidth.Should().Be(560);
        layout.BottomHeight.Should().Be(120);
    }

    [Test]
    public void Json_RoundTripsTheWholeLayout()
    {
        var layout = new WorkspaceLayout();
        layout.Dock("razor", WorkspaceLayout.RightZone, 0);
        layout.SetFloat("signals", 12, 34);
        layout.SetFloatSize("signals", 500, 250);
        layout.ToggleHidden("graph");
        layout.Resize(WorkspaceLayout.RightZone, 420);

        var restored = WorkspaceLayout.FromJson(layout.ToJson());

        restored.Zone(WorkspaceLayout.RightZone).Should().ContainInOrder("razor", "graph", "parameters");
        restored.Float("signals").Should().Be(new WorkspaceLayout.FloatInfo(12, 34, 500, 250));
        restored.IsHidden("graph").Should().BeTrue();
        restored.RightWidth.Should().Be(420);
    }

    [Test]
    public void FromJson_ToleratesNullAndGarbage()
    {
        WorkspaceLayout.FromJson(null).Zone(WorkspaceLayout.RightZone)
            .Should().ContainInOrder("graph", "parameters");
        WorkspaceLayout.FromJson("not json at all").Zone(WorkspaceLayout.BottomZone)
            .Should().ContainInOrder("razor", "signals");
    }

    [Test]
    public void Reset_RestoresTheDefaults_AndNotifies()
    {
        var layout = new WorkspaceLayout();
        layout.SetFloat("graph", 1, 2);
        layout.ToggleHidden("signals");
        layout.Resize(WorkspaceLayout.BottomZone, 400);
        var raised = 0;
        layout.Changed += () => raised++;

        layout.Reset();

        layout.Zone(WorkspaceLayout.RightZone).Should().ContainInOrder("graph", "parameters");
        layout.Float("graph").Should().BeNull();
        layout.IsHidden("signals").Should().BeFalse();
        layout.BottomHeight.Should().Be(235);
        raised.Should().Be(1);
    }
}
