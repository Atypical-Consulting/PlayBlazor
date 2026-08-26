using System.Globalization;
using System.Text;
using PlayBlazor.Model;
using PlayBlazor.State;

namespace PlayBlazor.CodeGen;

/// <summary>Generates the Razor snippet matching the current playground state.</summary>
public static class RazorSnippetGenerator
{
    public static string Generate(ComponentDescriptor component, PlaygroundState state)
    {
        var attributes = new List<string>();
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
            if (state.GetValue(parameter) is not { } value)
            {
                continue;
            }

            attributes.Add($"{parameter.Name}=\"{FormatValue(value)}\"");
        }

        if (attributes.Count == 0)
        {
            return $"<{component.DisplayName} />";
        }

        if (attributes.Count <= 2)
        {
            return $"<{component.DisplayName} {string.Join(" ", attributes)} />";
        }

        var indent = new string(' ', component.DisplayName.Length + 2);
        var builder = new StringBuilder();
        builder.Append('<').Append(component.DisplayName).Append(' ').Append(attributes[0]);
        foreach (var attribute in attributes.Skip(1))
        {
            builder.Append('\n').Append(indent).Append(attribute);
        }

        builder.Append(" />");
        return builder.ToString();
    }

    private static string FormatValue(object value)
        => value switch
        {
            bool boolean => boolean ? "true" : "false",
            Enum enumValue => $"{enumValue.GetType().Name}.{enumValue}",
            string text => text.Replace("\"", "&quot;"),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };
}
