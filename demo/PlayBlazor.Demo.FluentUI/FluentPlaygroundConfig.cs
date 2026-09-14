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
