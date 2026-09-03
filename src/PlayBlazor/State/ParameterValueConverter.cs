using System.Globalization;
using Microsoft.AspNetCore.Components;
using PlayBlazor.Model;

namespace PlayBlazor.State;

/// <summary>
/// The single text representation of a parameter value — used by the permalink serializer
/// and the text-based controls, so both sides always agree. Invariant culture throughout.
/// </summary>
public static class ParameterValueConverter
{
    /// <summary>Formats a value as the text a control shows and a permalink carries.</summary>
    /// <param name="parameter">The parameter, whose kind disambiguates types recognized structurally.</param>
    /// <param name="value">The value to format.</param>
    /// <returns>The invariant-culture text, or <c>null</c> when the type has no text form.</returns>
    public static string? Format(ParameterDescriptor parameter, object? value)
        => value switch
        {
            null => null,
            bool boolean => boolean ? "true" : "false",
            string s => s,
            char c => c.ToString(),
            MarkupString markup => markup.Value,
            Enum enumValue => enumValue.ToString(),
            DateTime dateTime => dateTime.ToString("o", CultureInfo.InvariantCulture),
            DateOnly date => date.ToString("o", CultureInfo.InvariantCulture),
            TimeSpan time => time.ToString("c", CultureInfo.InvariantCulture),
            TimeOnly time => time.ToString("o", CultureInfo.InvariantCulture),
            Array array => string.Join(", ", array.Cast<object?>()
                .Select(static item => Convert.ToString(item, CultureInfo.InvariantCulture))),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ when parameter.Kind == ControlKind.Color => value.ToString(),
            _ => null,
        };

    /// <summary>Parses text back into a parameter value — the exact inverse of <see cref="Format" />.</summary>
    /// <param name="parameter">The parameter whose type and kind drive the conversion.</param>
    /// <param name="text">The text to parse, in invariant culture.</param>
    /// <param name="value">The parsed value, when parsing succeeded.</param>
    /// <returns><c>false</c> for text that does not convert, leaving the caller's value untouched.</returns>
    public static bool TryParse(ParameterDescriptor parameter, string text, out object? value)
    {
        value = null;
        var type = Nullable.GetUnderlyingType(parameter.Type) ?? parameter.Type;
        try
        {
            value = parameter.Kind switch
            {
                ControlKind.Bool => bool.Parse(text),
                ControlKind.Enum => Enum.Parse(type, text),
                ControlKind.Number => Convert.ChangeType(text, type, CultureInfo.InvariantCulture),
                ControlKind.Date when type == typeof(DateOnly)
                    => DateOnly.Parse(text, CultureInfo.InvariantCulture),
                ControlKind.Date
                    => DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                ControlKind.Time when type == typeof(TimeOnly)
                    => TimeOnly.Parse(text, CultureInfo.InvariantCulture),
                ControlKind.Time => TimeSpan.Parse(text, CultureInfo.InvariantCulture),
                ControlKind.Color => Activator.CreateInstance(type, text),
                ControlKind.Slot => text,
                ControlKind.Icon => ParseText(type, text),
                ControlKind.Text => ParseText(type, text),
                _ => null,
            };
        }
        catch (Exception)
        {
            return false;
        }

        return value is not null;
    }

    private static object? ParseText(Type type, string text)
    {
        if (type == typeof(string))
        {
            return text;
        }

        if (type == typeof(char))
        {
            return text.Length > 0 ? text[0] : null;
        }

        if (type == typeof(MarkupString))
        {
            return new MarkupString(text);
        }

        if (type.IsArray && type.GetElementType() is { } element)
        {
            var parts = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var array = Array.CreateInstance(element, parts.Length);
            for (var i = 0; i < parts.Length; i++)
            {
                array.SetValue(
                    element == typeof(string) ? parts[i] : Convert.ChangeType(parts[i], element, CultureInfo.InvariantCulture),
                    i);
            }

            return array;
        }

        return null;
    }
}
