using Microsoft.AspNetCore.Components;
using PlayBlazor.Model;
using PlayBlazor.State;

namespace PlayBlazor.Rendering;

/// <summary>
/// Turns a descriptor plus the current state into the parameter dictionary handed to the specimen:
/// modified values, then host presets, then the component's own defaults. Slots are filled with
/// their preset or the user's text, and every event callback is replaced by an interceptor writing
/// to the event log.
/// </summary>
public static class ParameterDictionaryBuilder
{
    /// <summary>Builds the parameters to render the specimen with.</summary>
    /// <param name="component">The played component's descriptor.</param>
    /// <param name="state">The user's modifications.</param>
    /// <param name="options">Host presets and slot sources. Omit them to use defaults only.</param>
    /// <param name="eventLog">Receives intercepted callbacks. Omit it and events are not wired.</param>
    /// <returns>A dictionary suitable for <c>RenderTreeBuilder.AddMultipleAttributes</c>.</returns>
    public static Dictionary<string, object> Build(
        ComponentDescriptor component,
        PlaygroundState state,
        PlayBlazorOptions? options = null,
        PlaygroundEventLog? eventLog = null)
    {
        var result = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var parameter in component.Parameters)
        {
            if (parameter.Kind is ControlKind.Slot)
            {
                if (BuildSlot(component, parameter, state, options) is { } fragment)
                {
                    result[parameter.Name] = fragment;
                }
                continue;
            }
            if (parameter.Kind is ControlKind.Event)
            {
                if (eventLog is not null)
                {
                    var name = parameter.Name;
                    result[name] = EventCallbackInterceptor.Create(parameter.Type, arg => eventLog.Record(name, arg));
                }
                continue;
            }
            if (parameter.Kind is ControlKind.Unsupported)
            {
                // Non-drivable parameters become usable when the host presets them (Items, expressions…).
                if (TryGetPreset(component, parameter, options, out var unsupportedPreset))
                {
                    result[parameter.Name] = unsupportedPreset;
                }
                continue;
            }
            if (!state.IsModified(parameter.Name))
            {
                if (TryGetPreset(component, parameter, options, out var preset))
                {
                    result[parameter.Name] = preset;
                }
                continue;
            }

            if (state.GetValue(parameter) is { } value)
            {
                result[parameter.Name] = value;
            }
        }

        return result;
    }

    private static bool TryGetPreset(
        ComponentDescriptor component,
        ParameterDescriptor parameter,
        PlayBlazorOptions? options,
        out object value)
    {
        value = null!;
        if (options is not null
            && options.TryGetParameterPreset(component.Type, parameter.Name, out var preset)
            && preset is not null)
        {
            value = preset;
            return true;
        }

        return false;
    }

    private static object? BuildSlot(
        ComponentDescriptor component,
        ParameterDescriptor parameter,
        PlaygroundState state,
        PlayBlazorOptions? options)
    {
        // Only non-generic RenderFragment slots can carry text or an untyped preset.
        if (parameter.Type != typeof(RenderFragment))
        {
            return null;
        }

        // The user's typed text wins over a host preset.
        if (state.IsModified(parameter.Name) && state.GetValue(parameter) is string { Length: > 0 } text)
        {
            return (RenderFragment)(builder => builder.AddContent(0, text));
        }

        if (options is not null && options.TryGetSlotPreset(component.Type, parameter.Name, out var preset))
        {
            return preset;
        }

        return null;
    }
}
