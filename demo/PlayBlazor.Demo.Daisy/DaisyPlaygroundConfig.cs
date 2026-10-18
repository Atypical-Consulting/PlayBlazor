using System.Linq.Expressions;
using DaisyBlazor;
using DaisyBlazor.Data;
using DaisyBlazor.Theming;

namespace PlayBlazor.Demo.Daisy;

/// <summary>
/// Curation for the DaisyBlazor showcase. A SEED only: the listed surface, the icon catalogue and
/// the flagship <see cref="Button" />. Presets, scaffolds and variants for the rest land in
/// milestone 4, driven by the [Explicit] sweeps' inventory, verified against the pinned 0.6.0
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
        options.For<DataGridPager<Person>>();
        // PropertyColumn.Property is [EditorRequired] with no default: OnParametersSet compiles it,
        // so a bare closing NullReferenceExceptions. The grid scaffold that makes it visible (alone
        // a column only registers with its DataGrid and renders nothing) comes with its curation.
        options.For<PropertyColumn<Person, string>>()
            .Parameter(nameof(PropertyColumn<Person, string>.Property), (Expression<Func<Person, string>>)(p => p.Name));
        options.For<TemplateColumn<Person>>();

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
    }

    private static string StripArity(string name)
    {
        var backtick = name.IndexOf('`');
        return backtick < 0 ? name : name[..backtick];
    }
}
