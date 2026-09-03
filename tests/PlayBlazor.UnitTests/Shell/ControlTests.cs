using AwesomeAssertions;
using Bunit;
using NUnit.Framework;
using PlayBlazor.Model;
using PlayBlazor.Shell;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Shell;

public class ControlTests
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

    private static ParameterDescriptor Descriptor(string name, Type type, ControlKind kind, object? defaultValue = null)
        => new(name, type, kind, IsNullable: false, DefaultValue: defaultValue, HasDefault: true, Summary: "the docs");

    [Test]
    public void BoolControl_Change_ReportsBoolean()
    {
        object? reported = "unset";
        var cut = _context.Render<BoolControl>(ps => ps
            .Add(c => c.Parameter, Descriptor("Dense", typeof(bool), ControlKind.Bool, false))
            .Add(c => c.Value, false)
            .Add(c => c.ValueChanged, v => reported = v));

        cut.Find("input[type=checkbox]").Change(true);

        reported.Should().Be(true);
    }

    [Test]
    public void EnumControl_ListsNamesAndReportsEnumValue()
    {
        object? reported = null;
        var cut = _context.Render<EnumControl>(ps => ps
            .Add(c => c.Parameter, Descriptor("Size", typeof(FixtureSize), ControlKind.Enum, FixtureSize.Medium))
            .Add(c => c.Value, FixtureSize.Medium)
            .Add(c => c.ValueChanged, v => reported = v));

        cut.FindAll("option").Count.Should().Be(3);
        cut.Find("select").Change("Large");

        reported.Should().Be(FixtureSize.Large);
    }

    [Test]
    public void TextControl_EmptyInput_ReportsNull()
    {
        object? reported = "unset";
        var cut = _context.Render<TextControl>(ps => ps
            .Add(c => c.Parameter, Descriptor("Label", typeof(string), ControlKind.Text))
            .Add(c => c.Value, "hello")
            .Add(c => c.ValueChanged, v => reported = v));

        cut.Find("input[type=text]").Change("");

        reported.Should().BeNull();
    }

    [Test]
    public void NumberControl_ParsesWithParameterType()
    {
        object? reported = null;
        var cut = _context.Render<NumberControl>(ps => ps
            .Add(c => c.Parameter, Descriptor("Ratio", typeof(double), ControlKind.Number, 0.5))
            .Add(c => c.Value, 0.5)
            .Add(c => c.ValueChanged, v => reported = v));

        cut.Find("input[type=number]").Change("2.75");

        reported.Should().Be(2.75);
    }

    [Test]
    public void NumberControl_UnparsableInput_ReportsNothing()
    {
        object? reported = "unset";
        var cut = _context.Render<NumberControl>(ps => ps
            .Add(c => c.Parameter, Descriptor("Count", typeof(int), ControlKind.Number, 3))
            .Add(c => c.Value, 3)
            .Add(c => c.ValueChanged, v => reported = v));

        cut.Find("input[type=number]").Change("abc");

        reported.Should().Be("unset");
    }

    [Test]
    public void ControlHost_DispatchesOnKind()
    {
        var cut = _context.Render<ControlHost>(ps => ps
            .Add(c => c.Parameter, Descriptor("Dense", typeof(bool), ControlKind.Bool, false)));

        cut.FindAll("input[type=checkbox]").Count.Should().Be(1);
    }

    [Test]
    public void ControlHost_UnsupportedKind_RendersNothing()
    {
        var cut = _context.Render<ControlHost>(ps => ps
            .Add(c => c.Parameter, Descriptor("Endpoint", typeof(Uri), ControlKind.Unsupported)));

        cut.Markup.Trim().Should().BeEmpty();
    }
}
