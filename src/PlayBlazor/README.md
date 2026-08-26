# PlayBlazor

**Auto-generated playgrounds for any Blazor component library.** Point PlayBlazor at your
components and every public `[Parameter]` becomes an interactive control — no story files,
no hand-written wiring. Storybook asks you to write; PlayBlazor reflects.

```razor
@* One component, one line — in any docs page *@
<PlaygroundView Component="typeof(MudSelect<string>)" />

@* Or browse everything a library exposes *@
<PlaygroundExplorer Assemblies="@(new[] { typeof(MudButton).Assembly })" />
```

## What you get

- **Auto-generated controls** — bool → toggle, enum → select, string → text, numbers → numeric
  input, `RenderFragment` slots → editable sample text. Defaults are captured by instantiating
  the component once; XML doc summaries become tooltips.
- **Live Razor snippet** — the code panel always shows the markup matching the current
  configuration (only non-default parameters, idiomatic formatting), with copy to clipboard.
- **Event log** — every `EventCallback` is intercepted and logged with timestamp and payload,
  so you can *see* `OnClick` and `ValueChanged` fire.
- **Rendering environment** — dark surface, RTL, checkerboard, simulated viewport widths;
  a `PlaygroundEnvironment` is cascaded to the specimen and to your theme wrapper.
- **Permalinks** — Share copies a URL encoding the exact configuration (`?pb-MudButton=…`);
  opening it restores the playground. Stale links degrade gracefully.
- **Resilient by construction** — a component that throws is contained by an error boundary;
  one that cannot be instantiated is reported, never fatal. Scanning all of MudBlazor is a
  test in this repo.

## Setup

```csharp
builder.Services.AddPlayBlazor(options =>
{
    // Give slots realistic content (the user's typed text always wins):
    options.For<MudButton>().Slot(nameof(MudButton.ChildContent), b => b.AddContent(0, "Click me"));

    // Wrap every specimen in your theme infrastructure:
    options.ThemeWrapper = context => builder => { /* your provider, driven by context.Environment */ };
});
```

The shell has **zero UI dependencies** — system fonts, scoped CSS, no JS beyond
`navigator.clipboard`. It imposes nothing on the host site.

## v1 limitations

- Generic components need a closed type (`typeof(MudSelect<string>)`); the explorer tries
  `string` then `int` automatically.
- Complex-typed parameters are listed but not drivable; register richer mappers in a later
  version (`Color`/`Icon` kinds are reserved).
- Only the conventional `ChildContent` slot round-trips into the generated snippet.

## Roadmap

- **v2** — edit the snippet itself; parsed back into the controls (no arbitrary compilation).
- **v3** — full in-browser REPL (Roslyn).
- Extraction into a standalone repository once stabilized — MudBlazor is the incubation
  host and first demo library, not a dependency.
