using Microsoft.AspNetCore.Components;
using PlayBlazor.Rendering;

namespace PlayBlazor;

/// <summary>Host configuration for PlayBlazor: slot presets and theme wrapping.</summary>
public sealed class PlayBlazorOptions
{
    private readonly Dictionary<(Type Component, string Parameter), RenderFragment> _slotPresets = new();

    /// <summary>
    /// Wraps the rendered specimen in the host's theme infrastructure (e.g. a theme provider
    /// honoring <see cref="PlaygroundEnvironment.Dark"/>). Null renders the specimen bare.
    /// </summary>
    public RenderFragment<PlaygroundThemeContext>? ThemeWrapper { get; set; }

    public ComponentOptionsBuilder<TComponent> For<TComponent>() where TComponent : IComponent
        => new(this);

    internal void AddSlotPreset(Type componentType, string parameterName, RenderFragment content)
        => _slotPresets[(Normalize(componentType), parameterName)] = content;

    public bool TryGetSlotPreset(Type componentType, string parameterName, out RenderFragment fragment)
    {
        if (_slotPresets.TryGetValue((componentType, parameterName), out fragment!))
        {
            return true;
        }

        return _slotPresets.TryGetValue((Normalize(componentType), parameterName), out fragment!);
    }

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
}
