using System.Reflection;
using AwesomeAssertions;
using NUnit.Framework;
using PlayBlazor.Discovery;
using PlayBlazor.Model;
using PlayBlazor.UnitTests.Diagnostics;

namespace PlayBlazor.UnitTests.Shell;

/// <summary>
/// Every component a showcase LISTS must carry curation. The list is read from the host's own
/// filter rather than typed out here on purpose: a hand-maintained roster of components to check
/// cannot report the one thing that matters most — a component nobody remembered to add.
/// </summary>
/// <remarks>
/// That is not hypothetical. The Fluent showcase shipped its landing page's flagship
/// <c>FluentButton</c> bench with no slot, preset or variant at all: an empty button under copy
/// inviting the visitor to try chips that did not exist. The per-task test file had 89 hand-listed
/// cases and the flagship was not one of them, so nothing failed.
/// </remarks>
public class CurationCompletenessTests
{
    [Test]
    [TestCaseSource(typeof(ExploredLibraries), nameof(ExploredLibraries.All))]
    public void EveryListedComponent_CarriesCuration(Assembly assembly, Action<PlayBlazorOptions> configure)
    {
        var options = new PlayBlazorOptions();
        configure(options);
        var catalog = new ReflectionCatalogProvider(options: options);

        var listed = CuratedSurface.Of(assembly, options, catalog);
        listed.Should().NotBeEmpty("the showcase should list components");

        // Joined into one string so a run names EVERY bare bench, not just the first.
        var bare = string.Join(", ", listed.Where(c => !IsCurated(c, options)).Select(c => c.DisplayName));

        bare.Should().BeEmpty(
            "every component listed by the {0} showcase should carry a slot, preset, scaffold or "
            + "variant — exclude it from the listing instead of shipping an empty bench",
            assembly.GetName().Name);
    }

    /// <summary>
    /// All four forms of curation, not just the three a chip-and-slot check would notice: a
    /// component curated only with <c>.Parameter(...)</c> is curated, and several are.
    /// </summary>
    private static bool IsCurated(ComponentDescriptor component, PlayBlazorOptions options)
        => options.GetVariants(component.Type).Count > 0
           || options.TryGetScaffold(component.Type, out _)
           || component.Parameters.Any(p =>
               (p.Kind is ControlKind.Slot && options.TryGetSlotPreset(component.Type, p.Name, out _))
               || (options.TryGetParameterPreset(component.Type, p.Name, out var preset) && preset is not null));
}
