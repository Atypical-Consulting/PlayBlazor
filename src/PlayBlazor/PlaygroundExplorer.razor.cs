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

    [Parameter, EditorRequired]
    public IReadOnlyList<Assembly> Assemblies { get; set; } = default!;

    private IEnumerable<IGrouping<string, ComponentDescriptor>> Groups
        => _components
            .Where(c => c.DisplayName.Contains(_search, StringComparison.OrdinalIgnoreCase))
            .GroupBy(static c => c.Category);

    protected override void OnParametersSet()
    {
        _components = Assemblies
            .SelectMany(assembly => Catalog.Discover(assembly))
            .OrderBy(static c => c.DisplayName, StringComparer.Ordinal)
            .ToArray();
        _selected ??= _components.FirstOrDefault();
    }

    private void OnSearchChanged(ChangeEventArgs e)
        => _search = (string?)e.Value ?? string.Empty;
}
