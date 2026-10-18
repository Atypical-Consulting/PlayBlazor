using System.Text.RegularExpressions;
using AwesomeAssertions;
using Bunit;
using DaisyBlazor;
using DaisyBlazor.Data;
using NUnit.Framework;
using PlayBlazor.CodeGen;
using PlayBlazor.Demo.Daisy;
using PlayBlazor.Discovery;
using PlayBlazor.State;

namespace PlayBlazor.UnitTests.Shell;

/// <summary>
/// The tables, grids, filter panel and dashboard tiles (DaisyPlaygroundConfig.DataComponents), on
/// the bench as the explorer renders them. Same contract as the rest of this class: each selector
/// names the daisyUI markup the preset must produce — real rows, not just a header over an empty body.
/// </summary>
public partial class DaisyPresetTests
{
    private static readonly Type[] DataCurated =
    [
        typeof(Table<Person>), typeof(SimpleTable), typeof(DataGrid<Person>), typeof(DataTable<Person>),
        typeof(FilterPanel), typeof(KpiCard), typeof(StatusChip),
    ];

    [TestCase(typeof(Table<Person>), "div.overflow-x-auto > table.table > thead > tr > th[scope=col]", "Age")]
    [TestCase(typeof(Table<Person>), "table.table > tbody > tr > td", "Grace Hopper")]
    [TestCase(typeof(SimpleTable), "div.overflow-x-auto > table.table > tbody > tr > td", "Alan Turing")]
    [TestCase(typeof(DataGrid<Person>), "table.table > thead th[aria-sort=none] > button", "Name")]
    [TestCase(typeof(DataGrid<Person>), "table.table > tbody > tr > td", "Rear Admiral")]
    [TestCase(typeof(DataGrid<Person>), "div.mt-2 select.select.select-sm > option[selected]", "10")]
    [TestCase(typeof(DataGrid<Person>), "div.mt-2 span", "1-3 of 3")]
    [TestCase(typeof(DataTable<Person>), "table.table.table-zebra > tbody > tr.row-hover > td", "Ada Lovelace")]
    [TestCase(typeof(DataTable<Person>), "td.text-right button.btn[title='Edit Ada Lovelace']", "edit")]
    [TestCase(typeof(FilterPanel), "div.grid.grid-cols-12 input.input[placeholder='Northwind Traders']", null)]
    [TestCase(typeof(FilterPanel), "div.grid.grid-cols-12 select.select > option[selected]", "Pro")]
    [TestCase(typeof(FilterPanel), "span.badge.badge-info.badge-sm", "Region: Europe")]
    [TestCase(typeof(FilterPanel), "button.btn.btn-ghost", "Reset")]
    [TestCase(typeof(KpiCard), "div.w-80 > div.p-4[style*='border-left: 4px solid var(--color-primary)']", "€48.2 k")]
    [TestCase(typeof(StatusChip), "span.badge.badge-warning.badge-sm", "Delayed")]
    [TestCase(typeof(StatusChip), "span.badge .material-symbols-outlined", "schedule")]
    public void DataPreset_RendersItsDaisyMarkup(Type component, string selector, string? text)
        => DisplayPreset_RendersItsDaisyClass(component, selector, text);

    [TestCase(typeof(Table<Person>), "table.table > tbody > tr", 3)]
    [TestCase(typeof(Table<Person>), "table.table > tbody > tr > td", 9)]
    [TestCase(typeof(DataGrid<Person>), "table.table > tbody > tr > td", 9)]
    [TestCase(typeof(DataTable<Person>), "table.table > thead th", 4)]
    [TestCase(typeof(DataTable<Person>), "table.table > tbody > tr", 2)]
    [TestCase(typeof(FilterPanel), "span.badge", 2)]
    public void DataPreset_RendersEveryRow(Type component, string selector, int count)
        => DisplayContainer_RendersEveryChild(component, selector, count);

    [TestCase(typeof(Table<Person>), "Zebra", "table.table.table-zebra")]
    [TestCase(typeof(Table<Person>), "Compact", "table.table.table-sm")]
    [TestCase(typeof(Table<Person>), "Zebra", "tbody > tr.row-hover.cursor-pointer")]
    [TestCase(typeof(Table<Person>), "Loading", "tbody td[colspan='999'] > span.loading.loading-spinner")]
    [TestCase(typeof(SimpleTable), "Zebra hover", "table.table.table-zebra")]
    [TestCase(typeof(SimpleTable), "Compact", "table.table.table-sm")]
    [TestCase(typeof(SimpleTable), "Bordered", "table.table.border.border-base-300")]
    [TestCase(typeof(DataGrid<Person>), "Striped hover", "table.table-zebra > tbody > tr.row-hover")]
    [TestCase(typeof(DataGrid<Person>), "Dense bordered", "div.border.rounded-box table.table-sm")]
    [TestCase(typeof(DataGrid<Person>), "Loading", "tbody td > div.skeleton")]
    [TestCase(typeof(DataGrid<Person>), "Elevated", "div.w-full.shadow > div.overflow-x-auto")]
    [TestCase(typeof(DataTable<Person>), "Loading", "tbody td > div.skeleton.h-10")]
    [TestCase(typeof(DataTable<Person>), "Dense", "table.table.table-sm")]
    [TestCase(typeof(DataTable<Person>), "Elevated", "div.w-full.shadow")]
    [TestCase(typeof(StatusChip), "Colour override", "span.badge.badge-error")]
    [TestCase(typeof(StatusChip), "Large", "span.badge.badge-warning.badge-lg")]
    [TestCase(typeof(KpiCard), "Trend down", "div.p-4[style*='var(--color-error)']")]
    public void DataVariant_RendersItsDaisyModifier(Type component, string variant, string selector)
        => DisplayVariant_RendersItsDaisyModifier(component, variant, selector);

    [TestCaseSource(nameof(DataCurated))]
    public void EveryDataVariant_ChangesTheRenderedMarkup(Type component)
    {
        var variants = Configured().GetVariants(component);
        variants.Count.Should().BeInRange(2, 6);

        foreach (var variant in variants)
        {
            using var context = NewContext();
            var cut = Bench(context, component);
            var before = Normalized(cut.Find(".pb-specimen").InnerHtml);

            Variant(cut, variant.Name);

            Normalized(cut.Find(".pb-specimen").InnerHtml).Should().NotBe(
                before, $"{component.Name} / \"{variant.Name}\" should change what the bench shows");
        }
    }

    [Test]
    public void DataGridTwoPerPage_PagesThroughThePresetPager()
    {
        using var context = NewContext();
        var cut = Bench(context, typeof(DataGrid<Person>));

        Variant(cut, "Two per page");

        cut.FindAll(".pb-specimen table.table > tbody > tr").Should().HaveCount(2);
        cut.Find(".pb-specimen button[aria-label='Next page']").HasAttribute("disabled").Should().BeFalse();
    }

    [Test]
    public void TemplateColumn_FillsEveryCellFromItsTemplate()
    {
        using var context = NewContext();
        var cut = Bench(context, typeof(TemplateColumn<Person>));

        cut.FindAll(".pb-specimen table.table > tbody td > span.badge.badge-soft.badge-primary")
            .Select(b => b.TextContent).Should().Equal("Analyst", "Rear Admiral", "Cryptanalyst");
    }

    [Test]
    public void TableSnippet_TeachesTheRowTemplate()
    {
        var options = Configured();
        var table = new ReflectionCatalogProvider(options: options).Describe(typeof(Table<Person>));

        var snippet = RazorSnippetGenerator.Generate(table, new PlaygroundState(), options);

        snippet.Should().Contain("Items=\"@_people\"")
            .And.Contain("<RowTemplate>")
            .And.Contain("<Td>@context.Name</Td>")
            .And.Contain("</RowTemplate>");
    }

    /// <summary>
    /// The classes the data presets make DaisyBlazor compose at runtime, and the literal classes in
    /// its shipped markup, must exist in app.css. The last line pins an upstream note: DataGridPager
    /// still writes daisyUI 4's select-bordered, which daisyUI 5 dropped (its .select has a border).
    /// </summary>
    [Test]
    public void DataClasses_AreCompiledIntoAppCss()
    {
        var css = AppCss();
        string[] composed =
        [
            "table-zebra", "table-sm", "row-hover", "skeleton", "shadow", "rounded-box", "border-base-300",
            "badge-info", "badge-warning", "badge-error", "badge-lg", "badge-sm", "select-sm", "join-item",
        ];

        composed.Where(c => !Regex.IsMatch(css, $@"\.{Regex.Escape(c)}[\s{{:,.>\[)]"))
            .Should().BeEmpty("each class must be emitted for the data bench that composes it");
        css.Should().Contain(@"tbody_tr\:hover\]\:bg-base-200", "SimpleTable's Hover is a safelisted arbitrary class");
        Regex.IsMatch(css, @"\.select-bordered\b").Should().BeFalse("upstream note at 0.6.0: a daisyUI 4 class");
    }
}
