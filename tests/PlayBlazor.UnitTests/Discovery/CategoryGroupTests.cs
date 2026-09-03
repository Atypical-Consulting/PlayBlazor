using AwesomeAssertions;
using MudBlazor;
using NUnit.Framework;
using PlayBlazor.Discovery;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Discovery;

public class CategoryGroupTests
{
    private readonly ReflectionCatalogProvider _provider = new();

    [Test]
    public void MudBlazorCategoryAttribute_BecomesTheParameterGroup()
    {
        var descriptor = _provider.Describe(typeof(MudButton));

        var variant = descriptor.Parameters.Single(p => p.Name == nameof(MudButton.Variant));
        variant.Group.Should().Be("Appearance");
        variant.GroupOrder.Should().BeLessThan(int.MaxValue);
    }

    [Test]
    public void StringIconParameters_GetTheIconKind()
    {
        var descriptor = _provider.Describe(typeof(MudButton));

        descriptor.Parameters.Single(p => p.Name == nameof(MudButton.StartIcon))
            .Kind.Should().Be(PlayBlazor.Model.ControlKind.Icon);
    }

    [Test]
    public void ParameterWithoutCategory_FallsBackToGeneral()
    {
        var descriptor = _provider.Describe(typeof(BasicFixture));

        var dense = descriptor.Parameters.Single(p => p.Name == nameof(BasicFixture.Dense));
        dense.Group.Should().Be("General");
        dense.GroupOrder.Should().Be(int.MaxValue);
    }
}
