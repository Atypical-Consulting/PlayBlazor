using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PlayBlazor.Model;
using PlayBlazor.Shell;

namespace PlayBlazor.UnitTests.Shell;

public class IconPickerTests
{
    private BunitContext _context = null!;

    [SetUp]
    public void Setup() => _context = new BunitContext();

    [TearDown]
    public void TearDown() => _context.Dispose();

    private static readonly Dictionary<string, string> Icons = new(StringComparer.Ordinal)
    {
        ["Save"] = "<path d='save' />",
        ["Delete"] = "<path d='delete' />",
    };

    private static ParameterDescriptor IconParameter()
        => new("Icon", typeof(string), ControlKind.Icon, IsNullable: true,
            DefaultValue: null, HasDefault: true, Summary: "the docs");

    private void Register(PlayBlazorOptions options)
        => _context.Services.AddSingleton(options);

    [Test]
    public void WithCatalogue_ListsEveryNameAndReportsTheValue()
    {
        Register(new PlayBlazorOptions().Catalogue(Icons));
        object? reported = null;

        var cut = _context.Render<IconControl>(ps => ps
            .Add(c => c.Parameter, IconParameter())
            .Add(c => c.Value, null)
            .Add(c => c.ValueChanged, v => reported = v));

        cut.FindAll("datalist option").Count.Should().Be(2);
        cut.Find("input[list]").Change("Delete");

        reported.Should().Be("<path d='delete' />");
    }

    [Test]
    public void WithCatalogue_ShowsTheNameOfTheCurrentValue()
    {
        Register(new PlayBlazorOptions().Catalogue(Icons));

        var cut = _context.Render<IconControl>(ps => ps
            .Add(c => c.Parameter, IconParameter())
            .Add(c => c.Value, "<path d='save' />"));

        cut.Find("input[list]").GetAttribute("value").Should().Be("Save");
    }

    [Test]
    public void WithCatalogue_UnknownName_ReportsNull()
    {
        Register(new PlayBlazorOptions().Catalogue(Icons));
        object? reported = "unset";

        var cut = _context.Render<IconControl>(ps => ps
            .Add(c => c.Parameter, IconParameter())
            .Add(c => c.Value, null)
            .Add(c => c.ValueChanged, v => reported = v));

        cut.Find("input[list]").Change("NotAnIcon");

        reported.Should().BeNull();
    }

    [Test]
    public void WithoutCatalogue_KeepsThePlainTextBoxAndTheSvgPreview()
    {
        Register(new PlayBlazorOptions());

        var cut = _context.Render<IconControl>(ps => ps
            .Add(c => c.Parameter, IconParameter())
            .Add(c => c.Value, "<path d='save' />"));

        cut.FindAll("datalist").Should().BeEmpty();
        cut.FindAll("svg.pb-icon-preview").Count.Should().Be(1);
        cut.Find("input[type=text]").GetAttribute("value").Should().Be("<path d='save' />");
    }

    [Test]
    public void WithCataloguePreview_RendersTheThumbnail()
    {
        Register(new PlayBlazorOptions().Catalogue(
            Icons,
            static markup => builder =>
            {
                builder.OpenElement(0, "i");
                builder.AddAttribute(1, "class", "thumb");
                builder.AddContent(2, markup);
                builder.CloseElement();
            }));

        var cut = _context.Render<IconControl>(ps => ps
            .Add(c => c.Parameter, IconParameter())
            .Add(c => c.Value, "<path d='save' />"));

        cut.FindAll("i.thumb").Count.Should().Be(1);
    }
}
