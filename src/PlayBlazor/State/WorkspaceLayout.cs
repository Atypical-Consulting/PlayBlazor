using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlayBlazor.State;

/// <summary>
/// The workspace's panel arrangement: which panel sits in which dock zone (and where),
/// which ones float (and at what position/size), which are hidden, and the dock sizes.
/// Pure state — gestures come from the shell, persistence is a JSON string the host
/// stores wherever it wants (the workspace uses localStorage).
/// </summary>
public sealed class WorkspaceLayout
{
    public const string RightZone = "right";
    public const string BottomZone = "bottom";

    public sealed record FloatInfo(double X, double Y, double? Width, double? Height);

    private static readonly string[] DefaultRight = ["graph", "parameters"];
    private static readonly string[] DefaultBottom = ["razor", "signals"];

    private readonly List<string> _right = [.. DefaultRight];
    private readonly List<string> _bottom = [.. DefaultBottom];
    private readonly Dictionary<string, FloatInfo> _floats = new(StringComparer.Ordinal);
    private readonly HashSet<string> _hidden = new(StringComparer.Ordinal);

    public event Action? Changed;

    public double RightWidth { get; private set; } = 330;

    public double BottomHeight { get; private set; } = 235;

    public IReadOnlyList<string> Zone(string zone)
        => zone == RightZone ? _right : _bottom;

    public FloatInfo? Float(string panel)
        => _floats.TryGetValue(panel, out var info) ? info : null;

    public bool IsHidden(string panel)
        => _hidden.Contains(panel);

    /// <summary>Inserts the panel into a dock zone at the given position (clamped).</summary>
    public void Dock(string panel, string zone, int index)
    {
        Detach(panel);
        var target = zone == RightZone ? _right : _bottom;
        target.Insert(Math.Clamp(index, 0, target.Count), panel);
        Notify();
    }

    /// <summary>Detaches the panel into a floating palette, keeping any previous size.</summary>
    public void SetFloat(string panel, double x, double y)
    {
        var previous = Float(panel);
        Detach(panel);
        _floats[panel] = new FloatInfo(x, y, previous?.Width, previous?.Height);
        Notify();
    }

    public void SetFloatSize(string panel, double width, double height)
    {
        if (_floats.TryGetValue(panel, out var info))
        {
            _floats[panel] = info with { Width = width, Height = height };
            Notify();
        }
    }

    /// <summary>Returns the panel to the end of its default zone (double-click on its header).</summary>
    public void Redock(string panel)
        => Dock(panel, DefaultZoneOf(panel), int.MaxValue);

    public void ToggleHidden(string panel)
    {
        if (!_hidden.Remove(panel))
        {
            _hidden.Add(panel);
        }

        Notify();
    }

    public void Resize(string zone, double pixels)
    {
        if (zone == RightZone)
        {
            RightWidth = Math.Clamp(pixels, 240, 560);
        }
        else
        {
            BottomHeight = Math.Clamp(pixels, 120, 520);
        }

        Notify();
    }

    public void Reset()
    {
        _right.Clear();
        _right.AddRange(DefaultRight);
        _bottom.Clear();
        _bottom.AddRange(DefaultBottom);
        _floats.Clear();
        _hidden.Clear();
        RightWidth = 330;
        BottomHeight = 235;
        Notify();
    }

    public string ToJson()
        => JsonSerializer.Serialize(
            new WorkspaceLayoutDto(
                [.. _right],
                [.. _bottom],
                _floats.ToDictionary(
                    static p => p.Key,
                    static p => new FloatDto(p.Value.X, p.Value.Y, p.Value.Width, p.Value.Height)),
                [.. _hidden],
                RightWidth,
                BottomHeight),
            PlayBlazorJsonContext.Default.WorkspaceLayoutDto);

    /// <summary>Restores a layout; null, garbage, or a stale schema falls back to the defaults.</summary>
    public static WorkspaceLayout FromJson(string? json)
    {
        var layout = new WorkspaceLayout();
        if (string.IsNullOrWhiteSpace(json))
        {
            return layout;
        }

        try
        {
            var dto = JsonSerializer.Deserialize(json, PlayBlazorJsonContext.Default.WorkspaceLayoutDto);
            if (dto is null)
            {
                return layout;
            }

            layout._right.Clear();
            layout._right.AddRange((dto.Right ?? []).Distinct(StringComparer.Ordinal));
            layout._bottom.Clear();
            layout._bottom.AddRange((dto.Bottom ?? []).Distinct(StringComparer.Ordinal)
                .Except(layout._right, StringComparer.Ordinal));
            foreach (var (panel, f) in dto.Floats ?? [])
            {
                layout.Detach(panel);
                layout._floats[panel] = new FloatInfo(f.X, f.Y, f.W, f.H);
            }

            layout._hidden.UnionWith(dto.Hidden ?? []);
            layout.RightWidth = Math.Clamp(dto.RightWidth, 240, 560);
            layout.BottomHeight = Math.Clamp(dto.BottomHeight, 120, 520);
        }
        catch (JsonException)
        {
            return new WorkspaceLayout();
        }

        return layout;
    }

    /// <summary>Adopts another layout's state in place (persisted-JSON restore), then notifies.</summary>
    public void CopyFrom(WorkspaceLayout other)
    {
        _right.Clear();
        _right.AddRange(other._right);
        _bottom.Clear();
        _bottom.AddRange(other._bottom);
        _floats.Clear();
        foreach (var (panel, info) in other._floats)
        {
            _floats[panel] = info;
        }

        _hidden.Clear();
        _hidden.UnionWith(other._hidden);
        RightWidth = other.RightWidth;
        BottomHeight = other.BottomHeight;
        Notify();
    }

    private static string DefaultZoneOf(string panel)
        => DefaultRight.Contains(panel) ? RightZone : BottomZone;

    private void Detach(string panel)
    {
        _right.Remove(panel);
        _bottom.Remove(panel);
        _floats.Remove(panel);
    }

    private void Notify()
        => Changed?.Invoke();
}

internal sealed record FloatDto(
    [property: JsonPropertyName("x")] double X,
    [property: JsonPropertyName("y")] double Y,
    [property: JsonPropertyName("w")] double? W,
    [property: JsonPropertyName("h")] double? H);

internal sealed record WorkspaceLayoutDto(
    [property: JsonPropertyName("r")] List<string>? Right,
    [property: JsonPropertyName("b")] List<string>? Bottom,
    [property: JsonPropertyName("f")] Dictionary<string, FloatDto>? Floats,
    [property: JsonPropertyName("h")] List<string>? Hidden,
    [property: JsonPropertyName("rw")] double RightWidth,
    [property: JsonPropertyName("bh")] double BottomHeight);
