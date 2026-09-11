using Microsoft.AspNetCore.Components;

namespace PlayBlazor;

/// <summary>
/// The named values a host offers for one parameter type — the entries of an icon picker.
/// </summary>
/// <remarks>
/// The registered values are the very instances handed to the specimen, so
/// <see cref="TryGetName" /> normally matches the same object back. A host registering
/// values whose type overrides neither <see cref="object.Equals(object)" /> nor
/// <see cref="object.GetHashCode" /> therefore still round-trips.
/// </remarks>
public sealed class CatalogueDefinition
{
    private readonly Dictionary<object, string> _names = [];

    internal CatalogueDefinition(
        IReadOnlyDictionary<string, object?> named,
        Func<object?, RenderFragment>? preview)
    {
        Named = named;
        Preview = preview;
        foreach (var (name, value) in named)
        {
            if (value is not null)
            {
                // First name wins: two names sharing one value would otherwise flip-flop
                // the permalink depending on dictionary order.
                _names.TryAdd(value, name);
            }
        }
    }

    /// <summary>The offered values, by the name the picker shows.</summary>
    public IReadOnlyDictionary<string, object?> Named { get; }

    /// <summary>Renders one value as a thumbnail. Null lists names alone.</summary>
    public Func<object?, RenderFragment>? Preview { get; }

    /// <summary>The name a value is listed under — what a permalink carries instead of the value.</summary>
    /// <param name="value">The value to look up.</param>
    /// <param name="name">The name it is listed under, when the catalogue holds it.</param>
    /// <returns><c>false</c> for null and for values the catalogue does not offer.</returns>
    public bool TryGetName(object? value, out string name)
    {
        if (value is null)
        {
            name = string.Empty;
            return false;
        }

        return _names.TryGetValue(value, out name!);
    }
}
