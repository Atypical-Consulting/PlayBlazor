using AwesomeAssertions;
using NUnit.Framework;
using PlayBlazor.State;

namespace PlayBlazor.UnitTests.State;

public class EventLogTests
{
    [Test]
    public void Record_InsertsNewestFirst()
    {
        var log = new PlaygroundEventLog();

        log.Record("First", "a");
        log.Record("Second", "b");

        log.Entries.Select(e => e.Name).Should().ContainInOrder("Second", "First");
    }

    [Test]
    public void Record_FormatsPayloads()
    {
        var log = new PlaygroundEventLog();

        log.Record("A", null);
        log.Record("B", EventArgs.Empty);
        log.Record("C", 42);

        log.Entries.Select(e => e.Payload).Should().ContainInOrder("42", "", "(null)");
    }

    [Test]
    public void Record_CapsAtCapacity()
    {
        var log = new PlaygroundEventLog();

        for (var i = 0; i < PlaygroundEventLog.Capacity + 10; i++)
        {
            log.Record("E", i);
        }

        log.Entries.Should().HaveCount(PlaygroundEventLog.Capacity);
        log.Entries[0].Payload.Should().Be((PlaygroundEventLog.Capacity + 9).ToString());
    }

    [Test]
    public void Record_CapturesADetailDumpOfPayloadProperties()
    {
        var log = new PlaygroundEventLog();

        log.Record("OnClick", new Microsoft.AspNetCore.Components.Web.MouseEventArgs { Detail = 2, Button = 1 });

        log.Entries[0].Detail.Should().NotBeNull();
        log.Entries[0].Detail.Should().Contain("Detail = 2").And.Contain("Button = 1");
    }

    [Test]
    public void Record_FormatsCollectionsAsTheirItems()
    {
        var log = new PlaygroundEventLog();

        log.Record("SelectedValuesChanged", new HashSet<string> { "Alaska", "Nebraska" });
        log.Record("Big", Enumerable.Range(1, 9).ToArray());

        log.Entries[1].Payload.Should().Be("[Alaska, Nebraska]");
        log.Entries[0].Payload.Should().Be("[1, 2, 3, 4, 5, 6, …]");
    }

    [Test]
    public void Record_RaisesRecordedWithTheRawPayload()
    {
        var log = new PlaygroundEventLog();
        (string Name, object? Payload) seen = default;
        log.Recorded += (name, payload) => seen = (name, payload);

        log.Record("ValueChanged", 42);

        seen.Name.Should().Be("ValueChanged");
        seen.Payload.Should().Be(42);
    }

    [Test]
    public void Record_LeavesDetailNullWhenItAddsNothing()
    {
        var log = new PlaygroundEventLog();

        log.Record("A", null);
        log.Record("B", EventArgs.Empty);
        log.Record("C", 42);
        log.Record("D", "text");

        log.Entries.Should().OnlyContain(e => e.Detail == null);
    }

    [Test]
    public void Clear_EmptiesAndNotifies()
    {
        var log = new PlaygroundEventLog();
        log.Record("E", 1);
        var raised = 0;
        log.Changed += () => raised++;

        log.Clear();

        log.Entries.Should().BeEmpty();
        raised.Should().Be(1);
    }
}
