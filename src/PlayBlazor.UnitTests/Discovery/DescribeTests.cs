using AwesomeAssertions;
using NUnit.Framework;
using PlayBlazor.Discovery;
using PlayBlazor.Model;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Discovery;

public class DescribeTests
{
    private ReflectionCatalogProvider _provider = null!;

    [SetUp]
    public void Setup()
    {
        _provider = new ReflectionCatalogProvider();
    }

    [Test]
    public void Describe_ListsAllParameterProperties()
    {
        var descriptor = _provider.Describe(typeof(BasicFixture));

        descriptor.DisplayName.Should().Be("BasicFixture");
        descriptor.Category.Should().Be("PlayBlazor.UnitTests.Fixtures");
        descriptor.Warning.Should().BeNull();
        descriptor.Parameters.Select(p => p.Name).Should().BeEquivalentTo(
            "Dense", "Outlined", "Size", "Label", "Count", "Ratio",
            "MaxItems", "OnValueChanged", "ChildContent", "Endpoint");
    }

    [Test]
    public void Describe_ResolvesKindsAndNullability()
    {
        var descriptor = _provider.Describe(typeof(BasicFixture));
        var byName = descriptor.Parameters.ToDictionary(p => p.Name);

        byName["Dense"].Kind.Should().Be(ControlKind.Bool);
        byName["Size"].Kind.Should().Be(ControlKind.Enum);
        byName["Label"].Kind.Should().Be(ControlKind.Text);
        byName["Count"].Kind.Should().Be(ControlKind.Number);
        byName["MaxItems"].Kind.Should().Be(ControlKind.Number);
        byName["MaxItems"].IsNullable.Should().BeTrue();
        byName["OnValueChanged"].Kind.Should().Be(ControlKind.Event);
        byName["ChildContent"].Kind.Should().Be(ControlKind.Slot);
        byName["Endpoint"].Kind.Should().Be(ControlKind.Unsupported);
    }

    [Test]
    public void Describe_CapturesDefaultValues()
    {
        var descriptor = _provider.Describe(typeof(BasicFixture));
        var byName = descriptor.Parameters.ToDictionary(p => p.Name);

        byName["Dense"].HasDefault.Should().BeTrue();
        byName["Dense"].DefaultValue.Should().Be(false);
        byName["Outlined"].DefaultValue.Should().Be(true);
        byName["Size"].DefaultValue.Should().Be(FixtureSize.Medium);
        byName["Count"].DefaultValue.Should().Be(3);
        byName["Ratio"].DefaultValue.Should().Be(0.5);
        byName["Label"].DefaultValue.Should().BeNull();
        byName["Label"].HasDefault.Should().BeTrue();
    }

    [Test]
    public void Describe_ThrowingConstructor_ProducesWarningWithoutDefaults()
    {
        var descriptor = _provider.Describe(typeof(ThrowingCtorFixture));

        descriptor.Warning.Should().NotBeNull();
        descriptor.Parameters.Should().NotBeEmpty();
        descriptor.Parameters.Should().OnlyContain(p => !p.HasDefault);
    }

    [Test]
    public void Describe_StripsGenericArityFromDisplayName()
    {
        var descriptor = _provider.Describe(typeof(TestGeneric<string>));

        descriptor.DisplayName.Should().Be("TestGeneric");
    }

    private class TestGeneric<T> : Microsoft.AspNetCore.Components.ComponentBase
    {
        [Microsoft.AspNetCore.Components.Parameter]
        public T? Value { get; set; }
    }
}
