using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using NUnit.Framework;
using PlayBlazor.CodeGen;
using PlayBlazor.Discovery;
using PlayBlazor.Rendering;
using PlayBlazor.State;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Rendering;

public class SlotTests
{
    private static readonly RenderFragment Preset = builder => builder.AddContent(0, "preset!");

    private static PlayBlazor.Model.ComponentDescriptor Describe()
        => new ReflectionCatalogProvider().Describe(typeof(BasicFixture));

    [Test]
    public void Build_SlotPreset_IncludedWithoutModification()
    {
        var options = new PlayBlazorOptions();
        options.For<BasicFixture>().Slot("ChildContent", Preset);

        var parameters = ParameterDictionaryBuilder.Build(Describe(), new PlaygroundState(), options);

        parameters.Should().ContainKey("ChildContent");
        parameters["ChildContent"].Should().BeSameAs(Preset);
    }

    [Test]
    public void Build_ModifiedSlotText_WinsOverPreset()
    {
        var options = new PlayBlazorOptions();
        options.For<BasicFixture>().Slot("ChildContent", Preset);
        var state = new PlaygroundState();
        state.Set("ChildContent", "typed");

        var parameters = ParameterDictionaryBuilder.Build(Describe(), state, options);

        parameters["ChildContent"].Should().BeOfType<RenderFragment>().And.NotBeSameAs(Preset);
    }

    [Test]
    public void Build_NoPresetNoText_SlotOmitted()
    {
        ParameterDictionaryBuilder.Build(Describe(), new PlaygroundState(), new PlayBlazorOptions())
            .Should().NotContainKey("ChildContent");
    }

    [Test]
    public void Generate_SlotText_EmitsChildContent()
    {
        var state = new PlaygroundState();
        state.Set("ChildContent", "hello");

        RazorSnippetGenerator.Generate(Describe(), state)
            .Should().Be("<BasicFixture>hello</BasicFixture>");
    }

    [Test]
    public void Generate_SlotTextWithAttributes_SingleLine()
    {
        var state = new PlaygroundState();
        state.Set("Dense", true);
        state.Set("ChildContent", "hello");

        RazorSnippetGenerator.Generate(Describe(), state)
            .Should().Be("""<BasicFixture Dense="true">hello</BasicFixture>""");
    }

    [Test]
    public void Generate_SlotTextEscapesMarkup()
    {
        var state = new PlaygroundState();
        state.Set("ChildContent", "a <b> & c");

        RazorSnippetGenerator.Generate(Describe(), state)
            .Should().Be("<BasicFixture>a &lt;b&gt; &amp; c</BasicFixture>");
    }

    [Test]
    public void PlaygroundView_TypingSlotText_ShowsInPreview()
    {
        using var context = new BunitContext();
        context.Services.AddPlayBlazor();
        var cut = context.Render<PlaygroundView>(ps => ps.Add(v => v.Component, typeof(BasicFixture)));

        // Text inputs in declaration order: Label, then ChildContent (text slot).
        cut.FindAll("input[type=text]")[1].Change("slot text!");

        cut.Find(".basic-fixture").TextContent.Should().Contain("slot text!");
    }
}
