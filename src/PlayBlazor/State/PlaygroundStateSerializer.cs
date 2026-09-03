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
    /// <summary>Encodes a bench into a permalink value.</summary>
    /// <param name="descriptor">The played component's descriptor.</param>
    /// <param name="state">The modifications to capture; values with no text form are skipped.</param>
    /// <param name="environment">The environment flags; only non-default ones are written.</param>
    /// <returns>A base64url string safe to use as a query-string value.</returns>
    public static string Encode(ComponentDescriptor descriptor, PlaygroundState state, PlaygroundEnvironment environment)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var parameter in descriptor.Parameters)
        {
            if (!state.IsModified(parameter.Name))
            {
                continue;
            }

            var text = ParameterValueConverter.Format(parameter, state.GetValue(parameter));

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

    /// <summary>Restores a bench from a permalink value, in place.</summary>
    /// <param name="encoded">The value produced by <see cref="Encode" />.</param>
    /// <param name="descriptor">The played component's descriptor.</param>
    /// <param name="state">The state to populate.</param>
    /// <param name="environment">The environment to populate.</param>
    /// <remarks>
    /// Never throws: malformed input, unknown parameter names and values that no longer parse are
    /// each ignored, so a permalink shared before an API change still opens.
    /// </remarks>
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

            // Mismatched values in a stale permalink simply fail to parse and are skipped.
            if (ParameterValueConverter.TryParse(parameter, text, out var value))
            {
                state.Set(name, value);
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
