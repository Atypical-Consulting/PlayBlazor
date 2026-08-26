using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PlayBlazor.Discovery;

namespace PlayBlazor.UnitTests;

public class ServiceRegistrationTests
{
    [Test]
    public void AddPlayBlazor_RegistersSingletonCatalogProvider()
    {
        var services = new ServiceCollection().AddPlayBlazor().BuildServiceProvider();

        var first = services.GetRequiredService<IComponentCatalogProvider>();
        var second = services.GetRequiredService<IComponentCatalogProvider>();

        first.Should().BeOfType<ReflectionCatalogProvider>();
        first.Should().BeSameAs(second);
    }
}
