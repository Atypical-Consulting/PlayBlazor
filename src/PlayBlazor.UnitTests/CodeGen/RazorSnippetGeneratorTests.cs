using AwesomeAssertions;
using NUnit.Framework;
using PlayBlazor.CodeGen;
using PlayBlazor.Discovery;
using PlayBlazor.Model;
using PlayBlazor.State;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.CodeGen;

public class RazorSnippetGeneratorTests
{
    private ComponentDescriptor _descriptor = null!;

    [SetUp]
    public void Setup()
    {
        _descriptor = new ReflectionCatalogProvider().Describe(typeof(BasicFixture));
    }

    [Test]
    public void Generate_NoModifications_EmitsSelfClosingTag()
    {
        RazorSnippetGenerator.Generate(_descriptor, new PlaygroundState())
            .Should().Be("<BasicFixture />");
    }

    [Test]
    public void Generate_TwoAttributes_SingleLine()
    {
        var state = new PlaygroundState();
        state.Set("Dense", true);
        state.Set("Label", "Hello");

        RazorSnippetGenerator.Generate(_descriptor, state)
            .Should().Be("""<BasicFixture Dense="true" Label="Hello" />""");
    }

    [Test]
    public void Generate_ThreeAttributes_MultiLineAligned()
    {
        var state = new PlaygroundState();
        state.Set("Dense", true);
        state.Set("Size", FixtureSize.Large);
        state.Set("Count", 7);

        RazorSnippetGenerator.Generate(_descriptor, state).Should().Be(
            "<BasicFixture Dense=\"true\"\n" +
            "              Size=\"FixtureSize.Large\"\n" +
            "              Count=\"7\" />");
    }

    [Test]
    public void Generate_UsesDeclarationOrder_NotModificationOrder()
    {
        var state = new PlaygroundState();
        state.Set("Label", "x");
        state.Set("Dense", true);

        RazorSnippetGenerator.Generate(_descriptor, state)
            .Should().Be("""<BasicFixture Dense="true" Label="x" />""");
    }

    [Test]
    public void Generate_FormatsNumbersWithInvariantCulture()
    {
        var state = new PlaygroundState();
        state.Set("Ratio", 2.75);

        RazorSnippetGenerator.Generate(_descriptor, state)
            .Should().Be("""<BasicFixture Ratio="2.75" />""");
    }

    [Test]
    public void Generate_EscapesQuotesInStrings()
    {
        var state = new PlaygroundState();
        state.Set("Label", "say \"hi\"");

        RazorSnippetGenerator.Generate(_descriptor, state)
            .Should().Be("""<BasicFixture Label="say &quot;hi&quot;" />""");
    }

    [Test]
    public void Generate_SkipsNonDrivableAndNullValues()
    {
        var state = new PlaygroundState();
        state.Set("ChildContent", "ignored");
        state.Set("Label", null);

        RazorSnippetGenerator.Generate(_descriptor, state)
            .Should().Be("<BasicFixture />");
    }
}
