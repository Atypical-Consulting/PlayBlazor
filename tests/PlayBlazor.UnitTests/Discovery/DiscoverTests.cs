using AwesomeAssertions;
using NUnit.Framework;
using PlayBlazor.Discovery;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Discovery;

public class DiscoverTests
{
    private ReflectionCatalogProvider _provider = null!;

    [SetUp]
    public void Setup()
    {
        _provider = new ReflectionCatalogProvider();
    }

    [Test]
    public void Discover_FindsFixtureComponents()
    {
        var components = _provider.Discover(typeof(BasicFixture).Assembly);

        components.Select(c => c.DisplayName).Should().Contain("BasicFixture");
        components.Select(c => c.DisplayName).Should().Contain("ThrowingCtorFixture");
    }

    [Test]
    public void Discover_ClosesGenericsWithString()
    {
        var components = _provider.Discover(typeof(BasicFixture).Assembly);
        var generic = components.Single(c => c.DisplayName == "GenericFixture");

        generic.Type.Should().Be(typeof(GenericFixture<string>));
    }

    [Test]
    public void Discover_ExcludesAbstractComponents()
    {
        var components = _provider.Discover(typeof(BasicFixture).Assembly);

        components.Select(c => c.DisplayName).Should().NotContain("AbstractFixture");
    }

    [Test]
    public void Discover_IsSortedByDisplayName()
    {
        var components = _provider.Discover(typeof(BasicFixture).Assembly);

        components.Select(c => c.DisplayName).Should().BeInAscendingOrder(StringComparer.Ordinal);
    }
}
