using DaisyBlazor;
using DaisyBlazor.Theming;

namespace PlayBlazor.Demo.Daisy;

/// <summary>
/// Curation for the DaisyBlazor showcase. A SEED only: the listed surface, the icon catalogue and
/// the flagship <see cref="Button" />. Presets, scaffolds and variants for the rest land in
/// milestone 4, driven by the [Explicit] sweeps' inventory, verified against the pinned 0.6.0
/// package's own markup (its <c>.razor</c> source at tag v0.6.0 and shipped <c>safelist.css</c>).
/// </summary>
public static class DaisyPlaygroundConfig
{
    /// <summary>
    /// Not components a user plays with: the providers a host mounts once in its layout (they
    /// render nothing visible on their own and exist to receive services' output) — plus
    /// <see cref="PageBreadcrumb" />, which injects an <c>IBreadcrumbService</c> that only
    /// <c>AddDaisyBlazorBreadcrumbs&lt;TResource&gt;</c> registers, backed by an app-defined
    /// route source. Without one its injection throws past the error boundary (the render sweep's
    /// one escaped exception); the plain <see cref="Breadcrumbs" /> stays listed.
    /// </summary>
    private static readonly HashSet<string> Infrastructure =
    [
        nameof(ThemeProvider), nameof(DialogProvider), nameof(SnackbarProvider), nameof(PageBreadcrumb),
    ];

    /// <summary>Applies the DaisyBlazor curation to the playground options.</summary>
    /// <param name="options">The options to configure.</param>
    public static void Configure(PlayBlazorOptions options)
    {
        // A deny-list, as for Fluent: most of DaisyBlazor's public surface is meant to be played.
        options.ComponentFilter = type => !Infrastructure.Contains(StripArity(type.Name));

        // DaisyBlazor icons are Material LIGATURE NAMES ("ac_unit"), not SVG markup: an SVG
        // preview would render nothing, so the thumbnail is a span in the Material Symbols font
        // that wwwroot/index.html loads — the same element DaisyBlazor's own Icon renders.
        options.Catalogue(DaisyIconCatalogue.Material, static name => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "material-symbols-outlined");
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
