using AwesomeAssertions;
using NUnit.Framework;

namespace PlayBlazor.UnitTests;

public class CatalogueTests
{
    private static readonly Dictionary<string, string> Icons = new(StringComparer.Ordinal)
    {
        ["Save"] = "<path d='save' />",
        ["Delete"] = "<path d='delete' />",
    };

    [Test]
    public void Catalogue_IsFoundByType()
    {
        var options = new PlayBlazorOptions().Catalogue(Icons);

        options.TryGetCatalogue(typeof(string), out var catalogue).Should().BeTrue();
        catalogue.Named.Should().ContainKey("Save");
        catalogue.Named["Save"].Should().Be("<path d='save' />");
    }

    [Test]
    public void Catalogue_UnregisteredType_IsNotFound()
    {
        var options = new PlayBlazorOptions().Catalogue(Icons);

        options.TryGetCatalogue(typeof(int), out _).Should().BeFalse();
    }

    [Test]
    public void TryGetName_MapsAValueBackToItsName()
    {
        new PlayBlazorOptions().Catalogue(Icons).TryGetCatalogue(typeof(string), out var catalogue);

        catalogue.TryGetName("<path d='delete' />", out var name).Should().BeTrue();
        name.Should().Be("Delete");
    }

    [Test]
    public void TryGetName_UnknownValue_ReportsFalse()
    {
        new PlayBlazorOptions().Catalogue(Icons).TryGetCatalogue(typeof(string), out var catalogue);

        catalogue.TryGetName("<path d='unknown' />", out _).Should().BeFalse();
    }

    [Test]
    public void TryGetName_Null_ReportsFalse()
    {
        new PlayBlazorOptions().Catalogue(Icons).TryGetCatalogue(typeof(string), out var catalogue);

        catalogue.TryGetName(null, out _).Should().BeFalse();
    }

    [Test]
    public void TwoNamesForOneValue_FirstNameWins()
    {
        var sharedValue = "<path d='shared' />";
        var glyphs = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["First"] = sharedValue,
            ["Second"] = sharedValue,
        };

        new PlayBlazorOptions().Catalogue(glyphs).TryGetCatalogue(typeof(string), out var catalogue);

        catalogue.TryGetName(sharedValue, out var name).Should().BeTrue();
        name.Should().Be("First");
    }

    [Test]
    public void ReferenceType_WithoutEqualsOrHashCode_RoundTripsOnIdentity()
    {
        var glyph1 = new Glyph("<path d='glyph' />");
        var glyph2 = new Glyph("<path d='glyph' />");
        var glyphs = new Dictionary<string, Glyph>(StringComparer.Ordinal)
        {
            ["Glyph1"] = glyph1,
        };

        new PlayBlazorOptions().Catalogue(glyphs).TryGetCatalogue(typeof(Glyph), out var catalogue);

        // The exact instance should round-trip
        catalogue.TryGetName(glyph1, out var name).Should().BeTrue();
        name.Should().Be("Glyph1");

        // A different instance with identical contents should not round-trip (reference identity)
        catalogue.TryGetName(glyph2, out _).Should().BeFalse();
    }

    private sealed class Glyph(string markup)
    {
        public string Markup { get; } = markup;
    }
}
