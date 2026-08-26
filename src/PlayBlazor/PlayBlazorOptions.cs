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

    /// <summary>
    /// Wraps the rendered specimen in the host's theme infrastructure (e.g. a theme provider
    /// honoring <see cref="PlaygroundEnvironment.Dark"/>). Null renders the specimen bare.
    /// </summary>
    public RenderFragment<PlaygroundThemeContext>? ThemeWrapper { get; set; }

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
}
