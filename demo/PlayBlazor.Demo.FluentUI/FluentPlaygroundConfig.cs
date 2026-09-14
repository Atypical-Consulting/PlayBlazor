using Microsoft.FluentUI.AspNetCore.Components;

namespace PlayBlazor.Demo.FluentUI;

/// <summary>
/// Curation for the Fluent UI showcase. A seed only: the curated surface, presets, scaffolds
/// and variants land in milestone 3, driven by the [Explicit] sweep's inventory.
/// </summary>
public static class FluentPlaygroundConfig
{
    /// <summary>Not components a user plays with: providers, internal helpers, settings objects.</summary>
    private static readonly HashSet<string> Infrastructure =
    [
        "ColumnReorderOptions", "ColumnResizeOptions", "Defer", "FluentDialogProvider",
        "FluentErrorBoundary", "FluentKeyCodeProvider", "FluentLabelInfo", "FluentLayoutHamburger",
        "FluentMessageBarProvider", "FluentOptionString", "FluentProviders", "FluentToastProvider",
        "FluentTooltipProvider", "FreeOptionOutput",
    ];

    /// <summary>Applies the Fluent UI curation to the playground options.</summary>
    /// <param name="options">The options to configure.</param>
    public static void Configure(PlayBlazorOptions options)
    {
        // Fluent exposes 108 public components, of which ~94 are things a user actually plays with.
        // MudBlazor's config uses an allow-list because most of ITS surface is internal; here the
        // reverse holds, so name what to drop. These are stable categories, not a list that rots:
        // service providers, internal render helpers, and settings objects that are ComponentBase
        // by inheritance rather than by intent.
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
