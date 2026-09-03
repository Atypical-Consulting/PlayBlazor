using AwesomeAssertions;
using MudBlazor;
using NUnit.Framework;
using PlayBlazor.Discovery;

namespace PlayBlazor.UnitTests.Discovery;

/// <summary>
/// Anti-regression guard for the "generalized" positioning: scanning a real,
/// large component library must never throw and must find a substantial catalog.
/// </summary>
public class MudBlazorGeneralityTests
{
    [Test]
    public void Discover_MudBlazorAssembly_SucceedsWithSubstantialCatalog()
    {
        var provider = new ReflectionCatalogProvider();

        var components = provider.Discover(typeof(MudButton).Assembly);

        components.Should().HaveCountGreaterThan(50);
        components.Should().OnlyContain(c => c.Parameters != null);
        components.Select(c => c.DisplayName).Should().Contain("MudButton");
        components.Select(c => c.DisplayName).Should().Contain("MudSelect");
    }
}
