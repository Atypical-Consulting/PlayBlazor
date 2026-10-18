using AwesomeAssertions;
using Bunit;
using DaisyBlazor;
using DaisyBlazor.Data;
using DaisyBlazor.Feedback;
using DaisyBlazor.Theming;
using NUnit.Framework;
using PlayBlazor.Demo.Daisy;
using PlayBlazor.Discovery;

namespace PlayBlazor.UnitTests.Shell;

public class DaisyCurationTests
{
    private static (PlayBlazorOptions Options, IReadOnlyList<PlayBlazor.Model.ComponentDescriptor> Listed) Curated()
    {
        var options = new PlayBlazorOptions();
        DaisyPlaygroundConfig.Configure(options);
        var listed = new ReflectionCatalogProvider(options: options)
            .Discover(typeof(Button).Assembly)
            .Where(c => !options.IsExcluded(c.Type) && (options.ComponentFilter?.Invoke(c.Type) ?? true))
            .ToList();
        return (options, listed);
    }

    [Test]
    public void OnlyTheFourInfrastructureComponents_AreNotListed()
    {
        var names = Curated().Listed.Select(c => c.DisplayName).ToList();

        names.Should().NotContain([nameof(ThemeProvider), nameof(DialogProvider), nameof(SnackbarProvider), nameof(PageBreadcrumb)]);
        names.Should().Contain([nameof(Breadcrumbs), nameof(AppCard), nameof(AccessDenied), nameof(QuickAccessButton)]);
    }

    [TestCase(typeof(NumericField<>), typeof(NumericField<int>))]
    [TestCase(typeof(Table<>), typeof(Table<Person>))]
    [TestCase(typeof(DataGrid<>), typeof(DataGrid<Person>))]
    [TestCase(typeof(DataTable<>), typeof(DataTable<Person>))]
    [TestCase(typeof(DataGridPager<>), typeof(DataGridPager<Person>))]
    [TestCase(typeof(TemplateColumn<>), typeof(TemplateColumn<Person>))]
    public void GenericsAreClosedWithATypeWorthPlaying(Type open, Type expected)
        => Curated().Options.ResolvePreferredClosing(open.MakeGenericType(typeof(string))).Should().Be(expected);

    [Test]
    public void PropertyColumn_IsClosedOverPersonRows()
        => Curated().Options.ResolvePreferredClosing(typeof(PropertyColumn<string, string>))
            .Should().Be(typeof(PropertyColumn<Person, string>));

    [TestCase(typeof(Select<>))]
    [TestCase(typeof(RadioGroup<>))]
    [TestCase(typeof(ToggleGroup<>))]
    public void ChoiceComponents_StayClosedWithString(Type open)
    {
        var discovered = open.MakeGenericType(typeof(string));
        Curated().Options.ResolvePreferredClosing(discovered).Should().Be(discovered);
    }

    [Test]
    public void IconCatalogue_OffersMaterialLigatures_AndDrivesIconParameters()
    {
        DaisyIconCatalogue.Material.Should().HaveCountGreaterThan(1000);
        DaisyIconCatalogue.Material["Check"].Should().Be("check");

        var button = new ReflectionCatalogProvider(options: Curated().Options).Describe(typeof(Button));
        button.Parameters.Single(p => p.Name == nameof(Button.StartIcon)).Kind.Should().Be(PlayBlazor.Model.ControlKind.Icon);
    }

    [Test]
    public void NumericFieldInt_ParsesTypedInputBackToAnInt()
    {
        using var context = new BunitContext();
        int? received = null;
        var cut = context.Render<NumericField<int>>(ps => ps
            .Add(c => c.Value, 7)
            .Add(c => c.ValueChanged, v => received = v));

        var input = cut.Find("input[type=number]");
        input.GetAttribute("step").Should().BeNull("only double/float/decimal switch to step=\"any\"");
        input.Change("42");

        received.Should().Be(42);
    }
}
