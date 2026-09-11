using AwesomeAssertions;
using NUnit.Framework;
using PlayBlazor.Model;
using PlayBlazor.State;

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
}
