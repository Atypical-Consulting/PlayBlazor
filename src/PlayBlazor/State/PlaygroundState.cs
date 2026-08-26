using PlayBlazor.Model;

namespace PlayBlazor.State;

/// <summary>
/// Holds only the parameters the user has modified; everything else falls back to the
/// component's own defaults. <see cref="InstanceKey"/> changes only when values are
/// removed, so the preview can force a fresh component instance on reset (Blazor never
/// un-sets a previously supplied parameter on a live instance).
/// </summary>
public sealed class PlaygroundState
{
    private readonly Dictionary<string, object?> _modified = new(StringComparer.Ordinal);

    public event Action? Changed;

    public int InstanceKey { get; private set; }

    public IReadOnlyDictionary<string, object?> ModifiedValues => _modified;

    public bool IsModified(string parameterName)
        => _modified.ContainsKey(parameterName);

    public object? GetValue(ParameterDescriptor parameter)
        => _modified.TryGetValue(parameter.Name, out var value) ? value : parameter.DefaultValue;

    public void Set(string parameterName, object? value)
    {
        _modified[parameterName] = value;
        Changed?.Invoke();
    }

    public void Reset(string parameterName)
    {
        if (_modified.Remove(parameterName))
        {
            InstanceKey++;
            Changed?.Invoke();
        }
    }

    public void ResetAll()
    {
        if (_modified.Count == 0)
        {
            return;
        }

        _modified.Clear();
        InstanceKey++;
        Changed?.Invoke();
    }
}
