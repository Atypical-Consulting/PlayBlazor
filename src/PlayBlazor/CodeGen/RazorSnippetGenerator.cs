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
        string? childContent = null;
        foreach (var parameter in component.Parameters)
        {
            if (parameter.Kind is ControlKind.Slot)
            {
                // Only the conventional ChildContent slot round-trips into the snippet, as element content.
                if (parameter.Name == "ChildContent"
                    && state.IsModified(parameter.Name)
                    && state.GetValue(parameter) is string { Length: > 0 } text)
                {
                    childContent = EscapeContent(text);
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
            if (state.GetValue(parameter) is not { } value)
            {
                continue;
            }

            attributes.Add($"{parameter.Name}=\"{FormatValue(value)}\"");
        }

        var close = childContent is null ? " />" : $">{childContent}</{component.DisplayName}>";

        if (attributes.Count == 0)
        {
            return childContent is null ? $"<{component.DisplayName} />" : $"<{component.DisplayName}{close}";
        }

        if (attributes.Count <= 2)
        {
            return $"<{component.DisplayName} {string.Join(" ", attributes)}{close}";
        }

        var indent = new string(' ', component.DisplayName.Length + 2);
        var builder = new StringBuilder();
        builder.Append('<').Append(component.DisplayName).Append(' ').Append(attributes[0]);
        foreach (var attribute in attributes.Skip(1))
        {
            builder.Append('\n').Append(indent).Append(attribute);
        }

        builder.Append(close);
        return builder.ToString();
    }

    private static string EscapeContent(string text)
        => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

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
