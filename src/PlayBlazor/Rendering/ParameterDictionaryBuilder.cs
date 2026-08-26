using PlayBlazor.Model;
using PlayBlazor.State;

namespace PlayBlazor.Rendering;

public static class ParameterDictionaryBuilder
{
    public static Dictionary<string, object> Build(ComponentDescriptor component, PlaygroundState state)
    {
        var result = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var parameter in component.Parameters)
        {
            if (parameter.Kind is ControlKind.Slot or ControlKind.Event or ControlKind.Unsupported)
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
}
