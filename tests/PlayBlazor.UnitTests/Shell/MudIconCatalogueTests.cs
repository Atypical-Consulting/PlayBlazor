using AwesomeAssertions;
using MudBlazor;
using NUnit.Framework;
using PlayBlazor.DemoHost;

namespace PlayBlazor.UnitTests.Shell;

public class MudIconCatalogueTests
{
    [Test]
    public void Filled_HoldsTheMaterialConstants()
    {
        MudIconCatalogue.Filled.Should().ContainKey("Delete");
        MudIconCatalogue.Filled["Delete"].Should().Be(Icons.Material.Filled.Delete);
    }

    [Test]
    public void Filled_IsLargeEnoughToBeWorthAPicker()
        => MudIconCatalogue.Filled.Count.Should().BeGreaterThan(100);

    [Test]
    public void Filled_ValuesAreSvgMarkup()
        => MudIconCatalogue.Filled["Delete"].TrimStart().Should().StartWith("<");
}
