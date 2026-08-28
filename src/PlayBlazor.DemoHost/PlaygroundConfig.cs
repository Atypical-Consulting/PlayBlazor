using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace PlayBlazor.DemoHost;

/// <summary>
/// Curates the explorer to MudBlazor's documented surface (the components on
/// https://mudblazor.com/docs/overview) and seeds each one with defaults and a few
/// variants taken from the official examples.
/// </summary>
public static class PlaygroundConfig
{
    /// <summary>The documented component surface — parts (items, panels, columns) play inside their parents.</summary>
    private static readonly HashSet<string> Curated =
    [
        "MudAlert", "MudAppBar", "MudAutocomplete", "MudAvatar", "MudAvatarGroup", "MudBadge",
        "MudBreadcrumbs", "MudButton", "MudButtonGroup", "MudCard", "MudCarousel", "MudChart",
        "MudCheckBox", "MudChip", "MudChipSet", "MudColorPicker", "MudContainer", "MudDataGrid",
        "MudDatePicker", "MudDateRangePicker", "MudDivider", "MudDrawer", "MudElement",
        "MudExpansionPanels", "MudFab", "MudField", "MudFileUpload", "MudForm", "MudGrid",
        "MudHidden", "MudHighlighter", "MudIcon", "MudIconButton", "MudImage", "MudLink",
        "MudList", "MudMenu", "MudNavMenu", "MudNumericField", "MudOverlay", "MudPagination",
        "MudPaper", "MudPopover", "MudProgressCircular", "MudProgressLinear", "MudRadioGroup",
        "MudRating", "MudScrollToTop", "MudSelect", "MudSimpleTable", "MudSkeleton", "MudSlider",
        "MudStack", "MudStepper", "MudSwitch", "MudTable", "MudTabs", "MudText", "MudTextField",
        "MudTimePicker", "MudTimeline", "MudToggleGroup", "MudToolBar", "MudTooltip", "MudTreeView",
    ];

    private static readonly List<ChartSeries<int>> SampleSeries =
    [
        new([40, 20, 25, 27, 46]) { Name = "United States" },
        new([19, 24, 35, 13, 28]) { Name = "Germany" },
        new([8, 6, 11, 13, 4]) { Name = "Sweden" },
    ];

    private static string StripArity(string name)
    {
        var backtick = name.IndexOf('`');
        return backtick < 0 ? name : name[..backtick];
    }

    public static void Configure(PlayBlazorOptions options)
    {
        options.ComponentFilter = type => Curated.Contains(StripArity(type.Name));
        options.IconResolver = ComponentIcons.Resolve;

        // Discovery closes T with string, which MudFileUpload warns about at render time —
        // declare the closing it actually supports.
        options.For<MudFileUpload<IBrowserFile>>();

        options.For<MudAlert>()
            .Slot(nameof(MudAlert.ChildContent), b => b.AddContent(0, "The reactor is running at optimum temperature."), "The reactor is running at optimum temperature.")
            .Variant("Info", v => v.Set(nameof(MudAlert.Severity), Severity.Info))
            .Variant("Success", v => v.Set(nameof(MudAlert.Severity), Severity.Success))
            .Variant("Warning", v => v.Set(nameof(MudAlert.Severity), Severity.Warning))
            .Variant("Outlined error", v => v.Set(nameof(MudAlert.Severity), Severity.Error).Set(nameof(MudAlert.Variant), Variant.Outlined));

        options.For<MudButton>()
            .Slot(nameof(MudButton.ChildContent), b => b.AddContent(0, "Click me"), "Click me")
            .Variant("Filled primary", v => v.Set(nameof(MudButton.Variant), Variant.Filled).Set(nameof(MudButton.Color), Color.Primary))
            .Variant("Outlined", v => v.Set(nameof(MudButton.Variant), Variant.Outlined).Set(nameof(MudButton.Color), Color.Secondary))
            .Variant("Text", v => v.Set(nameof(MudButton.Variant), Variant.Text))
            .Variant("Disabled", v => v.Set(nameof(MudButton.Disabled), true));

        options.For<MudIconButton>()
            .Parameter(nameof(MudIconButton.Icon), Icons.Material.Filled.Delete)
            .Variant("Favorite", v => v.Set(nameof(MudIconButton.Icon), Icons.Material.Filled.Favorite).Set(nameof(MudIconButton.Color), Color.Error))
            .Variant("Filled", v => v.Set(nameof(MudIconButton.Variant), Variant.Filled).Set(nameof(MudIconButton.Color), Color.Primary));

        options.For<MudFab>()
            .Parameter(nameof(MudFab.StartIcon), Icons.Material.Filled.Add)
            .Parameter(nameof(MudFab.Color), Color.Primary)
            .Variant("Extended", v => v.Set(nameof(MudFab.Label), "Create").Set(nameof(MudFab.Color), Color.Secondary));

        options.For<MudButtonGroup>()
            .Slot(nameof(MudButtonGroup.ChildContent), DemoFragments.ButtonGroupButtons, DemoFragmentSources.ButtonGroupButtons)
            .Variant("Outlined primary", v => v.Set(nameof(MudButtonGroup.Variant), Variant.Outlined).Set(nameof(MudButtonGroup.Color), Color.Primary))
            .Variant("Text", v => v.Set(nameof(MudButtonGroup.Variant), Variant.Text));

        options.For<MudAvatar>()
            .Slot("ChildContent", b => b.AddContent(0, "PB"), "PB")
            .Parameter(nameof(MudAvatar.Color), Color.Primary)
            .Variant("Rounded", v => v.Set(nameof(MudAvatar.Rounded), true).Set(nameof(MudAvatar.Color), Color.Secondary))
            .Variant("Large", v => v.Set(nameof(MudAvatar.Size), Size.Large));

        options.For<MudAvatarGroup>()
            .Slot("ChildContent", DemoFragments.Avatars, DemoFragmentSources.Avatars)
            .Variant("Max 2", v => v.Set(nameof(MudAvatarGroup.Max), 2));

        options.For<MudBadge>()
            .Slot("ChildContent", DemoFragments.BadgeChild, DemoFragmentSources.BadgeChild)
            .Parameter(nameof(MudBadge.Content), 4)
            .Parameter(nameof(MudBadge.Color), Color.Primary)
            .Parameter(nameof(MudBadge.Overlap), true)
            .Variant("Dot", v => v.Set(nameof(MudBadge.Dot), true).Set(nameof(MudBadge.Color), Color.Error));

        options.For<MudBreadcrumbs>()
            .Parameter(nameof(MudBreadcrumbs.Items), new List<BreadcrumbItem>
            {
                new("Home", href: "#"),
                new("Components", href: "#"),
                new("Breadcrumbs", href: null, disabled: true),
            });

        options.For<MudCard>()
            .Slot("ChildContent", DemoFragments.CardBody, DemoFragmentSources.CardBody)
            .Variant("Elevated", v => v.Set(nameof(MudCard.Elevation), 8))
            .Variant("Outlined", v => v.Set(nameof(MudCard.Outlined), true));

        // The explorer closes MudChart<T> (T : INumber) with int — presets target that closing.
        options.For<MudChart<int>>()
            .Parameter("ChartLabels", new[] { "Jan", "Feb", "Mar", "Apr", "May" })
            .Parameter("ChartSeries", SampleSeries)
            .Variant("Donut", v => v.Set("ChartType", ChartType.Donut))
            .Variant("Pie", v => v.Set("ChartType", ChartType.Pie))
            .Variant("Bar", v => v.Set("ChartType", ChartType.Bar))
            .Variant("Line", v => v.Set("ChartType", ChartType.Line));

        options.For<MudCheckBox<bool>>()
            .Parameter(nameof(MudCheckBox<bool>.Label), "I agree to the terms")
            .Parameter(nameof(MudCheckBox<bool>.Color), Color.Primary);

        options.For<MudChip<string>>()
            .Slot("ChildContent", b => b.AddContent(0, "Blazor"), "Blazor")
            .Variant("Primary", v => v.Set("Color", Color.Primary))
            .Variant("Outlined", v => v.Set("Variant", Variant.Outlined).Set("Color", Color.Secondary))
            .Variant("With icon", v => v.Set("Icon", Icons.Material.Filled.Face).Set("Color", Color.Info));

        options.For<MudChipSet<string>>()
            .Slot("ChildContent", DemoFragments.Chips, DemoFragmentSources.Chips);

        options.For<MudDatePicker>()
            .Parameter(nameof(MudDatePicker.Label), "Select a date")
            .Variant("Static calendar", v => v.Set(nameof(MudDatePicker.PickerVariant), PickerVariant.Static));

        options.For<MudDateRangePicker>()
            .Parameter(nameof(MudDateRangePicker.Label), "Period");

        options.For<MudTimePicker>()
            .Parameter(nameof(MudTimePicker.Label), "Pick a time")
            .Variant("Static clock", v => v.Set(nameof(MudTimePicker.PickerVariant), PickerVariant.Static));

        options.For<MudExpansionPanels>()
            .Slot("ChildContent", DemoFragments.ExpansionPanels, DemoFragmentSources.ExpansionPanels);

        options.For<MudField>()
            .Parameter(nameof(MudField.Label), "Field label")
            .Slot("ChildContent", DemoFragments.FieldChild, DemoFragmentSources.FieldChild)
            .Variant("Outlined", v => v.Set(nameof(MudField.Variant), Variant.Outlined));

        options.For<MudGrid>()
            .Slot("ChildContent", DemoFragments.GridItems, DemoFragmentSources.GridItems)
            .Parameter(nameof(MudGrid.Spacing), 2);

        options.For<MudHighlighter>()
            .Parameter(nameof(MudHighlighter.Text), "The quick brown fox jumps over the lazy dog")
            .Parameter(nameof(MudHighlighter.HighlightedText), "fox");

        options.For<MudIcon>()
            .Parameter(nameof(MudIcon.Icon), Icons.Material.Filled.Home)
            .Parameter(nameof(MudIcon.Color), Color.Primary)
            .Variant("Large secondary", v => v.Set(nameof(MudIcon.Size), Size.Large).Set(nameof(MudIcon.Color), Color.Secondary));

        options.For<MudImage>()
            // Self-contained data URI: the bench must not depend on an external image host.
            .Parameter(nameof(MudImage.Src), "data:image/svg+xml," + Uri.EscapeDataString(
                """<svg xmlns="http://www.w3.org/2000/svg" width="280" height="160"><rect width="280" height="160" fill="#6d4aff"/><circle cx="70" cy="55" r="28" fill="#a18bff"/><path d="M0 160 90 80l60 50 50-36 80 66z" fill="#4c30c4"/><text x="14" y="146" font-family="monospace" font-size="13" fill="#fff">playblazor.svg</text></svg>"""))
            .Parameter(nameof(MudImage.Alt), "Sample image")
            .Parameter(nameof(MudImage.Elevation), 4)
            .Variant("Rounded", v => v.Set(nameof(MudImage.Class), "rounded-lg"));

        options.For<MudLink>()
            .Slot("ChildContent", b => b.AddContent(0, "Read the MudBlazor docs"), "Read the MudBlazor docs")
            .Parameter(nameof(MudLink.Href), "https://mudblazor.com")
            .Variant("Secondary underline", v => v.Set(nameof(MudLink.Color), Color.Secondary).Set(nameof(MudLink.Underline), Underline.Always));

        options.For<MudList<string>>()
            .Slot("ChildContent", DemoFragments.ListItems, DemoFragmentSources.ListItems);

        options.For<MudMenu>()
            .Parameter(nameof(MudMenu.Label), "Open menu")
            .Parameter(nameof(MudMenu.Variant), Variant.Filled)
            .Parameter(nameof(MudMenu.Color), Color.Primary)
            .Slot("ChildContent", DemoFragments.MenuItems, DemoFragmentSources.MenuItems);

        options.For<MudNavMenu>()
            .Slot("ChildContent", DemoFragments.NavLinks, DemoFragmentSources.NavLinks);

        // AppBar and Drawer default to Fixed=true — they'd render fixed OVER the workspace
        // chrome instead of inside the stage. Un-fix them and give them visible content.
        options.For<MudAppBar>()
            .Parameter(nameof(MudAppBar.Fixed), false)
            .Slot(nameof(MudAppBar.ChildContent), b => b.AddContent(0, "My application"), "My application");

        options.For<MudDrawer>()
            .Parameter(nameof(MudDrawer.Fixed), false)
            .Slot(nameof(MudDrawer.ChildContent), DemoFragments.NavLinks, DemoFragmentSources.NavLinks)
            .Variant("Open", v => v.Set(nameof(MudDrawer.Open), true));

        options.For<MudNumericField<double>>()
            .Parameter("Label", "Amount");

        options.For<MudOverlay>()
            .Slot("ChildContent", DemoFragments.OverlayChild, DemoFragmentSources.OverlayChild)
            .Variant("Visible dark", v => v.Set(nameof(MudOverlay.Visible), true)
                .Set(nameof(MudOverlay.DarkBackground), true)
                .Set(nameof(MudOverlay.Absolute), true));

        options.For<MudPagination>()
            .Parameter(nameof(MudPagination.Count), 10)
            .Variant("Primary", v => v.Set(nameof(MudPagination.Color), Color.Primary))
            .Variant("Rectangular", v => v.Set(nameof(MudPagination.Rectangular), true));

        options.For<MudPaper>()
            .Slot("ChildContent", DemoFragments.PaperChild, DemoFragmentSources.PaperChild)
            .Variant("Elevation 8", v => v.Set(nameof(MudPaper.Elevation), 8))
            .Variant("Outlined", v => v.Set(nameof(MudPaper.Outlined), true));

        options.For<MudPopover>()
            .Slot("ChildContent", b => b.AddContent(0, "Popover content"), "Popover content")
            .Variant("Open", v => v.Set(nameof(MudPopover.Open), true));

        options.For<MudProgressCircular>()
            .Parameter(nameof(MudProgressCircular.Color), Color.Primary)
            .Parameter(nameof(MudProgressCircular.Indeterminate), true)
            .Variant("Determinate 75%", v => v.Set(nameof(MudProgressCircular.Indeterminate), false).Set(nameof(MudProgressCircular.Value), 75.0));

        options.For<MudProgressLinear>()
            .Parameter(nameof(MudProgressLinear.Color), Color.Primary)
            .Parameter(nameof(MudProgressLinear.Value), 65.0)
            .Variant("Indeterminate", v => v.Set(nameof(MudProgressLinear.Indeterminate), true))
            .Variant("Striped", v => v.Set(nameof(MudProgressLinear.Striped), true).Set(nameof(MudProgressLinear.Size), Size.Large));

        options.For<MudRadioGroup<string>>()
            .Slot("ChildContent", DemoFragments.Radios, DemoFragmentSources.Radios);

        options.For<MudRating>()
            .Parameter(nameof(MudRating.SelectedValue), 3);

        options.For<MudSelect<string>>()
            .Parameter("Label", "Coffee")
            .Slot("ChildContent", DemoFragments.SelectItems, DemoFragmentSources.SelectItems)
            .Variant("Filled", v => v.Set("Variant", Variant.Filled))
            .Variant("Outlined", v => v.Set("Variant", Variant.Outlined));

        options.For<MudSimpleTable>()
            .Slot("ChildContent", DemoFragments.SimpleTableContent, DemoFragmentSources.SimpleTableContent)
            .Variant("Striped hover", v => v.Set(nameof(MudSimpleTable.Striped), true).Set(nameof(MudSimpleTable.Hover), true))
            .Variant("Dense outlined", v => v.Set(nameof(MudSimpleTable.Dense), true).Set(nameof(MudSimpleTable.Outlined), true));

        options.For<MudSkeleton>()
            .Parameter(nameof(MudSkeleton.Width), "220px")
            .Variant("Circle", v => v.Set(nameof(MudSkeleton.SkeletonType), SkeletonType.Circle)
                .Set(nameof(MudSkeleton.Width), "48px").Set(nameof(MudSkeleton.Height), "48px"))
            .Variant("Rectangle", v => v.Set(nameof(MudSkeleton.SkeletonType), SkeletonType.Rectangle)
                .Set(nameof(MudSkeleton.Height), "80px"));

        options.For<MudSlider<int>>()
            .Parameter("Value", 30)
            .Parameter("Color", Color.Primary);

        options.For<MudStack>()
            .Slot("ChildContent", DemoFragments.StackItems, DemoFragmentSources.StackItems)
            .Variant("Row", v => v.Set(nameof(MudStack.Row), true));

        options.For<MudStepper>()
            .Slot("ChildContent", DemoFragments.Steps, DemoFragmentSources.Steps);

        options.For<MudSwitch<bool>>()
            .Parameter(nameof(MudSwitch<bool>.Label), "Notifications")
            .Parameter(nameof(MudSwitch<bool>.Color), Color.Primary);

        options.For<MudTabs>()
            .Slot("ChildContent", DemoFragments.TabPanels, DemoFragmentSources.TabPanels)
            .Variant("Rounded", v => v.Set(nameof(MudTabs.Rounded), true))
            .Variant("Centered", v => v.Set(nameof(MudTabs.Centered), true));

        options.For<MudText>()
            .Slot("ChildContent", b => b.AddContent(0, "The quick brown fox jumps over the lazy dog"), "The quick brown fox jumps over the lazy dog")
            .Variant("h4", v => v.Set(nameof(MudText.Typo), Typo.h4))
            .Variant("subtitle1", v => v.Set(nameof(MudText.Typo), Typo.subtitle1))
            .Variant("overline", v => v.Set(nameof(MudText.Typo), Typo.overline));

        options.For<MudTextField<string>>()
            .Parameter("Label", "Name")
            .Variant("Filled", v => v.Set("Variant", Variant.Filled))
            .Variant("Outlined", v => v.Set("Variant", Variant.Outlined))
            .Variant("With helper", v => v.Set("HelperText", "First and last name"));

        options.For<MudTimeline>()
            .Slot("ChildContent", DemoFragments.TimelineItems, DemoFragmentSources.TimelineItems);

        options.For<MudToggleGroup<string>>()
            .Slot("ChildContent", DemoFragments.ToggleItems, DemoFragmentSources.ToggleItems)
            .Parameter("Color", Color.Primary);

        options.For<MudToolBar>()
            .Slot("ChildContent", DemoFragments.ToolBarContent, DemoFragmentSources.ToolBarContent);

        options.For<MudTooltip>()
            .Parameter(nameof(MudTooltip.Text), "A helpful hint")
            .Slot("ChildContent", DemoFragments.TooltipChild, DemoFragmentSources.TooltipChild)
            .Variant("Arrow", v => v.Set(nameof(MudTooltip.Arrow), true));

        options.For<MudTreeView<string>>()
            .Slot("ChildContent", DemoFragments.TreeItems, DemoFragmentSources.TreeItems);

        options.For<MudCarousel<string>>()
            .Slot("ChildContent", DemoFragments.CarouselItems, DemoFragmentSources.CarouselItems)
            .Parameter("Style", "height:200px;width:100%")
            .Parameter(nameof(MudCarousel<string>.AutoCycle), false);

        options.For<MudAutocomplete<string>>()
            .Parameter("Label", "US state")
            .Parameter(nameof(MudAutocomplete<string>.SearchFunc),
                (Func<string, CancellationToken, Task<IEnumerable<string>>>)SearchStates);

        // The grid demo: sample data as a parameter preset, columns as a slot preset.
        options.For<MudDataGrid<Person>>()
            .Parameter(nameof(MudDataGrid<Person>.Items), Person.Samples, "@_people")
            .Slot(nameof(MudDataGrid<Person>.Columns), source: """
<PropertyColumn Property="x => x.Name" />
<PropertyColumn Property="x => x.Role" />
<PropertyColumn Property="x => x.Age" />
""", content: columns =>
            {
                AddColumn<string>(columns, 0, p => p.Name);
                AddColumn<string>(columns, 2, p => p.Role);
                AddColumn<int>(columns, 4, p => p.Age);
            })
            .Variant("Dense striped", v => v.Set(nameof(MudDataGrid<Person>.Dense), true).Set(nameof(MudDataGrid<Person>.Striped), true))
            .Variant("Hover bordered", v => v.Set(nameof(MudDataGrid<Person>.Hover), true).Set(nameof(MudDataGrid<Person>.Bordered), true))
            .Related<PropertyColumn<Person, string>>();

        options.For<PropertyColumn<Person, string>>()
            .Parameter(nameof(PropertyColumn<Person, string>.Property),
                (Expression<Func<Person, string>>)(p => p.Name))
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<MudDataGrid<Person>>(0);
                builder.AddComponentParameter(1, nameof(MudDataGrid<Person>.Items), Person.Samples);
                builder.AddComponentParameter(2, nameof(MudDataGrid<Person>.Columns), (RenderFragment)(columns =>
                {
                    columns.AddContent(0, specimen);
                    AddColumn<int>(columns, 1, p => p.Age);
                }));
                builder.CloseComponent();
            })
            .Related<MudDataGrid<Person>>();

        // Providers make no sense as playground specimens (defense in depth next to the filter).
        options.Exclude<MudThemeProvider>()
            .Exclude<MudPopoverProvider>()
            .Exclude<MudDialogProvider>()
            .Exclude<MudSnackbarProvider>();

        // Demonstrates the theme hook: the host wraps every specimen and can react to the environment.
        options.ThemeWrapper = context => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", context.Environment.Dark ? "demo-specimen demo-specimen-dark" : "demo-specimen");
            builder.AddContent(2, context.Content);
            builder.CloseElement();
        };
    }

    private static void AddColumn<TProperty>(
        Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder columns,
        int sequence,
        Expression<Func<Person, TProperty>> property)
    {
        columns.OpenComponent<PropertyColumn<Person, TProperty>>(sequence);
        columns.AddComponentParameter(sequence + 1, nameof(PropertyColumn<Person, TProperty>.Property), property);
        columns.CloseComponent();
    }

    private static readonly string[] States =
    [
        "Alabama", "Alaska", "Arizona", "California", "Colorado", "Florida",
        "Georgia", "Montana", "Nevada", "New York", "Texas", "Washington",
    ];

    private static Task<IEnumerable<string>> SearchStates(string value, CancellationToken cancellationToken)
        => Task.FromResult(string.IsNullOrWhiteSpace(value)
            ? States.AsEnumerable()
            : States.Where(s => s.Contains(value, StringComparison.OrdinalIgnoreCase)));
}
