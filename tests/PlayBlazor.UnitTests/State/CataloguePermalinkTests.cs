using AwesomeAssertions;
using NUnit.Framework;
using PlayBlazor.Discovery;
using PlayBlazor.Model;
using PlayBlazor.Rendering;
using PlayBlazor.State;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.State;

public class CataloguePermalinkTests
{
    private const string SaveMarkup = "<path d='save' />";

    private static CatalogueDefinition Catalogue()
    {
        new PlayBlazorOptions()
            .Catalogue(new Dictionary<string, string>(StringComparer.Ordinal) { ["Save"] = SaveMarkup })
            .TryGetCatalogue(typeof(string), out var catalogue);
        return catalogue;
    }

    private static PlayBlazorOptions Options()
        => new PlayBlazorOptions()
            .Catalogue(new Dictionary<string, string>(StringComparer.Ordinal) { ["Save"] = SaveMarkup });

    private static ParameterDescriptor IconParameter()
        => new("Icon", typeof(string), ControlKind.Icon, IsNullable: true,
            DefaultValue: null, HasDefault: true, Summary: null);

    [Test]
    public void Format_CataloguedValue_YieldsItsName()
        => ParameterValueConverter.Format(IconParameter(), SaveMarkup, Catalogue())
            .Should().Be("Save");

    [Test]
    public void Format_ValueOutsideTheCatalogue_FallsBackToTheRawText()
        => ParameterValueConverter.Format(IconParameter(), "<path d='other' />", Catalogue())
            .Should().Be("<path d='other' />");

    [Test]
    public void TryParse_AName_YieldsTheCataloguedValue()
    {
        ParameterValueConverter.TryParse(IconParameter(), "Save", out var value, Catalogue())
            .Should().BeTrue();

        value.Should().Be(SaveMarkup);
    }

    [Test]
    public void TryParse_LegacyRawMarkup_StillResolves()
    {
        ParameterValueConverter.TryParse(IconParameter(), "<path d='legacy' />", out var value, Catalogue())
            .Should().BeTrue();

        value.Should().Be("<path d='legacy' />");
    }

    [Test]
    public void RoundTrip_ThroughTheCatalogue_IsStable()
    {
        var catalogue = Catalogue();
        var text = ParameterValueConverter.Format(IconParameter(), SaveMarkup, catalogue)!;

        ParameterValueConverter.TryParse(IconParameter(), text, out var value, catalogue).Should().BeTrue();

        value.Should().Be(SaveMarkup);
    }

    [Test]
    public void Decode_TextParameterNamedLikeACatalogueEntry_KeepsTheLiteralText()
    {
        // BasicFixture.Label is an ordinary ControlKind.Text string parameter — nothing about it
        // asks for the icon catalogue. "Save" is both a plausible label and a catalogued icon
        // name; the serializer must not let the catalogue registered for `string` reach a
        // parameter the catalogue was never meant to drive.
        var descriptor = new ReflectionCatalogProvider().Describe(typeof(BasicFixture));
        var options = Options();
        var state = new PlaygroundState();
        state.Set("Label", "Save");
        var encoded = PlaygroundStateSerializer.Encode(descriptor, state, new PlaygroundEnvironment(), options);

        var restored = new PlaygroundState();
        PlaygroundStateSerializer.Decode(encoded, descriptor, restored, new PlaygroundEnvironment(), options);

        restored.GetValue(descriptor.Parameters.Single(p => p.Name == "Label")).Should().Be("Save");
    }
}
