using Microsoft.AspNetCore.Components;
using PlayBlazor.Rendering;

namespace PlayBlazor;

/// <summary>Host configuration for PlayBlazor: slot presets and theme wrapping.</summary>
public sealed class PlayBlazorOptions
{
    private readonly Dictionary<(Type Component, string Parameter), RenderFragment> _slotPresets = new();

    private readonly Dictionary<(Type Component, string Parameter), object?> _parameterPresets = new();

    private readonly Dictionary<Type, Func<RenderFragment, RenderFragment>> _scaffolds = new();

    private readonly HashSet<Type> _excluded = [];

    private readonly Dictionary<Type, List<PlaygroundVariantDefinition>> _variants = new();

    private readonly Dictionary<Type, List<Type>> _related = new();

    /// <summary>
    /// Wraps the rendered specimen in the host's theme infrastructure (e.g. a theme provider
    /// honoring <see cref="PlaygroundEnvironment.Dark"/>). Null renders the specimen bare.
    /// </summary>
    public RenderFragment<PlaygroundThemeContext>? ThemeWrapper { get; set; }

    /// <summary>
    /// Supplies an SVG fragment (inner markup of a 24×24 viewBox) shown next to a component
    /// in the explorer — e.g. MudBlazor's <c>Icons.Material</c> constants. Null for no icon.
    /// </summary>
    public Func<Type, string?>? IconResolver { get; set; }

    /// <summary>
    /// Curates which discovered components the explorer lists (true = shown). Null shows all
    /// non-excluded components. Use it to restrict a large library to its documented surface.
    /// </summary>
    public Func<Type, bool>? ComponentFilter { get; set; }

    internal void AddVariant(Type componentType, PlaygroundVariantDefinition variant)
    {
        if (!_variants.TryGetValue(componentType, out var list))
        {
            _variants[componentType] = list = [];
        }

        list.Add(variant);
    }

    /// <summary>Named example configurations for one component (exact generic closing).</summary>
    public IReadOnlyList<PlaygroundVariantDefinition> GetVariants(Type componentType)
        => _variants.TryGetValue(componentType, out var list) ? list : [];

    internal void AddRelated(Type componentType, Type relatedType)
    {
        if (!_related.TryGetValue(componentType, out var list))
        {
            _related[componentType] = list = [];
        }

        if (!list.Contains(relatedType))
        {
            list.Add(relatedType);
        }
    }

    /// <summary>Components the graph panel links from this one (exact generic closing).</summary>
    public IReadOnlyList<Type> GetRelated(Type componentType)
        => _related.TryGetValue(componentType, out var list) ? list : [];

    /// <summary>
    /// Converts Debug.Assert failures into catchable exceptions while a playground is active.
    /// A failed assert in a component lifecycle would otherwise terminate the process in
    /// Debug builds. No-op in Release builds, where asserts are compiled out.
    /// </summary>
    public bool GuardDebugAsserts { get; set; } = true;

    /// <summary>Hides a component from <see cref="PlaygroundExplorer"/> (providers, internals…).</summary>
    public PlayBlazorOptions Exclude<TComponent>() where TComponent : IComponent
    {
        _excluded.Add(Normalize(typeof(TComponent)));
        return this;
    }

    public bool IsExcluded(Type componentType)
        => _excluded.Contains(Normalize(componentType));

    public ComponentOptionsBuilder<TComponent> For<TComponent>() where TComponent : IComponent
        => new(this);

    internal void AddSlotPreset(Type componentType, string parameterName, RenderFragment content)
        => _slotPresets[(Normalize(componentType), parameterName)] = content;

    internal void AddParameterPreset(Type componentType, string parameterName, object? value)
        => _parameterPresets[(componentType, parameterName)] = value;

    internal void AddScaffold(Type componentType, Func<RenderFragment, RenderFragment> scaffold)
        => _scaffolds[componentType] = scaffold;

    public bool TryGetSlotPreset(Type componentType, string parameterName, out RenderFragment fragment)
    {
        if (_slotPresets.TryGetValue((componentType, parameterName), out fragment!))
        {
            return true;
        }

        return _slotPresets.TryGetValue((Normalize(componentType), parameterName), out fragment!);
    }

    // Parameter presets and scaffolds carry values typed for ONE closing of a generic
    // component (Items = List<Person>). Serving them to another closing (the explorer's
    // MudDataGrid<string>) is an InvalidCastException — so both match the exact type only.
    // Slot presets are plain RenderFragments, safe to share across closings.
    public bool TryGetParameterPreset(Type componentType, string parameterName, out object? value)
        => _parameterPresets.TryGetValue((componentType, parameterName), out value);

    public bool TryGetScaffold(Type componentType, out Func<RenderFragment, RenderFragment> scaffold)
        => _scaffolds.TryGetValue(componentType, out scaffold!);

    private static Type Normalize(Type componentType)
        => componentType.IsGenericType ? componentType.GetGenericTypeDefinition() : componentType;
}

/// <summary>Fluent per-component configuration.</summary>
public sealed class ComponentOptionsBuilder<TComponent> where TComponent : IComponent
{
    private readonly PlayBlazorOptions _options;

    internal ComponentOptionsBuilder(PlayBlazorOptions options)
        => _options = options;

    public ComponentOptionsBuilder<TComponent> Slot(string parameterName, RenderFragment content)
    {
        _options.AddSlotPreset(typeof(TComponent), parameterName, content);
        return this;
    }

    /// <summary>
    /// Baseline value for one parameter: used when the user has not modified it
    /// (resolution: user modification &gt; host preset &gt; component default). Also makes
    /// otherwise non-drivable parameters (collections, expressions…) usable in the playground.
    /// </summary>
    public ComponentOptionsBuilder<TComponent> Parameter(string parameterName, object? value)
    {
        _options.AddParameterPreset(typeof(TComponent), parameterName, value);
        return this;
    }

    /// <summary>
    /// Renders the component inside a host-provided parent graph (a grid column inside its
    /// grid, a toggle item inside its group). The played specimen is passed in; return the
    /// surrounding markup with the specimen placed where it belongs.
    /// </summary>
    public ComponentOptionsBuilder<TComponent> Scaffold(Func<RenderFragment, RenderFragment> scaffold)
    {
        _options.AddScaffold(typeof(TComponent), scaffold);
        return this;
    }

    /// <summary>
    /// Declares a component the workspace's graph panel links to from this one — a grid to
    /// its column type and back, typically. Selecting the node opens that component's bench.
    /// </summary>
    public ComponentOptionsBuilder<TComponent> Related<TOther>() where TOther : IComponent
    {
        _options.AddRelated(typeof(TComponent), typeof(TOther));
        return this;
    }

    /// <summary>
    /// A named example configuration (an official docs example, typically). Applying a
    /// variant seeds the playground state with its values — the user tweaks from there,
    /// and the generated snippet reflects them.
    /// </summary>
    public ComponentOptionsBuilder<TComponent> Variant(string name, Action<PlaygroundVariantBuilder> configure)
    {
        var builder = new PlaygroundVariantBuilder();
        configure(builder);
        _options.AddVariant(typeof(TComponent), new PlaygroundVariantDefinition(name, builder.Values));
        return this;
    }
}

/// <summary>A named example configuration for one component.</summary>
public sealed record PlaygroundVariantDefinition(string Name, IReadOnlyDictionary<string, object?> Values);

/// <summary>Collects the parameter values of one variant (slot text included, by parameter name).</summary>
public sealed class PlaygroundVariantBuilder
{
    internal Dictionary<string, object?> Values { get; } = new(StringComparer.Ordinal);

    public PlaygroundVariantBuilder Set(string parameterName, object? value)
    {
        Values[parameterName] = value;
        return this;
    }
}
