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

    /// <summary>Raised whenever a value is set or reset.</summary>
    public event Action? Changed;

    /// <summary>
    /// Incremented every time a value is removed. Use it as the specimen's <c>@key</c>: Blazor cannot
    /// un-set a parameter it has already supplied, so a reset needs a fresh component instance.
    /// </summary>
    public int InstanceKey { get; private set; }

    /// <summary>The parameters the user has modified, by name. Untouched parameters are absent.</summary>
    public IReadOnlyDictionary<string, object?> ModifiedValues => _modified;

    /// <summary>Whether the user has modified this parameter.</summary>
    /// <param name="parameterName">The parameter, by name.</param>
    public bool IsModified(string parameterName)
        => _modified.ContainsKey(parameterName);

    /// <summary>The value in force for a parameter: the user's, or the component's own default.</summary>
    /// <param name="parameter">The parameter descriptor, which carries the captured default.</param>
    public object? GetValue(ParameterDescriptor parameter)
        => _modified.TryGetValue(parameter.Name, out var value) ? value : parameter.DefaultValue;

    /// <summary>Records a user modification for one parameter.</summary>
    /// <param name="parameterName">The parameter, by name.</param>
    /// <param name="value">The value to apply.</param>
    public void Set(string parameterName, object? value)
    {
        _modified[parameterName] = value;
        Changed?.Invoke();
    }

    /// <summary>Drops one parameter's modification, returning it to the component's default.</summary>
    /// <param name="parameterName">The parameter, by name. Unknown names are ignored.</param>
    public void Reset(string parameterName)
    {
        if (_modified.Remove(parameterName))
        {
            InstanceKey++;
            Changed?.Invoke();
        }
    }

    /// <summary>Drops every modification at once.</summary>
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
