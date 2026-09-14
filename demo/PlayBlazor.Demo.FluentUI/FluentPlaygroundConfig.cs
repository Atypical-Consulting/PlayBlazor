using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FluentUI.AspNetCore.Components;
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
        options.For<FluentCalendar<DateTime?>>();
        options.For<FluentDatePicker<DateTime?>>();
        options.For<FluentTimePicker<DateTime?>>();
        options.For<FluentNumberInput<int>>();

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
            .Related<FluentAppBar>();

        options.For<FluentNavItem>()
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<FluentNav>(0);
                builder.AddAttribute(1, nameof(FluentNav.ChildContent), specimen);
                builder.CloseComponent();
            },
            "<FluentNav>\n    {specimen}\n</FluentNav>")
            .Related<FluentNav>();

        options.For<FluentNavCategory>()
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<FluentNav>(0);
                builder.AddAttribute(1, nameof(FluentNav.ChildContent), specimen);
                builder.CloseComponent();
            },
            "<FluentNav>\n    {specimen}\n</FluentNav>")
            .Related<FluentNav>();

        options.For<FluentNavSectionHeader>()
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<FluentNav>(0);
                builder.AddAttribute(1, nameof(FluentNav.ChildContent), specimen);
                builder.CloseComponent();
            },
            "<FluentNav>\n    {specimen}\n</FluentNav>")
            .Related<FluentNav>();

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
            .Related<FluentRadioGroup<string>>();

        // FluentWizard takes its steps through a "Steps" fragment, not ChildContent.
        options.For<FluentWizardStep>()
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<FluentWizard>(0);
                builder.AddAttribute(1, nameof(FluentWizard.Steps), specimen);
                builder.CloseComponent();
            },
            "<FluentWizard>\n    <Steps>\n        {specimen}\n    </Steps>\n</FluentWizard>")
            .Related<FluentWizard>();

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
            .Related<FluentDataGrid<Person>>();

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
            .Related<FluentDataGridRow<Person>>();

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
            .Related<FluentDataGrid<Person>>();

        options.For<SelectColumn<Person>>()
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<FluentDataGrid<Person>>(0);
                builder.AddAttribute(1, nameof(FluentDataGrid<Person>.Items), SampleRows.AsQueryable());
                builder.AddAttribute(2, nameof(FluentDataGrid<Person>.ChildContent), specimen);
                builder.CloseComponent();
            },
            "<FluentDataGrid Items=\"@_people\">\n    {specimen}\n</FluentDataGrid>")
            .Related<FluentDataGrid<Person>>();

        options.For<TemplateColumn<Person>>()
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<FluentDataGrid<Person>>(0);
                builder.AddAttribute(1, nameof(FluentDataGrid<Person>.Items), SampleRows.AsQueryable());
                builder.AddAttribute(2, nameof(FluentDataGrid<Person>.ChildContent), specimen);
                builder.CloseComponent();
            },
            "<FluentDataGrid Items=\"@_people\">\n    {specimen}\n</FluentDataGrid>")
            .Related<FluentDataGrid<Person>>();

        options.For<HierarchicalSelectColumn<Person>>()
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<FluentDataGrid<Person>>(0);
                builder.AddAttribute(1, nameof(FluentDataGrid<Person>.Items), SampleRows.AsQueryable());
                builder.AddAttribute(2, nameof(FluentDataGrid<Person>.ChildContent), specimen);
                builder.CloseComponent();
            },
            "<FluentDataGrid Items=\"@_people\">\n    {specimen}\n</FluentDataGrid>")
            .Related<FluentDataGrid<Person>>();

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
            .Slot(nameof(FluentKeyCode.ChildContent), b => b.AddContent(0, "Press any key"), "Press any key");

        // FluentPaginator's sweep failure is not a missing parent — it wants a PaginationState
        // instance, so it gets a Parameter preset rather than a Scaffold.
        options.For<FluentPaginator>()
            .Parameter(nameof(FluentPaginator.State), new PaginationState(), "@_paginationState");
    }

    // StripArity is duplicated from the MudBlazor config on purpose: the two apps share no code
    // by design, and a shared helper would need a home in Demo.Shared, which must stay free of
    // library knowledge.
    private static string StripArity(string name)
    {
        var backtick = name.IndexOf('`');
        return backtick < 0 ? name : name[..backtick];
    }
}
