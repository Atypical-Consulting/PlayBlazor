using System.Reflection;
using MudBlazor;

namespace PlayBlazor.DemoHost;

/// <summary>
/// Every <c>Icons.Material.Filled</c> constant, by its C# name — the entries of the
/// playground's icon picker.
/// </summary>
/// <remarks>
/// These are <c>const</c> fields, which the compiler inlines: nothing in the demo's own markup
/// keeps the declaring type alive, and a trimmed publish would drop it. The demo's
/// <c>TrimmerRootAssembly Include="MudBlazor"</c> is what keeps this reflection working.
/// </remarks>
public static class MudIconCatalogue
{
    /// <summary>The filled Material set, by constant name.</summary>
    public static IReadOnlyDictionary<string, string> Filled { get; } =
        typeof(Icons.Material.Filled)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(static field => field.IsLiteral && field.FieldType == typeof(string))
            .ToDictionary(
                static field => field.Name,
                static field => (string)field.GetRawConstantValue()!,
                StringComparer.Ordinal);
}
