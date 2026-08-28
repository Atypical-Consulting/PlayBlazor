namespace PlayBlazor.State;

/// <summary>Rolling log of intercepted component events, newest first.</summary>
public sealed class PlaygroundEventLog
{
    public const int Capacity = 50;

    public sealed record Entry(DateTime Timestamp, string Name, string Payload, string? Detail);

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

        _entries.Insert(0, new Entry(DateTime.Now, name, text, DescribeProperties(payload)));
        if (_entries.Count > Capacity)
        {
            _entries.RemoveAt(_entries.Count - 1);
        }

        Changed?.Invoke();
    }

    /// <summary>
    /// One "Name = Value" line per public payload property — the unfolded view of an entry.
    /// Null for payloads with nothing beyond their <c>ToString()</c> (primitives, strings,
    /// empty args), so the shell knows the entry has no fold.
    /// </summary>
    private static string? DescribeProperties(object? payload)
    {
        if (payload is null or string || ReferenceEquals(payload, EventArgs.Empty)
            || payload.GetType().IsPrimitive || payload is decimal or DateTime or Enum)
        {
            return null;
        }

        var lines = new List<string>();
        foreach (var property in payload.GetType().GetProperties())
        {
            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            try
            {
                lines.Add($"{property.Name} = {property.GetValue(payload) ?? "null"}");
            }
            catch (Exception)
            {
                // A throwing getter simply drops off the detail view.
            }
        }

        return lines.Count == 0 ? null : string.Join('\n', lines);
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
