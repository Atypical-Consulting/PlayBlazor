using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components.Dialog.MessageBox;
using Microsoft.FluentUI.AspNetCore.Components.Utilities;

namespace PlayBlazor.Demo.FluentUI;

/// <summary>
/// Curation for the Fluent UI showcase. A seed only: the curated surface, presets, scaffolds
/// and variants land in milestone 3, driven by the [Explicit] sweep's inventory.
/// </summary>
public static class FluentPlaygroundConfig
{
    /// <summary>
    /// Not components a user plays with: providers, internal helpers, settings objects — plus
    /// <c>FluentOptionString</c>, a convenience subclass of the already-listed
    /// <c>FluentOption&lt;T&gt;</c> that would otherwise show the same widget twice.
    /// </summary>
    private static readonly HashSet<string> Infrastructure =
    [
        "ColumnReorderOptions", "ColumnResizeOptions", "Defer", "FluentDialogProvider",
        "FluentKeyCodeProvider", "FluentMessageBarProvider", "FluentOptionString", "FluentProviders",
        "FluentToastProvider", "FluentTooltipProvider", "FreeOptionOutput",
    ];

    /// <summary>Sample rows for the data-grid scaffolds, so a grid has something to show.</summary>
    private static readonly List<Person> SampleRows =
    [
        new("Ada Lovelace", "Analyst", 36),
        new("Grace Hopper", "Rear Admiral", 45),
        new("Alan Turing", "Cryptanalyst", 41),
    ];

    /// <summary>
    /// A live <see cref="PaginationState"/> for the paginator preset: page 1 of a
    /// <see cref="SampleRows"/>-sized set, two items per page.
    /// </summary>
    /// <remarks>
    /// <see cref="PaginationState.TotalItemCount"/> starts null and only
    /// <see cref="PaginationState.SetTotalItemCountAsync"/> sets it — until it does,
    /// <see cref="FluentPaginator"/> renders a bare, empty &lt;div&gt; with no summary and no
    /// buttons. Calling it once here, synchronously, is what gives the preset below real
    /// pagination chrome to show, rather than an empty box that merely fails to throw.
    /// </remarks>
    private static readonly PaginationState SamplePaginationState = CreateSamplePaginationState();

    private static PaginationState CreateSamplePaginationState()
    {
        var state = new PaginationState { ItemsPerPage = 2 };
        state.SetTotalItemCountAsync(SampleRows.Count).GetAwaiter().GetResult();
        return state;
    }

    /// <summary>
    /// Shared items for every list-driven form input closed at <c>&lt;string,string&gt;</c> or
    /// <c>&lt;string&gt;</c> — <see cref="FluentSelect{TOption,TValue}"/>,
    /// <see cref="FluentCombobox{TOption,TValue}"/>, <see cref="FluentAutocomplete{TOption,TValue}"/>
    /// and <see cref="FluentListbox{TOption,TValue}"/> all take an <c>IEnumerable&lt;string&gt;</c>
    /// Items — one literal, not one per component.
    /// </summary>
    private static readonly string[] ListOptions = ["Red", "Green", "Blue", "Yellow", "Purple"];

    /// <summary>
    /// The model behind the <see cref="EditForm"/> nested inside the
    /// <see cref="FluentWizardStepValidator"/> scaffold — its content is irrelevant, only the
    /// <see cref="EditContext"/> it cascades to the validator matters.
    /// </summary>
    private static readonly object WizardStepValidatorModel = new();

    /// <summary>
    /// The model behind the <see cref="EditForm"/> scaffold that gives the validation-display
    /// components (<see cref="FluentValidationSummary"/>, <see cref="FluentValidationMessage{TValue}"/>)
    /// the cascading <see cref="EditContext"/> they demand by name — distinct from
    /// <see cref="WizardStepValidatorModel"/>, which backs a different scaffold entirely.
    /// </summary>
    private static readonly Person ValidationScaffoldModel = new("Ada Lovelace", "Analyst", 36);

    /// <summary>Applies the Fluent UI curation to the playground options.</summary>
    /// <param name="options">The options to configure.</param>
    public static void Configure(PlayBlazorOptions options)
    {
        // Fluent exposes 108 public components, of which ~97 are things a user actually plays with.
        // MudBlazor's config uses an allow-list because most of ITS surface is internal; here the
        // reverse holds, so name what to drop. These are stable categories, not a list that rots:
        // service providers, internal render helpers, settings objects that are ComponentBase by
        // inheritance rather than by intent, and FluentOptionString (a duplicate widget, not
        // infrastructure by category — see its own comment above). A component that is documented,
        // parameterised and written by users in their own markup stays listed even if it needs a
        // later preset to demo well — that's a presets question, not a listing one.
        options.ComponentFilter = type => !Infrastructure.Contains(StripArity(type.Name));

        // Discovery closes an open generic with string, then int. All four of these reject string
        // at construction, naming the types they accept — so declare the closing worth playing.
        options.For<FluentCalendar<DateTime?>>()
            .Parameter(nameof(FluentCalendar<DateTime?>.Label), "Choose a date")
            .Variant("Multiple selection", v => v.Set(nameof(FluentCalendar<DateTime?>.SelectMode), CalendarSelectMode.Multiple))
            .Variant("Range selection", v => v.Set(nameof(FluentCalendar<DateTime?>.SelectMode), CalendarSelectMode.Range))
            .Variant("Years view", v => v.Set(nameof(FluentCalendar<DateTime?>.View), CalendarViews.Years));

        options.For<FluentDatePicker<DateTime?>>()
            .Parameter(nameof(FluentDatePicker<DateTime?>.Label), "Production date")
            .Variant("Native", v => v.Set(nameof(FluentDatePicker<DateTime?>.RenderStyle), DatePickerRenderStyle.Native))
            .Variant("Years view", v => v.Set(nameof(FluentDatePicker<DateTime?>.View), CalendarViews.Years));

        options.For<FluentTimePicker<DateTime?>>()
            .Parameter(nameof(FluentTimePicker<DateTime?>.Label), "Pick a time")
            .Parameter(nameof(FluentTimePicker<DateTime?>.Increment), 15)
            .Variant("Office hours", v => v.Set(nameof(FluentTimePicker<DateTime?>.StartHour), 8).Set(nameof(FluentTimePicker<DateTime?>.EndHour), 20))
            .Variant("Native", v => v.Set(nameof(FluentTimePicker<DateTime?>.RenderStyle), DatePickerRenderStyle.Native));

        options.For<FluentNumberInput<int>>()
            .Parameter(nameof(FluentNumberInput<int>.Label), "Quantity")
            .Parameter(nameof(FluentNumberInput<int>.Value), 42)
            .Parameter(nameof(FluentNumberInput<int>.Min), 0)
            .Parameter(nameof(FluentNumberInput<int>.Max), 100)
            .Variant("Step 5", v => v.Set(nameof(FluentNumberInput<int>.Step), 5))
            .Variant("Filled darker", v => v.Set(nameof(FluentNumberInput<int>.Appearance), TextInputAppearance.FilledDarker))
            .Variant("Large", v => v.Set(nameof(FluentNumberInput<int>.Size), TextInputSize.Large));

        // Fluent UI icons are Icon OBJECTS, not markup strings: without a catalogue the
        // parameter resolves to ControlKind.Unsupported and gets no control at all.
        options.Catalogue(FluentIconCatalogue.All, static icon => builder
            => builder.AddContent(0, icon.ToMarkup()));

        // --- Scaffolds: components that cannot live outside their required parent. ---
        // The render sweep (sweeps/2026-render.txt) found eleven such components; five escaped
        // the error boundary. Each scaffold wraps the played specimen in the parent the library
        // demands, verified against the pinned 5.0.0-rc.5 assembly rather than assumed.

        options.For<FluentAppBarItem>()
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<FluentAppBar>(0);
                builder.AddAttribute(1, nameof(FluentAppBar.ChildContent), specimen);
                builder.CloseComponent();
            },
            "<FluentAppBar>\n    {specimen}\n</FluentAppBar>")
            .Related<FluentAppBar>()
            .Parameter(nameof(FluentAppBarItem.Text), "Chat")
            .Parameter(nameof(FluentAppBarItem.IconRest), FluentIconCatalogue.All["Circle"])
            .Variant("With badge count", v => v.Set(nameof(FluentAppBarItem.Count), 5))
            .Variant("With tooltip", v => v.Set(nameof(FluentAppBarItem.Tooltip), "Chat"))
            .Variant("Custom icon", v => v.Set(nameof(FluentAppBarItem.IconRest), FluentIconCatalogue.All["Star"]));

        options.For<FluentNavItem>()
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<FluentNav>(0);
                builder.AddAttribute(1, nameof(FluentNav.ChildContent), specimen);
                builder.CloseComponent();
            },
            "<FluentNav>\n    {specimen}\n</FluentNav>")
            .Related<FluentNav>()
            .Slot(nameof(FluentNavItem.ChildContent), b => b.AddContent(0, "Dashboard"), "Dashboard")
            .Parameter(nameof(FluentNavItem.IconRest), FluentIconCatalogue.All["Settings"])
            .Variant("With link", v => v.Set(nameof(FluentNavItem.Href), "https://learn.microsoft.com/fluentui-blazor"))
            .Variant("Disabled", v => v.Set(nameof(FluentNavItem.Disabled), true))
            .Variant("With tooltip", v => v.Set(nameof(FluentNavItem.Tooltip), "Go to your dashboard"));

        options.For<FluentNavCategory>()
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<FluentNav>(0);
                builder.AddAttribute(1, nameof(FluentNav.ChildContent), specimen);
                builder.CloseComponent();
            },
            "<FluentNav>\n    {specimen}\n</FluentNav>")
            .Related<FluentNav>()
            // Expanded defaults to false, and the sub-item group is hidden until expanded — set it
            // true so the standalone bench actually shows the sub-items rather than a bare header.
            .Slot(nameof(FluentNavCategory.ChildContent), FluentDemoFragments.NavCategoryItems, FluentDemoFragmentSources.NavCategoryItems)
            .Parameter(nameof(FluentNavCategory.Title), "Settings")
            .Parameter(nameof(FluentNavCategory.Expanded), true)
            .Variant("Collapsed", v => v.Set(nameof(FluentNavCategory.Expanded), false))
            .Variant("Custom icon", v => v.Set(nameof(FluentNavCategory.IconRest), FluentIconCatalogue.All["Star"]))
            .Variant("With tooltip", v => v.Set(nameof(FluentNavCategory.Tooltip), "Application settings"));

        options.For<FluentNavSectionHeader>()
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<FluentNav>(0);
                builder.AddAttribute(1, nameof(FluentNav.ChildContent), specimen);
                builder.CloseComponent();
            },
            "<FluentNav>\n    {specimen}\n</FluentNav>")
            .Related<FluentNav>()
            // Title is the component's only real customization axis (besides the FluentNav owner
            // it validates against) — one variant is enough to show it is a live parameter rather
            // than a fabrication.
            .Parameter(nameof(FluentNavSectionHeader.Title), "General")
            .Variant("Longer title", v => v.Set(nameof(FluentNavSectionHeader.Title), "Advanced configuration"));

        // FluentRadio<TValue> cascades from a FluentRadioGroup<TValue> of the SAME TValue —
        // discovery already closes the placeholder with string, so play that same closing here.
        options.For<FluentRadio<string>>()
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<FluentRadioGroup<string>>(0);
                builder.AddAttribute(1, nameof(FluentRadioGroup<string>.ChildContent), specimen);
                builder.CloseComponent();
            },
            "<FluentRadioGroup>\n    {specimen}\n</FluentRadioGroup>")
            .Related<FluentRadioGroup<string>>()
            .Parameter(nameof(FluentRadio<string>.Value), "Apple")
            .Parameter(nameof(FluentRadio<string>.Label), "Apple")
            .Variant("Disabled", v => v.Set(nameof(FluentRadio<string>.Disabled), true));

        // FluentWizard takes its steps through a "Steps" fragment, not ChildContent.
        options.For<FluentWizardStep>()
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<FluentWizard>(0);
                builder.AddAttribute(1, nameof(FluentWizard.Steps), specimen);
                builder.CloseComponent();
            },
            "<FluentWizard>\n    <Steps>\n        {specimen}\n    </Steps>\n</FluentWizard>")
            .Related<FluentWizard>()
            .Slot(nameof(FluentWizardStep.ChildContent), b => b.AddContent(0, "Enter your name and email address."), "Enter your name and email address.")
            .Parameter(nameof(FluentWizardStep.Label), "Account")
            .Parameter(nameof(FluentWizardStep.Summary), "Create your account")
            .Variant("Disabled", v => v.Set(nameof(FluentWizardStep.Disabled), true))
            // DeferredLoading only affects a NON-active step's panel, and the scaffold's single
            // step is always the active one — that chip would render identically to the base
            // preset. IconCurrent is the icon this specimen (index 0 of a one-step wizard, always
            // Status.Current) actually draws — verified against FluentWizard.AddStep/
            // SetCurrentStatusToStep, which sets Status=Current on registration for the step at
            // Value (0 by default) — so overriding it is a real, visible difference.
            .Variant("Custom current-step icon", v => v.Set(nameof(FluentWizardStep.IconCurrent), FluentIconCatalogue.All["Star"]))
            .Variant("Numbered", v => v.Set(nameof(FluentWizardStep.DisplayStepNumber), true));

        // FluentWizardStepValidator needs BOTH a FluentWizardStep ancestor and an EditContext
        // from an EditForm — one parent is not enough, so the scaffold nests two.
        options.For<FluentWizardStepValidator>()
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<FluentWizard>(0);
                builder.AddAttribute(1, nameof(FluentWizard.Steps), (RenderFragment)(stepsBuilder =>
                {
                    stepsBuilder.OpenComponent<FluentWizardStep>(0);
                    stepsBuilder.AddAttribute(1, nameof(FluentWizardStep.Label), "Step 1");
                    stepsBuilder.AddAttribute(2, nameof(FluentWizardStep.ChildContent), (RenderFragment)(stepBuilder =>
                    {
                        stepBuilder.OpenComponent<EditForm>(0);
                        stepBuilder.AddAttribute(1, nameof(EditForm.Model), WizardStepValidatorModel);
                        stepBuilder.AddAttribute(2, nameof(EditForm.ChildContent), (RenderFragment<EditContext>)(_ => specimen));
                        stepBuilder.CloseComponent();
                    }));
                    stepsBuilder.CloseComponent();
                }));
                builder.CloseComponent();
            },
            """
            <FluentWizard>
                <Steps>
                    <FluentWizardStep Label="Step 1">
                        <EditForm Model="_model">
                            {specimen}
                        </EditForm>
                    </FluentWizardStep>
                </Steps>
            </FluentWizard>
            """)
            .Related<FluentWizardStep>();

        // FluentDataGridRow<TGridItem> registers itself as "OwningRow" for its own ChildContent,
        // and the grid falls back to a manual (row-driven) layout whenever ChildContent collects
        // no columns — placing a row (rather than a column) there is what triggers that mode.
        options.For<FluentDataGridRow<Person>>()
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<FluentDataGrid<Person>>(0);
                builder.AddAttribute(1, nameof(FluentDataGrid<Person>.Items), SampleRows.AsQueryable());
                builder.AddAttribute(2, nameof(FluentDataGrid<Person>.ChildContent), specimen);
                builder.CloseComponent();
            },
            "<FluentDataGrid Items=\"@_people\">\n    {specimen}\n</FluentDataGrid>")
            .Related<FluentDataGrid<Person>>()
            .Parameter(nameof(FluentDataGridRow<Person>.Item), SampleRows[0])
            // ChildContent here is the row's OWN cells — real <FluentDataGridCell> markup, not
            // the placeholder text a plain Slot(name, b => b.AddContent(...)) call would give a
            // manual/row-driven row nothing to show.
            .Slot(nameof(FluentDataGridRow<Person>.ChildContent), FluentDemoFragments.RowCells, FluentDemoFragmentSources.RowCells)
            .Variant("Header row", v => v.Set(nameof(FluentDataGridRow<Person>.RowType), DataGridRowType.Header))
            .Variant("Custom column widths", v => v.Set(nameof(FluentDataGridRow<Person>.GridTemplateColumns), "160px 1fr"));

        options.For<FluentDataGridCell<Person>>()
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<FluentDataGrid<Person>>(0);
                builder.AddAttribute(1, nameof(FluentDataGrid<Person>.Items), SampleRows.AsQueryable());
                builder.AddAttribute(2, nameof(FluentDataGrid<Person>.ChildContent), (RenderFragment)(rowBuilder =>
                {
                    rowBuilder.OpenComponent<FluentDataGridRow<Person>>(0);
                    rowBuilder.AddAttribute(1, nameof(FluentDataGridRow<Person>.ChildContent), specimen);
                    rowBuilder.CloseComponent();
                }));
                builder.CloseComponent();
            },
            """
            <FluentDataGrid Items="@_people">
                <FluentDataGridRow>
                    {specimen}
                </FluentDataGridRow>
            </FluentDataGrid>
            """)
            .Related<FluentDataGridRow<Person>>()
            .Parameter(nameof(FluentDataGridCell<Person>.Item), SampleRows[0])
            .Parameter(nameof(FluentDataGridCell<Person>.CellTitle), "Employee name")
            .Slot(nameof(FluentDataGridCell<Person>.ChildContent), b => b.AddContent(0, "Ada Lovelace"), "Ada Lovelace")
            .Variant("Header cell", v => v.Set(nameof(FluentDataGridCell<Person>.CellType), DataGridCellType.ColumnHeader))
            .Variant("Row header cell", v => v.Set(nameof(FluentDataGridCell<Person>.CellType), DataGridCellType.RowHeader));

        // PropertyColumn.Property is [EditorRequired] with no default — without a preset,
        // OnParametersSet NullReferenceExceptions compiling a null expression.
        options.For<PropertyColumn<Person, string>>()
            .Parameter(nameof(PropertyColumn<Person, string>.Property),
                (Expression<Func<Person, string>>)(p => p.Name))
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<FluentDataGrid<Person>>(0);
                builder.AddAttribute(1, nameof(FluentDataGrid<Person>.Items), SampleRows.AsQueryable());
                builder.AddAttribute(2, nameof(FluentDataGrid<Person>.ChildContent), specimen);
                builder.CloseComponent();
            },
            "<FluentDataGrid Items=\"@_people\">\n    {specimen}\n</FluentDataGrid>")
            .Related<FluentDataGrid<Person>>()
            .Variant("Sortable", v => v.Set(nameof(PropertyColumn<Person, string>.Sortable), true))
            .Variant("Right aligned", v => v.Set(nameof(PropertyColumn<Person, string>.Align), DataGridCellAlignment.End))
            .Variant("With tooltip", v => v.Set(nameof(PropertyColumn<Person, string>.Tooltip), true));

        // SelectColumn.Property decides checked state; its own default always returns false, so a
        // Parameter preset is what makes the sample grid open with a row already checked.
        options.For<SelectColumn<Person>>()
            .Parameter(nameof(SelectColumn<Person>.Property), (Func<Person, bool>)(p => p.Name == SampleRows[0].Name))
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<FluentDataGrid<Person>>(0);
                builder.AddAttribute(1, nameof(FluentDataGrid<Person>.Items), SampleRows.AsQueryable());
                builder.AddAttribute(2, nameof(FluentDataGrid<Person>.ChildContent), specimen);
                builder.CloseComponent();
            },
            "<FluentDataGrid Items=\"@_people\">\n    {specimen}\n</FluentDataGrid>")
            .Related<FluentDataGrid<Person>>()
            .Variant("Multiple selection", v => v.Set(nameof(SelectColumn<Person>.SelectMode), DataGridSelectMode.Multiple))
            .Variant("Sticky single selection", v => v.Set(nameof(SelectColumn<Person>.SelectMode), DataGridSelectMode.SingleSticky));

        // TemplateColumn.ChildContent is RenderFragment<TGridItem> — a CLOSED generic RenderFragment,
        // which ParameterDictionaryBuilder.BuildSlot cannot honor: it special-cases only the literal
        // non-generic RenderFragment (see src/PlayBlazor/Rendering/ParameterDictionaryBuilder.cs), and
        // ControlKind.Slot parameters never fall through to the Parameter-preset or Variant-state
        // paths either. No preset reaches ChildContent, so it keeps the field's own default
        // (a no-op fragment) and every cell in this column renders empty — a src/PlayBlazor gap this
        // demo-only task cannot close. What follows curates every OTHER reachable ColumnBase property
        // so the column is not left completely uncurated.
        options.For<TemplateColumn<Person>>()
            .Parameter(nameof(TemplateColumn<Person>.Title), "Bio")
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<FluentDataGrid<Person>>(0);
                builder.AddAttribute(1, nameof(FluentDataGrid<Person>.Items), SampleRows.AsQueryable());
                builder.AddAttribute(2, nameof(FluentDataGrid<Person>.ChildContent), specimen);
                builder.CloseComponent();
            },
            "<FluentDataGrid Items=\"@_people\">\n    {specimen}\n</FluentDataGrid>")
            .Related<FluentDataGrid<Person>>()
            .Variant("Centered", v => v.Set(nameof(TemplateColumn<Person>.Align), DataGridCellAlignment.Center))
            .Variant("With tooltip", v => v.Set(nameof(TemplateColumn<Person>.Tooltip), true));

        // Person doesn't implement IHierarchicalGridItem, so the constructor's own Property default
        // (which reads that interface) always evaluates false — a Parameter preset that reads the
        // item directly is what opens the sample grid with a row already checked, same as SelectColumn.
        // SelectMode must stay Multiple — HierarchicalSelectColumn.OnParametersSet throws otherwise —
        // so variants vary other real selection-chrome axes instead.
        options.For<HierarchicalSelectColumn<Person>>()
            .Parameter(nameof(HierarchicalSelectColumn<Person>.Property), (Func<Person, bool>)(p => p.Name == SampleRows[0].Name))
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<FluentDataGrid<Person>>(0);
                builder.AddAttribute(1, nameof(FluentDataGrid<Person>.Items), SampleRows.AsQueryable());
                builder.AddAttribute(2, nameof(FluentDataGrid<Person>.ChildContent), specimen);
                builder.CloseComponent();
            },
            "<FluentDataGrid Items=\"@_people\">\n    {specimen}\n</FluentDataGrid>")
            .Related<FluentDataGrid<Person>>()
            .Variant("Select-all disabled", v => v.Set(nameof(HierarchicalSelectColumn<Person>.SelectAllDisabled), true))
            .Variant("Checkbox-only selection", v => v.Set(nameof(HierarchicalSelectColumn<Person>.SelectFromEntireRow), false));

        // Both validation components demand a cascading EditContext and say so by name. An
        // EditForm over a throwaway model is the smallest thing that supplies one.
        options.For<FluentValidationSummary>()
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<EditForm>(0);
                builder.AddAttribute(1, nameof(EditForm.Model), ValidationScaffoldModel);
                builder.AddAttribute(2, nameof(EditForm.ChildContent),
                    (RenderFragment<EditContext>)(_ => specimen));
                builder.CloseComponent();
            },
            "<EditForm Model=\"@_model\">\n    {specimen}\n</EditForm>");

        // FluentValidationMessage<TValue> checks its cascading EditContext first, but — verified
        // by actually rendering it — throws a SECOND, different error the moment that check
        // passes: it also demands a Field or For value to know which messages to show. A Parameter
        // preset for Field (built from the same throwaway model) supplies that.
        options.For<FluentValidationMessage<string>>()
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<EditForm>(0);
                builder.AddAttribute(1, nameof(EditForm.Model), ValidationScaffoldModel);
                builder.AddAttribute(2, nameof(EditForm.ChildContent),
                    (RenderFragment<EditContext>)(_ => specimen));
                builder.CloseComponent();
            },
            "<EditForm Model=\"@_model\">\n    {specimen}\n</EditForm>")
            .Parameter(nameof(FluentValidationMessage<string>.Field),
                new FieldIdentifier(ValidationScaffoldModel, nameof(Person.Name)));

        // AddTag.Name is `required` — without a preset, BuildRenderTree throws before anything
        // shows. It is an HTML ELEMENT NAME (BuildRenderTree does OpenElement(0, Name)), not a
        // label: Fluent's own internal call sites always pass a real custom element name
        // ("fluent-badge-container", "fluent-drawer", "fluent-dialog", …) — never a plain word
        // like "priority", which would render as a meaningless unknown element. Reuse one of
        // Fluent's own: FluentBadge wraps its content in exactly this element.
        options.For<AddTag>()
            .Parameter(nameof(AddTag.Name), "fluent-badge-container");

        // FluentKeyCode needs either an Anchor id or ChildContent to attach its key-listening JS to.
        // ChildContent is the one a Slot preset can supply.
        options.For<FluentKeyCode>()
            .Slot(nameof(FluentKeyCode.ChildContent), b => b.AddContent(0, "Press any key"), "Press any key")
            .Variant("Global document listener", v => v.Set(nameof(FluentKeyCode.GlobalDocument), true))
            .Variant("Ignore key repeats", v => v.Set(nameof(FluentKeyCode.StopRepeat), true))
            .Variant("Div wrapper", v => v.Set(nameof(FluentKeyCode.TagName), FluentKeyCodeTag.Div));

        // FluentPaginator's sweep failure is not a missing parent — it wants a PaginationState
        // instance, so it gets a Parameter preset rather than a Scaffold. A bare `new
        // PaginationState()` is not enough, though: BuildRenderTree gates its ENTIRE output
        // (summary text, all four nav buttons) behind `State.TotalItemCount.HasValue`, which stays
        // null until SetTotalItemCountAsync runs — an unset total renders a bare, empty <div> with
        // nothing in it. SamplePaginationState below calls that once, synchronously, so the bench
        // shows real pagination chrome instead of an empty box that merely fails to throw.
        //
        // State is the ONLY other axis worth varying (SummaryTemplate/PaginationTextTemplate are
        // RenderFragment — a Variant can never carry one), but State is ControlKind.Unsupported:
        // ParameterDictionaryBuilder's Unsupported branch reads only the static Parameter preset
        // above and never consults live PlaygroundState, so `.Variant().Set(nameof(State), ...)`
        // would silently no-op — confirmed by rendering it and finding the "variant" reference-equal
        // to the base preset. Disabled is the only real, live-drivable parameter left.
        options.For<FluentPaginator>()
            .Parameter(nameof(FluentPaginator.State), SamplePaginationState, "@_paginationState")
            .Variant("Disabled", v => v.Set(nameof(FluentPaginator.Disabled), true));

        // --- Data: sortable lists, drag-and-drop, overflow, pull-to-refresh ---

        options.For<FluentDataGrid<Person>>()
            .Parameter(nameof(FluentDataGrid<Person>.Items), SampleRows.AsQueryable(), "@_people")
            .Slot(nameof(FluentDataGrid<Person>.ChildContent), source: """
                <PropertyColumn Property="p => p.Name" />
                <PropertyColumn Property="p => p.Role" />
                <PropertyColumn Property="p => p.Age" />
                """, content: columns =>
            {
                AddColumn<string>(columns, 0, p => p.Name);
                AddColumn<string>(columns, 2, p => p.Role);
                AddColumn<int>(columns, 4, p => p.Age);
            })
            .Variant("Striped rows", v => v.Set(nameof(FluentDataGrid<Person>.StripedRows), true))
            .Variant("Resizable columns", v => v.Set(nameof(FluentDataGrid<Person>.ResizableColumns), true))
            .Variant("Auto-fit columns", v => v.Set(nameof(FluentDataGrid<Person>.AutoFit), true))
            .Variant("Table display", v => v.Set(nameof(FluentDataGrid<Person>.DisplayMode), DataGridDisplayMode.Table))
            .Related<PropertyColumn<Person, string>>();

        // FluentSortableList<TItem>.ItemTemplate is [EditorRequired] RenderFragment<TItem> — the
        // same closed-generic-RenderFragment gap noted above for TemplateColumn.ChildContent, but
        // starker: BuildRenderTree returns immediately when ItemTemplate is null, so the specimen
        // renders NOTHING at all, not even a wrapper element — no error, no content, invisible.
        // No preset in src/PlayBlazor's current Slot/Parameter/Variant surface can reach it. Every
        // other real, reachable parameter is curated below so the type is not left bare, but the
        // bench itself will show an empty area regardless.
        options.For<FluentSortableList<string>>()
            .Parameter(nameof(FluentSortableList<string>.Items), new[] { "Design", "Build", "Test", "Ship" }, "@_tasks")
            .Parameter(nameof(FluentSortableList<string>.AriaLabel), "Project tasks")
            .Variant("Drag handles only", v => v.Set(nameof(FluentSortableList<string>.Handle), true))
            .Variant("Shared group", v => v.Set(nameof(FluentSortableList<string>.Group), "tasks"));

        // FluentDragContainer<TItem>.ChildContent is a plain (non-generic) RenderFragment — unlike
        // ItemTemplate above, a Slot preset reaches it fine. It cascades itself to its
        // FluentDropZone<TItem> children, so DropZones (real drop targets, not placeholder text)
        // is what actually belongs here.
        options.For<FluentDragContainer<string>>()
            .Slot(nameof(FluentDragContainer<string>.ChildContent), FluentDemoFragments.DropZones, FluentDemoFragmentSources.DropZones)
            .Related<FluentDropZone<string>>();

        // FluentDropZone<TItem>.Container is a `required` CascadingParameter of a
        // FluentDragContainer<TItem> — the scaffold supplies the one ancestor it demands.
        options.For<FluentDropZone<string>>()
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<FluentDragContainer<string>>(0);
                builder.AddAttribute(1, nameof(FluentDragContainer<string>.ChildContent), specimen);
                builder.CloseComponent();
            },
            "<FluentDragContainer>\n    {specimen}\n</FluentDragContainer>")
            .Related<FluentDragContainer<string>>()
            .Parameter(nameof(FluentDropZone<string>.Item), "Design mockups")
            .Parameter(nameof(FluentDropZone<string>.Draggable), true)
            .Parameter(nameof(FluentDropZone<string>.Droppable), true)
            .Slot(nameof(FluentDropZone<string>.ChildContent), b => b.AddContent(0, "Design mockups"), "Design mockups")
            .Variant("Drop target only", v => v.Set(nameof(FluentDropZone<string>.Droppable), true).Set(nameof(FluentDropZone<string>.Draggable), false))
            .Variant("Draggable only", v => v.Set(nameof(FluentDropZone<string>.Draggable), true).Set(nameof(FluentDropZone<string>.Droppable), false));

        options.For<FluentOverflow>()
            .Slot(nameof(FluentOverflow.ChildContent), FluentDemoFragments.OverflowItems, FluentDemoFragmentSources.OverflowItems)
            .Variant("Vertical", v => v.Set(nameof(FluentOverflow.Orientation), Orientation.Vertical))
            .Variant("Hidden until loaded", v => v.Set(nameof(FluentOverflow.VisibleOnLoad), false));

        options.For<FluentPullToRefresh>()
            .Slot(nameof(FluentPullToRefresh.ChildContent), b => b.AddContent(0, "Pull down to refresh the feed."), "Pull down to refresh the feed.")
            .Variant("Pull from bottom", v => v.Set(nameof(FluentPullToRefresh.Direction), PullDirection.Up))
            .Variant("Static tip hidden", v => v.Set(nameof(FluentPullToRefresh.ShowStaticTip), false))
            .Variant("Disabled", v => v.Set(nameof(FluentPullToRefresh.Disabled), true));

        // --- Form inputs: text, choice, value-entry and file components. ---
        // Verified against the pinned 5.0.0-rc.5 assembly and the official v5 docs site
        // (fluentui-blazor-v5.azurewebsites.net) rather than assumed — v4-era guesses are exactly
        // what stalled an earlier task in this project.

        options.For<FluentTextInput>()
            .Parameter(nameof(FluentTextInput.Label), "Name")
            .Parameter(nameof(FluentTextInput.Placeholder), "Ada Lovelace")
            .Variant("Underline", v => v.Set(nameof(FluentTextInput.Appearance), TextInputAppearance.Underline))
            .Variant("Filled darker", v => v.Set(nameof(FluentTextInput.Appearance), TextInputAppearance.FilledDarker))
            .Variant("Email", v => v.Set(nameof(FluentTextInput.TextInputType), TextInputType.Email).Set(nameof(FluentTextInput.Label), "Email"));

        options.For<FluentTextArea>()
            .Parameter(nameof(FluentTextArea.Label), "Description")
            .Parameter(nameof(FluentTextArea.Value), "A brief description of the specimen.")
            .Variant("Filled darker", v => v.Set(nameof(FluentTextArea.Appearance), TextAreaAppearance.FilledDarker))
            .Variant("Small", v => v.Set(nameof(FluentTextArea.Size), TextAreaSize.Small))
            .Variant("Resizable", v => v.Set(nameof(FluentTextArea.Resize), TextAreaResize.Both));

        options.For<FluentSelect<string, string>>()
            .Parameter(nameof(FluentSelect<string, string>.Label), "Color")
            .Parameter(nameof(FluentSelect<string, string>.Placeholder), "Select a color")
            .Parameter(nameof(FluentSelect<string, string>.Items), ListOptions, "@_colors")
            .Variant("Filled darker", v => v.Set(nameof(FluentSelect<string, string>.Appearance), ListAppearance.FilledDarker))
            .Variant("Small", v => v.Set(nameof(FluentSelect<string, string>.Size), ListSize.Small))
            .Variant("Large", v => v.Set(nameof(FluentSelect<string, string>.Size), ListSize.Large));

        options.For<FluentCombobox<string, string>>()
            .Parameter(nameof(FluentCombobox<string, string>.Label), "Color")
            .Parameter(nameof(FluentCombobox<string, string>.Placeholder), "Select your color")
            .Parameter(nameof(FluentCombobox<string, string>.Items), ListOptions, "@_colors")
            .Variant("Multiple", v => v.Set(nameof(FluentCombobox<string, string>.Multiple), true))
            .Variant("Filled darker", v => v.Set(nameof(FluentCombobox<string, string>.Appearance), ListAppearance.FilledDarker));

        options.For<FluentAutocomplete<string, string>>()
            .Parameter(nameof(FluentAutocomplete<string, string>.Label), "Colors")
            .Parameter(nameof(FluentAutocomplete<string, string>.Placeholder), "Type to search...")
            .Parameter(nameof(FluentAutocomplete<string, string>.Items), ListOptions, "@_colors")
            .Variant("Single selection", v => v.Set(nameof(FluentAutocomplete<string, string>.Multiple), false))
            .Variant("Max 2 selections", v => v.Set(nameof(FluentAutocomplete<string, string>.MaximumSelectedOptions), 2));

        options.For<FluentListbox<string, string>>()
            .Parameter(nameof(FluentListbox<string, string>.Label), "Color")
            .Parameter(nameof(FluentListbox<string, string>.Items), ListOptions, "@_colors")
            .Variant("Filled darker", v => v.Set(nameof(FluentListbox<string, string>.Appearance), ListAppearance.FilledDarker))
            .Variant("Transparent", v => v.Set(nameof(FluentListbox<string, string>.Appearance), ListAppearance.Transparent));

        options.For<FluentOption<string>>()
            .Parameter(nameof(FluentOption<string>.Value), "red")
            .Parameter(nameof(FluentOption<string>.Text), "Red")
            .Variant("Selected", v => v.Set(nameof(FluentOption<string>.Selected), true))
            .Variant("Disabled", v => v.Set(nameof(FluentOption<string>.Disabled), true))
            .Variant("With description", v => v.Set(nameof(FluentOption<string>.Description), "A warm, vivid color"));

        options.For<FluentCheckbox>()
            .Parameter(nameof(FluentCheckbox.Label), "I agree to the terms")
            .Variant("Three-state", v => v.Set(nameof(FluentCheckbox.ThreeState), true))
            .Variant("Circular", v => v.Set(nameof(FluentCheckbox.Shape), CheckboxShape.Circular))
            .Variant("Large", v => v.Set(nameof(FluentCheckbox.Size), CheckboxSize.Large));

        options.For<FluentSwitch>()
            .Parameter(nameof(FluentSwitch.Label), "Notifications")
            .Variant("Checked", v => v.Set(nameof(FluentSwitch.Value), true))
            .Variant("Label above", v => v.Set(nameof(FluentSwitch.LabelPosition), LabelPosition.Above))
            .Variant("Disabled", v => v.Set(nameof(FluentSwitch.Disabled), true));

        // Basic Radio Group example straight off the docs: a Label per FluentRadio, one disabled.
        options.For<FluentRadioGroup<string>>()
            .Slot(nameof(FluentRadioGroup<string>.ChildContent), FluentDemoFragments.RadioOptions, FluentDemoFragmentSources.RadioOptions)
            .Parameter(nameof(FluentRadioGroup<string>.Label), "Favorite fruit")
            .Parameter(nameof(FluentRadioGroup<string>.Wrap), true)
            .Variant("Vertical", v => v.Set(nameof(FluentRadioGroup<string>.Orientation), Orientation.Vertical))
            .Variant("Required", v => v.Set(nameof(FluentRadioGroup<string>.Required), true));

        options.For<FluentSlider<int>>()
            .Parameter(nameof(FluentSlider<int>.Label), "Volume")
            .Parameter(nameof(FluentSlider<int>.Value), 50)
            .Variant("10-40 step 5", v => v.Set(nameof(FluentSlider<int>.Min), 10).Set(nameof(FluentSlider<int>.Max), 40).Set(nameof(FluentSlider<int>.Step), 5))
            .Variant("Vertical", v => v.Set(nameof(FluentSlider<int>.Orientation), Orientation.Vertical))
            .Variant("Small", v => v.Set(nameof(FluentSlider<int>.Size), SliderSize.Small));

        options.For<FluentColorPicker>()
            .Parameter(nameof(FluentColorPicker.SelectedColor), "#6d4aff")
            .Variant("Color wheel", v => v.Set(nameof(FluentColorPicker.View), ColorPickerView.ColorWheel))
            .Variant("HSV square", v => v.Set(nameof(FluentColorPicker.View), ColorPickerView.HsvSquare))
            .Variant("Vertical", v => v.Set(nameof(FluentColorPicker.Orientation), Orientation.Vertical));

        options.For<FluentColorPickerInput>()
            .Parameter(nameof(FluentColorPickerInput.Value), "#0078D4")
            .Parameter(nameof(FluentColorPickerInput.Label), "Brand color")
            .Variant("Color wheel", v => v.Set(nameof(FluentColorPickerInput.View), ColorPickerView.ColorWheel))
            .Variant("Swatch only", v => v.Set(nameof(FluentColorPickerInput.HideTextInput), true));

        // FluentField wraps an input as its content parameter — IncludeInputSlot (true by
        // default) does the slot="input" wiring, so a plain specimen is enough.
        options.For<FluentField>()
            .Slot(nameof(FluentField.ChildContent), b =>
            {
                b.OpenComponent<FluentTextInput>(0);
                b.AddComponentParameter(1, nameof(FluentTextInput.Placeholder), "you@example.com");
                b.CloseComponent();
            }, "<FluentTextInput Placeholder=\"you@example.com\" />")
            .Parameter(nameof(FluentField.Label), "Email")
            .Variant("Required", v => v.Set(nameof(FluentField.Required), true))
            .Variant("Small", v => v.Set(nameof(FluentField.Size), FieldSize.Small))
            .Variant("Disabled", v => v.Set(nameof(FluentField.Disabled), true));

        options.For<FluentLabel>()
            .Slot(nameof(FluentLabel.ChildContent), b => b.AddContent(0, "Selected fruit: Banana"), "Selected fruit: Banana")
            .Variant("Required marker", v => v.Set(nameof(FluentLabel.Required), true))
            .Variant("Semibold", v => v.Set(nameof(FluentLabel.Weight), LabelWeight.Semibold))
            .Variant("Large", v => v.Set(nameof(FluentLabel.Size), LabelSize.Large));

        options.For<FluentInputFile>()
            .Slot(nameof(FluentInputFile.ChildContent), b => b.AddContent(0, "Drag files here, or click to browse."), "Drag files here, or click to browse.")
            .Parameter(nameof(FluentInputFile.Accept), "image/*")
            .Parameter(nameof(FluentInputFile.Height), "200px")
            .Variant("Multiple files", v => v.Set(nameof(FluentInputFile.Multiple), true).Set(nameof(FluentInputFile.MaximumFileCount), 4))
            .Variant("No drag-drop zone", v => v.Set(nameof(FluentInputFile.DragDropZoneVisible), false));

        // --- Display and layout: badges, avatar, card, typography, progress, grid/stack/layout/splitter. ---
        // Verified against the pinned 5.0.0-rc.5 assembly with ilspycmd rather than assumed.

        // FluentBadge.Content is real display text (unlike AddTag.Name, which the container
        // wrapping it passes to OpenElement as an element name — see AddTag's own comment above).
        // OffsetX/OffsetY are sbyte and left alone per the brief: a numeric control for sbyte is
        // out of scope for this task.
        options.For<FluentBadge>()
            .Parameter(nameof(FluentBadge.Content), "New")
            .Variant("Brand tint", v => v.Set(nameof(FluentBadge.Appearance), BadgeAppearance.Tint).Set(nameof(FluentBadge.Color), BadgeColor.Brand))
            .Variant("Danger", v => v.Set(nameof(FluentBadge.Color), BadgeColor.Danger))
            .Variant("Rounded, large", v => v.Set(nameof(FluentBadge.Shape), BadgeShape.Rounded).Set(nameof(FluentBadge.Size), BadgeSize.Large));

        // FluentCounterBadge.Count defaults to null, and ShowWhen only renders it once Count > 0
        // (verified in BuildRenderTree: _render is false without a dot, a pattern or a positive
        // count) — without a preset the bench would show an empty badge shell.
        options.For<FluentCounterBadge>()
            .Parameter(nameof(FluentCounterBadge.Count), 4)
            .Variant("Overflow", v => v.Set(nameof(FluentCounterBadge.Count), 128).Set(nameof(FluentCounterBadge.OverflowCount), 99))
            .Variant("Dot", v => v.Set(nameof(FluentCounterBadge.Dot), true))
            // GetCount() (decompiled) returns Count only when ShowWhen(Count) is true, and the
            // default ShowWhen is `Count => Count > 0` — Count=0 always fails that predicate, so
            // ShowZero alone (it only gates whether the badge SHELL renders, via _render) never
            // makes the "count" attribute appear. ShowWhen must be overridden too.
            .Variant("Show zero", v => v.Set(nameof(FluentCounterBadge.Count), 0)
                .Set(nameof(FluentCounterBadge.ShowZero), true)
                .Set(nameof(FluentCounterBadge.ShowWhen), (Func<int?, bool>)(count => count.HasValue)));

        // FluentPresenceBadge.Status already defaults to Available and picks its own icon
        // internally (GetPresenceIcon), so it looks like itself with no preset — only variants.
        options.For<FluentPresenceBadge>()
            .Variant("Busy", v => v.Set(nameof(FluentPresenceBadge.Status), PresenceStatus.Busy))
            .Variant("Away, out of office", v => v.Set(nameof(FluentPresenceBadge.Status), PresenceStatus.Away).Set(nameof(FluentPresenceBadge.OutOfOffice), true))
            .Variant("Large", v => v.Set(nameof(FluentPresenceBadge.Size), BadgeSize.Large));

        // FluentAvatar.Name both feeds the accessible name AND drives the web component's own
        // initials generation (GetInitialsValue only overrides it when Initials is set explicitly).
        options.For<FluentAvatar>()
            .Parameter(nameof(FluentAvatar.Name), "Ada Lovelace")
            .Variant("Colorful", v => v.Set(nameof(FluentAvatar.Color), AvatarColor.Colorful))
            .Variant("Square, large", v => v.Set(nameof(FluentAvatar.Shape), AvatarShape.Square).Set(nameof(FluentAvatar.Size), AvatarSize.Size48))
            .Variant("Active ring", v => v.Set(nameof(FluentAvatar.Active), true).Set(nameof(FluentAvatar.ActiveAppearance), AvatarActiveAppearance.Ring));

        options.For<FluentCard>()
            .Slot(nameof(FluentCard.ChildContent), FluentDemoFragments.CardBody, FluentDemoFragmentSources.CardBody)
            .Parameter(nameof(FluentCard.Width), "280px")
            .Variant("Filled", v => v.Set(nameof(FluentCard.Appearance), CardAppearance.Filled))
            .Variant("Outline", v => v.Set(nameof(FluentCard.Appearance), CardAppearance.Outline))
            .Variant("Large shadow", v => v.Set(nameof(FluentCard.Shadow), CardShadow.Large));

        options.For<FluentDivider>()
            .Slot(nameof(FluentDivider.ChildContent), b => b.AddContent(0, "OR"), "OR")
            .Variant("Vertical", v => v.Set(nameof(FluentDivider.Vertical), true))
            .Variant("Brand", v => v.Set(nameof(FluentDivider.Appearance), DividerAppearance.Brand))
            .Variant("Inset", v => v.Set(nameof(FluentDivider.Inset), true));

        // FluentGrid cascades ITSELF (CascadingValue<FluentGrid>) around ChildContent so its
        // FluentGridItem children can read layout state — confirmed in BuildRenderTree. The grid
        // itself never throws without items, but Slot gives it real columns to show.
        options.For<FluentGrid>()
            .Slot(nameof(FluentGrid.ChildContent), FluentDemoFragments.GridItems, FluentDemoFragmentSources.GridItems)
            .Parameter(nameof(FluentGrid.Spacing), 2)
            .Variant("Centered", v => v.Set(nameof(FluentGrid.Justify), JustifyContent.Center))
            .Variant("Space between", v => v.Set(nameof(FluentGrid.Justify), JustifyContent.SpaceBetween))
            .Variant("Adaptive rendering", v => v.Set(nameof(FluentGrid.AdaptiveRendering), true));

        // FluentGridItem's [CascadingParameter] Grid is nullable and never null-checked before
        // use, so it renders standalone without throwing (confirmed in BuildRenderTree) — but a
        // lone item shows no column proportions without a FluentGrid around it. Scaffold nests it
        // in one alongside its own Slot/Parameter so the standalone bench still looks reasonable.
        options.For<FluentGridItem>()
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<FluentGrid>(0);
                builder.AddAttribute(1, nameof(FluentGrid.ChildContent), specimen);
                builder.CloseComponent();
            },
            "<FluentGrid>\n    {specimen}\n</FluentGrid>")
            .Related<FluentGrid>()
            .Slot(nameof(FluentGridItem.ChildContent), b => b.AddContent(0, "Column content"), "Column content")
            .Parameter(nameof(FluentGridItem.Xs), 6)
            .Variant("Quarter width", v => v.Set(nameof(FluentGridItem.Xs), 3))
            .Variant("Hidden on small screens", v => v.Set(nameof(FluentGridItem.HiddenWhen), GridItemHidden.Xs));

        options.For<FluentStack>()
            .Slot(nameof(FluentStack.ChildContent), FluentDemoFragments.StackItems, FluentDemoFragmentSources.StackItems)
            .Variant("Vertical", v => v.Set(nameof(FluentStack.Orientation), Orientation.Vertical))
            .Variant("Centered", v => v.Set(nameof(FluentStack.HorizontalAlignment), HorizontalAlignment.Center).Set(nameof(FluentStack.VerticalAlignment), VerticalAlignment.Center))
            .Variant("Wrap", v => v.Set(nameof(FluentStack.Wrap), true));

        // FluentSpacer renders an empty div: its whole job is to occupy space inside a flex
        // parent (FluentStack), so there is no meaningful ChildContent to preset. Its StyleValue
        // (decompiled) writes "width" ONLY for Horizontal orientation and "height" ONLY for
        // Vertical. A plain block <div> already gives Vertical a full-width band with no parent
        // at all (block width:auto == 100% of container) — that is why Orientation=Vertical +
        // Height alone is genuinely visible standalone. Horizontal has no such rescue: it is
        // meant to rely on a flex ROW's cross-axis stretch for its height, by design.
        //
        // A single FluentStack cannot rescue BOTH orientations at once, though — Scaffold is
        // registered once per Type and wraps EVERY variant (PlaygroundView.WrappedSpecimen has
        // no bare/wrapped toggle), and CSS Flexbox's align-items:stretch only ever reaches the
        // CROSS axis of a direct flex child: a row stretches height but leaves an empty,
        // width-unset item's own (main-axis) width at 0 — exactly the Vertical variants, which
        // set no Width and get no flex-grow (FluentSpacer's own flex-grow condition requires the
        // UNSET dimension to match Orientation, which Height already satisfies). A column
        // wrapper just swaps which orientation breaks. No nesting of FluentStacks escapes this;
        // the item's own main axis never auto-stretches without flex-grow, in either direction.
        //
        // CSS Grid does not have this asymmetry: a grid item's default align-items/justify-items
        // is "stretch" on BOTH axes at once. A middle grid column with a guaranteed minimum
        // track size keeps every orientation visible in the SAME wrapper — an explicit dimension
        // the specimen itself sets (Width for Horizontal, Height for Vertical) still wins over
        // stretch per spec, so this never overrides what the real component renders; it only
        // guarantees the axis FluentSpacer itself leaves unset is never zero.
        options.For<FluentSpacer>()
            .Scaffold(specimen => builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "style",
                    "display:grid; grid-template-columns:auto minmax(40px,max-content) auto; " +
                    "align-items:stretch; justify-items:stretch; gap:8px");
                builder.OpenComponent<FluentCard>(2);
                builder.AddAttribute(3, nameof(FluentCard.Width), "120px");
                builder.AddAttribute(4, nameof(FluentCard.ChildContent), (RenderFragment)(b => b.AddContent(0, "Before")));
                builder.CloseComponent();
                builder.AddContent(5, specimen);
                builder.OpenComponent<FluentCard>(6);
                builder.AddAttribute(7, nameof(FluentCard.Width), "120px");
                builder.AddAttribute(8, nameof(FluentCard.ChildContent), (RenderFragment)(b => b.AddContent(0, "After")));
                builder.CloseComponent();
                builder.CloseElement();
            },
            """
            <div style="display:grid; grid-template-columns:auto minmax(40px,max-content) auto; align-items:stretch; justify-items:stretch; gap:8px">
                <FluentCard Width="120px">Before</FluentCard>
                {specimen}
                <FluentCard Width="120px">After</FluentCard>
            </div>
            """)
            .Parameter(nameof(FluentSpacer.Orientation), Orientation.Vertical)
            .Parameter(nameof(FluentSpacer.Height), "40px")
            .Variant("Vertical, tall", v => v.Set(nameof(FluentSpacer.Height), "120px"))
            .Variant("Vertical, short", v => v.Set(nameof(FluentSpacer.Height), "8px"))
            .Variant("Horizontal (the component's own default orientation)", v => v
                .Set(nameof(FluentSpacer.Orientation), Orientation.Horizontal)
                .Set(nameof(FluentSpacer.Width), "40px"));

        // FluentLayout defaults Height to "100dvh" when unset (OnParametersSet-free — see
        // StyleValue), which would blow out the workspace stage; bound it for the bench.
        options.For<FluentLayout>()
            .Slot(nameof(FluentLayout.ChildContent), FluentDemoFragments.LayoutAreas, FluentDemoFragmentSources.LayoutAreas)
            .Parameter(nameof(FluentLayout.Height), "320px")
            .Variant("Global scrollbar", v => v.Set(nameof(FluentLayout.GlobalScrollbar), true))
            .Variant("Mobile breakdown 480px", v => v.Set(nameof(FluentLayout.MobileBreakdownWidth), 480));

        // FluentLayoutItem's [CascadingParameter] LayoutContainer is likewise null-safe
        // throughout (RenderThisArea, AddGridAreaStyles, AddStickyStyle all null-check it), so it
        // never throws alone — Scaffold gives it the grid-template-areas context it needs to look
        // like a real layout panel rather than a bare div.
        options.For<FluentLayoutItem>()
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<FluentLayout>(0);
                builder.AddAttribute(1, nameof(FluentLayout.Height), "200px");
                builder.AddAttribute(2, nameof(FluentLayout.ChildContent), specimen);
                builder.CloseComponent();
            },
            "<FluentLayout Height=\"200px\">\n    {specimen}\n</FluentLayout>")
            .Related<FluentLayout>()
            .Slot(nameof(FluentLayoutItem.ChildContent), b => b.AddContent(0, "Panel content"), "Panel content")
            .Parameter(nameof(FluentLayoutItem.Area), LayoutArea.Content)
            .Variant("Header", v => v.Set(nameof(FluentLayoutItem.Area), LayoutArea.Header))
            .Variant("Sticky", v => v.Set(nameof(FluentLayoutItem.Sticky), true));

        options.For<FluentText>()
            .Slot(nameof(FluentText.ChildContent), b => b.AddContent(0, "The quick brown fox jumps over the lazy dog"), "The quick brown fox jumps over the lazy dog")
            .Variant("Heading", v => v.Set(nameof(FluentText.As), TextTag.H4).Set(nameof(FluentText.Size), TextSize.Size600).Set(nameof(FluentText.Weight), TextWeight.Semibold))
            .Variant("Subtle", v => v.Set(nameof(FluentText.Size), TextSize.Size200))
            .Variant("Italic underline", v => v.Set(nameof(FluentText.Italic), true).Set(nameof(FluentText.Underline), true));

        options.For<FluentHighlighter>()
            .Parameter(nameof(FluentHighlighter.Text), "The quick brown fox jumps over the lazy dog")
            .Parameter(nameof(FluentHighlighter.HighlightedText), "fox")
            .Variant("Case sensitive", v => v.Set(nameof(FluentHighlighter.CaseSensitive), true))
            .Variant("Multiple terms", v => v.Set(nameof(FluentHighlighter.HighlightedText), "quick brown").Set(nameof(FluentHighlighter.Delimiters), " "));

        options.For<FluentImage>()
            // Self-contained data URI: the bench must not depend on an external image host.
            .Parameter(nameof(FluentImage.Source), "data:image/svg+xml," + Uri.EscapeDataString(
                """<svg xmlns="http://www.w3.org/2000/svg" width="280" height="160"><rect width="280" height="160" fill="#0078d4"/><circle cx="70" cy="55" r="28" fill="#50a0e0"/><path d="M0 160 90 80l60 50 50-36 80 66z" fill="#004c8c"/><text x="14" y="146" font-family="monospace" font-size="13" fill="#fff">playblazor.svg</text></svg>"""))
            .Parameter(nameof(FluentImage.AlternateText), "Sample image")
            .Parameter(nameof(FluentImage.Width), "280px")
            .Variant("Rounded, bordered", v => v.Set(nameof(FluentImage.Shape), ImageShape.Rounded).Set(nameof(FluentImage.Bordered), true))
            .Variant("Cover, shadow", v => v.Set(nameof(FluentImage.Fit), ImageFit.Cover).Set(nameof(FluentImage.Shadow), true));

        options.For<FluentSkeleton>()
            .Parameter(nameof(FluentSkeleton.Width), "220px")
            .Variant("Circle", v => v.Set(nameof(FluentSkeleton.Circular), true).Set(nameof(FluentSkeleton.Width), "48px").Set(nameof(FluentSkeleton.Height), "48px"))
            .Variant("Icon + title", v => v.Set(nameof(FluentSkeleton.Pattern), SkeletonPattern.IconTitle))
            .Variant("No shimmer", v => v.Set(nameof(FluentSkeleton.Shimmer), false));

        // FluentProgress is [Obsolete] (renamed to FluentProgressBar) but still a real,
        // documented, instantiable component — presets live on their own type, since variant and
        // preset storage is keyed by exact Type, not walked up the inheritance chain. The
        // pragma is scoped to this block only; TreatWarningsAsErrors would otherwise fail the
        // build on the very obsolescence this task's brief asks us to curate around.
#pragma warning disable CS0618
        options.For<FluentProgress>()
            .Parameter(nameof(FluentProgress.Value), 65)
            .Variant("Indeterminate", v => v.Set(nameof(FluentProgress.Value), null))
            .Variant("Success", v => v.Set(nameof(FluentProgress.State), ProgressState.Success));
#pragma warning restore CS0618

        options.For<FluentProgressBar>()
            .Parameter(nameof(FluentProgressBar.Value), 65)
            .Variant("Indeterminate", v => v.Set(nameof(FluentProgressBar.Value), null))
            .Variant("Error state", v => v.Set(nameof(FluentProgressBar.State), ProgressState.Error))
            .Variant("Large, square", v => v.Set(nameof(FluentProgressBar.Thickness), ProgressThickness.Large).Set(nameof(FluentProgressBar.Shape), ProgressShape.Square));

        // FluentProgressRing is likewise [Obsolete] (renamed to FluentSpinner) but still real;
        // it already shows a spinning ring with its own defaults, so only variants are needed.
#pragma warning disable CS0618
        options.For<FluentProgressRing>()
            .Variant("Large", v => v.Set(nameof(FluentProgressRing.Size), SpinnerSize.Large))
            .Variant("Tiny", v => v.Set(nameof(FluentProgressRing.Size), SpinnerSize.Tiny))
            .Variant("Inverted", v => v.Set(nameof(FluentProgressRing.AppearanceInverted), true));
#pragma warning restore CS0618

        options.For<FluentSpinner>()
            .Variant("Large", v => v.Set(nameof(FluentSpinner.Size), SpinnerSize.Large))
            .Variant("Tiny", v => v.Set(nameof(FluentSpinner.Size), SpinnerSize.Tiny))
            .Variant("Inverted", v => v.Set(nameof(FluentSpinner.AppearanceInverted), true));

        options.For<FluentRatingDisplay>()
            .Parameter(nameof(FluentRatingDisplay.Value), 3.5)
            .Parameter(nameof(FluentRatingDisplay.Max), (byte)5)
            .Parameter(nameof(FluentRatingDisplay.Count), 128.0)
            .Variant("Compact", v => v.Set(nameof(FluentRatingDisplay.Compact), true))
            .Variant("Large, brand", v => v.Set(nameof(FluentRatingDisplay.Size), RatingSize.Large).Set(nameof(FluentRatingDisplay.Color), RatingDisplayColor.Brand));

        options.For<FluentMultiSplitter>()
            .Slot(nameof(FluentMultiSplitter.ChildContent), FluentDemoFragments.SplitterPanes, FluentDemoFragmentSources.SplitterPanes)
            .Parameter(nameof(FluentMultiSplitter.Height), "200px")
            .Variant("Vertical", v => v.Set(nameof(FluentMultiSplitter.Orientation), Orientation.Vertical))
            // BarSize feeds --fluent-multi-splitter-bar-size, consumed by the shipped
            // bundle.scp.css directly as `width:var(--fluent-multi-splitter-bar-size)` with no
            // calc()/unit wrapper (its own default is `var(--spacingVerticalS)`, a real length
            // token) — a bare number is an invalid CSS length and the property is dropped, so
            // this needs an explicit unit.
            .Variant("Thick bar", v => v.Set(nameof(FluentMultiSplitter.BarSize), "12px"));

        // FluentMultiSplitterPane's [CascadingParameter] Splitter is nullable and null-checked
        // throughout (Next(), IsLast, IsResizable, …), so it renders alone without throwing — but
        // Scaffold gives it a real sibling pane so the split itself is visible on the bench.
        options.For<FluentMultiSplitterPane>()
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<FluentMultiSplitter>(0);
                builder.AddAttribute(1, nameof(FluentMultiSplitter.Height), "200px");
                builder.AddAttribute(2, nameof(FluentMultiSplitter.ChildContent), (RenderFragment)(paneBuilder =>
                {
                    paneBuilder.AddContent(0, specimen);
                    paneBuilder.OpenComponent<FluentMultiSplitterPane>(1);
                    paneBuilder.AddAttribute(2, nameof(FluentMultiSplitterPane.ChildContent), (RenderFragment)(b => b.AddContent(0, "Other pane")));
                    paneBuilder.CloseComponent();
                }));
                builder.CloseComponent();
            },
            """
            <FluentMultiSplitter Height="200px">
                {specimen}
                <FluentMultiSplitterPane>Other pane</FluentMultiSplitterPane>
            </FluentMultiSplitter>
            """)
            .Related<FluentMultiSplitter>()
            .Slot(nameof(FluentMultiSplitterPane.ChildContent), b => b.AddContent(0, "Pane content"), "Pane content")
            .Parameter(nameof(FluentMultiSplitterPane.Size), "50%")
            .Variant("Collapsible", v => v.Set(nameof(FluentMultiSplitterPane.Collapsible), true))
            .Variant("Fixed 200px", v => v.Set(nameof(FluentMultiSplitterPane.Size), "200px"));

        // --- Navigation and overlays: nav/tabs/menu/accordion/tree/wizard/app-bar structure, plus
        // the dialog/popover/tooltip/toast/message family. Verified against the pinned
        // 5.0.0-rc.5 assembly with ilspycmd rather than assumed. FluentNavItem, FluentNavCategory,
        // FluentNavSectionHeader, FluentWizardStep and FluentAppBarItem were already scaffolded by
        // Task 3 (see their chains above, under the earlier "Scaffolds" comment) — their presets
        // were added to those SAME chains rather than opened again here.

        options.For<FluentNav>()
            .Slot(nameof(FluentNav.ChildContent), FluentDemoFragments.NavContent, FluentDemoFragmentSources.NavContent)
            .Parameter(nameof(FluentNav.Width), "240px")
            .Variant("Single expanded category", v => v.Set(nameof(FluentNav.UseSingleExpanded), true))
            .Variant("Compact density", v => v.Set(nameof(FluentNav.Density), NavDensity.Small))
            .Variant("No icons", v => v.Set(nameof(FluentNav.UseIcons), false));

        options.For<FluentTabs>()
            .Slot(nameof(FluentTabs.ChildContent), FluentDemoFragments.TabsContent, FluentDemoFragmentSources.TabsContent)
            .Variant("Vertical", v => v.Set(nameof(FluentTabs.Orientation), Orientation.Vertical))
            .Variant("Subtle appearance", v => v.Set(nameof(FluentTabs.Appearance), TabsAppearance.Subtle))
            .Variant("Small", v => v.Set(nameof(FluentTabs.Size), TabsSize.Small));

        // FluentTab never reads its own ChildContent in BuildRenderTree — the active tab's panel
        // is drawn by the OWNING FluentTabs instead (see FluentTabs.BuildRenderTree, which walks
        // its registered Tabs and emits `item.ChildContent` itself). A ChildContent Slot on the
        // standalone specimen would register and compile but never render anything different —
        // exactly the silent-nothing trap the brief warns about — so Header is the preset here,
        // since it IS what a bare FluentTab actually draws.
        options.For<FluentTab>()
            .Parameter(nameof(FluentTab.Header), "Home")
            .Variant("With icon", v => v.Set(nameof(FluentTab.IconStart), FluentIconCatalogue.All["Circle"]))
            .Variant("Disabled", v => v.Set(nameof(FluentTab.Disabled), true))
            // DeferredLoading only affects whether the OWNING FluentTabs renders this tab's
            // panel — this standalone specimen has no owner (no scaffold), so the flag has
            // nothing to act on and the chip would be indistinguishable from the base preset.
            // HeaderStyle is a real, visible difference on the bare <fluent-tab> itself.
            .Variant("Styled header", v => v.Set(nameof(FluentTab.HeaderStyle), "color: var(--colorPaletteRedForeground1); font-weight: 600;"));

        // FluentMenu has no Open/Visible parameter in v5: OpenOnHover/OpenOnContext only pick
        // WHICH interaction opens it, and Trigger just names an external anchor id — none forces
        // the initial render open, and the underlying <fluent-menu> web component sets no "open"
        // attribute of its own. No preset can make the dropdown itself visible on a static bench.
        // The trigger-inside-ChildContent shape below (a FluentMenuButton alongside the items,
        // both inside FluentMenu.ChildContent) is the real v5 idiom — confirmed by decompiling
        // FluentSplitButton.BuildRenderTree, which composes the identical slot="trigger" shape —
        // so the bench at least shows a real, clickable trigger instead of nothing.
        options.For<FluentMenu>()
            .Slot(nameof(FluentMenu.ChildContent), FluentDemoFragments.MenuContent, FluentDemoFragmentSources.MenuContent)
            .Variant("Open on hover", v => v.Set(nameof(FluentMenu.OpenOnHover), true))
            .Variant("Context menu", v => v.Set(nameof(FluentMenu.OpenOnContext), true))
            .Variant("Bounded height", v => v.Set(nameof(FluentMenu.Height), "200px"));

        options.For<FluentMenuItem>()
            .Parameter(nameof(FluentMenuItem.Label), "Cut")
            .Variant("With icon", v => v.Set(nameof(FluentMenuItem.IconStart), FluentIconCatalogue.All["Delete"]))
            .Variant("Checkbox", v => v.Set(nameof(FluentMenuItem.Role), MenuItemRole.Checkbox).Set(nameof(FluentMenuItem.Checked), true))
            .Variant("Disabled", v => v.Set(nameof(FluentMenuItem.Disabled), true));

        // Unlike FluentMenu, <fluent-menu-list> renders unconditionally visible — it has no
        // open/closed gating at all, so the Slot alone already shows the bench something real.
        // Its own real parameter surface is otherwise just ChildContent plus two click callbacks,
        // with no scalar axis to build a Variant from.
        options.For<FluentMenuList>()
            .Slot(nameof(FluentMenuList.ChildContent), FluentDemoFragments.MenuListItems, FluentDemoFragmentSources.MenuListItems);

        options.For<FluentMenuButton>()
            .Slot(nameof(FluentMenuButton.ChildContent), b => b.AddContent(0, "More options"), "More options")
            .Variant("Primary", v => v.Set(nameof(FluentMenuButton.Appearance), ButtonAppearance.Primary))
            .Variant("Icon only", v => v.Set(nameof(FluentMenuButton.IconOnly), true).Set(nameof(FluentMenuButton.IconStart), FluentIconCatalogue.All["Settings"]))
            .Variant("Disabled", v => v.Set(nameof(FluentMenuButton.Disabled), true));

        options.For<FluentSplitButton>()
            .Slot(nameof(FluentSplitButton.ChildContent), FluentDemoFragments.SplitButtonItems, FluentDemoFragmentSources.SplitButtonItems)
            .Parameter(nameof(FluentSplitButton.Label), "Save")
            .Variant("Primary", v => v.Set(nameof(FluentSplitButton.Appearance), ButtonAppearance.Primary))
            .Variant("With icon", v => v.Set(nameof(FluentSplitButton.IconStart), FluentIconCatalogue.All["Checkmark"]))
            .Variant("Small", v => v.Set(nameof(FluentSplitButton.Size), ButtonSize.Small));

        options.For<FluentToggleButton>()
            .Slot(nameof(FluentToggleButton.ChildContent), b => b.AddContent(0, "Bold"), "Bold")
            .Variant("Pressed", v => v.Set(nameof(FluentToggleButton.Pressed), true))
            .Variant("Mixed", v => v.Set(nameof(FluentToggleButton.Mixed), true))
            .Variant("Primary", v => v.Set(nameof(FluentToggleButton.Appearance), ButtonAppearance.Primary));

        options.For<FluentCompoundButton>()
            .Slot(nameof(FluentCompoundButton.Description), b => b.AddContent(0, "Choose how notifications reach you."), "Choose how notifications reach you.")
            .Parameter(nameof(FluentCompoundButton.Label), "Notification settings")
            .Variant("With icon", v => v.Set(nameof(FluentCompoundButton.IconStart), FluentIconCatalogue.All["Settings"]))
            .Variant("Primary", v => v.Set(nameof(FluentCompoundButton.Appearance), ButtonAppearance.Primary))
            .Variant("Disabled", v => v.Set(nameof(FluentCompoundButton.Disabled), true));

        options.For<FluentAnchorButton>()
            .Slot(nameof(FluentAnchorButton.ChildContent), b => b.AddContent(0, "Download"), "Download")
            .Parameter(nameof(FluentAnchorButton.Href), "https://learn.microsoft.com/fluentui-blazor")
            .Variant("Primary", v => v.Set(nameof(FluentAnchorButton.Appearance), ButtonAppearance.Primary))
            .Variant("Open in new tab", v => v.Set(nameof(FluentAnchorButton.Target), LinkTarget.Blank))
            .Variant("With icon", v => v.Set(nameof(FluentAnchorButton.IconStart), FluentIconCatalogue.All["ArrowDown"]));

        options.For<FluentLink>()
            .Slot(nameof(FluentLink.ChildContent), b => b.AddContent(0, "Learn more"), "Learn more")
            .Parameter(nameof(FluentLink.Href), "https://learn.microsoft.com/fluentui-blazor")
            .Variant("Subtle", v => v.Set(nameof(FluentLink.Appearance), LinkAppearance.Subtle))
            .Variant("Inline", v => v.Set(nameof(FluentLink.Inline), true))
            .Variant("With icon", v => v.Set(nameof(FluentLink.IconEnd), FluentIconCatalogue.All["ArrowRight"]));

        options.For<FluentAccordion>()
            .Slot(nameof(FluentAccordion.ChildContent), FluentDemoFragments.AccordionItems, FluentDemoFragmentSources.AccordionItems)
            // ExpandMode defaults to null on the C# side, but the web component's own
            // connectedCallback falls back to "multi" when unset — Multi is therefore already
            // the default and would render identically to the un-varied specimen. Single is the
            // value that actually differs.
            .Variant("Single expanded", v => v.Set(nameof(FluentAccordion.ExpandMode), AccordionExpandMode.Single))
            .Variant("Block", v => v.Set(nameof(FluentAccordion.Block), true))
            .Variant("Marker at the end", v => v.Set(nameof(FluentAccordion.MarkerPosition), AccordionItemMarkerPosition.End));

        options.For<FluentAccordionItem>()
            .Slot(nameof(FluentAccordionItem.ChildContent), b => b.AddContent(0, "Ships within 2 business days via standard courier."), "Ships within 2 business days via standard courier.")
            .Parameter(nameof(FluentAccordionItem.Header), "Shipping details")
            // Expanded defaults to false; without this the standalone bench shows only a header.
            .Parameter(nameof(FluentAccordionItem.Expanded), true)
            .Variant("Collapsed", v => v.Set(nameof(FluentAccordionItem.Expanded), false))
            .Variant("Disabled", v => v.Set(nameof(FluentAccordionItem.Disabled), true))
            .Variant("Large", v => v.Set(nameof(FluentAccordionItem.Size), AccordionItemSize.Large));

        // FluentDialog has no Open/Visible parameter in v5 either — it only appears via
        // ShowAsync() or the DialogService, both a JS round-trip after render, so no preset can
        // make it visible on the static bench (confirmed: BuildRenderTree sets no "open"
        // attribute on the underlying <fluent-dialog>/<fluent-drawer> element at all). The best
        // available curation is realistic declarative content — a real FluentDialogBody, ready to
        // look right the moment a real user calls ShowAsync().
        options.For<FluentDialog>()
            .Slot(nameof(FluentDialog.ChildContent), FluentDemoFragments.DialogContent, FluentDemoFragmentSources.DialogContent)
            .Variant("Non-modal", v => v.Set(nameof(FluentDialog.Modal), false))
            .Variant("Drawer, from the end", v => v.Set(nameof(FluentDialog.Alignment), DialogAlignment.End))
            .Variant("Prevent Escape dismiss", v => v.Set(nameof(FluentDialog.PreventDismissOnEscape), true));

        // Unlike FluentDialog, FluentDialogBody renders its ChildContent in a plain <div> with no
        // dependency on a cascading IDialogInstance (null-checked throughout) — it looks right
        // standalone, outside any FluentDialog wrapper. Scaffolding it INSIDE a FluentDialog would
        // make it invisible again, since the wrapper itself never opens (see FluentDialog above).
        // FixedHeaderFooter is its only real non-slot parameter, so one variant is honest rather
        // than padded.
        options.For<FluentDialogBody>()
            .Slot(nameof(FluentDialogBody.TitleTemplate), b => b.AddContent(0, "Delete file?"), "Delete file?")
            .Slot(nameof(FluentDialogBody.ChildContent), b => b.AddContent(0, "This action permanently deletes the selected file. It cannot be undone."), "This action permanently deletes the selected file. It cannot be undone.")
            .Variant("Scrollable content", v => v.Set(nameof(FluentDialogBody.FixedHeaderFooter), false));

        // FluentMessageBox.Message is a MarkupStringSanitized? — every one of its constructors is
        // `internal` to the library assembly, so no value can be constructed from this demo
        // project at all; Icon defaults to a real CheckmarkCircle and needs no override (Icon
        // itself is `internal static class CoreIcons`, likewise unreachable here). IconColor is
        // the one publicly settable axis that still varies the specimen meaningfully.
        options.For<FluentMessageBox>()
            .Variant("Warning", v => v.Set(nameof(FluentMessageBox.IconColor), Color.Warning))
            .Variant("Error", v => v.Set(nameof(FluentMessageBox.IconColor), Color.Error))
            .Variant("Info", v => v.Set(nameof(FluentMessageBox.IconColor), Color.Info));

        options.For<FluentMessageBar>()
            .Parameter(nameof(FluentMessageBar.Title), "Action required")
            .Slot(nameof(FluentMessageBar.ChildContent), b => b.AddContent(0, "Your subscription expires in 3 days."), "Your subscription expires in 3 days.")
            .Variant("Warning", v => v.Set(nameof(FluentMessageBar.Intent), MessageBarIntent.Warning))
            .Variant("Error", v => v.Set(nameof(FluentMessageBar.Intent), MessageBarIntent.Error))
            .Variant("Notification layout", v => v.Set(nameof(FluentMessageBar.Layout), MessageBarLayout.Notification));

        // FluentToast.Opened IS a real [Parameter] bool, reflected straight onto the "opened"
        // attribute of <fluent-toast-b> — unlike FluentMenu/FluentTooltip/FluentDialog, this one
        // genuinely can be forced visible.
        options.For<FluentToast>()
            .Parameter(nameof(FluentToast.Opened), true)
            .Parameter(nameof(FluentToast.Title), "File saved")
            .Slot(nameof(FluentToast.ChildContent), b => b.AddContent(0, "Your changes have been saved."), "Your changes have been saved.")
            .Variant("Success", v => v.Set(nameof(FluentToast.Intent), ToastIntent.Success))
            .Variant("Error", v => v.Set(nameof(FluentToast.Intent), ToastIntent.Error))
            .Variant("Progress", v => v.Set(nameof(FluentToast.Intent), ToastIntent.Progress));

        // FluentTooltip has no Open/Visible parameter in v5 — it only appears on hover of its
        // Anchor element (BuildRenderTree sets no "open" state at all), so no preset forces it
        // open on the static bench; the Scaffold at least anchors it to a real, hoverable button
        // instead of an id that resolves to nothing. UseTooltipService=false makes the specimen
        // render inline at its own position instead of being registered with ITooltipService and
        // drawn elsewhere by <FluentTooltipProvider/> (documented as a valid use: "Set this to
        // false when ChildContent is dynamic").
        //
        // Sweep finding: the demo's MainLayout renders <FluentProviders/>, and decompiling
        // FluentProviders.BuildRenderTree shows it DOES compose <FluentTooltipProvider/> (along
        // with FluentDialogProvider, FluentToastProvider, FluentKeyCodeProvider) — so the running
        // demo app already satisfies FluentTooltip's provider requirement. The sweep's "<FluentTo
        // oltipProvider/> needs to be added to the main layout" warning is a bUnit RenderSweep
        // harness artifact: that suite renders the specimen without MainLayout, so no provider is
        // ever present there. No provider setup was needed in the demo.
        options.For<FluentTooltip>()
            .Scaffold(specimen => builder =>
            {
                builder.OpenElement(0, "div");
                builder.OpenComponent<FluentButton>(1);
                builder.AddComponentParameter(2, nameof(FluentButton.Id), "fluentui-demo-tooltip-anchor");
                builder.AddComponentParameter(3, nameof(FluentButton.ChildContent), (RenderFragment)(b => b.AddContent(0, "Hover me")));
                builder.CloseComponent();
                builder.AddContent(4, specimen);
                builder.CloseElement();
            },
            """
            <div>
                <FluentButton Id="fluentui-demo-tooltip-anchor">Hover me</FluentButton>
                {specimen}
            </div>
            """)
            .Parameter(nameof(FluentTooltip.Anchor), "fluentui-demo-tooltip-anchor")
            .Parameter(nameof(FluentTooltip.UseTooltipService), false)
            .Slot(nameof(FluentTooltip.ChildContent), b => b.AddContent(0, "A helpful hint"), "A helpful hint")
            // Positioning is null by default, and the shipped bundle's JS fallback
            // (`n[o] ?? n.above`) resolves an UNSET positioning to "above" — so an explicit
            // Above variant risks reproducing the default box exactly. After is unambiguous.
            .Variant("After", v => v.Set(nameof(FluentTooltip.Positioning), Positioning.After))
            .Variant("Below", v => v.Set(nameof(FluentTooltip.Positioning), Positioning.Below))
            .Variant("Long delay", v => v.Set(nameof(FluentTooltip.Delay), 1000));

        // FluentPopover.AnchorId is `required` at compile time only — Blazor's runtime parameter
        // binding never enforces `required`, and BuildRenderTree never null-checks it either, so
        // it renders without throwing even unset; it just floats unanchored. Opened IS a real
        // [Parameter] bool though (reflected onto the "opened" attribute), so — unlike
        // FluentMenu/FluentTooltip — this one genuinely can be forced visible; the Scaffold gives
        // it a real anchor element to visibly point at.
        options.For<FluentPopover>()
            .Scaffold(specimen => builder =>
            {
                builder.OpenElement(0, "div");
                builder.OpenComponent<FluentButton>(1);
                builder.AddComponentParameter(2, nameof(FluentButton.Id), "fluentui-demo-popover-anchor");
                builder.AddComponentParameter(3, nameof(FluentButton.ChildContent), (RenderFragment)(b => b.AddContent(0, "Show details")));
                builder.CloseComponent();
                builder.AddContent(4, specimen);
                builder.CloseElement();
            },
            """
            <div>
                <FluentButton Id="fluentui-demo-popover-anchor">Show details</FluentButton>
                {specimen}
            </div>
            """)
            .Parameter(nameof(FluentPopover.AnchorId), "fluentui-demo-popover-anchor")
            .Parameter(nameof(FluentPopover.Opened), true)
            .Slot(nameof(FluentPopover.ChildContent), FluentDemoFragments.PopoverBody, FluentDemoFragmentSources.PopoverBody)
            .Variant("Wide", v => v.Set(nameof(FluentPopover.Width), "320px"))
            .Variant("With offset", v => v.Set(nameof(FluentPopover.OffsetVertical), 16).Set(nameof(FluentPopover.OffsetHorizontal), 16))
            .Variant("Nested", v => v.Set(nameof(FluentPopover.Nested), true));

        // FluentOverlay.Visible IS a real [Parameter] bool (reflected onto the "visible"
        // attribute) — unlike the anchor-driven overlay family above, this one is straightforward.
        options.For<FluentOverlay>()
            .Parameter(nameof(FluentOverlay.Visible), true)
            .Slot(nameof(FluentOverlay.ChildContent), b => b.AddContent(0, "Saving changes..."), "Saving changes...")
            .Variant("Interactive", v => v.Set(nameof(FluentOverlay.Interactive), true))
            .Variant("Full screen", v => v.Set(nameof(FluentOverlay.FullScreen), true))
            .Variant("Low opacity", v => v.Set(nameof(FluentOverlay.Opacity), 15));

        options.For<FluentWizard>()
            .Slot(nameof(FluentWizard.Steps), FluentDemoFragments.WizardSteps, FluentDemoFragmentSources.WizardSteps)
            .Variant("Stepper on top", v => v.Set(nameof(FluentWizard.StepperPosition), StepperPosition.Top))
            .Variant("Outside border", v => v.Set(nameof(FluentWizard.Border), WizardBorder.Outside))
            .Variant("Free navigation", v => v.Set(nameof(FluentWizard.StepSequence), WizardStepSequence.Any));

        options.For<FluentTreeView>()
            .Slot(nameof(FluentTreeView.ChildContent), FluentDemoFragments.TreeViewItems, FluentDemoFragmentSources.TreeViewItems)
            .Variant("Small", v => v.Set(nameof(FluentTreeView.Size), TreeSize.Small))
            .Variant("Transparent", v => v.Set(nameof(FluentTreeView.Appearance), TreeAppearance.Transparent))
            .Variant("Hide selection", v => v.Set(nameof(FluentTreeView.HideSelection), true));

        options.For<FluentTreeItem>()
            .Slot(nameof(FluentTreeItem.ChildContent), FluentDemoFragments.TreeItemChildren, FluentDemoFragmentSources.TreeItemChildren)
            .Parameter(nameof(FluentTreeItem.Text), "Documents")
            // Without this the nested children Slot above has nothing to reveal on the bench.
            .Parameter(nameof(FluentTreeItem.Expanded), true)
            .Variant("With icon", v => v.Set(nameof(FluentTreeItem.IconStart), FluentIconCatalogue.All["Star"]))
            .Variant("Collapsed", v => v.Set(nameof(FluentTreeItem.Expanded), false))
            .Variant("Custom height", v => v.Set(nameof(FluentTreeItem.Height), "40px"));

        options.For<FluentAppBar>()
            .Slot(nameof(FluentAppBar.ChildContent), FluentDemoFragments.AppBarItems, FluentDemoFragmentSources.AppBarItems)
            .Variant("Horizontal", v => v.Set(nameof(FluentAppBar.Orientation), Orientation.Horizontal))
            .Variant("No search in overflow", v => v.Set(nameof(FluentAppBar.PopoverShowSearch), false))
            .Variant("Hide active indicator", v => v.Set(nameof(FluentAppBar.HideActiveIndicator), true));
    }

    // StripArity is duplicated from the MudBlazor config on purpose: the two apps share no code
    // by design, and a shared helper would need a home in Demo.Shared, which must stay free of
    // library knowledge.
    private static string StripArity(string name)
    {
        var backtick = name.IndexOf('`');
        return backtick < 0 ? name : name[..backtick];
    }

    // Mirrors MudBlazor's own AddColumn helper: PropertyColumn.Property is an Expression, not a
    // RenderFragment, so building FluentDataGrid<Person>'s columns is plain C# rather than markup.
    private static void AddColumn<TProperty>(
        Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder columns,
        int sequence,
        Expression<Func<Person, TProperty>> property)
    {
        columns.OpenComponent<PropertyColumn<Person, TProperty>>(sequence);
        columns.AddComponentParameter(sequence + 1, nameof(PropertyColumn<Person, TProperty>.Property), property);
        columns.CloseComponent();
    }
}
