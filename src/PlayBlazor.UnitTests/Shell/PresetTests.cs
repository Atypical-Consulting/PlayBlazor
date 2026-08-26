using AwesomeAssertions;
using Bunit;
using NUnit.Framework;
using PlayBlazor.CodeGen;
using PlayBlazor.Discovery;
using PlayBlazor.Rendering;
using PlayBlazor.State;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Shell;

public class PresetTests
{
    private BunitContext _context = null!;

    [SetUp]
    public void Setup()
    {
        _context = new BunitContext();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    private IRenderedComponent<PlaygroundView> RenderView(Action<PlayBlazorOptions>? configure = null)
    {
        _context.Services.AddPlayBlazor(configure);
        return _context.Render<PlaygroundView>(ps => ps.Add(v => v.Component, typeof(BasicFixture)));
    }

    [Test]
    public void ParameterPreset_AppliesWithoutModification()
    {
        var cut = RenderView(o => o.For<BasicFixture>().Parameter(nameof(BasicFixture.Dense), true));

        cut.Find(".basic-fixture").TextContent.Should().Contain("Dense=True");
        cut.FindAll("input[type=checkbox]")[0].HasAttribute("checked").Should().BeTrue();
    }

    [Test]
    public void UserModification_WinsOverPreset()
    {
        var cut = RenderView(o => o.For<BasicFixture>().Parameter(nameof(BasicFixture.Count), 42));

        cut.FindAll("input[type=number]")[0].Change("7"); // Count

        cut.Find(".basic-fixture").TextContent.Should().Contain("Count=7");
    }

    [Test]
    public void RowReset_ReturnsToPreset_NotComponentDefault()
    {
        var cut = RenderView(o => o.For<BasicFixture>().Parameter(nameof(BasicFixture.Dense), true));
        cut.FindAll("input[type=checkbox]")[0].Change(false);
        cut.Find(".basic-fixture").TextContent.Should().Contain("Dense=False");

        cut.Find(".pb-row-reset").Click();

        cut.Find(".basic-fixture").TextContent.Should().Contain("Dense=True");
    }

    [Test]
    public void Preset_IsNotEmittedInSnippet()
    {
        var cut = RenderView(o => o.For<BasicFixture>().Parameter(nameof(BasicFixture.Dense), true));

        cut.Find(".pb-code code").TextContent.Should().Be("<BasicFixture />");
    }

    [Test]
    public void UnsupportedParameter_PresetIsInjected()
    {
        var descriptor = new ReflectionCatalogProvider().Describe(typeof(BasicFixture));
        var options = new PlayBlazorOptions();
        var endpoint = new Uri("https://example.test/");
        options.For<BasicFixture>().Parameter(nameof(BasicFixture.Endpoint), endpoint);

        var parameters = ParameterDictionaryBuilder.Build(descriptor, new PlaygroundState(), options);

        parameters["Endpoint"].Should().BeSameAs(endpoint);
    }

    [Test]
    public void Scaffold_WrapsSpecimen()
    {
        var cut = RenderView(o => o.For<BasicFixture>().Scaffold(specimen => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "scaffold-marker");
            builder.AddContent(2, specimen);
            builder.CloseElement();
        }));

        cut.Find(".pb-specimen .scaffold-marker .basic-fixture").Should().NotBeNull();
    }

    [Test]
    public void Scaffold_ControlsStillDriveTheSpecimen()
    {
        var cut = RenderView(o => o.For<BasicFixture>().Scaffold(specimen => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "scaffold-marker");
            builder.AddContent(2, specimen);
            builder.CloseElement();
        }));

        cut.FindAll("input[type=checkbox]")[0].Change(true); // Dense

        cut.Find(".scaffold-marker .basic-fixture").TextContent.Should().Contain("Dense=True");
        cut.Find(".pb-code code").TextContent.Should().Be("""<BasicFixture Dense="true" />""");
    }

    [Test]
    public void OpenGenericPreset_AppliesToClosedType()
    {
        var descriptor = new ReflectionCatalogProvider().Describe(typeof(GenericFixture<int>));
        var options = new PlayBlazorOptions();
        options.For<GenericFixture<string>>().Parameter("Value", 5);

        var state = new PlaygroundState();
        var generated = RazorSnippetGenerator.Generate(descriptor, state);

        ParameterDictionaryBuilder.Build(descriptor, state, options).Should().ContainKey("Value");
        generated.Should().Be("<GenericFixture />");
    }
}
