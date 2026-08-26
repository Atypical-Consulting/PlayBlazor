using Microsoft.AspNetCore.Components;

namespace PlayBlazor.Rendering;

/// <summary>Per-playground rendering environment, cascaded to the specimen.</summary>
public sealed class PlaygroundEnvironment
{
    public bool Dark { get; set; }

    public bool Rtl { get; set; }

    public bool Checkerboard { get; set; }

    /// <summary>Simulated viewport width in pixels; null renders at natural width.</summary>
    public int? ViewportWidth { get; set; }
}

/// <summary>Passed to <see cref="PlayBlazorOptions.ThemeWrapper"/> so the host can wrap the specimen in its own theme provider.</summary>
public sealed record PlaygroundThemeContext(RenderFragment Content, PlaygroundEnvironment Environment);
