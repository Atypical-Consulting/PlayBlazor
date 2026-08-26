using Microsoft.AspNetCore.Components;
using PlayBlazor.Model;

namespace PlayBlazor.Discovery;

public static class ControlKindResolver
{
    public static (ControlKind Kind, bool IsNullable) Resolve(Type parameterType)
    {
        var underlying = Nullable.GetUnderlyingType(parameterType);
        var isNullable = underlying is not null;
        var type = underlying ?? parameterType;

        if (type == typeof(bool))
        {
            return (ControlKind.Bool, isNullable);
        }
        if (type.IsEnum)
        {
            return (ControlKind.Enum, isNullable);
        }
        if (type == typeof(string))
        {
            return (ControlKind.Text, isNullable);
        }
        if (IsNumeric(type))
        {
            return (ControlKind.Number, isNullable);
        }
        if (IsRenderFragment(type))
        {
            return (ControlKind.Slot, false);
        }
        if (IsEventCallback(type))
        {
            return (ControlKind.Event, false);
        }

        return (ControlKind.Unsupported, isNullable);
    }

    private static bool IsNumeric(Type type)
        => type == typeof(int) || type == typeof(long) || type == typeof(short)
           || type == typeof(byte) || type == typeof(double) || type == typeof(float)
           || type == typeof(decimal);

    private static bool IsRenderFragment(Type type)
        => type == typeof(RenderFragment)
           || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(RenderFragment<>));

    private static bool IsEventCallback(Type type)
        => type == typeof(EventCallback)
           || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EventCallback<>));
}
