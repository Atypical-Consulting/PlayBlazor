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
