using AwesomeAssertions;
using NUnit.Framework;
using PlayBlazor.Discovery;
using PlayBlazor.Model;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Discovery;

public class CatalogueKindTests
{
    private static PlayBlazorOptions WithGlyphCatalogue()
        => new PlayBlazorOptions().Catalogue(new Dictionary<string, CatalogueFixture.Glyph>(StringComparer.Ordinal)
        {
            ["Save"] = new("<path d='save' />"),
        });

    private static ParameterDescriptor Parameter(PlayBlazorOptions? options, string name)
        => new ReflectionCatalogProvider(options: options)
            .Describe(typeof(CatalogueFixture))
            .Parameters.Single(p => p.Name == name);

    [Test]
    public void CataloguedType_BecomesAnIconControl()
        => Parameter(WithGlyphCatalogue(), "Icon").Kind.Should().Be(ControlKind.Icon);

    [Test]
    public void CataloguedType_WithoutACatalogue_StaysUnsupported()
        => Parameter(null, "Icon").Kind.Should().Be(ControlKind.Unsupported);

    [Test]
    public void OrdinaryString_IsNotWidenedByAStringCatalogue()
    {
        var options = new PlayBlazorOptions().Catalogue(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Save"] = "<path d='save' />",
        });

        Parameter(options, "Label").Kind.Should().Be(ControlKind.Text);
    }

    [Test]
    public void StringNamedIcon_StaysAnIconControl()
        => Parameter(null, "TrailingIcon").Kind.Should().Be(ControlKind.Icon);
}
