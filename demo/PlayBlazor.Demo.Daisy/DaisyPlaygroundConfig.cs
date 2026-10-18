using System.Linq.Expressions;
using DaisyBlazor;
using DaisyBlazor.Data;
using DaisyBlazor.Theming;

namespace PlayBlazor.Demo.Daisy;

/// <summary>
/// Curation for the DaisyBlazor showcase, in progress (milestone 4): the listed surface, the icon
/// catalogue, the flagship <see cref="Button" /> and the scaffolds of the child components
/// (<see cref="Scaffolds" />). Presets and variants for the rest land in later batches, driven by the [Explicit] sweeps' inventory, verified against the pinned 0.6.0
/// package's own markup (its <c>.razor</c> source at tag v0.6.0 and shipped <c>safelist.css</c>).
/// </summary>
/// <remarks>
/// Rendering hazards every preset must respect:
/// <list type="bullet">
/// <item>Theme: the preset marks <c>daisyblazor-dark</c> <c>--prefersdark</c>, so on a dark OS the
/// specimens would turn dark inside the light shell. wwwroot/index.html pins
/// <c>data-theme="daisyblazor"</c> on <c>&lt;html&gt;</c> to stop that.</item>
/// <item>Tailwind emits only classes it has seen. A class a preset passes through Class/Style must
/// appear literally in a .cs or .razor file of this project (the CLI scans them). Classes
/// DaisyBlazor composes at runtime come only from its safelist.css, and a hole there is reported
/// upstream, never patched here.</item>
/// <item>Known safelist holes at 0.6.0: <c>gap-{n}</c> for n &gt; 12 (Stack.Spacing, Grid.Spacing)
/// renders no gap; <c>tooltip-neutral</c> (Tooltip Color=Dark) is safelisted but bundled daisyUI 5
/// does not define it, so the tooltip falls back to the default colour.</item>
/// </list>
/// </remarks>
public static class DaisyPlaygroundConfig
{
    /// <summary>
    /// Not components a user plays with. Each is excluded for its own reason, and nothing else is:
    /// <list type="bullet">
    /// <item><see cref="ThemeProvider" /> calls JS to set <c>data-theme</c> on <c>&lt;html&gt;</c>
    /// and writes localStorage and a cookie, so a specimen would re-theme the whole shell.</item>
    /// <item><see cref="DialogProvider" /> and <see cref="SnackbarProvider" /> only render the
    /// output of <c>IDialogService</c> and <c>ISnackbar</c>; alone they show nothing.</item>
    /// <item><see cref="PageBreadcrumb" /> injects an <c>IBreadcrumbService</c> that only
    /// <c>AddDaisyBlazorBreadcrumbs&lt;TResource&gt;</c> registers, backed by an app-defined route
    /// source. Without one its injection throws past the error boundary (the render sweep's one
    /// ESCAPED exception). The plain <see cref="Breadcrumbs" /> stays listed.</item>
    /// </list>
    /// The app-level helpers (AccessDenied, AppCard, Feature*, QuickAccessButton, Detail*) are
    /// public and documented, so they stay listed and get curated like any other component.
    /// </summary>
    private static readonly HashSet<string> Infrastructure =
    [
        nameof(ThemeProvider), nameof(DialogProvider), nameof(SnackbarProvider), nameof(PageBreadcrumb),
    ];

    /// <summary>Sample rows for the table and data-grid presets, so a grid has something to show.</summary>
    public static IReadOnlyList<Person> SampleRows { get; } =
    [
        new("Ada Lovelace", "Analyst", 36),
        new("Grace Hopper", "Rear Admiral", 45),
        new("Alan Turing", "Cryptanalyst", 41),
    ];

    /// <summary>Applies the DaisyBlazor curation to the playground options.</summary>
    /// <param name="options">The options to configure.</param>
    public static void Configure(PlayBlazorOptions options)
    {
        // A deny-list, as for Fluent: most of DaisyBlazor's public surface is meant to be played.
        options.ComponentFilter = type => !Infrastructure.Contains(StripArity(type.Name));

        // Discovery closes every open generic with string. These declare the closing only where
        // string teaches nonsense; presets for them land with the batches that curate them.
        // NumericField parses through Convert.ChangeType(raw, Nullable.GetUnderlyingType(T) ?? T),
        // so int round-trips ("42" -> 42, "4.5" -> default) and step stays the browser's 1 (only
        // double/float/decimal switch to step="any"). With string the sweep passes but the bench
        // shows a number input bound to text.
        options.For<NumericField<int>>();

        // Row-typed components: a table of strings has no columns worth showing.
        options.For<Table<Person>>();
        options.For<DataGrid<Person>>();
        options.For<DataTable<Person>>();
        // DataGridPager<Person>, PropertyColumn<Person, string> and TemplateColumn<Person> are closed
        // by their scaffolds in Scaffolds() below: alone, none of them renders anything.

        // Autocomplete, Select, SelectItem, Radio, RadioGroup, ToggleGroup and ToggleItem stay
        // <string>: string options are exactly what a demo of them wants.

        // DaisyBlazor icons are Material LIGATURE NAMES ("ac_unit"), not SVG markup: an SVG
        // preview would render nothing, so the thumbnail is a span in the font class Icon itself
        // uses without a cascaded IconOptions (IconOptions.DefaultFontClass, the Material Symbols
        // Outlined family wwwroot/index.html loads). The picker feeds string parameters whose name
        // ends in "Icon" (Button.StartIcon/EndIcon, IconButton.Icon, ToggleItem.Icon, ...). Icon.Name
        // does not match that rule, so it gets a preset and variants instead (batch 5).
        options.Catalogue(DaisyIconCatalogue.Material, static name => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", IconOptions.DefaultFontClass);
            builder.AddContent(2, name);
            builder.CloseElement();
        });

        // The landing page's flagship. Button defaults to Variant.Text (btn-ghost) and
        // Color.Default (no color class): a near-invisible bench, so start it Filled.
        options.For<Button>()
            .Slot(nameof(Button.ChildContent), b => b.AddContent(0, "Click me"), "Click me")
            .Parameter(nameof(Button.Variant), Variant.Filled)
            .Variant("Primary", v => v.Set(nameof(Button.Color), Color.Primary))
            .Variant("Outlined", v => v.Set(nameof(Button.Variant), Variant.Outlined).Set(nameof(Button.Color), Color.Secondary))
            .Variant("Ghost", v => v.Set(nameof(Button.Variant), Variant.Text))
            .Variant("With icon", v => v.Set(nameof(Button.StartIcon), Icons.Material.Filled.Check))
            .Variant("Loading", v => v.Set(nameof(Button.Loading), true))
            .Variant("Large", v => v.Set(nameof(Button.Size), Size.Large));

        Scaffolds(options);
    }

    /// <summary>
    /// Components that cannot live alone. The render sweep calls almost all of them healthy, which
    /// is misleading: at 0.6.0 Tab, SelectItem, TablePager, DataGridPager and both column types render
    /// NOTHING without their parent (they register with it, or render only when it cascades), and the
    /// rest render degraded markup (a Radio with no group name, a ToggleItem join-item outside a .join,
    /// a MenuItem without role="menuitem"). Each scaffold places the specimen in its parent, beside a
    /// sibling, as the component's .razor at tag v0.6.0 expects. DaisyScaffoldTests pins the daisyUI
    /// class each child produces once scaffolded. Scaffold markup lives in DaisyDemoFragments.razor.
    /// </summary>
    private static void Scaffolds(PlayBlazorOptions options)
    {
        // --- Ghosts: render nothing alone. ---

        // Tabs renders every tab's button and the active panel; a Tab only registers. Registration
        // follows initialisation, and the specimen (inside a DynamicComponent) always initialises
        // after the scaffold's sibling, so it is tab 1 and ActiveIndex=1 shows its panel.
        // Upstream at 0.6.0, shared by SelectItem and both column types below: the child never tells
        // its parent that its own parameters changed, and the parent renders before the child gets
        // them, so an edit shows one render late (DaisyScaffoldTests pins it). Left visible here
        // rather than masked with a re-render helper: a real app sees the same lag.
        options.For<Tab>()
            .Scaffold(DaisyDemoFragments.InTabs, DaisyDemoFragmentSources.InTabs)
            .Related<Tabs>()
            .Parameter(nameof(Tab.Title), "Biography")
            .Slot(nameof(Tab.ChildContent), DaisyDemoFragments.TabPanel, DaisyDemoFragmentSources.TabPanel)
            .Variant("Rich title", v => v.Set(nameof(Tab.TitleContent), "★ Biography"))
            .Variant("Disabled", v => v.Set(nameof(Tab.Disabled), true));

        // Select emits one <option> per registered item, in a native <select>. Lags one render (see Tab).
        options.For<SelectItem<string>>()
            .Scaffold(DaisyDemoFragments.InSelect, DaisyDemoFragmentSources.InSelect)
            .Related<Select<string>>()
            .Parameter(nameof(SelectItem<string>.Value), "banana")
            .Slot(nameof(SelectItem<string>.ChildContent), b => b.AddContent(0, "Banana"), "Banana")
            .Variant("Longer label", v => v.Set(nameof(SelectItem<string>.ChildContent), "Banana (ripe)"))
            .Variant("Disabled", v => v.Set(nameof(SelectItem<string>.Disabled), true));

        // TablePager renders only under a cascading ITablePagerHost, which Table cascades as itself.
        // RowsPerPage=2 over three rows gives the pager a second page to move to.
        options.For<TablePager>()
            .Scaffold(DaisyDemoFragments.InTable, DaisyDemoFragmentSources.InTable)
            .Related<Table<Person>>()
            .Parameter(nameof(TablePager.PageSizeOptions), new[] { 2, 5, 10 }, "@(new[] { 2, 5, 10 })")
            .Variant("Start-aligned", v => v.Set(nameof(TablePager.Style), "justify-content: flex-start"));

        // DataGridPager renders only when a DataGrid cascades; it belongs in DataGrid.PagerContent.
        // PageSizeOptions is read once, in OnInitialized: a variant would not re-run it, so the grid
        // in the scaffold carries the page sizes and the variants stick to the pager's own text.
        options.For<DataGridPager<Person>>()
            .Scaffold(DaisyDemoFragments.InDataGridPager, DaisyDemoFragmentSources.InDataGridPager)
            .Related<DataGrid<Person>>()
            .Parameter(nameof(DataGridPager<Person>.RowsPerPageText), "People per page:")
            .Variant("Slash separator", v => v.Set(nameof(DataGridPager<Person>.InfoSeparatorText), "/"))
            .Variant("Short label", v => v.Set(nameof(DataGridPager<Person>.RowsPerPageText), "Rows:"));

        // A column registers with its cascading DataGrid owner, which renders the header and cells.
        // Lags one render (see Tab).
        // Property is [EditorRequired] with no default: OnParametersSet compiles it, so without this
        // preset the bare closing NullReferenceExceptions. SortBy (Func<Person, object>) stays
        // undriven: Property already sorts, and a delegate has no control to drive it.
        options.For<PropertyColumn<Person, string>>()
            .Scaffold(DaisyDemoFragments.InDataGrid, DaisyDemoFragmentSources.InDataGrid)
            .Related<DataGrid<Person>>()
            .Parameter(nameof(PropertyColumn<Person, string>.Property), (Expression<Func<Person, string>>)(p => p.Name), "p => p.Name")
            .Parameter(nameof(PropertyColumn<Person, string>.Title), "Name")
            .Variant("Not sortable", v => v.Set(nameof(PropertyColumn<Person, string>.Sortable), false))
            .Variant("Bold cells", v => v.Set(nameof(PropertyColumn<Person, string>.CellClass), "font-bold"))
            .Variant("Custom title", v => v.Set(nameof(PropertyColumn<Person, string>.Title), "Full name"));

        // Deliberately imperfect: the cell template (CellTemplate, or its ChildContent alias) is a
        // RenderFragment<CellContext<Person>>, and PlayBlazor's ParameterDictionaryBuilder.BuildSlot
        // only fills the non-generic RenderFragment; a generic one takes neither a slot nor a
        // parameter preset. So the specimen's cells render empty beside the scaffold's Age column,
        // and only the header-side parameters are curated. Closing that is a src/PlayBlazor change
        // (it would also unlock Fluent's TemplateColumn), not something a preset can fake.
        // SortBy stays undriven: without a template there is nothing to sort by.
        options.For<TemplateColumn<Person>>()
            .Scaffold(DaisyDemoFragments.InDataGrid, DaisyDemoFragmentSources.InDataGrid)
            .Related<DataGrid<Person>>()
            .Parameter(nameof(TemplateColumn<Person>.Title), "Role")
            .Variant("Custom title", v => v.Set(nameof(TemplateColumn<Person>.Title), "Position"))
            .Variant("Primary header", v => v.Set(nameof(TemplateColumn<Person>.HeaderClass), "text-primary"));

        // --- Degraded alone: they render, but wrong. ---

        // name=Parent?.Name: without a RadioGroup the radios share no name and do not group.
        // The group's Value is the specimen's, so the specimen starts checked.
        options.For<Radio<string>>()
            .Scaffold(DaisyDemoFragments.InRadioGroup, DaisyDemoFragmentSources.InRadioGroup)
            .Related<RadioGroup<string>>()
            .Parameter(nameof(Radio<string>.Value), "apple")
            .Parameter(nameof(Radio<string>.Label), "Apple")
            .Variant("Primary", v => v.Set(nameof(Radio<string>.Color), Color.Primary))
            .Variant("Large", v => v.Set(nameof(Radio<string>.Size), Size.Large))
            .Variant("Disabled", v => v.Set(nameof(Radio<string>.Disabled), true));

        // A join-item needs a .join parent, and the selection colour comes from the group.
        options.For<ToggleItem<string>>()
            .Scaffold(DaisyDemoFragments.InToggleGroup, DaisyDemoFragmentSources.InToggleGroup)
            .Related<ToggleGroup<string>>()
            .Parameter(nameof(ToggleItem<string>.Value), "grid")
            .Parameter(nameof(ToggleItem<string>.Text), "Grid")
            .Variant("With icon", v => v.Set(nameof(ToggleItem<string>.Icon), Icons.Material.Filled.GridView))
            .Variant("Not selected", v => v.Set(nameof(ToggleItem<string>.Value), "table"))
            .Variant("Disabled", v => v.Set(nameof(ToggleItem<string>.Disabled), true));

        // role="menuitem" (and the li's role="none") only under a Menu, which renders its items only
        // while open. See InMenu for why the scaffold's Menu carries a transform.
        options.For<MenuItem>()
            .Scaffold(DaisyDemoFragments.InMenu, DaisyDemoFragmentSources.InMenu)
            .Related<Menu>()
            .Parameter(nameof(MenuItem.Icon), Icons.Material.Filled.Edit)
            .Slot(nameof(MenuItem.ChildContent), b => b.AddContent(0, "Edit"), "Edit")
            .Variant("Primary icon", v => v.Set(nameof(MenuItem.IconColor), Color.Primary))
            .Variant("As a link", v => v.Set(nameof(MenuItem.Href), "https://daisyui.com"))
            .Variant("Disabled", v => v.Set(nameof(MenuItem.Disabled), true));

        // ExpansionPanels gives its panels a shared <details name> (a single-open accordion) and
        // their shadow.
        options.For<ExpansionPanel>()
            .Scaffold(DaisyDemoFragments.InExpansionPanels, DaisyDemoFragmentSources.InExpansionPanels)
            .Related<ExpansionPanels>()
            .Parameter(nameof(ExpansionPanel.Text), "Shipping")
            .Parameter(nameof(ExpansionPanel.IsExpanded), true)
            .Slot(nameof(ExpansionPanel.ChildContent), b => b.AddContent(0, "Orders ship within two business days."), "Orders ship within two business days.")
            .Variant("Collapsed", v => v.Set(nameof(ExpansionPanel.IsExpanded), false))
            .Variant("With icon", v => v.Set(nameof(ExpansionPanel.Icon), Icons.Material.Filled.LocalShipping))
            .Variant("Dense", v => v.Set(nameof(ExpansionPanel.Dense), true));

        // The Timeline decides the side of each box, and the <hr> connectors only join sibling
        // <li>s inside ul.timeline.
        options.For<TimelineItem>()
            .Scaffold(DaisyDemoFragments.InTimeline, DaisyDemoFragmentSources.InTimeline)
            .Related<Timeline>()
            .Parameter(nameof(TimelineItem.Color), Color.Primary)
            .Slot(nameof(TimelineItem.ChildContent), b => b.AddContent(0, "1815: Ada Lovelace is born"), "1815: Ada Lovelace is born")
            .Variant("With icon", v => v.Set(nameof(TimelineItem.Icon), Icons.Material.Filled.CheckCircle))
            .Variant("Opposite content", v => v.Set(nameof(TimelineItem.ItemOpposite), "London"))
            .Variant("Large dot", v => v.Set(nameof(TimelineItem.Size), Size.Large));

        options.For<CarouselItem>()
            .Scaffold(DaisyDemoFragments.InCarousel, DaisyDemoFragmentSources.InCarousel)
            .Related<Carousel>()
            .Slot(nameof(CarouselItem.ChildContent), DaisyDemoFragments.CarouselSlide, DaisyDemoFragmentSources.CarouselSlide)
            .Variant("Full width", v => v.Set(nameof(CarouselItem.Width), "100%"));

        // The scaffold's Dock sets Fixed=false: the default pins it over the explorer.
        options.For<DockItem>()
            .Scaffold(DaisyDemoFragments.InDock, DaisyDemoFragmentSources.InDock)
            .Related<Dock>()
            .Parameter(nameof(DockItem.Icon), Icons.Material.Filled.Home)
            .Parameter(nameof(DockItem.Label), "Home")
            .Variant("Active", v => v.Set(nameof(DockItem.Active), true))
            .Variant("Icon only", v => v.Set(nameof(DockItem.Label), string.Empty).Set(nameof(DockItem.AriaLabel), "Home"));

        // The actions unfold on :focus-within, so the specimen shows once the trigger is clicked.
        options.For<FabAction>()
            .Scaffold(DaisyDemoFragments.InFab, DaisyDemoFragmentSources.InFab)
            .Related<Fab>()
            .Parameter(nameof(FabAction.Icon), Icons.Material.Filled.Edit)
            .Parameter(nameof(FabAction.Tooltip), "Edit")
            .Variant("Secondary", v => v.Set(nameof(FabAction.Color), Color.Secondary))
            .Variant("Tooltip above", v => v.Set(nameof(FabAction.TooltipPosition), "top"));

        // daisyUI only colours the connector between same-colour steps, so siblings are the point.
        options.For<Step>()
            .Scaffold(DaisyDemoFragments.InSteps, DaisyDemoFragmentSources.InSteps)
            .Related<Steps>()
            .Parameter(nameof(Step.Completed), true)
            .Slot(nameof(Step.ChildContent), b => b.AddContent(0, "Register"), "Register")
            .Variant("Current step", v => v.Set(nameof(Step.Completed), false).Set(nameof(Step.Active), true))
            .Variant("Custom marker", v => v.Set(nameof(Step.DataContent), "★"))
            .Variant("Accent", v => v.Set(nameof(Step.Color), Color.Tertiary));

        // ListItem is a menu <li>: its parent is ItemList (ul.menu), not List.
        options.For<ListItem>()
            .Scaffold(DaisyDemoFragments.InItemList, DaisyDemoFragmentSources.InItemList)
            .Related<ItemList>()
            .Parameter(nameof(ListItem.Icon), Icons.Material.Filled.Inbox)
            .Parameter(nameof(ListItem.Text), "Inbox")
            .Variant("As a link", v => v.Set(nameof(ListItem.Href), "#inbox"))
            .Variant("Disabled", v => v.Set(nameof(ListItem.Disabled), true));

        // ListRow is the .list-row of daisyUI's .list, which List renders.
        options.For<ListRow>()
            .Scaffold(DaisyDemoFragments.InList, DaisyDemoFragmentSources.InList)
            .Related<List>()
            .Parameter(nameof(ListRow.Icon), Icons.Material.Filled.Person)
            .Parameter(nameof(ListRow.Title), "Ada Lovelace")
            .Parameter(nameof(ListRow.Subtitle), "Analyst")
            .Variant("Trailing content", v => v.Set(nameof(ListRow.TrailingContent), "36"))
            .Variant("No subtitle", v => v.Set(nameof(ListRow.Subtitle), string.Empty));

        // Item is MudItem's replacement, a col-span-N cell: its parent is the 12-column Grid, not
        // ItemList.
        options.For<Item>()
            .Scaffold(DaisyDemoFragments.InGrid, DaisyDemoFragmentSources.InGrid)
            .Related<Grid>()
            .Parameter(nameof(Item.xs), 6)
            .Slot(nameof(Item.ChildContent), DaisyDemoFragments.GridCell, DaisyDemoFragmentSources.GridCell)
            .Variant("Full width", v => v.Set(nameof(Item.xs), 12))
            .Variant("One third", v => v.Set(nameof(Item.xs), 4));

        // Tr, Td and Th are passthroughs: outside table > thead/tbody > tr the browser's parser
        // drops the element and keeps only its text. SimpleTable renders the <table class="table">.
        options.For<Tr>()
            .Scaffold(DaisyDemoFragments.InTbody, DaisyDemoFragmentSources.InTbody)
            .Related<SimpleTable>()
            .Slot(nameof(Tr.ChildContent), DaisyDemoFragments.RowCells, DaisyDemoFragmentSources.RowCells)
            .Variant("Highlighted", v => v.Set(nameof(Tr.Class), "bg-base-200"));

        options.For<Td>()
            .Scaffold(DaisyDemoFragments.InBodyRow, DaisyDemoFragmentSources.InBodyRow)
            .Related<Tr>()
            .Slot(nameof(Td.ChildContent), b => b.AddContent(0, "Ada Lovelace"), "Ada Lovelace")
            .Variant("Bold", v => v.Set(nameof(Td.Class), "font-bold"));

        options.For<Th>()
            .Scaffold(DaisyDemoFragments.InHeaderRow, DaisyDemoFragmentSources.InHeaderRow)
            .Related<Tr>()
            .Slot(nameof(Th.ChildContent), b => b.AddContent(0, "Name"), "Name")
            .Variant("Primary", v => v.Set(nameof(Th.Class), "text-primary"));

        // The card regions draw their padding and layout against the .card surface.
        options.For<CardHeader>()
            .Scaffold(DaisyDemoFragments.InCardTop, DaisyDemoFragmentSources.InCardTop)
            .Related<Card>()
            .Slot(nameof(CardHeader.CardHeaderContent), DaisyDemoFragments.CardHeaderText, DaisyDemoFragmentSources.CardHeaderText)
            .Variant("With actions", v => v.Set(nameof(CardHeader.CardHeaderActions), "⋯"));

        options.For<CardContent>()
            .Scaffold(DaisyDemoFragments.InCard, DaisyDemoFragmentSources.InCard)
            .Related<Card>()
            .Slot(nameof(CardContent.ChildContent), DaisyDemoFragments.CardBodyText, DaisyDemoFragmentSources.CardBodyText)
            .Variant("Centered", v => v.Set(nameof(CardContent.Class), "items-center text-center"));

        options.For<CardActions>()
            .Scaffold(DaisyDemoFragments.InCardBottom, DaisyDemoFragmentSources.InCardBottom)
            .Related<Card>()
            .Slot(nameof(CardActions.ChildContent), DaisyDemoFragments.CardButtons, DaisyDemoFragmentSources.CardButtons)
            .Variant("End-aligned", v => v.Set(nameof(CardActions.Class), "justify-end"));

        // .stat cells are divided and laid out by their .stats parent.
        options.For<Stat>()
            .Scaffold(DaisyDemoFragments.InStats, DaisyDemoFragmentSources.InStats)
            .Related<Stats>()
            .Parameter(nameof(Stat.Title), "Downloads")
            .Parameter(nameof(Stat.Value), "31K")
            .Parameter(nameof(Stat.Desc), "Jan 1st - Feb 1st")
            .Variant("Rising", v => v.Set(nameof(Stat.Desc), "↗︎ 21% more than last month").Set(nameof(Stat.DescColor), Color.Success))
            .Variant("Falling", v => v.Set(nameof(Stat.Desc), "↘︎ 9% less than last month").Set(nameof(Stat.DescColor), Color.Error));

        // .footer-title heads a <nav> column inside a .footer.
        options.For<FooterTitle>()
            .Scaffold(DaisyDemoFragments.InFooter, DaisyDemoFragmentSources.InFooter)
            .Related<Footer>()
            .Slot(nameof(FooterTitle.ChildContent), b => b.AddContent(0, "Services"), "Services")
            .Variant("Primary", v => v.Set(nameof(FooterTitle.Class), "text-primary"));
    }

    private static string StripArity(string name)
    {
        var backtick = name.IndexOf('`');
        return backtick < 0 ? name : name[..backtick];
    }
}
