using System.Reflection;
using System.Text;
using AwesomeAssertions;
using NUnit.Framework;
using PlayBlazor.Discovery;
using PlayBlazor.Model;
using PlayBlazor.Rendering;
using PlayBlazor.State;
using PlayBlazor.UnitTests.Diagnostics;

namespace PlayBlazor.UnitTests.Shell;

/// <summary>
/// Selecting a variant chip must change what the specimen renders. Every curated variant of every
/// explored library is applied exactly as the shell applies it, and the resulting parameter
/// dictionary is compared with the un-varied one — a chip that produces the same dictionary is a
/// chip that highlights and does nothing.
/// </summary>
/// <remarks>
/// This is the cheap net under the defect class this project keeps re-discovering by hand: a
/// variant value that the compiler accepts and the machinery then silently drops. There are three
/// known ways for that to happen, and this test sees all three —
/// <list type="bullet">
/// <item><description><c>null</c>: both <c>ApplyVariant</c> implementations skip null values, so a
/// variant can never express "unset" (an indeterminate progress bar, an inherited colour).</description></item>
/// <item><description><see cref="ControlKind.Unsupported" />: <see cref="ParameterDictionaryBuilder" />
/// reads only the host preset for those, never <see cref="PlaygroundState" />.</description></item>
/// <item><description>A generic <c>RenderFragment&lt;T&gt;</c> slot: <c>BuildSlot</c> accepts only
/// the literal non-generic <see cref="Microsoft.AspNetCore.Components.RenderFragment" />.</description></item>
/// </list>
/// The check is on the parameter dictionary rather than on rendered HTML deliberately: the
/// dictionary is the entire input to the render, needs no browser and no JS interop, and cannot be
/// fooled by a component that happens to ignore a parameter — that last case is a library fact, and
/// belongs in the comment beside the variant, not in this test.
/// </remarks>
public class VariantLivenessTests
{
    [Test]
    [TestCaseSource(typeof(ExploredLibraries), nameof(ExploredLibraries.All))]
    public void EveryVariant_ChangesTheParameterDictionary(Assembly assembly, Action<PlayBlazorOptions> configure)
    {
        var options = new PlayBlazorOptions();
        configure(options);
        var catalog = new ReflectionCatalogProvider(options: options);

        var dead = new StringBuilder();
        var checkedVariants = 0;

        foreach (var component in CuratedSurface.Of(assembly, options, catalog))
        {
            // No event log: EventCallbackInterceptor.Create builds a fresh delegate per call, so
            // wiring events would make every dictionary differ from every other one for free.
            var baseline = ParameterDictionaryBuilder.Build(component, new PlaygroundState(), options);

            foreach (var variant in options.GetVariants(component.Type))
            {
                checkedVariants++;
                var state = new PlaygroundState();
                foreach (var (name, value) in variant.Values)
                {
                    // Exactly what PlaygroundView.ApplyVariant and PlaygroundWorkspace.ApplyVariant
                    // do — including the null skip, which is half of what this test exists to catch.
                    if (value is not null)
                    {
                        state.Set(name, value);
                    }
                }

                var applied = ParameterDictionaryBuilder.Build(component, state, options);
                if (!Differs(baseline, applied))
                {
                    dead.AppendLine(
                        $"{component.DisplayName} / \"{variant.Name}\" changes nothing — "
                        + $"keys set: {string.Join(", ", variant.Values.Keys)}");
                }
            }
        }

        checkedVariants.Should().BeGreaterThan(0, "the library should have curated variants to check");
        dead.ToString().Should().BeEmpty(
            $"every variant of {assembly.GetName().Name} should change what the specimen renders");
    }

    /// <summary>Whether two parameter dictionaries would hand the specimen anything different.</summary>
    private static bool Differs(
        IReadOnlyDictionary<string, object> baseline,
        IReadOnlyDictionary<string, object> applied)
    {
        if (baseline.Count != applied.Count)
        {
            return true;
        }

        foreach (var (name, value) in baseline)
        {
            if (!applied.TryGetValue(name, out var other) || !Equals(value, other))
            {
                return true;
            }
        }

        return false;
    }
}
