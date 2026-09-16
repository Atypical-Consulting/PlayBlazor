using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PlayBlazor.Discovery;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Discovery;

/// <summary>
/// Capturing a component's defaults means constructing it once. Libraries whose components take a
/// constructor dependency — Fluent UI v5, where nearly every component wants a LibraryConfiguration
/// — cannot be built by <see cref="Activator" /> alone, so the provider asks the host's container.
/// </summary>
public class InjectedDefaultsTests
{
    private static IServiceProvider WithConfig()
        => new ServiceCollection()
            .AddSingleton(new InjectedCtorFixture.Config())
            .BuildServiceProvider();

    private static IServiceProvider Empty()
        => new ServiceCollection().BuildServiceProvider();

    [Test]
    public void ConstructorDependency_ResolvedFromTheContainer_CapturesDefaults()
    {
        var descriptor = new ReflectionCatalogProvider(services: WithConfig())
            .Describe(typeof(InjectedCtorFixture));

        descriptor.CanInstantiate.Should().BeTrue();
        descriptor.Warning.Should().BeNull();
        descriptor.Parameters.Single(p => p.Name == "Label").DefaultValue.Should().Be("from-ctor");
        descriptor.Parameters.Single(p => p.Name == "Dense").DefaultValue.Should().Be(true);
    }

    [Test]
    public void ConstructorDependency_MissingFromTheContainer_ReportsItWithoutThrowing()
    {
        var descriptor = new ReflectionCatalogProvider(services: Empty())
            .Describe(typeof(InjectedCtorFixture));

        descriptor.CanInstantiate.Should().BeFalse();
        descriptor.Warning.Should().NotBeNull();
    }

    [Test]
    public void NoContainerAtAll_IsTheOldBehaviour()
    {
        var descriptor = new ReflectionCatalogProvider().Describe(typeof(InjectedCtorFixture));

        descriptor.CanInstantiate.Should().BeFalse();
    }

    [Test]
    public void AContainerDoesNotDisturbAParameterlessComponent()
    {
        var withContainer = new ReflectionCatalogProvider(services: WithConfig())
            .Describe(typeof(BasicFixture));
        var without = new ReflectionCatalogProvider().Describe(typeof(BasicFixture));

        withContainer.CanInstantiate.Should().BeTrue();
        withContainer.Parameters.Single(p => p.Name == "Count").DefaultValue
            .Should().Be(without.Parameters.Single(p => p.Name == "Count").DefaultValue);
    }

    [Test]
    public void ADisposableComponent_IsDisposedAfterItsDefaultsAreRead()
    {
        DisposableFixture.Disposals = 0;

        new ReflectionCatalogProvider(services: Empty()).Describe(typeof(DisposableFixture));

        DisposableFixture.Disposals.Should().Be(1);
    }
}
