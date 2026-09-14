using Microsoft.FluentUI.AspNetCore.Components;

namespace PlayBlazor.Demo.FluentUI;

/// <summary>
/// The Fluent icons the picker offers. Each entry is an <see cref="Icon" /> INSTANCE — the same
/// object handed to the specimen, which is what lets a permalink map it back to its name.
/// </summary>
public static class FluentIconCatalogue
{
    /// <summary>An icon carrying hand-written SVG, so the demo needs no icon package.</summary>
    private sealed class DemoIcon(string name, string content)
        : Icon(name, IconVariant.Regular, IconSize.Size24, content);

    private const string Stroke = "fill='none' stroke='currentColor' stroke-width='2' "
                                  + "stroke-linecap='round' stroke-linejoin='round'";

    private static readonly (string Name, string Content)[] Shapes =
    [
        ("Circle", "<circle cx='12' cy='12' r='8' fill='currentColor' />"),
        ("CircleOutline", $"<circle cx='12' cy='12' r='8' {Stroke} />"),
        ("Square", "<rect x='5' y='5' width='14' height='14' rx='2' fill='currentColor' />"),
        ("SquareOutline", $"<rect x='5' y='5' width='14' height='14' rx='2' {Stroke} />"),
        ("Triangle", "<path d='M12 4 L20 19 H4 Z' fill='currentColor' />"),
        ("Checkmark", $"<path d='M4 13 l5 5 L20 7' {Stroke} />"),
        ("Dismiss", $"<path d='M6 6 L18 18 M18 6 L6 18' {Stroke} />"),
        ("Add", $"<path d='M12 5 V19 M5 12 H19' {Stroke} />"),
        ("Subtract", $"<path d='M5 12 H19' {Stroke} />"),
        ("ArrowUp", $"<path d='M12 19 V5 M6 11 l6-6 6 6' {Stroke} />"),
        ("ArrowDown", $"<path d='M12 5 V19 M6 13 l6 6 6-6' {Stroke} />"),
        ("ArrowLeft", $"<path d='M19 12 H5 M11 6 l-6 6 6 6' {Stroke} />"),
        ("ArrowRight", $"<path d='M5 12 H19 M13 6 l6 6-6 6' {Stroke} />"),
        ("ChevronUp", $"<path d='M6 15 l6-6 6 6' {Stroke} />"),
        ("ChevronDown", $"<path d='M6 9 l6 6 6-6' {Stroke} />"),
        ("Play", "<path d='M8 5 L19 12 L8 19 Z' fill='currentColor' />"),
        ("Pause", "<path d='M8 5 h3 v14 h-3 Z M13 5 h3 v14 h-3 Z' fill='currentColor' />"),
        ("Star", "<path d='M12 3 l2.6 6.3 6.8 .5 -5.2 4.4 1.6 6.6 -5.8-3.6 -5.8 3.6 1.6-6.6 "
                 + "-5.2-4.4 6.8-.5 Z' fill='currentColor' />"),
        ("Heart", "<path d='M12 20 C6 16 3 12.5 3 9.2 A4.2 4.2 0 0 1 12 7 A4.2 4.2 0 0 1 21 9.2 "
                  + "C21 12.5 18 16 12 20 Z' fill='currentColor' />"),
        ("Search", $"<circle cx='11' cy='11' r='6' {Stroke} /><path d='M15.5 15.5 L20 20' {Stroke} />"),
        ("Settings", $"<circle cx='12' cy='12' r='3' {Stroke} /><circle cx='12' cy='12' r='8' {Stroke} />"),
        ("Delete", $"<path d='M5 7 h14 M10 7 V5 h4 v2 M7 7 l1 13 h8 l1-13' {Stroke} />"),
        ("Edit", $"<path d='M4 20 h4 L19 9 l-4-4 -11 11 Z' {Stroke} />"),
        ("Warning", $"<path d='M12 4 L21 20 H3 Z M12 10 v4 M12 17 v.5' {Stroke} />"),
    ];

    /// <summary>The offered icons, by name.</summary>
    public static IReadOnlyDictionary<string, Icon> All { get; } =
        Shapes.ToDictionary(
            static shape => shape.Name,
            static shape => (Icon)new DemoIcon(shape.Name, shape.Content),
            StringComparer.Ordinal);
}
