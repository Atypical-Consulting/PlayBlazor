namespace PlayBlazor.Demo.FluentUI;

/// <summary>
/// Curation for the Fluent UI showcase. A seed only: the curated surface, presets, scaffolds
/// and variants land in milestone 3, driven by the [Explicit] sweep's inventory.
/// </summary>
public static class FluentPlaygroundConfig
{
    /// <summary>Applies the Fluent UI curation to the playground options.</summary>
    /// <param name="options">The options to configure.</param>
    public static void Configure(PlayBlazorOptions options)
    {
        // Fluent UI icons are Icon OBJECTS, not markup strings: without a catalogue the
        // parameter resolves to ControlKind.Unsupported and gets no control at all.
        options.Catalogue(FluentIconCatalogue.All, static icon => builder
            => builder.AddContent(0, icon.ToMarkup()));
    }
}
