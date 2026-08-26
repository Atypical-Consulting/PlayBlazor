namespace PlayBlazor.State;

/// <summary>Rolling log of intercepted component events, newest first.</summary>
public sealed class PlaygroundEventLog
{
    public const int Capacity = 100;

    public sealed record Entry(DateTime Timestamp, string Name, string Payload);

    private readonly List<Entry> _entries = [];

    public IReadOnlyList<Entry> Entries => _entries;

    public event Action? Changed;

    public void Record(string name, object? payload)
    {
        var text = payload switch
        {
            null => "(null)",
            EventArgs args when ReferenceEquals(args, EventArgs.Empty) => string.Empty,
            _ => payload.ToString() ?? "(null)",
        };

        _entries.Insert(0, new Entry(DateTime.Now, name, text));
        if (_entries.Count > Capacity)
        {
            _entries.RemoveAt(_entries.Count - 1);
        }

        Changed?.Invoke();
    }

    public void Clear()
    {
        if (_entries.Count == 0)
        {
            return;
        }

        _entries.Clear();
        Changed?.Invoke();
    }
}
