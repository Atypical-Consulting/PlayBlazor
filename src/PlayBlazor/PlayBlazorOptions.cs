using Microsoft.AspNetCore.Components;
using PlayBlazor.Rendering;

namespace PlayBlazor;

/// <summary>Host configuration for PlayBlazor: slot presets and theme wrapping.</summary>
public sealed class PlayBlazorOptions
{
    private readonly Dictionary<(Type Component, string Parameter), RenderFragment> _slotPresets = new();

    private readonly Dictionary<(Type Component, string Parameter), string> _slotSources = new();

    private readonly Dictionary<(Type Component, string Parameter), string> _parameterSources = new();

    private readonly Dictionary<(Type Component, string Parameter), object?> _parameterPresets = new();

    private readonly Dictionary<Type, Func<RenderFragment, RenderFragment>> _scaffolds = new();

    private readonly Dictionary<Type, string> _scaffoldSources = new();

    private readonly HashSet<Type> _excluded = [];

    private readonly Dictionary<Type, List<PlaygroundVariantDefinition>> _variants = new();

    private readonly Dictionary<Type, List<Type>> _related = new();

    private readonly Dictionary<Type, Type> _preferredClosings = new();

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

    /// <summary>Whether <see cref="Exclude{TComponent}" /> hid this component from the explorer.</summary>
    /// <param name="componentType">The component to test; any closing of a generic matches.</param>
    public bool IsExcluded(Type componentType)
        => _excluded.Contains(Normalize(componentType));

    /// <summary>Opens the fluent configuration for one component.</summary>
    /// <typeparam name="TComponent">
    /// The component to configure. Close a generic with the type you actually want played
    /// (<c>For&lt;MudDataGrid&lt;Person&gt;&gt;()</c>): that closing becomes the preferred one,
    /// replacing the placeholder discovery would otherwise pick.
    /// </typeparam>
    public ComponentOptionsBuilder<TComponent> For<TComponent>() where TComponent : IComponent
    {
        RememberClosing(typeof(TComponent));
        return new(this);
    }

    private void RememberClosing(Type componentType)
    {
        if (componentType.IsConstructedGenericType)
        {
            _preferredClosings[componentType.GetGenericTypeDefinition()] = componentType;
        }
    }

    /// <summary>
    /// Discovery closes open generics with placeholder arguments (string, int). When the host
    /// configured a specific closing (<c>For&lt;MudDataGrid&lt;Person&gt;&gt;()</c>), that closing
    /// is the one worth playing — presets, scaffolds, variants and links all live on it.
    /// </summary>
    public Type ResolvePreferredClosing(Type discoveredType)
        => discoveredType.IsConstructedGenericType
           && _preferredClosings.TryGetValue(discoveredType.GetGenericTypeDefinition(), out var preferred)
            ? preferred
            : discoveredType;

    internal void AddSlotPreset(Type componentType, string parameterName, RenderFragment content, string? source)
    {
        _slotPresets[(Normalize(componentType), parameterName)] = content;
        if (source is not null)
        {
            _slotSources[(Normalize(componentType), parameterName)] = source;
        }
    }

    internal void AddParameterPreset(Type componentType, string parameterName, object? value, string? source)
    {
        _parameterPresets[(componentType, parameterName)] = value;
        if (source is not null)
        {
            _parameterSources[(componentType, parameterName)] = source;
        }
    }

    internal void AddScaffold(Type componentType, Func<RenderFragment, RenderFragment> scaffold, string? source)
    {
        _scaffolds[componentType] = scaffold;
        if (source is not null)
        {
            _scaffoldSources[componentType] = source;
        }
    }

    /// <summary>The razor text of the scaffold, with a <c>{specimen}</c> marker where the
    /// played component sits — the generated code wraps the snippet in it.</summary>
    public bool TryGetScaffoldSource(Type componentType, out string source)
        => _scaffoldSources.TryGetValue(componentType, out source!);

    /// <summary>The fragment a host registered for a slot, used until the user types their own.</summary>
    /// <param name="componentType">The component owning the slot.</param>
    /// <param name="parameterName">The slot parameter.</param>
    /// <param name="fragment">The registered fragment, when one exists.</param>
    /// <returns><c>true</c> when a preset was found. Slot presets are shared across generic closings.</returns>
    public bool TryGetSlotPreset(Type componentType, string parameterName, out RenderFragment fragment)
    {
        if (_slotPresets.TryGetValue((componentType, parameterName), out fragment!))
        {
            return true;
        }

        return _slotPresets.TryGetValue((Normalize(componentType), parameterName), out fragment!);
    }

    /// <summary>The baseline value a host registered for a parameter.</summary>
    /// <param name="componentType">The component owning the parameter, at its exact generic closing.</param>
    /// <param name="parameterName">The parameter.</param>
    /// <param name="value">The registered value, when one exists.</param>
    /// <returns><c>true</c> when a preset was found.</returns>
    /// <remarks>
    /// Parameter presets and scaffolds carry values typed for ONE closing of a generic component
    /// (<c>Items = List&lt;Person&gt;</c>). Serving them to another closing (the explorer's
    /// <c>MudDataGrid&lt;string&gt;</c>) is an <see cref="InvalidCastException" /> — so both match
    /// the exact type only. Slot presets are plain fragments, safe to share across closings.
    /// </remarks>
    public bool TryGetParameterPreset(Type componentType, string parameterName, out object? value)
        => _parameterPresets.TryGetValue((componentType, parameterName), out value);

    /// <summary>The razor text a host provided for a slot preset — the copy-pasteable truth.</summary>
    public bool TryGetSlotSource(Type componentType, string parameterName, out string source)
        => _slotSources.TryGetValue((Normalize(componentType), parameterName), out source!);

    /// <summary>The razor expression a host provided for a parameter preset (e.g. <c>@_people</c>).</summary>
    public bool TryGetParameterSource(Type componentType, string parameterName, out string source)
        => _parameterSources.TryGetValue((componentType, parameterName), out source!)
           || _parameterSources.TryGetValue((Normalize(componentType), parameterName), out source!);

    /// <summary>The parent graph a host registered around this component's specimen.</summary>
    /// <param name="componentType">The component, at its exact generic closing.</param>
    /// <param name="scaffold">The wrapper placing the specimen inside its required parent, when one exists.</param>
    /// <returns><c>true</c> when a scaffold was found.</returns>
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

    /// <param name="parameterName">The slot to fill.</param>
    /// <param name="content">The fragment rendered on the stage.</param>
    /// <param name="source">The razor text of that fragment — shown verbatim in the generated
    /// code so users copy something that actually reproduces the bench.</param>
    public ComponentOptionsBuilder<TComponent> Slot(string parameterName, RenderFragment content, string? source = null)
    {
        _options.AddSlotPreset(typeof(TComponent), parameterName, content, source);
        return this;
    }

    /// <summary>
    /// Baseline value for one parameter: used when the user has not modified it
    /// (resolution: user modification &gt; host preset &gt; component default). Also makes
    /// otherwise non-drivable parameters (collections, expressions…) usable in the playground.
    /// </summary>
    public ComponentOptionsBuilder<TComponent> Parameter(string parameterName, object? value, string? source = null)
    {
        _options.AddParameterPreset(typeof(TComponent), parameterName, value, source);
        return this;
    }

    /// <summary>
    /// Renders the component inside a host-provided parent graph (a grid column inside its
    /// grid, a toggle item inside its group). The played specimen is passed in; return the
    /// surrounding markup with the specimen placed where it belongs.
    /// </summary>
    /// <param name="scaffold">Wraps the played specimen in its required parent graph.</param>
    /// <param name="source">The scaffold's razor text with a <c>{specimen}</c> marker — the
    /// generated code panel wraps the component snippet in it, so what users copy runs.</param>
    public ComponentOptionsBuilder<TComponent> Scaffold(Func<RenderFragment, RenderFragment> scaffold, string? source = null)
    {
        _options.AddScaffold(typeof(TComponent), scaffold, source);
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

    /// <summary>Sets one parameter of the variant.</summary>
    /// <param name="parameterName">The parameter to seed, by name.</param>
    /// <param name="value">The value applied when the variant is selected.</param>
    /// <returns>The same builder, for chaining.</returns>
    public PlaygroundVariantBuilder Set(string parameterName, object? value)
    {
        Values[parameterName] = value;
        return this;
    }
}
