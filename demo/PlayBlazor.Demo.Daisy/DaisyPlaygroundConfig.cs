using System.Linq.Expressions;
using DaisyBlazor;
using DaisyBlazor.Data;
using DaisyBlazor.Feedback;
using DaisyBlazor.Layout;
using DaisyBlazor.Theming;
using DaisyBlazor.Validation;

namespace PlayBlazor.Demo.Daisy;

/// <summary>
/// Curation for the DaisyBlazor showcase, in progress (milestone 4): the listed surface, the icon
/// catalogue, the flagship <see cref="Button" />, the scaffolds of the child components
/// (<see cref="Scaffolds" />), the cascading, validation and overlay components (<see cref="ContextAndOverlays" />), the actions and form inputs (<see cref="FormsAndActions" />), the data display and feedback components (<see cref="DisplayAndFeedback" />) and the layout, navigation, mockup and app-level helpers (<see cref="LayoutAndNavigation" />). Presets and variants for the rest land in later batches, driven by the [Explicit] sweeps' inventory, verified against the pinned 0.6.0
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
/// <item>Not safelist holes but upstream notes at 0.6.0: <c>label-text</c> (FieldError, Field, TextField)
/// is a daisyUI 4 class that daisyUI 5 no longer defines, so it styles nothing; and Dialog's header
/// comment advises <c>Class="modal-box"</c> for a static preview, which daisyUI renders at opacity 0
/// outside an open <c>.modal</c> (see <see cref="ContextAndOverlays" />).</item>
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

    /// <summary>
    /// One server error, for "Email", cascaded by the ServerValidation preset and the FieldError
    /// scaffold. Nothing on the bench mutates it: inputs only read their error from the state.
    /// </summary>
    public static ServerValidationState SampleValidation { get; } = CreateSampleValidation();

    private static ServerValidationState CreateSampleValidation()
    {
        var state = new ServerValidationState();
        state.AddError("Email", "This email is already registered.");
        return state;
    }

    /// <summary>
    /// A self-contained photo stand-in (an SVG data URI) for Avatar, Image, Mask, ChatBubble and
    /// Diff, so the bench never depends on an external image host.
    /// </summary>
    public static string SamplePhoto { get; } = Photo("#605dff", "#ffd166", "#1e1b4b");

    /// <summary>The same scene in greyscale: the second pane of the Diff preset.</summary>
    public static string SamplePhotoMuted { get; } = Photo("#9ca3af", "#e5e7eb", "#374151");

    private static string Photo(string sky, string sun, string hills) => "data:image/svg+xml," + Uri.EscapeDataString(
        $"""<svg xmlns="http://www.w3.org/2000/svg" width="192" height="192" viewBox="0 0 192 192"><rect width="192" height="192" fill="{sky}"/><circle cx="140" cy="52" r="22" fill="{sun}"/><path d="M0 192 64 96l48 56 32-28 48 68z" fill="{hills}"/></svg>""");

    /// <summary>The options the Autocomplete preset searches, so typing "an" finds three.</summary>
    public static IReadOnlyList<string> ListOptions { get; } =
        ["Apple", "Apricot", "Banana", "Blueberry", "Cherry", "Mango", "Orange", "Pineapple"];

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
        ContextAndOverlays(options);
        FormsAndActions(options);
        DisplayAndFeedback(options);
        LayoutAndNavigation(options);
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

    /// <summary>
    /// Components that need a cascade, a required object or containment rather than a plain parent:
    /// the validation family, the dialog bodies and the page-level overlays. Verified against the
    /// .razor source at tag v0.6.0 and the compiled app.css; DaisyScaffoldTests pins each one.
    /// </summary>
    private static void ContextAndOverlays(PlayBlazorOptions options)
    {
        // --- Validation ---

        // State is [EditorRequired] and has no control. Inputs with a FieldName read their error from
        // it (TextField's FieldName, not its MudBlazor-compat For, which does nothing). State is the
        // only non-slot parameter and an unsupported one, so no variant can swap it: none is offered.
        options.For<ServerValidation>()
            .Parameter(nameof(ServerValidation.State), SampleValidation, "@_validation")
            .Slot(nameof(ServerValidation.ChildContent), DaisyDemoFragments.ServerValidatedField, DaisyDemoFragmentSources.ServerValidatedField)
            .Related<FieldError>();

        // FieldError renders nothing without a cascading ServerValidationState.
        options.For<FieldError>()
            .Scaffold(DaisyDemoFragments.InServerValidation, DaisyDemoFragmentSources.InServerValidation)
            .Related<ServerValidation>()
            .Parameter(nameof(FieldError.For), "Email")
            .Variant("Field without an error", v => v.Set(nameof(FieldError.For), "Name"))
            .Variant("Qualified server name", v => v.Set(nameof(FieldError.For), "request.Email"));

        // Form is a flex column that fields register with (TextField, NumericField and Select call
        // Form?.Register); a field validates on edit, so clearing the Required name shows "Required".
        // It renders a <div>, not a <form>: the Submit button has no submit semantics until the Form is
        // wrapped in an EditForm or <form>, as DaisyBlazor's own example says. Model and Validation are
        // objects without a control and IsValid is output, so the variants use Class.
        options.For<Form>()
            .Slot(nameof(Form.ChildContent), DaisyDemoFragments.FormFields, DaisyDemoFragmentSources.FormFields)
            .Related<TextField>()
            .Variant("Constrained width", v => v.Set(nameof(Form.Class), "max-w-md"))
            .Variant("Roomy", v => v.Set(nameof(Form.Class), "gap-6"));

        // Field is MudField's replacement: a READ-ONLY labelled value (fieldset + label + surface), so
        // its content is text, not an input. Its default Variant.Text draws only a bottom border.
        options.For<Field>()
            .Parameter(nameof(Field.Label), "Invoice no.")
            .Slot(nameof(Field.ChildContent), b => b.AddContent(0, "INV-2041"), "INV-2041")
            .Variant("Outlined", v => v.Set(nameof(Field.Variant), Variant.Outlined))
            .Variant("Filled", v => v.Set(nameof(Field.Variant), Variant.Filled))
            .Variant("With adornment", v => v.Set(nameof(Field.Adornment), Adornment.Start).Set(nameof(Field.AdornmentIcon), Icons.Material.Filled.Receipt));

        // The hint is the inner .validator's sibling and turns visible once the input is :user-invalid,
        // i.e. after the user has typed something that is not an email and left the field: that is
        // daisyUI's CSS-only validation, so the untouched bench shows only the input. The input's
        // classes are literal in DaisyDemoFragments.razor, which is what makes Tailwind emit them.
        options.For<Validator>()
            .Slot(nameof(Validator.ChildContent), DaisyDemoFragments.EmailInput, DaisyDemoFragmentSources.EmailInput)
            .Parameter(nameof(Validator.Hint), "Enter a valid email address.")
            .Related<ValidatorHint>()
            .Variant("Longer hint", v => v.Set(nameof(Validator.Hint), "Use your work address, for example ada@example.com."))
            .Variant("Full width", v => v.Set(nameof(Validator.Class), "w-full"));

        // .validator-hint is visibility:hidden in daisyUI until a preceding sibling .validator is
        // invalid, so a standalone hint shows nothing. The scaffold puts it after an aria-invalid input.
        options.For<ValidatorHint>()
            .Scaffold(DaisyDemoFragments.AfterInvalidInput, DaisyDemoFragmentSources.AfterInvalidInput)
            .Related<Validator>()
            .Slot(nameof(ValidatorHint.ChildContent), b => b.AddContent(0, "Enter a valid email address."), "Enter a valid email address.")
            .Variant("Other message", v => v.Set(nameof(ValidatorHint.ChildContent), "An email needs a domain after the @."))
            .Variant("Bold", v => v.Set(nameof(ValidatorHint.Class), "font-semibold"));

        // --- Dialog bodies ---
        // Dialog, ConfirmDialog and MessageBoxDialog render inside the .modal-box DialogProvider opens,
        // and read a cascading IMudDialogInstance that is null here. Every button null-checks it
        // (MudDialog?.Close), so nothing throws, but outside the provider the buttons do nothing.
        // Upstream at 0.6.0: Dialog's header comment advises Class="modal-box" for a static preview,
        // but daisyUI's .modal-box is opacity:0 (scale .95) until an open .modal parent fades it in, so
        // that preview is invisible. The box here is the one DaisyBlazor's own DialogCoverExample uses.

        options.For<Dialog>()
            .Parameter(nameof(Dialog.Class), "rounded-box bg-base-100 max-w-lg p-6 shadow-xl ring-1 ring-base-300")
            .Parameter(nameof(Dialog.Title), "Deploy v2.4.0 to production?")
            .Slot(nameof(Dialog.DialogContent), DaisyDemoFragments.DialogBody, DaisyDemoFragmentSources.DialogBody)
            .Slot(nameof(Dialog.DialogActions), DaisyDemoFragments.DialogButtons, DaisyDemoFragmentSources.DialogButtons)
            .Related<Modal>()
            .Variant("Rich title", v => v.Set(nameof(Dialog.TitleContent), "⚠ Deploy now?"))
            .Variant("Extra body text", v => v.Set(nameof(Dialog.ChildContent), "Rollback stays available for 24 hours."));

        options.For<ConfirmDialog>()
            .Scaffold(DaisyDemoFragments.InDialogBox, DaisyDemoFragmentSources.InDialogBox)
            .Related<Dialog>()
            .Parameter(nameof(ConfirmDialog.ContentText), "Delete this project? This cannot be undone.")
            .Parameter(nameof(ConfirmDialog.ButtonText), "Delete")
            .Parameter(nameof(ConfirmDialog.Color), Color.Error)
            .Variant("Neutral confirm", v => v.Set(nameof(ConfirmDialog.Color), Color.Primary).Set(nameof(ConfirmDialog.ButtonText), "Confirm"))
            .Variant("Custom cancel", v => v.Set(nameof(ConfirmDialog.CancelText), "Keep it"));

        // No Title parameter: the title comes from IDialogService.ShowMessageBoxAsync through the
        // dialog instance, so a static preview shows the message and buttons only.
        options.For<MessageBoxDialog>()
            .Scaffold(DaisyDemoFragments.InDialogBox, DaisyDemoFragmentSources.InDialogBox)
            .Related<Dialog>()
            .Parameter(nameof(MessageBoxDialog.Message), "Save your changes before closing?")
            .Parameter(nameof(MessageBoxDialog.YesText), "Save")
            .Parameter(nameof(MessageBoxDialog.NoText), "Discard")
            .Parameter(nameof(MessageBoxDialog.CancelText), "Cancel")
            .Variant("OK only", v => v.Set(nameof(MessageBoxDialog.NoText), string.Empty).Set(nameof(MessageBoxDialog.CancelText), string.Empty).Set(nameof(MessageBoxDialog.YesText), "OK"))
            .Variant("Multi-line message", v => v.Set(nameof(MessageBoxDialog.Message), "3 files changed.\nSave them before closing?"));

        // --- Page-level overlays ---

        // Open=true renders <dialog class="modal modal-open"> and dialog.js calls showModal(): the
        // dialog joins the browser's TOP LAYER, which no transform or overflow on an ancestor can
        // contain, and makes everything else inert. So Open stays false (the default) and the
        // scaffold explains it; the "Open" variants are opt-in. CloseButton guarantees a way out
        // besides Escape and the backdrop: the action button only closes in an app that binds Open.
        options.For<Modal>()
            .Scaffold(DaisyDemoFragments.WithModalNote, DaisyDemoFragmentSources.WithModalNote)
            .Related<Dialog>()
            .Parameter(nameof(Modal.Title), "Invite a team member")
            .Parameter(nameof(Modal.CloseButton), true)
            .Slot(nameof(Modal.ChildContent), DaisyDemoFragments.ModalBody, DaisyDemoFragmentSources.ModalBody)
            .Slot(nameof(Modal.DialogActions), DaisyDemoFragments.ModalButtons, DaisyDemoFragmentSources.ModalButtons)
            .Variant("Open", v => v.Set(nameof(Modal.Open), true))
            .Variant("Open as an alert", v => v.Set(nameof(Modal.Open), true).Set(nameof(Modal.AlertDialog), true).Set(nameof(Modal.BackdropClick), false));

        // Persistent (the default) with Open docks the panel beside the content (drawer-open), inline.
        // Temporary is left to the control, not offered as a variant: daisyUI's overlay panel is
        // position:fixed over the viewport and a checked toggle locks the page scroll.
        options.For<Drawer>()
            .Scaffold(DaisyDemoFragments.InDrawerFrame, DaisyDemoFragmentSources.InDrawerFrame)
            .Related<ItemList>()
            .Parameter(nameof(Drawer.Open), true)
            .Parameter(nameof(Drawer.Class), "h-full")
            .Slot(nameof(Drawer.SideContent), DaisyDemoFragments.DrawerSide, DaisyDemoFragmentSources.DrawerSide)
            .Slot(nameof(Drawer.ChildContent), DaisyDemoFragments.DrawerMain, DaisyDemoFragmentSources.DrawerMain)
            .Variant("From the end", v => v.Set(nameof(Drawer.End), true))
            .Variant("Closed", v => v.Set(nameof(Drawer.Open), false))
            .Variant("With toggle button", v => v.Set(nameof(Drawer.ToggleContent), "☰ Menu"));

        // Deliberately global: daisyUI applies a theme through
        // ":root:has(input.theme-controller[value=x]:checked)", so checking the specimen re-themes the
        // WHOLE page, not just the stage. That is the component's purpose, and it reverts when the
        // specimen unmounts (uncheck, reset or leave the bench). That selector outranks the
        // [data-theme=daisyblazor] light pin in wwwroot/index.html, so it still works. A radio cannot
        // be unchecked by clicking it again: reset the bench to return to the light theme.
        options.For<ThemeController>()
            .Parameter(nameof(ThemeController.Value), "dark")
            .Parameter(nameof(ThemeController.Class), "inline-flex items-center gap-3")
            .Slot(nameof(ThemeController.ChildContent), b => b.AddContent(0, "Dark theme"), "Dark theme")
            .Variant("Radio", v => v.Set(nameof(ThemeController.Type), ThemeControllerType.Radio).Set(nameof(ThemeController.Name), "theme"))
            .Variant("Synthwave", v => v.Set(nameof(ThemeController.Value), "synthwave"))
            .Variant("Hidden input", v => v.Set(nameof(ThemeController.HideInput), true));
    }

    /// <summary>
    /// The actions and form inputs. Verified against each component's .razor at tag v0.6.0 and the
    /// compiled app.css: the colour and size classes they compose (btn-*, checkbox-*, toggle-*,
    /// range-*, file-input-*, bg-* on the rating mask) come from the safelist's brace patterns, and
    /// the few classes a variant passes itself (join-vertical, mask-heart) are literal below, which
    /// is what makes Tailwind emit them. DaisyPresetTests pins the control each preset renders.
    /// Upstream at 0.6.0, not safelist holes: TextField, NumericField, Select and DatePicker add
    /// <c>input-bordered</c>/<c>select-bordered</c> and Autocomplete <c>input-disabled</c>, daisyUI 4
    /// classes daisyUI 5 no longer defines. They style nothing, and nothing is lost: daisyUI 5 draws
    /// the border by default and Autocomplete's disabled input greys itself out.
    /// </summary>
    private static void FormsAndActions(PlayBlazorOptions options)
    {
        // --- Actions ---

        // A .join: daisyUI joins the child .btn borders. A non-default group Variant/Color/Size
        // overrides the children's (OverrideStyles, on by default); the children are bare Buttons
        // (ghost by default), so the group's Filled is what makes them solid. There is no Vertical
        // parameter: daisyUI's join-vertical through Class stacks them.
        options.For<ButtonGroup>()
            .Slot(nameof(ButtonGroup.ChildContent), DaisyDemoFragments.GroupButtons, DaisyDemoFragmentSources.GroupButtons)
            .Parameter(nameof(ButtonGroup.Variant), Variant.Filled)
            .Related<Button>()
            .Variant("Primary", v => v.Set(nameof(ButtonGroup.Color), Color.Primary))
            .Variant("Outlined", v => v.Set(nameof(ButtonGroup.Variant), Variant.Outlined))
            .Variant("Small", v => v.Set(nameof(ButtonGroup.Size), Size.Small))
            .Variant("Vertical", v => v.Set(nameof(ButtonGroup.Class), "join-vertical"));

        // Always btn-ghost btn-circle: Color tints the icon, not the button. Title is the native
        // tooltip and, for an icon-only button, its accessible name.
        options.For<IconButton>()
            .Parameter(nameof(IconButton.Icon), Icons.Material.Filled.Favorite)
            .Parameter(nameof(IconButton.Title), "Add to favourites")
            .Related<Button>()
            .Variant("Primary", v => v.Set(nameof(IconButton.Color), Color.Primary))
            .Variant("Error", v => v.Set(nameof(IconButton.Color), Color.Error))
            .Variant("Large", v => v.Set(nameof(IconButton.Size), Size.Large))
            .Variant("Disabled", v => v.Set(nameof(IconButton.Disabled), true))
            .Variant("As a link", v => v.Set(nameof(IconButton.Href), "https://daisyui.com"));

        // daisyUI's .fab is position:fixed at the viewport corner. Fixed=false (the same answer as
        // FabAction's scaffold) makes it absolute in the frame's relative box. The actions unfold on
        // :focus-within, so click the trigger to see them. ShowClose's overlay is the close button
        // laid over the trigger while open.
        options.For<Fab>()
            .Scaffold(DaisyDemoFragments.InFabFrame, DaisyDemoFragmentSources.InFabFrame)
            .Parameter(nameof(Fab.Fixed), false)
            .Parameter(nameof(Fab.AriaLabel), "Actions")
            .Slot(nameof(Fab.ChildContent), DaisyDemoFragments.FabActions, DaisyDemoFragmentSources.FabActions)
            .Related<FabAction>()
            .Variant("Secondary", v => v.Set(nameof(Fab.Color), Color.Secondary))
            .Variant("Flower", v => v.Set(nameof(Fab.Flower), true))
            .Variant("Custom icon", v => v.Set(nameof(Fab.Icon), Icons.Material.Filled.MoreVert))
            .Variant("No close overlay", v => v.Set(nameof(Fab.ShowClose), false));

        // A label around a hidden checkbox: clicking swaps .swap-on and .swap-off. The animations are
        // DaisyBlazor's own SwapRotateExample and SwapFlipExample at v0.6.0.
        options.For<Swap>()
            .Slot(nameof(Swap.On), DaisyDemoFragments.SwapOn, DaisyDemoFragmentSources.SwapOn)
            .Slot(nameof(Swap.Off), DaisyDemoFragments.SwapOff, DaisyDemoFragmentSources.SwapOff)
            .Parameter(nameof(Swap.AriaLabel), "Dark mode")
            .Variant("Rotate", v => v.Set(nameof(Swap.Animation), SwapAnimation.Rotate))
            .Variant("Flip", v => v.Set(nameof(Swap.Animation), SwapAnimation.Flip))
            .Variant("Active", v => v.Set(nameof(Swap.Active), true))
            .Variant("Text labels", v => v.Set(nameof(Swap.On), "ON").Set(nameof(Swap.Off), "OFF"));

        // --- Text and number fields ---

        // Variant.Outlined (the default) is the plain daisyUI 5 .input; Filled adds bg-base-200 and
        // Text is input-ghost. Dense (or Margin.Dense) is the only size axis: input-sm.
        options.For<TextField>()
            .Parameter(nameof(TextField.Label), "Email")
            .Parameter(nameof(TextField.InputType), InputType.Email)
            .Parameter(nameof(TextField.Placeholder), "ada@example.com")
            .Parameter(nameof(TextField.HelperText), "We never share your email.")
            .Related<Form>()
            .Variant("Filled", v => v.Set(nameof(TextField.Variant), Variant.Filled))
            .Variant("Ghost", v => v.Set(nameof(TextField.Variant), Variant.Text))
            .Variant("Dense", v => v.Set(nameof(TextField.Dense), true))
            .Variant("Disabled", v => v.Set(nameof(TextField.Disabled), true))
            .Variant("With error", v => v.Set(nameof(TextField.Error), true).Set(nameof(TextField.ErrorText), "This email is already registered."))
            .Variant("Multi-line", v => v.Set(nameof(TextField.Lines), 3).Set(nameof(TextField.Placeholder), "Tell us about yourself"));

        // Min, Max and Step render only when passed (NumericField tracks which were set), so the
        // preset passes all three, and 42 sits on the step grid from 0.
        options.For<NumericField<int>>()
            .Parameter(nameof(NumericField<int>.Label), "Quantity")
            .Parameter(nameof(NumericField<int>.Value), 42)
            .Parameter(nameof(NumericField<int>.Min), 0)
            .Parameter(nameof(NumericField<int>.Max), 100)
            .Parameter(nameof(NumericField<int>.Step), 2)
            .Parameter(nameof(NumericField<int>.HelperText), "Even numbers from 0 to 100.")
            .Related<TextField>()
            .Variant("Filled", v => v.Set(nameof(NumericField<int>.Variant), Variant.Filled))
            .Variant("Dense", v => v.Set(nameof(NumericField<int>.Dense), true))
            .Variant("Disabled", v => v.Set(nameof(NumericField<int>.Disabled), true))
            .Variant("With unit", v => v.Set(nameof(NumericField<int>.Adornment), Adornment.End).Set(nameof(NumericField<int>.AdornmentText), "kg"));

        // SearchFunc is the whole component: without it a search never runs and the list never
        // opens. A delegate has no control, so the preset is fixed (type two letters: "an"). Variant
        // is accepted for MudBlazor compatibility and does nothing, so no variant uses it.
        // ToStringFunc stays null: for strings, ToString() is already the display text.
        options.For<Autocomplete<string>>()
            .Parameter(nameof(Autocomplete<string>.Label), "Fruit")
            .Parameter(nameof(Autocomplete<string>.Placeholder), "Type two letters, e.g. an")
            .Parameter(
                nameof(Autocomplete<string>.SearchFunc),
                (Func<string, CancellationToken, Task<IEnumerable<string>>>)SearchFruits,
                "@((text, _) => Task.FromResult(_fruits.Where(f => f.Contains(text, StringComparison.OrdinalIgnoreCase))))")
            .Related<Select<string>>()
            .Variant("Dense", v => v.Set(nameof(Autocomplete<string>.Dense), true))
            .Variant("Disabled", v => v.Set(nameof(Autocomplete<string>.Disabled), true))
            .Variant("Search icon", v => v.Set(nameof(Autocomplete<string>.Adornment), Adornment.Start).Set(nameof(Autocomplete<string>.AdornmentIcon), Icons.Material.Filled.Search))
            .Variant("Clearable", v => v.Set(nameof(Autocomplete<string>.Clearable), true).Set(nameof(Autocomplete<string>.Value), "Banana"))
            .Variant("Required", v => v.Set(nameof(Autocomplete<string>.Required), true));

        // --- Choices ---

        // A native <select>: each SelectItem registers and becomes an <option>. SelectedValues is an
        // IEnumerable without a control, so it is a fixed preset, and a variant could not set it; it
        // only counts once MultiSelection is on (single mode reads Value), hence the Multiple variant.
        options.For<Select<string>>()
            .Parameter(nameof(Select<string>.Label), "Fruit")
            .Parameter(nameof(Select<string>.Value), "banana")
            .Parameter(nameof(Select<string>.SelectedValues), new[] { "apple", "cherry" }, "@(new[] { \"apple\", \"cherry\" })")
            .Slot(nameof(Select<string>.ChildContent), DaisyDemoFragments.SelectOptions, DaisyDemoFragmentSources.SelectOptions)
            .Related<SelectItem<string>>()
            .Variant("Filled", v => v.Set(nameof(Select<string>.Variant), Variant.Filled))
            .Variant("Dense", v => v.Set(nameof(Select<string>.Dense), true))
            .Variant("Disabled", v => v.Set(nameof(Select<string>.Disabled), true))
            .Variant("Clearable", v => v.Set(nameof(Select<string>.Clearable), true).Set(nameof(Select<string>.ClearText), "No fruit"))
            .Variant("Multiple", v => v.Set(nameof(Select<string>.MultiSelection), true));

        // The group owns the shared radio name and the selection; colour and size are per Radio.
        // Upstream at 0.6.0: the group cascades itself IsFixed and a Radio's own parameters do not
        // change when the group's Value does, so a Value set from OUTSIDE (a variant here, a parent
        // assigning a bound field in an app) does not move the check until something re-renders the
        // radios. A click works: it is the browser that checks the radio. So no variant changes Value
        // (DaisyPresetTests pins the lag).
        options.For<RadioGroup<string>>()
            .Parameter(nameof(RadioGroup<string>.Label), "Fruit")
            .Parameter(nameof(RadioGroup<string>.Value), "banana")
            .Slot(nameof(RadioGroup<string>.ChildContent), DaisyDemoFragments.RadioOptions, DaisyDemoFragmentSources.RadioOptions)
            .Related<Radio<string>>()
            .Variant("Horizontal", v => v.Set(nameof(RadioGroup<string>.Horizontal), true))
            .Variant("Roomy", v => v.Set(nameof(RadioGroup<string>.Class), "gap-3"))
            .Variant("No caption", v => v.Set(nameof(RadioGroup<string>.Label), string.Empty));

        // A .join of buttons; the selected one takes btn-{Color}. Values, like Select's
        // SelectedValues, is a fixed preset that only counts in MultiSelection mode.
        options.For<ToggleGroup<string>>()
            .Parameter(nameof(ToggleGroup<string>.Value), "grid")
            .Parameter(nameof(ToggleGroup<string>.Values), new[] { "list", "table" }, "@(new[] { \"list\", \"table\" })")
            .Slot(nameof(ToggleGroup<string>.ChildContent), DaisyDemoFragments.ToggleItems, DaisyDemoFragmentSources.ToggleItems)
            .Related<ToggleItem<string>>()
            .Variant("Secondary", v => v.Set(nameof(ToggleGroup<string>.Color), Color.Secondary))
            .Variant("Small", v => v.Set(nameof(ToggleGroup<string>.Size), Size.Small))
            .Variant("Check mark", v => v.Set(nameof(ToggleGroup<string>.CheckMark), true))
            .Variant("Disabled", v => v.Set(nameof(ToggleGroup<string>.Disabled), true))
            .Variant("Multiple", v => v.Set(nameof(ToggleGroup<string>.SelectionMode), SelectionMode.MultiSelection));

        // Deliberately Checked, not Value. Checkbox and Switch read the MudBlazor alias Checked as
        // soon as CheckedChanged has a delegate, and the bench wires EVERY EventCallback to its event
        // log, CheckedChanged included. So on the bench Checked is the source of truth and the Value
        // control does nothing; a page that binds @bind-Value gets Value instead. Checked="true" is
        // also what checks the box in pasted markup with no binding (_usesChecked at v0.6.0).
        // Indeterminate is a DOM property, not an attribute: Checkbox sets it after render through
        // JS interop (Reflect.set), so that variant changes the live checkbox, not the markup.
        options.For<Checkbox>()
            .Parameter(nameof(Checkbox.Label), "Remember me")
            .Parameter(nameof(Checkbox.Checked), true)
            .Related<Switch>()
            .Variant("Primary", v => v.Set(nameof(Checkbox.Color), Color.Primary))
            .Variant("Large", v => v.Set(nameof(Checkbox.Size), Size.Large))
            .Variant("Disabled", v => v.Set(nameof(Checkbox.Disabled), true))
            .Variant("Indeterminate", v => v.Set(nameof(Checkbox.Indeterminate), true).Set(nameof(Checkbox.Checked), false));

        // Checked rather than Value, for the same reason as Checkbox.
        options.For<Switch>()
            .Parameter(nameof(Switch.Label), "Email notifications")
            .Parameter(nameof(Switch.Checked), true)
            .Related<Checkbox>()
            .Variant("Success", v => v.Set(nameof(Switch.Color), Color.Success))
            .Variant("Large", v => v.Set(nameof(Switch.Size), Size.Large))
            .Variant("Small", v => v.Set(nameof(Switch.Size), Size.Small))
            .Variant("Disabled", v => v.Set(nameof(Switch.Disabled), true));

        // Label is the slider's aria-label only: Range renders a bare <input type="range">, no text.
        // Value, Min, Max and Step are doubles.
        options.For<DaisyBlazor.Range>()
            .Parameter(nameof(DaisyBlazor.Range.Label), "Volume")
            .Parameter(nameof(DaisyBlazor.Range.Value), 40d)
            .Variant("Primary", v => v.Set(nameof(DaisyBlazor.Range.Color), Color.Primary))
            .Variant("Small", v => v.Set(nameof(DaisyBlazor.Range.Size), Size.Small))
            .Variant("Large", v => v.Set(nameof(DaisyBlazor.Range.Size), Size.Large))
            .Variant("Steps of 25", v => v.Set(nameof(DaisyBlazor.Range.Step), 25d));

        // Radios masked as stars (mask-star-2) filled bg-{Color}; Warning is the default colour.
        // Rating has no Label parameter: the group is aria-labelled "Rating".
        options.For<Rating>()
            .Parameter(nameof(Rating.Value), 3)
            .Variant("Hearts", v => v.Set(nameof(Rating.Mask), "mask-heart").Set(nameof(Rating.Color), Color.Error))
            .Variant("Primary", v => v.Set(nameof(Rating.Color), Color.Primary))
            .Variant("Large", v => v.Set(nameof(Rating.Size), Size.Large))
            .Variant("Ten stars", v => v.Set(nameof(Rating.Max), 10))
            .Variant("Read-only", v => v.Set(nameof(Rating.ReadOnly), true));

        // A native <input type="color"> beside its hex. PickerVariant and ColorPickerView are
        // MudBlazor-compat parameters the markup never reads, so no variant uses them.
        options.For<ColorPicker>()
            .Parameter(nameof(ColorPicker.Label), "Brand colour")
            .Parameter(nameof(ColorPicker.Value), "#605dff")
            .Variant("Other colour", v => v.Set(nameof(ColorPicker.Value), "#f43098"))
            .Variant("Compact", v => v.Set(nameof(ColorPicker.FullWidth), false))
            .Variant("Disabled", v => v.Set(nameof(ColorPicker.Disabled), true));

        // A text field in DateFormat (default: the current culture's short date) with the native
        // date input laid invisibly over the calendar glyph. That overlay is DatePicker.razor.css,
        // scoped CSS that reaches the page through PlayBlazor.Demo.Daisy.styles.css, which imports
        // DaisyBlazor's bundle. Color is MudBlazor-compat and does nothing.
        options.For<DatePicker>()
            .Parameter(nameof(DatePicker.Label), "Birth date")
            .Parameter(nameof(DatePicker.Date), new DateTime(1815, 12, 10), "@(new DateTime(1815, 12, 10))")
            .Variant("Filled", v => v.Set(nameof(DatePicker.Variant), Variant.Filled))
            .Variant("Dense", v => v.Set(nameof(DatePicker.Dense), true))
            .Variant("Disabled", v => v.Set(nameof(DatePicker.Disabled), true))
            .Variant("ISO format", v => v.Set(nameof(DatePicker.DateFormat), "yyyy-MM-dd"))
            .Variant("Picker only", v => v.Set(nameof(DatePicker.Editable), false));

        options.For<FileInput>()
            .Parameter(nameof(FileInput.Label), "Profile picture")
            .Parameter(nameof(FileInput.Accept), "image/*")
            .Variant("Primary", v => v.Set(nameof(FileInput.Color), Color.Primary))
            .Variant("Small", v => v.Set(nameof(FileInput.Size), Size.Small))
            .Variant("Large", v => v.Set(nameof(FileInput.Size), Size.Large))
            .Variant("Ghost", v => v.Set(nameof(FileInput.Ghost), true))
            .Variant("Multiple", v => v.Set(nameof(FileInput.Multiple), true));

        // The label wraps a span and the input it floats over; it has no axis of its own beyond Class.
        options.For<FloatingLabel>()
            .Parameter(nameof(FloatingLabel.Label), "Email")
            .Slot(nameof(FloatingLabel.ChildContent), DaisyDemoFragments.FloatingInput, DaisyDemoFragmentSources.FloatingInput)
            .Related<TextField>()
            .Variant("Work email", v => v.Set(nameof(FloatingLabel.Label), "Work email"))
            .Variant("Narrow", v => v.Set(nameof(FloatingLabel.Class), "max-w-xs"))
            .Variant("Bold", v => v.Set(nameof(FloatingLabel.Class), "font-semibold"));
    }

    /// <summary>
    /// Data display and feedback. Verified against each component's .razor at tag v0.6.0 and the
    /// compiled app.css. The classes they compose come from the safelist (alert-*, badge-*,
    /// status-*, chat-bubble-*, tooltip-*, loading-*, progress-*, mask-*, kbd-*, shadow-*,
    /// indicator-*, the aspect-* defaults); the few a preset passes itself through Class (w-96,
    /// h-24, menu-horizontal, ...) are literal below, which is what makes Tailwind emit them.
    /// Containers get two or three of their real children (Card, Carousel, Stats, Timeline, List,
    /// ItemList, ExpansionPanels, Indicator). Image slots use the self-contained
    /// <see cref="SamplePhoto" /> data URIs, never an external host. Remember that the bench wires
    /// EVERY EventCallback to its event log: Chip therefore shows its close button, and Card and
    /// Paper take their clickable (role="button") form. DaisyPresetTests pins each root class.
    /// </summary>
    private static void DisplayAndFeedback(PlayBlazorOptions options)
    {
        // --- Containers ---

        // Elevation maps 0 / 1 shadow-sm / 2-3 shadow / 4-6 shadow-md / 7-12 shadow-lg / 13+ shadow-xl.
        options.For<Card>()
            .Parameter(nameof(Card.Class), "w-80")
            .Slot(nameof(Card.ChildContent), DaisyDemoFragments.CardParts, DaisyDemoFragmentSources.CardParts)
            .Related<CardContent>()
            .Variant("Outlined", v => v.Set(nameof(Card.Outlined), true))
            .Variant("Flat", v => v.Set(nameof(Card.Elevation), 0))
            .Variant("Raised", v => v.Set(nameof(Card.Elevation), 6))
            .Variant("Floating", v => v.Set(nameof(Card.Elevation), 24));

        // A horizontal scroll-snap strip: w-80 shows one slide and a peek of the next.
        options.For<Carousel>()
            .Parameter(nameof(Carousel.Class), "rounded-box gap-4 w-80")
            .Slot(nameof(Carousel.ChildContent), DaisyDemoFragments.CarouselSlides, DaisyDemoFragmentSources.CarouselSlides)
            .Related<CarouselItem>()
            .Variant("Snap to center", v => v.Set(nameof(Carousel.Snap), CarouselSnap.Center))
            .Variant("Snap to end", v => v.Set(nameof(Carousel.Snap), CarouselSnap.End))
            .Variant("Vertical", v => v.Set(nameof(Carousel.Vertical), true).Set(nameof(Carousel.Class), "rounded-box h-40 w-64"));

        options.For<Stats>()
            .Parameter(nameof(Stats.Shadow), true)
            .Slot(nameof(Stats.ChildContent), DaisyDemoFragments.StatCells, DaisyDemoFragmentSources.StatCells)
            .Related<Stat>()
            .Variant("Vertical", v => v.Set(nameof(Stats.Vertical), true))
            .Variant("Flat", v => v.Set(nameof(Stats.Shadow), false))
            .Variant("Primary surface", v => v.Set(nameof(Stats.Class), "bg-primary text-primary-content"));

        // Alternate (the default) switches sides per item; the Timeline decides each item's side.
        options.For<Timeline>()
            .Slot(nameof(Timeline.ChildContent), DaisyDemoFragments.TimelineItems, DaisyDemoFragmentSources.TimelineItems)
            .Related<TimelineItem>()
            .Variant("Compact", v => v.Set(nameof(Timeline.TimelinePosition), TimelinePosition.Start))
            .Variant("Boxes on the start side", v => v.Set(nameof(Timeline.TimelinePosition), TimelinePosition.End))
            .Variant("Reversed", v => v.Set(nameof(Timeline.Reverse), true));

        // daisyUI 5's .list of .list-row; Background and Rounded default to true.
        options.For<List>()
            .Parameter(nameof(List.Class), "w-80")
            .Slot(nameof(List.ChildContent), DaisyDemoFragments.ListRows, DaisyDemoFragmentSources.ListRows)
            .Related<ListRow>()
            .Variant("Shadowed", v => v.Set(nameof(List.Shadowed), true))
            .Variant("No background", v => v.Set(nameof(List.Background), false))
            .Variant("Square corners", v => v.Set(nameof(List.Rounded), false));

        // A .menu (always w-full p-0): Class adds the surface, not a width that would fight w-full.
        options.For<ItemList>()
            .Parameter(nameof(ItemList.Class), "bg-base-200 rounded-box")
            .Slot(nameof(ItemList.ChildContent), DaisyDemoFragments.ListItems, DaisyDemoFragmentSources.ListItems)
            .Related<ListItem>()
            .Variant("Dense", v => v.Set(nameof(ItemList.Dense), true))
            .Variant("Horizontal", v => v.Set(nameof(ItemList.Class), "bg-base-200 rounded-box menu-horizontal"))
            .Variant("Large", v => v.Set(nameof(ItemList.Class), "bg-base-200 rounded-box menu-lg"));

        // Single-open accordion through a shared <details name>; Elevation is each panel's shadow.
        options.For<ExpansionPanels>()
            .Parameter(nameof(ExpansionPanels.Class), "w-96")
            .Slot(nameof(ExpansionPanels.ChildContent), DaisyDemoFragments.Panels, DaisyDemoFragmentSources.Panels)
            .Related<ExpansionPanel>()
            .Variant("Multi-expansion", v => v.Set(nameof(ExpansionPanels.MultiExpansion), true))
            .Variant("Dense", v => v.Set(nameof(ExpansionPanels.Dense), true))
            .Variant("Flat", v => v.Set(nameof(ExpansionPanels.Elevation), 0))
            .Variant("Raised", v => v.Set(nameof(ExpansionPanels.Elevation), 6));

        // DaisyBlazor's IndicatorContentExample: a badge over the corner of a button.
        options.For<Indicator>()
            .Slot(nameof(Indicator.Item), DaisyDemoFragments.IndicatorBadge, DaisyDemoFragmentSources.IndicatorBadge)
            .Slot(nameof(Indicator.ChildContent), DaisyDemoFragments.OutlinedButton("Tickets"), DaisyDemoFragmentSources.TicketsButton)
            .Related<Badge>()
            .Variant("Bottom end", v => v.Set(nameof(Indicator.Vertical), IndicatorVertical.Bottom))
            .Variant("Top start", v => v.Set(nameof(Indicator.Horizontal), IndicatorHorizontal.Start))
            .Variant("Top center", v => v.Set(nameof(Indicator.Horizontal), IndicatorHorizontal.Center))
            .Variant("Middle", v => v.Set(nameof(Indicator.Vertical), IndicatorVertical.Middle));

        // --- Badges, chips and dots ---

        // Indicator mode (the default) overlays a badge-sm on ChildContent. There is no Size axis:
        // the badge is always badge-sm (badge-xs as a Dot). Content is an object and has no control,
        // so the count is fixed; Max caps it. Inline mode is left to the control: it renders the
        // ChildContent INSIDE the badge, which with this button child would nest a button in a span.
        options.For<Badge>()
            .Parameter(nameof(Badge.Content), 8, "8")
            .Slot(nameof(Badge.ChildContent), DaisyDemoFragments.OutlinedButton("Inbox"), DaisyDemoFragmentSources.InboxButton)
            .Related<Indicator>()
            .Variant("Secondary", v => v.Set(nameof(Badge.Color), Color.Secondary))
            .Variant("Error", v => v.Set(nameof(Badge.Color), Color.Error))
            .Variant("Dot", v => v.Set(nameof(Badge.Dot), true))
            .Variant("Capped at 5", v => v.Set(nameof(Badge.Max), 5))
            .Variant("Bordered", v => v.Set(nameof(Badge.Bordered), true))
            .Variant("Overlapping", v => v.Set(nameof(Badge.Overlap), true));

        // A .badge with badge-{color}, badge-{sm,lg} and badge-{outline,soft}, all safelisted.
        // On the bench OnClick and OnClose have delegates (the event log), so the label is a button
        // and the close button shows; without an OnClose handler a page gets a plain span.
        options.For<Chip>()
            .Parameter(nameof(Chip.Text), "Blazor")
            .Parameter(nameof(Chip.Color), Color.Primary)
            .Related<Badge>()
            .Variant("Success", v => v.Set(nameof(Chip.Color), Color.Success))
            .Variant("Outlined", v => v.Set(nameof(Chip.Variant), Variant.Outlined))
            .Variant("Soft", v => v.Set(nameof(Chip.Variant), Variant.Text))
            .Variant("Small", v => v.Set(nameof(Chip.Size), Size.Small))
            .Variant("Large", v => v.Set(nameof(Chip.Size), Size.Large))
            .Variant("With icon", v => v.Set(nameof(Chip.Icon), Icons.Material.Filled.Bolt));

        // status-{color} and status-{xs..xl} are safelisted (Medium adds no class). AriaLabel makes
        // the dot role="img", which a bare dot needs to mean anything to a screen reader.
        options.For<Status>()
            .Parameter(nameof(Status.Color), Color.Success)
            .Parameter(nameof(Status.AriaLabel), "Online")
            .Related<Indicator>()
            .Variant("Error", v => v.Set(nameof(Status.Color), Color.Error).Set(nameof(Status.AriaLabel), "Offline"))
            .Variant("Warning", v => v.Set(nameof(Status.Color), Color.Warning).Set(nameof(Status.AriaLabel), "Away"))
            .Variant("Extra small", v => v.Set(nameof(Status.Size), StatusSize.ExtraSmall))
            .Variant("Extra large", v => v.Set(nameof(Status.Size), StatusSize.ExtraLarge))
            .Variant("Animated", v => v.Set(nameof(Status.Animated), true));

        options.For<Kbd>()
            .Slot(nameof(Kbd.ChildContent), b => b.AddContent(0, "Ctrl"), "Ctrl")
            .Variant("Small", v => v.Set(nameof(Kbd.Size), Size.Small))
            .Variant("Large", v => v.Set(nameof(Kbd.Size), Size.Large))
            .Variant("Extra large", v => v.Set(nameof(Kbd.Class), "kbd-xl"))
            .Variant("Other key", v => v.Set(nameof(Kbd.ChildContent), "Shift"));

        // --- Alerts and states ---

        options.For<Alert>()
            .Parameter(nameof(Alert.Severity), Severity.Info)
            .Slot(nameof(Alert.ChildContent), b => b.AddContent(0, "12 unread messages. Tap to see them."), "12 unread messages. Tap to see them.")
            .Related<ErrorAlert>()
            .Variant("Success", v => v.Set(nameof(Alert.Severity), Severity.Success).Set(nameof(Alert.ChildContent), "Your purchase has been confirmed."))
            .Variant("Warning", v => v.Set(nameof(Alert.Severity), Severity.Warning).Set(nameof(Alert.ChildContent), "Your trial ends in 3 days."))
            .Variant("Error", v => v.Set(nameof(Alert.Severity), Severity.Error).Set(nameof(Alert.ChildContent), "The payment could not be processed."))
            .Variant("Outlined", v => v.Set(nameof(Alert.Variant), Variant.Outlined))
            .Variant("Soft", v => v.Set(nameof(Alert.Variant), Variant.Text))
            .Variant("Dense, no icon", v => v.Set(nameof(Alert.Dense), true).Set(nameof(Alert.NoIcon), true));

        // A filled error Alert; a blank Message renders nothing at all, hence the preset.
        options.For<ErrorAlert>()
            .Parameter(nameof(ErrorAlert.Message), "Could not save: the server is unreachable.")
            .Related<Alert>()
            .Variant("Validation error", v => v.Set(nameof(ErrorAlert.Message), "The email address is already registered."))
            .Variant("Narrow", v => v.Set(nameof(ErrorAlert.Class), "max-w-xs"))
            .Variant("Long message", v => v.Set(nameof(ErrorAlert.Message), "We could not reach the payment provider. Nothing was charged; try again in a few minutes or contact support."));

        // Message is [EditorRequired]: without it the state is an icon over an empty line.
        options.For<EmptyState>()
            .Parameter(nameof(EmptyState.Message), "No projects yet")
            .Parameter(nameof(EmptyState.Icon), Icons.Material.Filled.FolderOpen)
            .Parameter(nameof(EmptyState.Subtitle), "Create your first project to get started.")
            .Slot(nameof(EmptyState.ChildContent), DaisyDemoFragments.NewProjectButton, DaisyDemoFragmentSources.NewProjectButton)
            .Related<AccessDenied>()
            .Variant("No search results", v => v.Set(nameof(EmptyState.Icon), Icons.Material.Filled.SearchOff).Set(nameof(EmptyState.Message), "No results for “zeppelin”").Set(nameof(EmptyState.Subtitle), "Try a different keyword."))
            .Variant("Inbox zero", v => v.Set(nameof(EmptyState.Icon), Icons.Material.Filled.Inbox).Set(nameof(EmptyState.Message), "You're all caught up").Set(nameof(EmptyState.Subtitle), "New messages will show up here."))
            .Variant("Offline", v => v.Set(nameof(EmptyState.Icon), Icons.Material.Filled.CloudOff).Set(nameof(EmptyState.Message), "You are offline"));

        // No [EditorRequired] input and no auth dependency (no AuthorizeView, no
        // AuthenticationState cascade): every string is a parameter. The defaults are a lock icon,
        // "Access denied" and min-height 400px; the preset trims the height to bench size. The
        // action renders only with ActionText (ActionHref defaults to "/"). "#" is a placeholder:
        // it does not keep a click on the bench (see LayoutAndNavigation's summary).
        options.For<AccessDenied>()
            .Parameter(nameof(AccessDenied.Message), "You need the Billing admin role to view invoices.")
            .Parameter(nameof(AccessDenied.ActionText), "Back to home")
            .Parameter(nameof(AccessDenied.ActionHref), "#")
            .Parameter(nameof(AccessDenied.MinHeight), "16rem")
            .Related<EmptyState>()
            .Variant("Custom title", v => v.Set(nameof(AccessDenied.Title), "Members only"))
            .Variant("Warning icon", v => v.Set(nameof(AccessDenied.Icon), Icons.Material.Filled.Block).Set(nameof(AccessDenied.IconColor), Color.Warning))
            .Variant("No action", v => v.Set(nameof(AccessDenied.ActionText), string.Empty))
            .Variant("No icon", v => v.Set(nameof(AccessDenied.Icon), string.Empty));

        // A <div class="tooltip" data-tip> plus an sr-only copy of the text. The tip shows on hover
        // or focus, or always with Open, which the colour and placement variants turn on.
        // Color.Dark is NOT offered: it composes tooltip-neutral, which the safelist lists but
        // bundled daisyUI 5 does not define, so the tip keeps its default colour (upstream hole).
        options.For<Tooltip>()
            .Parameter(nameof(Tooltip.Text), "Saves a draft")
            .Slot(nameof(Tooltip.ChildContent), DaisyDemoFragments.OutlinedButton("Save"), DaisyDemoFragmentSources.HoverButton)
            .Related<Button>()
            .Variant("Always open", v => v.Set(nameof(Tooltip.Open), true))
            .Variant("Primary", v => v.Set(nameof(Tooltip.Color), Color.Primary).Set(nameof(Tooltip.Open), true))
            .Variant("Error", v => v.Set(nameof(Tooltip.Color), Color.Error).Set(nameof(Tooltip.Open), true))
            .Variant("Below", v => v.Set(nameof(Tooltip.Placement), Placement.Bottom).Set(nameof(Tooltip.Open), true))
            .Variant("Right", v => v.Set(nameof(Tooltip.Placement), Placement.Right).Set(nameof(Tooltip.Open), true));

        // --- Progress and loading ---

        options.For<Loading>()
            .Variant("Dots", v => v.Set(nameof(Loading.Type), LoadingType.Dots))
            .Variant("Ring", v => v.Set(nameof(Loading.Type), LoadingType.Ring))
            .Variant("Ball", v => v.Set(nameof(Loading.Type), LoadingType.Ball))
            .Variant("Bars", v => v.Set(nameof(Loading.Type), LoadingType.Bars))
            .Variant("Infinity", v => v.Set(nameof(Loading.Type), LoadingType.Infinity))
            .Variant("Large primary", v => v.Set(nameof(Loading.Size), Size.Large).Set(nameof(Loading.Color), Color.Primary));

        // Indeterminate (the default) is a plain loading-spinner; the preset shows the determinate
        // radial-progress ring, which is what Value drives.
        options.For<ProgressCircular>()
            .Parameter(nameof(ProgressCircular.Indeterminate), false)
            .Parameter(nameof(ProgressCircular.Value), 65d)
            .Parameter(nameof(ProgressCircular.AriaLabel), "Upload progress")
            .Related<RadialProgress>()
            .Variant("Primary", v => v.Set(nameof(ProgressCircular.Color), Color.Primary))
            .Variant("Large", v => v.Set(nameof(ProgressCircular.Size), Size.Large))
            .Variant("Indeterminate", v => v.Set(nameof(ProgressCircular.Indeterminate), true))
            .Variant("Out of 8", v => v.Set(nameof(ProgressCircular.Max), 8d).Set(nameof(ProgressCircular.Value), 3d));

        // daisyUI's .progress is width:100%; w-56 keeps the bar a readable length on the bench.
        options.For<ProgressLinear>()
            .Parameter(nameof(ProgressLinear.Value), 40d)
            .Parameter(nameof(ProgressLinear.AriaLabel), "Upload progress")
            .Parameter(nameof(ProgressLinear.Class), "w-56")
            .Related<ProgressCircular>()
            .Variant("Primary", v => v.Set(nameof(ProgressLinear.Color), Color.Primary))
            .Variant("Success, full", v => v.Set(nameof(ProgressLinear.Color), Color.Success).Set(nameof(ProgressLinear.Value), 100d))
            .Variant("Thick", v => v.Set(nameof(ProgressLinear.Size), Size.Large))
            .Variant("Thin", v => v.Set(nameof(ProgressLinear.Size), Size.Small))
            .Variant("Indeterminate", v => v.Set(nameof(ProgressLinear.Indeterminate), true));

        options.For<RadialProgress>()
            .Parameter(nameof(RadialProgress.Value), 70d)
            .Parameter(nameof(RadialProgress.AriaLabel), "Storage used")
            .Related<ProgressCircular>()
            .Variant("Primary", v => v.Set(nameof(RadialProgress.Color), Color.Primary))
            .Variant("Success, full", v => v.Set(nameof(RadialProgress.Color), Color.Success).Set(nameof(RadialProgress.Value), 100d))
            .Variant("Small", v => v.Set(nameof(RadialProgress.SizeRem), "3rem"))
            .Variant("Thick ring", v => v.Set(nameof(RadialProgress.Thickness), "0.75rem"));

        // Skeleton is a block with no intrinsic width: on the flex bench it would collapse, so every
        // shape sets its own size (DaisyBlazor's SkeletonShapesExample).
        options.For<Skeleton>()
            .Parameter(nameof(Skeleton.Width), "12rem")
            .Parameter(nameof(Skeleton.Height), "1rem")
            .Variant("Circle", v => v.Set(nameof(Skeleton.SkeletonType), SkeletonType.Circle).Set(nameof(Skeleton.Width), "4rem").Set(nameof(Skeleton.Height), "4rem"))
            .Variant("Rectangle", v => v.Set(nameof(Skeleton.SkeletonType), SkeletonType.Rectangle).Set(nameof(Skeleton.Width), "8rem").Set(nameof(Skeleton.Height), "5rem"))
            .Variant("Heading line", v => v.Set(nameof(Skeleton.Width), "16rem").Set(nameof(Skeleton.Height), "2rem"));

        // One animated cell (daisyUI paints the digits with ::before/::after); MinDigits emits
        // --digits, so 7 reads "07" with 2.
        options.For<Countdown>()
            .Parameter(nameof(Countdown.Value), 42)
            .Parameter(nameof(Countdown.Class), "font-mono text-5xl")
            .Variant("Two digits", v => v.Set(nameof(Countdown.Value), 7).Set(nameof(Countdown.MinDigits), 2))
            .Variant("Three digits", v => v.Set(nameof(Countdown.MinDigits), 3))
            .Variant("Hundreds", v => v.Set(nameof(Countdown.Value), 365))
            .Variant("With label", v => v.Set(nameof(Countdown.Label), "sec"));

        // --- Images and shapes ---

        options.For<Avatar>()
            .Parameter(nameof(Avatar.Image), SamplePhoto, "@_photo")
            .Parameter(nameof(Avatar.Alt), "Ada Lovelace")
            .Related<Image>()
            .Variant("Initials", v => v.Set(nameof(Avatar.Image), string.Empty).Set(nameof(Avatar.ChildContent), "AL").Set(nameof(Avatar.Color), Color.Primary))
            .Variant("Outlined initials", v => v.Set(nameof(Avatar.Image), string.Empty).Set(nameof(Avatar.ChildContent), "GH").Set(nameof(Avatar.Variant), Variant.Outlined).Set(nameof(Avatar.Color), Color.Secondary))
            .Variant("Large", v => v.Set(nameof(Avatar.Size), Size.Large))
            .Variant("Small", v => v.Set(nameof(Avatar.Size), Size.Small))
            .Variant("Rounded box", v => v.Set(nameof(Avatar.Rounded), true).Set(nameof(Avatar.Size), Size.Large))
            .Variant("Square", v => v.Set(nameof(Avatar.Square), true).Set(nameof(Avatar.Size), Size.Large));

        // A plain <img>; ObjectFit maps only "contain", "cover", "fill", "none" and "scale-down".
        options.For<Image>()
            .Parameter(nameof(Image.Src), SamplePhoto, "@_photo")
            .Parameter(nameof(Image.Alt), "Mountains at dusk")
            .Parameter(nameof(Image.Width), 192)
            .Related<Mask>()
            .Variant("Cover, wide", v => v.Set(nameof(Image.ObjectFit), "cover").Set(nameof(Image.Width), 288).Set(nameof(Image.Height), 120))
            .Variant("Contain, wide", v => v.Set(nameof(Image.ObjectFit), "contain").Set(nameof(Image.Width), 288).Set(nameof(Image.Height), 120))
            .Variant("Rounded", v => v.Set(nameof(Image.Class), "rounded-box"))
            .Variant("Fluid", v => v.Set(nameof(Image.Fluid), true).Set(nameof(Image.Width), 960));

        // mask-* are all safelisted at 0.6.0, the four triangle/decagon extras included.
        options.For<Mask>()
            .Parameter(nameof(Mask.Class), "w-24")
            .Slot(nameof(Mask.ChildContent), DaisyDemoFragments.MaskedPhoto, DaisyDemoFragmentSources.MaskedPhoto)
            .Related<Image>()
            .Variant("Circle", v => v.Set(nameof(Mask.Shape), MaskShape.Circle))
            .Variant("Hexagon", v => v.Set(nameof(Mask.Shape), MaskShape.Hexagon))
            .Variant("Heart", v => v.Set(nameof(Mask.Shape), MaskShape.Heart))
            .Variant("Star", v => v.Set(nameof(Mask.Shape), MaskShape.Star2))
            .Variant("Decagon", v => v.Set(nameof(Mask.Shape), MaskShape.Decagon))
            .Variant("Left half", v => v.Set(nameof(Mask.Half), 1));

        // daisyUI's .diff is width:100%, aspect-16/9 by default: w-96 keeps it bench-sized. Drag the
        // handle (or focus and use the keyboard) to reveal the greyscale pane.
        options.For<Diff>()
            .Parameter(nameof(Diff.Class), "w-96 max-w-full rounded-box")
            .Parameter(nameof(Diff.Item1Label), "Colour")
            .Parameter(nameof(Diff.Item2Label), "Greyscale")
            .Slot(nameof(Diff.Item1), DaisyDemoFragments.DiffBefore, DaisyDemoFragmentSources.DiffBefore)
            .Slot(nameof(Diff.Item2), DaisyDemoFragments.DiffAfter, DaisyDemoFragmentSources.DiffAfter)
            .Variant("Square", v => v.Set(nameof(Diff.AspectClass), "aspect-square").Set(nameof(Diff.Class), "w-64 rounded-box"))
            .Variant("4:3", v => v.Set(nameof(Diff.AspectClass), "aspect-4/3"))
            .Variant("Narrow", v => v.Set(nameof(Diff.Class), "w-64 rounded-box"));

        // --- Text and glyphs ---

        // Name does not end in "Icon", so the icon picker does not drive it: a preset and variants
        // that set other Icons.Material.Filled constants instead.
        options.For<Icon>()
            .Parameter(nameof(Icon.Name), Icons.Material.Filled.Favorite, "@Icons.Material.Filled.Favorite")
            .Related<IconButton>()
            .Variant("Home", v => v.Set(nameof(Icon.Name), Icons.Material.Filled.Home))
            .Variant("Rocket", v => v.Set(nameof(Icon.Name), Icons.Material.Filled.RocketLaunch).Set(nameof(Icon.Color), Color.Primary))
            .Variant("Error", v => v.Set(nameof(Icon.Color), Color.Error))
            .Variant("Large", v => v.Set(nameof(Icon.Size), Size.Large))
            .Variant("Small", v => v.Set(nameof(Icon.Size), Size.Small))
            .Variant("Outlined", v => v.Set(nameof(Icon.Outlined), true));

        // Typo picks both the element (h1-h6 or p) and its utility classes.
        options.For<Typography>()
            .Slot(nameof(Typography.ChildContent), b => b.AddContent(0, "The Analytical Engine weaves algebraic patterns."), "The Analytical Engine weaves algebraic patterns.")
            .Variant("Heading 1", v => v.Set(nameof(Typography.Typo), Typo.h1))
            .Variant("Heading 4", v => v.Set(nameof(Typography.Typo), Typo.h4))
            .Variant("Subtitle", v => v.Set(nameof(Typography.Typo), Typo.subtitle1))
            .Variant("Caption", v => v.Set(nameof(Typography.Typo), Typo.caption))
            .Variant("Overline", v => v.Set(nameof(Typography.Typo), Typo.overline))
            .Variant("Primary, centred", v => v.Set(nameof(Typography.Color), Color.Primary).Set(nameof(Typography.Align), "center"));

        // A horizontal rule with optional text; Vertical (divider-horizontal) draws a vertical line,
        // which needs a height, hence its Class. w-64 gives the horizontal one a length on the bench.
        options.For<Divider>()
            .Parameter(nameof(Divider.Class), "w-64")
            .Slot(nameof(Divider.ChildContent), b => b.AddContent(0, "OR"), "OR")
            .Variant("Vertical", v => v.Set(nameof(Divider.Vertical), true).Set(nameof(Divider.Class), "h-24"))
            .Variant("Other text", v => v.Set(nameof(Divider.ChildContent), "Continue with"))
            .Variant("Wide", v => v.Set(nameof(Divider.Class), "w-96"));

        // Elevation: 0 none, 1 shadow-xs, 2-3 shadow-sm, 4-6 shadow-md, 7-12 shadow-lg, 13-18
        // shadow-xl, 19+ shadow-2xl; each of those is in the safelist and in app.css.
        options.For<Paper>()
            .Parameter(nameof(Paper.Class), "p-6 w-64")
            .Slot(nameof(Paper.ChildContent), DaisyDemoFragments.PaperText, DaisyDemoFragmentSources.PaperText)
            .Related<Card>()
            .Variant("Flat", v => v.Set(nameof(Paper.Elevation), 0))
            .Variant("Elevation 3", v => v.Set(nameof(Paper.Elevation), 3))
            .Variant("Elevation 6", v => v.Set(nameof(Paper.Elevation), 6))
            .Variant("Elevation 12", v => v.Set(nameof(Paper.Elevation), 12))
            .Variant("Elevation 24", v => v.Set(nameof(Paper.Elevation), 24))
            .Variant("Outlined", v => v.Set(nameof(Paper.Outlined), true));

        // The bubble belongs in a full-width row: chat-end only moves it right when the row is wide.
        options.For<ChatBubble>()
            .Parameter(nameof(ChatBubble.Class), "w-full")
            .Parameter(nameof(ChatBubble.Header), "Ana · 18:02")
            .Parameter(nameof(ChatBubble.Footer), "Delivered")
            .Slot(nameof(ChatBubble.Image), DaisyDemoFragments.ChatAvatar, DaisyDemoFragmentSources.ChatAvatar)
            .Slot(nameof(ChatBubble.ChildContent), b => b.AddContent(0, "The hotfix is on staging, you can test it."), "The hotfix is on staging, you can test it.")
            .Related<Avatar>()
            .Variant("End", v => v.Set(nameof(ChatBubble.Placement), ChatPlacement.End).Set(nameof(ChatBubble.Footer), "Seen at 18:05"))
            .Variant("Primary", v => v.Set(nameof(ChatBubble.Color), Color.Primary))
            .Variant("End, secondary", v => v.Set(nameof(ChatBubble.Placement), ChatPlacement.End).Set(nameof(ChatBubble.Color), Color.Secondary))
            .Variant("Error", v => v.Set(nameof(ChatBubble.Color), Color.Error));
    }

    /// <summary>
    /// Layout, navigation, mockups and the app-level helpers. Verified against each component's
    /// .razor at tag v0.6.0 and the compiled app.css: the classes they compose (gap-*, stack-*,
    /// max-w-screen-*, dock-*, dropdown-*, tabs-*, link-*, bg-{color} text-{color}-content,
    /// border-{color}, col-span-*) come from the safelist; the bench-sizing classes a preset passes
    /// itself (w-96, min-h-64, ...) are literal here or in DaisyDemoFragments.razor, which is what
    /// makes Tailwind emit them. The helpers' own look (AppCard, Feature*) is scoped CSS that
    /// reaches the page through DaisyBlazor's bundle.scp.css. DaisyLayoutPresetTests.cs pins each one.
    /// The bench shrink-wraps its specimen, so the w-full components sit in InWideFrame.
    /// <para>
    /// Hrefs: <c>"#"</c> is the placeholder the task and DaisyBlazor's docs use, but it is resolved
    /// against <c>&lt;base href="/"&gt;</c>, so in the explorer (<c>/explorer?pb-...</c>) a click
    /// navigates to the landing page <c>/#</c>, not to a hash on the bench. No static href can
    /// stay on the bench (the query string carries the bench state), so links that would leave
    /// the app use <c>Target="_blank"</c> where the component has a Target, and components that
    /// call <c>NavigationManager.NavigateTo</c> themselves (FeatureNavCard.Href) leave it unset.
    /// </para>
    /// </summary>
    private static void LayoutAndNavigation(PlayBlazorOptions options)
    {
        // --- Layout ---

        // Spacing composes gap-{Spacing}. Upstream hole at 0.6.0: the safelist stops at gap-12, so
        // Spacing 13+ (Grid and Stack both take any int) silently renders NO gap. Every variant
        // here stays at 12 or below; the hole is listed in the class remarks for upstream.
        options.For<Grid>()
            .Parameter(nameof(Grid.Class), "w-96")
            .Slot(nameof(Grid.ChildContent), DaisyDemoFragments.GridPapers, DaisyDemoFragmentSources.GridPapers)
            .Related<Item>()
            .Variant("No gap", v => v.Set(nameof(Grid.Spacing), 0))
            .Variant("Tight", v => v.Set(nameof(Grid.Spacing), 2))
            .Variant("Widest safelisted gap", v => v.Set(nameof(Grid.Spacing), 12));

        // Same gap-{n} hole as Grid: keep Spacing at 12 or below.
        options.For<Stack>()
            .Slot(nameof(Stack.ChildContent), DaisyDemoFragments.StackPapers, DaisyDemoFragmentSources.StackPapers)
            .Related<Spacer>()
            .Variant("Row", v => v.Set(nameof(Stack.Row), true))
            .Variant("Centered", v => v.Set(nameof(Stack.AlignItems), AlignItems.Center))
            .Variant("No gap", v => v.Set(nameof(Stack.Spacing), 0))
            .Variant("Widest safelisted gap", v => v.Set(nameof(Stack.Spacing), 12))
            .Variant("Row, wrapping", v => v.Set(nameof(Stack.Row), true).Set(nameof(Stack.Wrap), true).Set(nameof(Stack.Class), "w-64"));

        // daisyUI's .stack (overlapping cards), not the flex Stack above.
        options.For<StackOverlay>()
            .Slot(nameof(StackOverlay.ChildContent), DaisyDemoFragments.OverlayCards, DaisyDemoFragmentSources.OverlayCards)
            .Related<Stack>()
            .Variant("Top", v => v.Set(nameof(StackOverlay.Align), StackAlign.Top))
            .Variant("Bottom", v => v.Set(nameof(StackOverlay.Align), StackAlign.Bottom))
            .Variant("Start", v => v.Set(nameof(StackOverlay.Align), StackAlign.Start))
            .Variant("End", v => v.Set(nameof(StackOverlay.Align), StackAlign.End));

        // mx-auto w-full max-w-screen-{size}: in the 64rem frame, Large (the default) fills it and
        // Small (640px) / Medium (768px) visibly narrow it. ExtraLarge and False would look like
        // Large inside the frame, so they are left to the control.
        options.For<Container>()
            .Scaffold(DaisyDemoFragments.InWideFrame, DaisyDemoFragmentSources.InWideFrame)
            .Slot(nameof(Container.ChildContent), DaisyDemoFragments.ContainerContent, DaisyDemoFragmentSources.ContainerContent)
            .Related<Stack>()
            .Variant("Small", v => v.Set(nameof(Container.MaxWidth), MaxWidth.Small))
            .Variant("Medium", v => v.Set(nameof(Container.MaxWidth), MaxWidth.Medium))
            .Variant("No gutters", v => v.Set(nameof(Container.Gutters), false));

        // An empty flex-1 div: only visible through the siblings it pushes apart, hence the row.
        options.For<Spacer>()
            .Scaffold(DaisyDemoFragments.InStackRow, DaisyDemoFragmentSources.InStackRow)
            .Related<Stack>()
            .Variant("Dotted leader", v => v.Set(nameof(Spacer.Class), "mx-2 border-b-2 border-dotted border-base-content/30"))
            .Variant("Highlighted", v => v.Set(nameof(Spacer.Class), "mx-2 h-6 rounded bg-primary/20"));

        // MinHeight defaults to min-h-screen, a whole viewport per specimen: the preset trims it.
        options.For<Hero>()
            .Scaffold(DaisyDemoFragments.InWideFrame, DaisyDemoFragmentSources.InWideFrame)
            .Parameter(nameof(Hero.MinHeight), "min-h-64")
            .Parameter(nameof(Hero.Class), "rounded-box bg-base-200")
            .Slot(nameof(Hero.ChildContent), DaisyDemoFragments.HeroContent, DaisyDemoFragmentSources.HeroContent)
            .Related<Button>()
            .Variant("Background image", v => v.Set(nameof(Hero.BackgroundImage), SamplePhoto).Set(nameof(Hero.ContentClass), "text-neutral-content"))
            .Variant("Primary", v => v.Set(nameof(Hero.Class), "rounded-box bg-primary text-primary-content"))
            .Variant("Taller", v => v.Set(nameof(Hero.MinHeight), "min-h-96"));

        // Color paints bg-{color} text-{color}-content, so it is a variant rather than a Class
        // background that would fight it. Horizontal=false (footer-vertical) looks like the
        // default stacked flow, so only Horizontal=true is offered.
        options.For<Footer>()
            .Scaffold(DaisyDemoFragments.InWideFrame, DaisyDemoFragmentSources.InWideFrame)
            .Parameter(nameof(Footer.Class), "rounded-box")
            .Slot(nameof(Footer.ChildContent), DaisyDemoFragments.FooterColumns, DaisyDemoFragmentSources.FooterColumns)
            .Related<FooterTitle>()
            .Variant("Neutral", v => v.Set(nameof(Footer.Color), Color.Dark))
            .Variant("Primary", v => v.Set(nameof(Footer.Color), Color.Primary))
            .Variant("Side by side", v => v.Set(nameof(Footer.Horizontal), true))
            .Variant("Centered", v => v.Set(nameof(Footer.Center), true))
            .Variant("Compact", v => v.Set(nameof(Footer.Padding), "p-4"));

        // --- Navigation ---

        // Each region renders only when its fragment is set; Color pairs bg-{color} with its content
        // colour (Default is bg-base-100).
        options.For<Navbar>()
            .Scaffold(DaisyDemoFragments.InWideFrame, DaisyDemoFragmentSources.InWideFrame)
            .Parameter(nameof(Navbar.Class), "rounded-box")
            .Slot(nameof(Navbar.Start), DaisyDemoFragments.NavbarBrand, DaisyDemoFragmentSources.NavbarBrand)
            .Slot(nameof(Navbar.Center), DaisyDemoFragments.NavbarLinks, DaisyDemoFragmentSources.NavbarLinks)
            .Slot(nameof(Navbar.End), DaisyDemoFragments.NavbarActions, DaisyDemoFragmentSources.NavbarActions)
            .Related<Menu>()
            .Variant("Primary", v => v.Set(nameof(Navbar.Color), Color.Primary))
            .Variant("Neutral", v => v.Set(nameof(Navbar.Color), Color.Dark))
            .Variant("Bordered", v => v.Set(nameof(Navbar.Bordered), true))
            .Variant("Shadow", v => v.Set(nameof(Navbar.Shadow), true));

        // Items (List<BreadcrumbItem>) has no control, so the trail is a fixed preset and no variant
        // can swap it. A null Href is the current page (plain text, aria-current="page").
        options.For<Breadcrumbs>()
            .Parameter(
                nameof(Breadcrumbs.Items),
                new List<BreadcrumbItem>
                {
                    new("Home", "#", Icon: Icons.Material.Filled.Home),
                    new("Projects", "#", Icon: Icons.Material.Filled.Folder),
                    new("Checkout redesign", null),
                },
                "@(new List<BreadcrumbItem> { new(\"Home\", \"#\", Icon: Icons.Material.Filled.Home), new(\"Projects\", \"#\", Icon: Icons.Material.Filled.Folder), new(\"Checkout redesign\", null) })")
            .Related<Navbar>()
            .Variant("Bold", v => v.Set(nameof(Breadcrumbs.Class), "font-semibold"))
            .Variant("Muted", v => v.Set(nameof(Breadcrumbs.Class), "opacity-70"))
            .Variant("Scrolls when narrow", v => v.Set(nameof(Breadcrumbs.Class), "max-w-40"));

        // Fixed=false is mandatory: daisyUI's .dock is position:fixed at the viewport bottom, over
        // the explorer. The frame's transform contains it even if the control turns Fixed back on.
        options.For<Dock>()
            .Scaffold(DaisyDemoFragments.InDockFrame, DaisyDemoFragmentSources.InDockFrame)
            .Parameter(nameof(Dock.Fixed), false)
            .Slot(nameof(Dock.ChildContent), DaisyDemoFragments.DockItems, DaisyDemoFragmentSources.DockItems)
            .Related<DockItem>()
            .Variant("Extra small", v => v.Set(nameof(Dock.Size), DockSize.Xs))
            .Variant("Small", v => v.Set(nameof(Dock.Size), DockSize.Sm))
            .Variant("Large", v => v.Set(nameof(Dock.Size), DockSize.Lg))
            .Variant("Extra large", v => v.Set(nameof(Dock.Size), DockSize.Xl));

        // CSS-only <details>: click the trigger to open it. The trigger is styled through
        // ActivatorClass, as Dropdown's own docs say: a <Button> inside the <summary> swallows the
        // click and the dropdown never opens. The placement variants also force it Open so the
        // list shows where it lands.
        options.For<Dropdown>()
            .Scaffold(DaisyDemoFragments.InPopupFrame, DaisyDemoFragmentSources.InPopupFrame)
            .Parameter(nameof(Dropdown.ActivatorClass), "btn btn-outline")
            .Slot(nameof(Dropdown.Activator), b => b.AddContent(0, "Options"), "Options")
            .Slot(nameof(Dropdown.ChildContent), DaisyDemoFragments.DropdownItems, DaisyDemoFragmentSources.DropdownItems)
            .Related<Menu>()
            .Variant("Open", v => v.Set(nameof(Dropdown.Open), true))
            .Variant("Opens on hover", v => v.Set(nameof(Dropdown.Hover), true))
            .Variant("Opens upward", v => v.Set(nameof(Dropdown.Placement), DropdownPlacement.Top).Set(nameof(Dropdown.Open), true))
            .Variant("Aligned to the end", v => v.Set(nameof(Dropdown.Placement), DropdownPlacement.End).Set(nameof(Dropdown.Open), true))
            .Variant("Opens to the right", v => v.Set(nameof(Dropdown.Placement), DropdownPlacement.Right).Set(nameof(Dropdown.Open), true));

        // Menu renders its items only while open, and injects IJSRuntime for menu.js (focus moves
        // only; bench-safe). Open=true shows the list straight away; an open Menu lays a
        // `fixed inset-0` backdrop, which InPopupFrame's transform keeps to the frame (see InMenu).
        // A click outside closes it, and it stays closed until the bench resets: Open only
        // overrides the inner state when the parameter itself changes (#108 upstream).
        // ActivatorContent is a RenderFragment<MenuActivator>, which the bench cannot fill.
        options.For<Menu>()
            .Scaffold(DaisyDemoFragments.InPopupFrame, DaisyDemoFragmentSources.InPopupFrame)
            .Parameter(nameof(Menu.Label), "Actions")
            .Parameter(nameof(Menu.Open), true)
            .Slot(nameof(Menu.ChildContent), DaisyDemoFragments.MenuItems, DaisyDemoFragmentSources.MenuItems)
            .Related<MenuItem>()
            .Variant("Icon activator", v => v.Set(nameof(Menu.Icon), Icons.Material.Filled.MoreVert))
            .Variant("Dense", v => v.Set(nameof(Menu.Dense), true))
            .Variant("Large activator", v => v.Set(nameof(Menu.Size), Size.Large))
            .Variant("Horizontal list", v => v.Set(nameof(Menu.ListClass), "menu-horizontal"))
            .Variant("Aligned to the end", v => v.Set(nameof(Menu.AnchorOrigin), Origin.BottomRight));

        // A menu <li> around the framework NavLink, which adds menu-active when the URL matches.
        // "Current page" uses Href="explorer", which Prefix-matches the explorer's own URL, so the
        // highlight shows on the bench. A click on either navigates (see the summary on "#").
        options.For<NavMenuLink>()
            .Scaffold(DaisyDemoFragments.InMenuList, DaisyDemoFragmentSources.InMenuList)
            .Parameter(nameof(NavMenuLink.Href), "#")
            .Parameter(nameof(NavMenuLink.Icon), Icons.Material.Filled.Home)
            .Slot(nameof(NavMenuLink.ChildContent), b => b.AddContent(0, "Dashboard"), "Dashboard")
            .Related<Menu>()
            .Variant("Current page", v => v.Set(nameof(NavMenuLink.Href), "explorer"))
            .Variant("Disabled", v => v.Set(nameof(NavMenuLink.Disabled), true))
            .Variant("Other icon", v => v.Set(nameof(NavMenuLink.Icon), Icons.Material.Filled.Inbox));

        // Selected is 1-based; page 5 of 10 shows both ellipses. SelectedChanged is wired to the
        // event log on the bench, so a click moves the selection.
        options.For<Pagination>()
            .Parameter(nameof(Pagination.Count), 10)
            .Parameter(nameof(Pagination.Selected), 5)
            .Related<TablePager>()
            .Variant("Primary", v => v.Set(nameof(Pagination.Color), Color.Primary))
            .Variant("First and last buttons", v => v.Set(nameof(Pagination.ShowFirstButton), true).Set(nameof(Pagination.ShowLastButton), true))
            .Variant("Wider window", v => v.Set(nameof(Pagination.MiddleCount), 5))
            .Variant("Few pages", v => v.Set(nameof(Pagination.Count), 3).Set(nameof(Pagination.Selected), 2))
            .Variant("Disabled", v => v.Set(nameof(Pagination.Disabled), true));

        // Steps has no index: the current step is the Active one, the ones before it Completed
        // (step-primary, which colours the connector up to the current step).
        options.For<Steps>()
            .Slot(nameof(Steps.ChildContent), DaisyDemoFragments.StepItems, DaisyDemoFragmentSources.StepItems)
            .Related<Step>()
            .Variant("Vertical", v => v.Set(nameof(Steps.Vertical), true))
            .Variant("Wide", v => v.Set(nameof(Steps.Class), "w-[40rem]"));

        // Tabs injects IJSRuntime only on first focus (tabs.js stops arrow keys scrolling the page).
        // Each Tab registers with it; the panel takes the variant's surface (tab-content block ...).
        options.For<Tabs>()
            .Parameter(nameof(Tabs.Class), "w-96")
            .Slot(nameof(Tabs.ChildContent), DaisyDemoFragments.TabItems, DaisyDemoFragmentSources.TabItems)
            .Related<Tab>()
            .Variant("Border", v => v.Set(nameof(Tabs.Variant), TabsVariant.Border))
            .Variant("Lift", v => v.Set(nameof(Tabs.Variant), TabsVariant.Lift))
            .Variant("Small", v => v.Set(nameof(Tabs.Size), Size.Small))
            .Variant("Large", v => v.Set(nameof(Tabs.Size), Size.Large))
            .Variant("Second tab active", v => v.Set(nameof(Tabs.ActiveIndex), 1));

        // Target="_blank" keeps the bench: the link opens in a new tab instead of leaving the app.
        options.For<TextLink>()
            .Parameter(nameof(TextLink.Href), "https://daisyui.com")
            .Parameter(nameof(TextLink.Target), "_blank")
            .Slot(nameof(TextLink.ChildContent), b => b.AddContent(0, "Read the daisyUI docs"), "Read the daisyUI docs")
            .Related<Button>()
            .Variant("Secondary", v => v.Set(nameof(TextLink.Color), Color.Secondary))
            .Variant("Accent", v => v.Set(nameof(TextLink.Color), Color.Tertiary))
            .Variant("Always underlined", v => v.Set(nameof(TextLink.Underline), Underline.Always))
            .Variant("No underline", v => v.Set(nameof(TextLink.Underline), Underline.None))
            .Variant("Heading", v => v.Set(nameof(TextLink.Typo), Typo.h5));

        // --- Mockups ---

        options.For<MockupBrowser>()
            .Parameter(nameof(MockupBrowser.Url), "acme.cloud")
            .Parameter(nameof(MockupBrowser.Class), "w-96")
            .Slot(nameof(MockupBrowser.ChildContent), DaisyDemoFragments.BrowserPage, DaisyDemoFragmentSources.BrowserPage)
            .Related<MockupWindow>()
            .Variant("Primary border", v => v.Set(nameof(MockupBrowser.BorderColor), Color.Primary))
            .Variant("Tinted", v => v.Set(nameof(MockupBrowser.Class), "w-96 bg-base-200"))
            .Variant("Other page", v => v.Set(nameof(MockupBrowser.Url), "acme.cloud/pricing"));

        // Lines (IReadOnlyList<string>) has no control: a fixed preset, one <pre data-prefix> each.
        options.For<MockupCode>()
            .Parameter(nameof(MockupCode.Lines), new[] { "npm i daisyui", "installing...", "Done!" }, "@(new[] { \"npm i daisyui\", \"installing...\", \"Done!\" })")
            .Related<MockupWindow>()
            .Variant("Chevron prompt", v => v.Set(nameof(MockupCode.Prefix), ">"))
            .Variant("No prefix", v => v.Set(nameof(MockupCode.Prefix), string.Empty))
            .Variant("Primary", v => v.Set(nameof(MockupCode.Class), "bg-primary text-primary-content"));

        // .mockup-phone is width:100% (max 462px) at a 462/978 aspect ratio: on the shrink-wrapped
        // bench it needs a width, and w-48 keeps it under 400px tall.
        options.For<MockupPhone>()
            .Parameter(nameof(MockupPhone.Class), "w-48")
            .Slot(nameof(MockupPhone.ChildContent), DaisyDemoFragments.PhoneScreen, DaisyDemoFragmentSources.PhoneScreen)
            .Related<MockupBrowser>()
            .Variant("Small", v => v.Set(nameof(MockupPhone.Class), "w-36"))
            .Variant("Large", v => v.Set(nameof(MockupPhone.Class), "w-64"));

        options.For<MockupWindow>()
            .Parameter(nameof(MockupWindow.Title), "Deployments")
            .Parameter(nameof(MockupWindow.Class), "w-96")
            .Slot(nameof(MockupWindow.ChildContent), DaisyDemoFragments.WindowContent, DaisyDemoFragmentSources.WindowContent)
            .Related<MockupBrowser>()
            .Variant("Primary border", v => v.Set(nameof(MockupWindow.BorderColor), Color.Primary))
            .Variant("Tinted", v => v.Set(nameof(MockupWindow.Class), "w-96 bg-base-200"))
            .Variant("Untitled", v => v.Set(nameof(MockupWindow.Title), string.Empty));

        // --- App-level helpers ---
        // Every [EditorRequired] member is preset, so neither the bench nor the generated snippet
        // trips RZ2012.

        options.For<PageHeader>()
            .Scaffold(DaisyDemoFragments.InWideFrame, DaisyDemoFragmentSources.InWideFrame)
            .Parameter(nameof(PageHeader.Title), "Invoices")
            .Parameter(nameof(PageHeader.Subtitle), "42 open, 3 overdue")
            .Slot(nameof(PageHeader.Actions), DaisyDemoFragments.NewInvoiceButton, DaisyDemoFragmentSources.NewInvoiceButton)
            .Related<SectionCard>()
            .Variant("With icon", v => v.Set(nameof(PageHeader.Icon), Icons.Material.Filled.ReceiptLong))
            .Variant("Level 2 heading", v => v.Set(nameof(PageHeader.HeadingLevel), 2))
            .Variant("Full-contrast subtitle", v => v.Set(nameof(PageHeader.SubtitleColor), Color.Dark));

        options.For<SectionCard>()
            .Parameter(nameof(SectionCard.Title), "Deployment")
            .Parameter(nameof(SectionCard.Icon), Icons.Material.Filled.RocketLaunch)
            .Parameter(nameof(SectionCard.Class), "w-96")
            .Slot(nameof(SectionCard.ChildContent), DaisyDemoFragments.SectionBody, DaisyDemoFragmentSources.SectionBody)
            .Related<DetailCard>()
            .Variant("No divider", v => v.Set(nameof(SectionCard.ShowDivider), false))
            .Variant("Flat", v => v.Set(nameof(SectionCard.Elevation), 0))
            .Variant("Larger title", v => v.Set(nameof(SectionCard.TitleTypo), Typo.h5))
            .Variant("Header actions", v => v.Set(nameof(SectionCard.HeaderActions), "⋯"));

        // A SectionCard whose body is a dense, hoverable table of DetailRows.
        options.For<DetailCard>()
            .Parameter(nameof(DetailCard.Title), "Northwind Traders")
            .Parameter(nameof(DetailCard.Icon), Icons.Material.Filled.Business)
            .Parameter(nameof(DetailCard.Class), "w-96")
            .Slot(nameof(DetailCard.ChildContent), DaisyDemoFragments.DetailRows, DaisyDemoFragmentSources.DetailRows)
            .Related<DetailRow>()
            .Variant("Roomy", v => v.Set(nameof(DetailCard.Dense), false))
            .Variant("Flat", v => v.Set(nameof(DetailCard.Elevation), 0))
            .Variant("No icon", v => v.Set(nameof(DetailCard.Icon), string.Empty));

        options.For<DetailRow>()
            .Scaffold(DaisyDemoFragments.InDetailCard, DaisyDemoFragmentSources.InDetailCard)
            .Related<DetailCard>()
            .Parameter(nameof(DetailRow.Label), "Owner")
            .Parameter(nameof(DetailRow.Value), "Ana Silva")
            .Variant("Long value", v => v.Set(nameof(DetailRow.Value), "ana.silva@northwind-traders.example.com"))
            .Variant("Custom value content", v => v.Set(nameof(DetailRow.ValueContent), "Enterprise plan"))
            .Variant("Highlighted", v => v.Set(nameof(DetailRow.Class), "bg-base-200"));

        // BackRoute="#" (see the summary: a click leaves the explorer for the landing page).
        options.For<DetailLayout>()
            .Scaffold(DaisyDemoFragments.InWideFrame, DaisyDemoFragmentSources.InWideFrame)
            .Parameter(nameof(DetailLayout.Title), "Northwind Traders")
            .Parameter(nameof(DetailLayout.BackRoute), "#")
            .Slot(nameof(DetailLayout.MainContent), DaisyDemoFragments.DetailMain, DaisyDemoFragmentSources.DetailMain)
            .Related<DetailCard>()
            .Variant("Subtitle", v => v.Set(nameof(DetailLayout.Subtitle), "Customer since March 2021"))
            .Variant("Custom back label", v => v.Set(nameof(DetailLayout.BackLabel), "All customers"))
            .Variant("With sidebar", v => v.Set(nameof(DetailLayout.Sidebar), "Account manager: Ana Silva"))
            .Variant("Status", v => v.Set(nameof(DetailLayout.StatusSlot), "Active"));

        // Upstream at 0.6.0: BadgeText also DISABLES the button (IsDisabled => Disabled || Loading
        // || BadgeText is not null), so the badge variant shows a greyed-out shortcut. Left visible.
        options.For<QuickAccessButton>()
            .Scaffold(DaisyDemoFragments.InNarrowFrame, DaisyDemoFragmentSources.InNarrowFrame)
            .Parameter(nameof(QuickAccessButton.Label), "New invoice")
            .Parameter(nameof(QuickAccessButton.Icon), Icons.Material.Filled.Add)
            .Related<Button>()
            .Variant("Primary", v => v.Set(nameof(QuickAccessButton.Color), Color.Primary))
            .Variant("Loading", v => v.Set(nameof(QuickAccessButton.Loading), true))
            .Variant("Disabled", v => v.Set(nameof(QuickAccessButton.Disabled), true))
            .Variant("With badge", v => v.Set(nameof(QuickAccessButton.BadgeText), "3").Set(nameof(QuickAccessButton.TooltipText), "3 drafts waiting"));

        // A null gradient falls back to the theme's primary/secondary (scoped CSS).
        options.For<FeatureHero>()
            .Scaffold(DaisyDemoFragments.InWideFrame, DaisyDemoFragmentSources.InWideFrame)
            .Parameter(nameof(FeatureHero.Icon), Icons.Material.Filled.Payments)
            .Parameter(nameof(FeatureHero.Title), "Billing")
            .Parameter(nameof(FeatureHero.Eyebrow), "Finance")
            .Parameter(nameof(FeatureHero.Subtitle), "Invoices, payments and reports in one place.")
            .Slot(nameof(FeatureHero.Actions), DaisyDemoFragments.NewInvoiceButton, DaisyDemoFragmentSources.NewInvoiceButton)
            .Related<FeatureHomePage>()
            .Variant("Dense", v => v.Set(nameof(FeatureHero.Dense), true))
            .Variant("Ocean gradient", v => v.Set(nameof(FeatureHero.GradientStart), "#0ea5e9").Set(nameof(FeatureHero.GradientEnd), "#6366f1"))
            .Variant("Sunset gradient", v => v.Set(nameof(FeatureHero.GradientStart), "#f97316").Set(nameof(FeatureHero.GradientEnd), "#db2777"));

        // A whole feature page, kept compact: a title and two nav cards. RequireAccessCheck
        // defaults to false (verified at v0.6.0) and is deliberately never a variant: true wraps the
        // page in <AuthorizeView>, which needs a cascading AuthenticationState this demo does not
        // provide, so the specimen would throw.
        options.For<FeatureHomePage>()
            .Scaffold(DaisyDemoFragments.InWideFrame, DaisyDemoFragmentSources.InWideFrame)
            .Parameter(nameof(FeatureHomePage.Title), "Billing")
            .Parameter(nameof(FeatureHomePage.Subtitle), "Invoices, payments and reports.")
            .Slot(nameof(FeatureHomePage.NavCards), DaisyDemoFragments.HomeNavCards, DaisyDemoFragmentSources.HomeNavCards)
            .Related<FeatureNavCard>()
            .Variant("Hero banner", v => v.Set(nameof(FeatureHomePage.Icon), Icons.Material.Filled.Payments))
            .Variant("Hero, sunset gradient", v => v.Set(nameof(FeatureHomePage.Icon), Icons.Material.Filled.Payments).Set(nameof(FeatureHomePage.GradientStart), "#f97316").Set(nameof(FeatureHomePage.GradientEnd), "#db2777"))
            .Variant("Section title", v => v.Set(nameof(FeatureHomePage.NavCardsTitle), "Areas"))
            .Variant("With error", v => v.Set(nameof(FeatureHomePage.Error), "Could not load the latest totals."));

        // Href is left unset: the card calls NavigationManager.NavigateTo itself, which would take
        // the explorer away (and "#" resolves to the landing page). Without it the "go" arrow is not
        // rendered. Upstream at 0.6.0: the inner Card always gets an OnClick delegate, so the card is
        // role="button" tabindex="0" even with no Href, a focusable "button" that does nothing.
        options.For<FeatureNavCard>()
            .Scaffold(DaisyDemoFragments.InNarrowFrame, DaisyDemoFragmentSources.InNarrowFrame)
            .Parameter(nameof(FeatureNavCard.Icon), Icons.Material.Filled.ReceiptLong)
            .Parameter(nameof(FeatureNavCard.Title), "Invoices")
            .Parameter(nameof(FeatureNavCard.Subtitle), "Billing")
            .Parameter(nameof(FeatureNavCard.Description), "Create, send and track invoices.")
            .Parameter(nameof(FeatureNavCard.AvatarColor), Color.Primary)
            .Related<FeatureHomePage>()
            .Variant("Secondary avatar", v => v.Set(nameof(FeatureNavCard.AvatarColor), Color.Secondary))
            .Variant("Gradient avatar", v => v.Set(nameof(FeatureNavCard.GradientStart), "#0ea5e9").Set(nameof(FeatureNavCard.GradientEnd), "#6366f1"))
            .Variant("Extra content", v => v.Set(nameof(FeatureNavCard.ChildContent), "3 overdue"));

        // A 220px gradient tile. OnCardClick is wired to the bench's event log, so a click logs
        // instead of navigating to Route (default "/").
        options.For<AppCard>()
            .Parameter(nameof(AppCard.Name), "Invoices")
            .Parameter(nameof(AppCard.Icon), Icons.Material.Filled.ReceiptLong)
            .Parameter(nameof(AppCard.GradientStart), "#0ea5e9")
            .Parameter(nameof(AppCard.GradientEnd), "#6366f1")
            .Related<FeatureNavCard>()
            .Variant("With badge", v => v.Set(nameof(AppCard.Badge), "3"))
            .Variant("Sunset gradient", v => v.Set(nameof(AppCard.GradientStart), "#f97316").Set(nameof(AppCard.GradientEnd), "#db2777"))
            .Variant("Long name", v => v.Set(nameof(AppCard.Name), "Purchase orders and approvals"));
    }

    private static Task<IEnumerable<string>> SearchFruits(string text, CancellationToken cancellationToken)
        => Task.FromResult(ListOptions.Where(f => f.Contains(text, StringComparison.OrdinalIgnoreCase)));

    private static string StripArity(string name)
    {
        var backtick = name.IndexOf('`');
        return backtick < 0 ? name : name[..backtick];
    }
}
