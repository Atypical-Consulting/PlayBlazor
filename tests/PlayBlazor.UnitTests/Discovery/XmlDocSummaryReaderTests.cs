using System.Text;
using AwesomeAssertions;
using NUnit.Framework;
using PlayBlazor.Discovery;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Discovery;

public class XmlDocSummaryReaderTests
{
    private const string Xml = """
        <?xml version="1.0"?>
        <doc>
          <assembly><name>Fixture</name></assembly>
          <members>
            <member name="T:PlayBlazor.UnitTests.Fixtures.BasicFixture">
              <summary>A basic fixture.</summary>
            </member>
            <member name="P:PlayBlazor.UnitTests.Fixtures.BasicFixture.Dense">
              <summary>
                Renders with <see cref="T:PlayBlazor.UnitTests.Fixtures.FixtureSize"/> compact spacing.
              </summary>
            </member>
            <member name="P:PlayBlazor.UnitTests.Fixtures.GenericFixture`1.Value">
              <summary>The bound value.</summary>
            </member>
          </members>
        </doc>
        """;

    private static XmlDocSummaryReader CreateReader()
        => XmlDocSummaryReader.FromStream(new MemoryStream(Encoding.UTF8.GetBytes(Xml)));

    [Test]
    public void GetTypeSummary_ReturnsSummary()
    {
        CreateReader().GetTypeSummary(typeof(BasicFixture)).Should().Be("A basic fixture.");
    }

    [Test]
    public void GetPropertySummary_NormalizesWhitespaceAndSeeRefs()
    {
        var property = typeof(BasicFixture).GetProperty(nameof(BasicFixture.Dense))!;

        CreateReader().GetPropertySummary(property)
            .Should().Be("Renders with FixtureSize compact spacing.");
    }

    [Test]
    public void GetPropertySummary_ResolvesClosedGenericsToOpenDefinition()
    {
        var property = typeof(GenericFixture<string>).GetProperty("Value")!;

        CreateReader().GetPropertySummary(property).Should().Be("The bound value.");
    }

    [Test]
    public void GetTypeSummary_UnknownType_ReturnsNull()
    {
        CreateReader().GetTypeSummary(typeof(ThrowingCtorFixture)).Should().BeNull();
    }

    [Test]
    public void Describe_WithReader_PopulatesSummaries()
    {
        var provider = new ReflectionCatalogProvider(CreateReader());

        var descriptor = provider.Describe(typeof(BasicFixture));

        descriptor.Summary.Should().Be("A basic fixture.");
        descriptor.Parameters.Single(p => p.Name == "Dense").Summary
            .Should().Be("Renders with FixtureSize compact spacing.");
        descriptor.Parameters.Single(p => p.Name == "Label").Summary.Should().BeNull();
    }
}
