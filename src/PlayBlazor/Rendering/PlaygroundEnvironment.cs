using Microsoft.AspNetCore.Components;

namespace PlayBlazor.Rendering;

/// <summary>Per-playground rendering environment, cascaded to the specimen.</summary>
public sealed class PlaygroundEnvironment
{
    /// <summary>Renders the stage dark. The host's <see cref="PlayBlazorOptions.ThemeWrapper" /> is expected to honor it.</summary>
    public bool Dark { get; set; }

    /// <summary>Renders the specimen right-to-left.</summary>
    public bool Rtl { get; set; }

    /// <summary>Draws a checkerboard behind the specimen, to reveal transparency and bounds.</summary>
    public bool Checkerboard { get; set; }

    /// <summary>Simulated viewport width in pixels; null renders at natural width.</summary>
    public int? ViewportWidth { get; set; }
}

/// <summary>Passed to <see cref="PlayBlazorOptions.ThemeWrapper"/> so the host can wrap the specimen in its own theme provider.</summary>
public sealed record PlaygroundThemeContext(RenderFragment Content, PlaygroundEnvironment Environment);
