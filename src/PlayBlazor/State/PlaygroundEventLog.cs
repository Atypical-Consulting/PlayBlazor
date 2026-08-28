namespace PlayBlazor.State;

/// <summary>Rolling log of intercepted component events, newest first.</summary>
public sealed class PlaygroundEventLog
{
    public const int Capacity = 50;

    public sealed record Entry(DateTime Timestamp, string Name, string Payload, string? Detail);

    private readonly List<Entry> _entries = [];

    public IReadOnlyList<Entry> Entries => _entries;

    public event Action? Changed;

    /// <summary>Raised for every intercepted event, payload included — the shell uses it to
    /// flow <c>XxxChanged</c> events back into the <c>Xxx</c> parameter.</summary>
    public event Action<string, object?>? Recorded;

    public void Record(string name, object? payload)
    {
        var text = payload switch
        {
            null => "(null)",
            EventArgs args when ReferenceEquals(args, EventArgs.Empty) => string.Empty,
            string s => s,
            System.Collections.IEnumerable items => FormatCollection(items),
            _ => payload.ToString() ?? "(null)",
        };

        _entries.Insert(0, new Entry(DateTime.Now, name, text, DescribeProperties(payload)));
        if (_entries.Count > Capacity)
        {
            _entries.RemoveAt(_entries.Count - 1);
        }

        Recorded?.Invoke(name, payload);
        Changed?.Invoke();
    }

    /// <summary>Collections read as their first items, not as a CLR type name.</summary>
    private static string FormatCollection(System.Collections.IEnumerable items)
    {
        var shown = new List<string>();
        var more = false;
        foreach (var item in items)
        {
            if (shown.Count == 6)
            {
                more = true;
                break;
            }

            shown.Add(item?.ToString() ?? "null");
        }

        return $"[{string.Join(", ", shown)}{(more ? ", …" : string.Empty)}]";
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
