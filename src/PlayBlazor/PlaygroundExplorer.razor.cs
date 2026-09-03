using System.Reflection;
using Microsoft.AspNetCore.Components;
using PlayBlazor.Discovery;
using PlayBlazor.Model;

namespace PlayBlazor;

/// <summary>Browses every discovered component of the given assemblies, hosting a playground for the selection.</summary>
public partial class PlaygroundExplorer : ComponentBase
{
    private IReadOnlyList<ComponentDescriptor> _components = [];
    private ComponentDescriptor? _selected;
    private string _search = string.Empty;

    [Inject]
    private IComponentCatalogProvider Catalog { get; set; } = default!;

    [Inject]
    private PlayBlazorOptions Options { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// The assemblies to scan. Every public concrete component they expose is listed, minus those
    /// hidden by <see cref="PlayBlazorOptions.Exclude{TComponent}" /> or by
    /// <see cref="PlayBlazorOptions.ComponentFilter" />.
    /// </summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<Assembly> Assemblies { get; set; } = default!;

    private IEnumerable<IGrouping<string, ComponentDescriptor>> Groups
        => _components
            .Where(c => c.DisplayName.Contains(_search, StringComparison.OrdinalIgnoreCase))
            .GroupBy(static c => c.Category)
            .OrderBy(static g => g.Key, StringComparer.Ordinal);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _components = Assemblies
            .SelectMany(assembly => Catalog.Discover(assembly))
            .Where(c => !Options.IsExcluded(c.Type) && (Options.ComponentFilter?.Invoke(c.Type) ?? true))
            .OrderBy(static c => c.DisplayName, StringComparer.Ordinal)
            .ToArray();
        // A shared permalink names its component (?pb-MudRating=…) — land the visitor on it.
        _selected ??= FindPermalinkTarget() ?? _components.FirstOrDefault();
    }

    private ComponentDescriptor? FindPermalinkTarget()
    {
        var query = new Uri(Navigation.Uri).Query.TrimStart('?');
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=');
            var key = Uri.UnescapeDataString(separator < 0 ? pair : pair[..separator]);
            if (!key.StartsWith("pb-", StringComparison.Ordinal))
            {
                continue;
            }

            var name = key["pb-".Length..];
            if (_components.FirstOrDefault(c => c.DisplayName == name) is { } match)
            {
                return match;
            }
        }

        return null;
    }

    private void OnSearchChanged(ChangeEventArgs e)
        => _search = (string?)e.Value ?? string.Empty;
}
