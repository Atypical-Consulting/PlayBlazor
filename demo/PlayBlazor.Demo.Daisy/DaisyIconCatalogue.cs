using System.Reflection;
using DaisyBlazor;

namespace PlayBlazor.Demo.Daisy;

/// <summary>
/// Every <c>Icons.Material.Filled</c> constant DaisyBlazor exposes, by its C# name — the entries
/// of the playground's icon picker.
/// </summary>
/// <remarks>
/// The values are Material Symbols LIGATURE names (<c>Icons.Material.Filled.Map</c> is literally
/// <c>"map"</c>), not SVG markup. They are <c>const</c> fields the compiler inlines, so nothing
/// keeps the declaring type alive in a trimmed publish; the demo's
/// <c>TrimmerRootAssembly Include="DaisyBlazor.Components"</c> is what keeps this reflection
/// working. <c>Icons.Material.Outlined</c> is skipped on purpose: it holds two MudBlazor-compat
/// aliases of glyphs already listed here.
/// </remarks>
public static class DaisyIconCatalogue
{
    /// <summary>The Material Symbols ligatures, by constant name.</summary>
    public static IReadOnlyDictionary<string, string> Material { get; } =
        typeof(Icons.Material.Filled)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(static field => field.IsLiteral && field.FieldType == typeof(string))
            .ToDictionary(
                static field => field.Name,
                static field => (string)field.GetRawConstantValue()!,
                StringComparer.Ordinal);
}
