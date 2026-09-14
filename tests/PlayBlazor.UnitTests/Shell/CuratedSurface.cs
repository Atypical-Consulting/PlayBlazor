using System.Reflection;
using PlayBlazor.Discovery;
using PlayBlazor.Model;

namespace PlayBlazor.UnitTests.Shell;

/// <summary>
/// The components a host's explorer actually lists, described at the closing it actually plays —
/// the same two steps <c>PlaygroundExplorer</c> and <c>PlaygroundWorkspace</c> take, in the same
/// order. Tests that sweep "everything a visitor can reach" share this so they cannot drift from
/// the shell, or from each other, over what counts as curated surface.
/// </summary>
public static class CuratedSurface
{
    /// <summary>Every listed component of an assembly, in a stable order.</summary>
    /// <param name="assembly">The explored library.</param>
    /// <param name="options">The host configuration, whose filter and exclusions decide the list.</param>
    /// <param name="catalog">The provider describing each component.</param>
    public static IReadOnlyList<ComponentDescriptor> Of(
        Assembly assembly,
        PlayBlazorOptions options,
        ReflectionCatalogProvider catalog)
        => catalog.Discover(assembly)
            .Where(c => !options.IsExcluded(c.Type) && (options.ComponentFilter?.Invoke(c.Type) ?? true))
            .Select(c => options.ResolvePreferredClosing(c.Type) is var preferred && preferred != c.Type
                ? catalog.Describe(preferred)
                : c)
            .OrderBy(c => c.DisplayName, StringComparer.Ordinal)
            .ToArray();
}
