using System.Text.RegularExpressions;
using AwesomeAssertions;
using Bunit;
using DaisyBlazor;
using DaisyBlazor.Data;
using NUnit.Framework;
using PlayBlazor.Demo.Daisy;

namespace PlayBlazor.UnitTests.Shell;

/// <summary>
/// DaisyBlazor's child components, rendered on the bench exactly as the explorer renders them:
/// through PlaygroundView, so through their scaffold. The render sweep calls almost all of them
/// healthy because alone they throw nothing, yet Tab, SelectItem, TablePager, DataGridPager and the
/// two column types render NO markup alone, and the rest render degraded markup. Every selector
/// below names markup only the parent produces (the tab button, the option, the radio's group name,
/// the .join around a join-item...), so each case fails if its scaffold is removed.
/// </summary>
public partial class DaisyScaffoldTests
{
    private static readonly Type[] Scaffolded =
    [
        typeof(Tab), typeof(SelectItem<string>), typeof(Radio<string>), typeof(ToggleItem<string>),
        typeof(MenuItem), typeof(ExpansionPanel), typeof(TimelineItem), typeof(CarouselItem),
        typeof(DockItem), typeof(FabAction), typeof(Step), typeof(ListItem), typeof(ListRow),
        typeof(Item), typeof(Tr), typeof(Td), typeof(Th), typeof(CardHeader), typeof(CardContent),
        typeof(CardActions), typeof(Stat), typeof(FooterTitle), typeof(TablePager),
        typeof(DataGridPager<Person>), typeof(PropertyColumn<Person, string>), typeof(TemplateColumn<Person>),
    ];

    private static PlayBlazorOptions Configured()
    {
        var options = new PlayBlazorOptions();
        DaisyPlaygroundConfig.Configure(options);
        return options;
    }

    private static BunitContext NewContext()
    {
        var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddDaisyBlazor();
        context.Services.AddPlayBlazor(DaisyPlaygroundConfig.Configure);
        return context;
    }

    private static IRenderedComponent<PlaygroundView> Bench(BunitContext context, Type component)
        => context.Render<PlaygroundView>(ps => ps.Add(c => c.Component, component));

    [TestCaseSource(nameof(Scaffolded))]
    public void EveryChild_HasAScaffoldWithSource_AndLinksToItsParent(Type component)
    {
        var options = Configured();

        options.TryGetScaffold(component, out _).Should().BeTrue();
        options.TryGetScaffoldSource(component, out var source).Should().BeTrue();
        source.Should().Contain("{specimen}");
        options.GetRelated(component).Should().NotBeEmpty();
    }

    // Ghosts: alone, these render nothing at all.
    [TestCase(typeof(Tab), "div[role=tablist] button.tab.tab-active", "Biography")]
    [TestCase(typeof(SelectItem<string>), "select.select option[selected]", "Banana")]
    [TestCase(typeof(TablePager), "div.overflow-x-auto + div .join > button.join-item[aria-label='Next page']", null)]
    [TestCase(typeof(DataGridPager<Person>), "div.overflow-x-auto + div .join > button.join-item[aria-label='Next page']", null)]
    [TestCase(typeof(PropertyColumn<Person, string>), "table.table tbody tr td", "Ada Lovelace")]
    [TestCase(typeof(TemplateColumn<Person>), "table.table thead th", "Role")]
    // Degraded alone: they render, but without what their parent gives them.
    [TestCase(typeof(Radio<string>), "div[role=radiogroup] input.radio[name^=radiogroup-][checked]", null)]
    [TestCase(typeof(ToggleItem<string>), "div.join > button.join-item.btn.btn-primary", "Grid")]
    [TestCase(typeof(MenuItem), "ul[role=menu] > li[role=none] > button[role=menuitem]", "Edit")]
    [TestCase(typeof(ExpansionPanel), "details.collapse[name^=xp-][open] .collapse-title", "Shipping")]
    [TestCase(typeof(TimelineItem), "ul.timeline > li > .timeline-box", "1815")]
    [TestCase(typeof(CarouselItem), "div.carousel > div.carousel-item", "Slide 1")]
    [TestCase(typeof(DockItem), "div.dock.relative > button .dock-label", "Home")]
    [TestCase(typeof(FabAction), "div.relative > div.fab.absolute button.btn-circle[aria-label=Edit]", null)]
    [TestCase(typeof(Step), "ul.steps > li.step.step-primary[data-content]", "Register")]
    [TestCase(typeof(ListItem), "ul.menu > li", "Inbox")]
    [TestCase(typeof(ListRow), "ul.list > li.list-row", "Ada Lovelace")]
    [TestCase(typeof(Item), "div.grid.grid-cols-12 > div.col-span-6", "Grid cell")]
    [TestCase(typeof(Tr), "table.table > tbody > tr > td", "Ada Lovelace")]
    [TestCase(typeof(Td), "table.table > tbody > tr > td", "Ada Lovelace")]
    [TestCase(typeof(Th), "table.table > thead > tr > th[scope=col]", "Name")]
    [TestCase(typeof(CardHeader), "div.card > div.card-title-area .card-title", "Ada Lovelace")]
    [TestCase(typeof(CardContent), "div.card > div.card-body .card-title", "Analytical Engine")]
    [TestCase(typeof(CardActions), "div.card > div.card-actions > button.btn", "Read more")]
    [TestCase(typeof(Stat), "div.stats > div.stat > .stat-value", "31K")]
    [TestCase(typeof(FooterTitle), "footer.footer > nav > h6.footer-title", "Services")]
    public void Child_RendersItsDaisyMarkup_InsideItsParent(Type component, string selector, string? text)
    {
        using var context = NewContext();
        var cut = Bench(context, component);

        cut.FindAll(".pb-specimen .pb-error").Should().BeEmpty();
        var matches = cut.FindAll($".pb-specimen {selector}");
        matches.Should().NotBeEmpty($"{component.Name} should render `{selector}` through its scaffold");
        if (text is not null)
        {
            matches.Select(m => m.TextContent).Should().Contain(t => t.Contains(text));
        }
    }

    [Test]
    public void Dock_InTheScaffold_IsNotPinnedToTheViewport()
    {
        // Dock defaults to Fixed=true: daisyUI's .dock is position:fixed at the viewport bottom,
        // and only the `relative` utility (Fixed=false) takes it back into the flow.
        using var context = NewContext();
        var dock = Bench(context, typeof(DockItem)).Find(".pb-specimen div.dock");

        dock.ClassList.Should().Contain("relative");
    }

    [Test]
    public void Pagers_ShowTheRangeOfTheirHost()
    {
        using var context = NewContext();

        Bench(context, typeof(TablePager)).Find(".pb-specimen").TextContent.Should().Contain("1-2 / 3");
        Bench(context, typeof(DataGridPager<Person>)).Find(".pb-specimen").TextContent.Should().Contain("1-2 of 3");
    }

    /// <summary>
    /// Every variant must change the specimen's markup when clicked on a live bench, which is how the
    /// explorer applies the first variant (no reset, so no fresh instance). This sees what the
    /// parameter-dictionary liveness gate cannot: a parent that renders its child's state and is not
    /// told it changed.
    /// </summary>
    [TestCaseSource(nameof(Scaffolded))]
    public void EveryVariant_ChangesTheRenderedChild(Type component)
    {
        var variants = Configured().GetVariants(component);
        variants.Should().NotBeEmpty();

        foreach (var variant in variants)
        {
            using var context = NewContext();
            var cut = Bench(context, component);
            var before = Normalized(cut.Find(".pb-specimen").InnerHtml);

            cut.FindAll("button.pb-variant").Single(b => b.TextContent == variant.Name).Click();
            if (LagsOneRender.Contains(component))
            {
                cut.Render();
            }

            Normalized(cut.Find(".pb-specimen").InnerHtml).Should().NotBe(
                before, $"{component.Name} / \"{variant.Name}\" should change what the bench shows");
        }
    }

    /// <summary>
    /// Upstream at 0.6.0: Tab, SelectItem and the DataGrid columns only register with their parent,
    /// which renders their state; none tells the parent when its own parameters change. The parent
    /// re-renders BEFORE the child receives its new values, so the bench shows the previous state
    /// until the next render. Pinned here so the lag stays visible: when this list can be emptied,
    /// the comments beside their curation go too.
    /// </summary>
    private static readonly HashSet<Type> LagsOneRender =
        [typeof(Tab), typeof(SelectItem<string>), typeof(PropertyColumn<Person, string>), typeof(TemplateColumn<Person>)];

    [Test]
    public void RegisteringChildren_StillLagOneRender_Upstream()
    {
        foreach (var (component, variant, expected) in new[]
                 {
                     (typeof(Tab), "Rich title", "★ Biography"),
                     (typeof(SelectItem<string>), "Longer label", "Banana (ripe)"),
                     (typeof(PropertyColumn<Person, string>), "Custom title", "Full name"),
                     (typeof(TemplateColumn<Person>), "Custom title", "Position"),
                 })
        {
            using var context = NewContext();
            var cut = Bench(context, component);

            cut.FindAll("button.pb-variant").Single(b => b.TextContent == variant).Click();
            cut.Find(".pb-specimen").TextContent.Should().NotContain(expected, "the parent renders before the child updates");

            cut.Render();
            cut.Find(".pb-specimen").TextContent.Should().Contain(expected);
        }
    }

    // Tabs, RadioGroup, ExpansionPanels and the form fields mint a GUID per instance for their ids,
    // and Blazor renumbers its event-handler attributes on every render.
    private static string Normalized(string html) => Noise().Replace(html, "{noise}");

    [GeneratedRegex("[0-9a-f]{32}|[0-9a-f]{8}(?=-)|(?<=dz-field-)[0-9a-f]+|blazor:\\w+=\"\\d+\"")]
    private static partial Regex Noise();
}
