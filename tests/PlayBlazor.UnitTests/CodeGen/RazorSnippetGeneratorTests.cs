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
        state.Set("OnValueChanged", "ignored");
        state.Set("Label", null);

        RazorSnippetGenerator.Generate(_descriptor, state)
            .Should().Be("<BasicFixture />");
    }

    [Test]
    public void Generate_WithOptions_ShowsHostPresetsAndSlots()
    {
        var options = new PlayBlazorOptions();
        options.For<BasicFixture>()
            .Parameter(nameof(BasicFixture.Count), 9)
            .Parameter(nameof(BasicFixture.Endpoint), new Uri("https://example.test/"))
            .Slot(nameof(BasicFixture.ChildContent), builder => { });

        RazorSnippetGenerator.Generate(_descriptor, new PlaygroundState(), options)
            .Should().Be("""<BasicFixture Count="9" Endpoint="@_endpoint">@* … *@</BasicFixture>""");
    }

    [Test]
    public void Generate_PresetOnUndrivableParameter_SynthesizesFieldReference_NotAStringifiedType()
    {
        // Extra (splatting) and Payload (opaque object) are ControlKind.Undrivable. Neither has a
        // literal Razor form — a Dictionary<,> or a bare object stringifies to something like
        // "System.Collections.Generic.Dictionary`2[...]", which is not valid attribute syntax.
        // A host preset must synthesize the same @_fieldName reference Unsupported already gets.
        var descriptor = new ReflectionCatalogProvider().Describe(typeof(SplattingFixture));
        var options = new PlayBlazorOptions();
        options.For<SplattingFixture>()
            .Parameter(nameof(SplattingFixture.Extra), new Dictionary<string, object> { ["style"] = "color:red" })
            .Parameter(nameof(SplattingFixture.Payload), new object());

        RazorSnippetGenerator.Generate(descriptor, new PlaygroundState(), options)
            .Should().Be("""<SplattingFixture Extra="@_extra" Payload="@_payload" />""");
    }

    [Test]
    public void Generate_UserModification_WinsOverThePreset()
    {
        var options = new PlayBlazorOptions();
        options.For<BasicFixture>().Parameter(nameof(BasicFixture.Count), 9);
        var state = new PlaygroundState();
        state.Set("Count", 4);

        RazorSnippetGenerator.Generate(_descriptor, state, options)
            .Should().Be("""<BasicFixture Count="4" />""");
    }

    [Test]
    public void Generate_NamedSlotPresets_BecomeChildElements()
    {
        var descriptor = new ReflectionCatalogProvider().Describe(typeof(SlottedFixture));
        var options = new PlayBlazorOptions();
        options.For<SlottedFixture>()
            .Slot(nameof(SlottedFixture.Header), builder => { })
            .Slot(nameof(SlottedFixture.ChildContent), builder => { });

        RazorSnippetGenerator.Generate(descriptor, new PlaygroundState(), options).Should().Be(
            "<SlottedFixture>\n" +
            "    @* … *@\n" +
            "    <Header>@* … *@</Header>\n" +
            "</SlottedFixture>");
    }

    [Test]
    public void Generate_SlotSource_IsEmittedVerbatim_SoTheSnippetIsCopyPasteable()
    {
        var options = new PlayBlazorOptions();
        options.For<BasicFixture>().Slot(nameof(BasicFixture.ChildContent), builder => { }, """
            <MudListItem Text="Inbox" />
            <MudListItem Text="Sent" />
            """);

        RazorSnippetGenerator.Generate(_descriptor, new PlaygroundState(), options).Should().Be(
            "<BasicFixture>\n" +
            "    <MudListItem Text=\"Inbox\" />\n" +
            "    <MudListItem Text=\"Sent\" />\n" +
            "</BasicFixture>");
    }

    [Test]
    public void Generate_SingleLineChildSource_StaysInline()
    {
        var options = new PlayBlazorOptions();
        options.For<BasicFixture>().Slot(nameof(BasicFixture.ChildContent), builder => { }, "Click me");

        RazorSnippetGenerator.Generate(_descriptor, new PlaygroundState(), options)
            .Should().Be("<BasicFixture>Click me</BasicFixture>");
    }

    [Test]
    public void Generate_NamedSlotSource_FillsTheChildElement()
    {
        var descriptor = new ReflectionCatalogProvider().Describe(typeof(SlottedFixture));
        var options = new PlayBlazorOptions();
        options.For<SlottedFixture>().Slot(nameof(SlottedFixture.Header), builder => { }, "<b>Title</b>");

        RazorSnippetGenerator.Generate(descriptor, new PlaygroundState(), options).Should().Be(
            "<SlottedFixture>\n" +
            "    <Header>\n" +
            "        <b>Title</b>\n" +
            "    </Header>\n" +
            "</SlottedFixture>");
    }

    [Test]
    public void Generate_ParameterSource_WinsOverThePlaceholder()
    {
        var options = new PlayBlazorOptions();
        options.For<BasicFixture>()
            .Parameter(nameof(BasicFixture.Endpoint), new Uri("https://example.test/"), "@_endpoint");

        RazorSnippetGenerator.Generate(_descriptor, new PlaygroundState(), options)
            .Should().Be("""<BasicFixture Endpoint="@_endpoint" />""");
    }

    [Test]
    public void Generate_ScaffoldSource_WrapsTheSnippet_SoScaffoldedBenchesCopyComplete()
    {
        var options = new PlayBlazorOptions();
        options.For<BasicFixture>().Scaffold(static specimen => specimen, """
            <Parent Items="@_items">
                {specimen}
            </Parent>
            """);
        var state = new PlaygroundState();
        state.Set("Dense", true);

        RazorSnippetGenerator.Generate(_descriptor, state, options).Should().Be(
            "<Parent Items=\"@_items\">\n" +
            "    <BasicFixture Dense=\"true\" />\n" +
            "</Parent>");
    }

    [Test]
    public void Generate_SourceLines_AreColorizedLikeGeneratedCode()
    {
        var options = new PlayBlazorOptions();
        options.For<BasicFixture>().Slot(nameof(BasicFixture.ChildContent), builder => { },
            """<MudListItem Text="Inbox" />""");

        var markup = RazorSnippetGenerator.GenerateMarkup(_descriptor, new PlaygroundState(), options).Value;

        markup.Should().Contain("pb-tok-tag\">MudListItem")
            .And.Contain("pb-tok-attr\">Text")
            .And.Contain("pb-tok-val\">Inbox");
    }

    [Test]
    public void Generate_GenericClosing_BecomesTypeAttributes()
    {
        var descriptor = new ReflectionCatalogProvider().Describe(typeof(GenericFixture<int>));

        RazorSnippetGenerator.Generate(descriptor, new PlaygroundState())
            .Should().Be("""<GenericFixture TItem="int" />""");
    }

    [Test]
    public void Generate_CataloguedIconValue_OmitsTheAttribute_SinceItHasNoLiteralForm()
    {
        var options = new PlayBlazorOptions().Catalogue(new Dictionary<string, CatalogueFixture.Glyph>(StringComparer.Ordinal)
        {
            ["Save"] = new("<path d='save' />"),
        });
        var descriptor = new ReflectionCatalogProvider(options: options).Describe(typeof(CatalogueFixture));
        var state = new PlaygroundState();
        state.Set("Icon", new CatalogueFixture.Glyph("<path d='save' />"));

        // Glyph has no defined Razor text form; emitting whatever ToString() happens to return
        // would look like real markup but not compile — omitting the attribute is honest,
        // copy-pasteable output, exactly what a host with no catalogue registered would produce.
        RazorSnippetGenerator.Generate(descriptor, state, options)
            .Should().Be("<CatalogueFixture />");
    }

    [Test]
    public void Generate_StringIconValue_StillAppearsVerbatim_LikeAMudBlazorIconString()
    {
        var descriptor = new ReflectionCatalogProvider().Describe(typeof(CatalogueFixture));
        var state = new PlaygroundState();
        state.Set("TrailingIcon", "<path d='trailing' />");

        RazorSnippetGenerator.Generate(descriptor, state)
            .Should().Be("""<CatalogueFixture TrailingIcon="<path d='trailing' />" />""");
    }
}
