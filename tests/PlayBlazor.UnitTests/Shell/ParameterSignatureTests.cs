using AwesomeAssertions;
using NUnit.Framework;
using PlayBlazor.Discovery;
using PlayBlazor.Shell.Workspace;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Shell;

public class ParameterSignatureTests
{
    private readonly ReflectionCatalogProvider _provider = new();

    private string SignatureOf(string parameterName)
    {
        var descriptor = _provider.Describe(typeof(BasicFixture));
        return ParameterSignature.Format(descriptor.Parameters.Single(p => p.Name == parameterName));
    }

    [Test]
    public void Bool_WithDefault()
        => SignatureOf(nameof(BasicFixture.Dense))
            .Should().Be("[Parameter] public bool Dense { get; set; } = false;");

    [Test]
    public void Enum_WithDefault()
        => SignatureOf(nameof(BasicFixture.Size))
            .Should().Be("[Parameter] public FixtureSize Size { get; set; } = FixtureSize.Medium;");

    [Test]
    public void NullableNumber_WithoutInitializer()
        => SignatureOf(nameof(BasicFixture.MaxItems))
            .Should().Be("[Parameter] public int? MaxItems { get; set; }");

    [Test]
    public void GenericEventCallback()
        => SignatureOf(nameof(BasicFixture.OnValueChanged))
            .Should().Be("[Parameter] public EventCallback<string> OnValueChanged { get; set; }");

    [Test]
    public void NullableString_WithStringDefaultQuoted()
    {
        SignatureOf(nameof(BasicFixture.Label))
            .Should().Be("[Parameter] public string? Label { get; set; }");

        var descriptor = _provider.Describe(typeof(BasicFixture));
        var label = descriptor.Parameters.Single(p => p.Name == nameof(BasicFixture.Label))
            with { DefaultValue = "Hi", HasDefault = true };
        ParameterSignature.Format(label)
            .Should().Be("[Parameter] public string? Label { get; set; } = \"Hi\";");
    }
}
