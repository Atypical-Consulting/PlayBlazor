using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PlayBlazor.Model;
using PlayBlazor.Rendering;

namespace PlayBlazor.State;

/// <summary>
/// Encodes the modified playground values and non-default environment flags as
/// compact JSON in base64url, for use as a shareable query-string value.
/// Unserializable values are skipped on encode; unknown or mismatched values are
/// ignored on decode — a stale permalink degrades gracefully.
/// </summary>
public static class PlaygroundStateSerializer
{
    public static string Encode(ComponentDescriptor descriptor, PlaygroundState state, PlaygroundEnvironment environment)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var parameter in descriptor.Parameters)
        {
            if (!state.IsModified(parameter.Name))
            {
                continue;
            }

            var text = state.GetValue(parameter) switch
            {
                null => null,
                bool boolean => boolean ? "true" : "false",
                string s => s,
                Enum enumValue => enumValue.ToString(),
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => null,
            };

            if (text is not null)
            {
                values[parameter.Name] = text;
            }
        }

        var payload = new PermalinkPayload(
            values.Count > 0 ? values : null,
            environment.Dark,
            environment.Rtl,
            environment.Checkerboard,
            environment.ViewportWidth);

        var json = JsonSerializer.SerializeToUtf8Bytes(payload, PlayBlazorJsonContext.Default.PermalinkPayload);
        return Convert.ToBase64String(json).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static void Decode(string encoded, ComponentDescriptor descriptor, PlaygroundState state, PlaygroundEnvironment environment)
    {
        PermalinkPayload? payload;
        try
        {
            var base64 = encoded.Replace('-', '+').Replace('_', '/');
            var padded = base64.PadRight(base64.Length + ((4 - base64.Length % 4) % 4), '=');
            payload = JsonSerializer.Deserialize(
                Encoding.UTF8.GetString(Convert.FromBase64String(padded)),
                PlayBlazorJsonContext.Default.PermalinkPayload);
        }
        catch (Exception)
        {
            return;
        }

        if (payload is null)
        {
            return;
        }

        environment.Dark = payload.Dark;
        environment.Rtl = payload.Rtl;
        environment.Checkerboard = payload.Checkerboard;
        environment.ViewportWidth = payload.ViewportWidth;

        if (payload.Values is null)
        {
            return;
        }

        foreach (var (name, text) in payload.Values)
        {
            var parameter = descriptor.Parameters.FirstOrDefault(p => p.Name == name);
            if (parameter is null)
            {
                continue;
            }

            var underlying = Nullable.GetUnderlyingType(parameter.Type) ?? parameter.Type;
            try
            {
                object? value = parameter.Kind switch
                {
                    ControlKind.Bool => bool.Parse(text),
                    ControlKind.Enum => Enum.Parse(underlying, text),
                    ControlKind.Text => text,
                    ControlKind.Slot => text,
                    ControlKind.Number => Convert.ChangeType(text, underlying, CultureInfo.InvariantCulture),
                    _ => null,
                };

                if (value is not null)
                {
                    state.Set(name, value);
                }
            }
            catch (Exception)
            {
                // Mismatched value in a stale permalink — skip this parameter.
            }
        }
    }
}

internal sealed record PermalinkPayload(
    [property: JsonPropertyName("v")] Dictionary<string, string>? Values,
    [property: JsonPropertyName("d")] bool Dark,
    [property: JsonPropertyName("r")] bool Rtl,
    [property: JsonPropertyName("c")] bool Checkerboard,
    [property: JsonPropertyName("w")] int? ViewportWidth);

[JsonSerializable(typeof(PermalinkPayload))]
[JsonSerializable(typeof(WorkspaceLayoutDto))]
internal sealed partial class PlayBlazorJsonContext : JsonSerializerContext;
