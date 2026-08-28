using System.Globalization;
using PlayBlazor.Model;

namespace PlayBlazor.Shell.Workspace;

/// <summary>
/// Renders a parameter as the C# declaration a library author would recognize —
/// <c>[Parameter] public bool Dense { get; set; } = false;</c> — used as the row tooltip.
/// </summary>
public static class ParameterSignature
{
    public static string Format(ParameterDescriptor parameter)
    {
        var typeName = TypeName(parameter.Type);
        if (parameter.IsNullable && !typeName.EndsWith('?'))
        {
            typeName += "?";
        }

        var declaration = $"[Parameter] public {typeName} {parameter.Name} {{ get; set; }}";
        return Initializer(parameter) is { } initializer
            ? $"{declaration} = {initializer};"
            : declaration;
    }

    private static string? Initializer(ParameterDescriptor parameter)
    {
        if (!parameter.HasDefault || parameter.DefaultValue is null
            || parameter.Kind is ControlKind.Event or ControlKind.Slot or ControlKind.Unsupported)
        {
            return null;
        }

        return parameter.DefaultValue switch
        {
            bool b => b ? "true" : "false",
            string s => $"\"{s}\"",
            Enum e => $"{e.GetType().Name}.{e}",
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            var other => other.ToString(),
        };
    }

    internal static string TypeName(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            return TypeName(underlying) + "?";
        }

        if (type.IsGenericType)
        {
            var name = type.Name[..type.Name.IndexOf('`')];
            return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(TypeName))}>";
        }

        return type switch
        {
            _ when type == typeof(bool) => "bool",
            _ when type == typeof(int) => "int",
            _ when type == typeof(long) => "long",
            _ when type == typeof(short) => "short",
            _ when type == typeof(byte) => "byte",
            _ when type == typeof(double) => "double",
            _ when type == typeof(float) => "float",
            _ when type == typeof(decimal) => "decimal",
            _ when type == typeof(string) => "string",
            _ when type == typeof(object) => "object",
            _ => type.Name,
        };
    }
}
