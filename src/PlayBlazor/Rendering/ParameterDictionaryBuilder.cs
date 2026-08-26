using Microsoft.AspNetCore.Components;
using PlayBlazor.Model;
using PlayBlazor.State;

namespace PlayBlazor.Rendering;

public static class ParameterDictionaryBuilder
{
    public static Dictionary<string, object> Build(
        ComponentDescriptor component,
        PlaygroundState state,
        PlayBlazorOptions? options = null)
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
            if (parameter.Kind is ControlKind.Event or ControlKind.Unsupported)
            {
                continue;
            }
            if (!state.IsModified(parameter.Name))
            {
                continue;
            }

            if (state.GetValue(parameter) is { } value)
            {
                result[parameter.Name] = value;
            }
        }

        return result;
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
