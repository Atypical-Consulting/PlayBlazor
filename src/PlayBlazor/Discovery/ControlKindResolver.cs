using System.Reflection;
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
        if (type == typeof(string) || type == typeof(char) || type == typeof(MarkupString))
        {
            return (ControlKind.Text, isNullable);
        }
        if (IsNumeric(type))
        {
            return (ControlKind.Number, isNullable);
        }
        if (type == typeof(DateTime) || type == typeof(DateOnly))
        {
            return (ControlKind.Date, isNullable);
        }
        if (type == typeof(TimeSpan) || type == typeof(TimeOnly))
        {
            return (ControlKind.Time, isNullable);
        }
        if (IsCsvArray(type))
        {
            return (ControlKind.Text, isNullable);
        }
        if (IsRenderFragment(type))
        {
            return (ControlKind.Slot, false);
        }
        if (IsEventCallback(type))
        {
            return (ControlKind.Event, false);
        }
        if (LooksLikeColor(type))
        {
            return (ControlKind.Color, isNullable);
        }

        return (ControlKind.Unsupported, isNullable);
    }

    private static bool IsNumeric(Type type)
        => type == typeof(int) || type == typeof(long) || type == typeof(short)
           || type == typeof(byte) || type == typeof(double) || type == typeof(float)
           || type == typeof(decimal);

    /// <summary>One-dimensional arrays of strings or numbers edit fine as comma-separated text.</summary>
    internal static bool IsCsvArray(Type type)
        => type.IsArray && type.GetArrayRank() == 1 && type.GetElementType() is { } element
           && (element == typeof(string) || IsNumeric(element));

    /// <summary>
    /// A library color type recognized structurally, without a compile-time dependency:
    /// public R/G/B properties plus a constructor taking one string (a CSS color) —
    /// MudBlazor's <c>MudColor</c> fits, and so does any similar type.
    /// </summary>
    internal static bool LooksLikeColor(Type type)
    {
        if (type.IsPrimitive || type.IsEnum || !type.Name.Contains("Color", StringComparison.Ordinal))
        {
            return false;
        }

        var hasChannels = new[] { "R", "G", "B" }.All(channel =>
            type.GetProperty(channel, BindingFlags.Public | BindingFlags.Instance)?.PropertyType is { } t
            && (IsNumeric(t) || t == typeof(byte)));
        return hasChannels && type.GetConstructor([typeof(string)]) is not null;
    }

    private static bool IsRenderFragment(Type type)
        => type == typeof(RenderFragment)
           || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(RenderFragment<>));

    private static bool IsEventCallback(Type type)
        => type == typeof(EventCallback)
           || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EventCallback<>));
}
