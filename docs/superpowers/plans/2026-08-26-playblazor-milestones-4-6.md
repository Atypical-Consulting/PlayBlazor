# PlayBlazor Milestones 4–6 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete PlayBlazor v1: slot presets + editable fallback, event log, rendering environment (dark/RTL/viewport/checker), permalinks, and the multi-component explorer.

**Architecture:** Extends the milestone 1–3 core. `PlayBlazorOptions` (DI singleton) carries host configuration: slot presets and a theme wrapper. `ParameterDictionaryBuilder` grows slots + intercepted events. `PlaygroundView` gains an events section, a stage toolbar (environment), and a Share button (state → base64url in the URL). `PlaygroundExplorer` composes a category tree with a `PlaygroundView`.

**Tech Stack:** same as milestones 1–3 (net10.0, RCL, NUnit + bUnit on MTP).

**Spec:** `docs/superpowers/specs/2026-08-26-playblazor-design.md`

## Global Constraints

Same as the milestone 1–3 plan (`2026-08-26-playblazor-milestones-1-3.md`), plus:
- Test-relied selectors from milestones 1–3 must keep working (`.pb-control`, `.pb-row-reset`, `.pb-code code`, `.pb-copy`, `.pb-preview`, `.pb-error`, `.pb-warning`, `.pb-uncontrolled`).
- The dev server runs in `dotnet watch` during implementation — keep the DemoHost page compiling at every commit.
- Commands need `DOTNET_ROOT=$HOME/.dotnet` and `PATH=$HOME/.dotnet:$PATH` (SDK 10.0.400).

---

## Jalon 4 — Slots & événements

### Task 14: PlayBlazorOptions + presets de slots

**Files:**
- Create: `src/PlayBlazor/PlayBlazorOptions.cs`
- Modify: `src/PlayBlazor/PlayBlazorServiceCollectionExtensions.cs`
- Test: `src/PlayBlazor.UnitTests/OptionsTests.cs`

**Interfaces (produced):**

```csharp
namespace PlayBlazor;

public sealed class PlayBlazorOptions
{
    public ComponentOptionsBuilder<TComponent> For<TComponent>() where TComponent : IComponent;
    // internal: bool TryGetSlotPreset(Type componentType, string parameterName, out RenderFragment fragment)
    // internal: RenderFragment<PlaygroundThemeContext>? ThemeWrapper (Task 17)
}

public sealed class ComponentOptionsBuilder<TComponent>
{
    public ComponentOptionsBuilder<TComponent> Slot(string parameterName, RenderFragment content);
}

// AddPlayBlazor gains an overload:
public static IServiceCollection AddPlayBlazor(this IServiceCollection services, Action<PlayBlazorOptions>? configure = null);
// registers PlayBlazorOptions as a singleton; PlaygroundView injects it.
```

Preset lookup matches the open generic type too: a preset registered `For<GenericFixture<string>>` matches exactly; a preset registered for the closed type used by the view wins; fall back to comparing `GetGenericTypeDefinition()` when both are generic of the same definition.

**Steps:** failing tests (preset stored and retrieved; closed/open generic match; AddPlayBlazor(configure) wires options) → implement → green → commit `PlayBlazor: add host options with slot presets`.

### Task 15: Slots pilotables — preset, texte éditable, codegen enfant

**Files:**
- Modify: `src/PlayBlazor/Rendering/ParameterDictionaryBuilder.cs`
- Modify: `src/PlayBlazor/PlaygroundView.razor` / `.razor.cs`
- Modify: `src/PlayBlazor/CodeGen/RazorSnippetGenerator.cs`
- Test: `src/PlayBlazor.UnitTests/Rendering/SlotTests.cs`

**Behavior locked in:**
- `ParameterDictionaryBuilder.Build(descriptor, state, options)` — new third parameter.
- For a `Slot` parameter of exact type `RenderFragment` (non-generic):
  - options preset → always included in the dictionary;
  - else if state has a non-empty string → wrapped `builder => builder.AddContent(0, text)`;
  - else omitted.
- Non-generic `RenderFragment` slots become **controllable**: they get a `TextControl` (they move from the Uncontrolled group into the rows; `.pb-control` count for `BasicFixture` goes 7 → 8 — update `RendersOneControlPerDrivableParameter`).
- Generic `RenderFragment<T>` stays uncontrolled unless preset.
- CodeGen: a text-modified `ChildContent`-style slot emits child content instead of self-closing:
  `<BasicFixture Dense="true">hello</BasicFixture>` (multi-attribute alignment unchanged; closing tag on the same line as the last attribute's `>`). Preset slots do not appear in the snippet.

**Steps:** failing tests (preset injected into dictionary; text slot wraps; codegen child content single-line and multi-line; empty text omitted) → implement → green (fix the 7→8 count) → commit `PlayBlazor: drive RenderFragment slots via presets and editable text`.

### Task 16: Log d'événements

**Files:**
- Create: `src/PlayBlazor/State/PlaygroundEventLog.cs`
- Create: `src/PlayBlazor/Rendering/EventCallbackInterceptor.cs`
- Modify: `src/PlayBlazor/Rendering/ParameterDictionaryBuilder.cs`
- Modify: `src/PlayBlazor/PlaygroundView.razor` / `.razor.cs` / `.razor.css`
- Create: `src/PlayBlazor.UnitTests/Fixtures/EventFixture.razor`
- Test: `src/PlayBlazor.UnitTests/State/EventLogTests.cs`, `src/PlayBlazor.UnitTests/Shell/EventPanelTests.cs`

**Interfaces (produced):**

```csharp
public sealed class PlaygroundEventLog       // namespace PlayBlazor.State
{
    public const int Capacity = 100;
    public sealed record Entry(DateTime Timestamp, string Name, string Payload);
    public IReadOnlyList<Entry> Entries { get; }   // newest first
    public event Action? Changed;
    public void Record(string name, object? payload);   // payload → ToString() ?? "(null)"; EventArgs.Empty → ""
    public void Clear();
}

public static class EventCallbackInterceptor   // namespace PlayBlazor.Rendering
{
    // callbackType: EventCallback or EventCallback<T>; returns a boxed instance whose
    // invocation calls handler(argOrNull). Built via EventCallback.Factory reflection, cached.
    public static object Create(Type callbackType, Action<object?> handler);
}
```

- `ParameterDictionaryBuilder`: every `Event` parameter is always included, bound to `log.Record(parameter.Name, arg)`.
- `EventFixture`: renders a `<button class="event-source">` whose `@onclick` invokes `[Parameter] EventCallback<string> OnPing` with `"ping!"`.
- UI: under Parameters, eyebrow `Events` + `.pb-events` list (`.pb-event-name`, `.pb-event-payload`, time `HH:mm:ss`), `Clear` button `.pb-events-clear`, empty state text "Interactions will show up here."
- bUnit: click `.event-source` in preview → `.pb-events` shows `OnPing` + `ping!`; clear empties it.

**Steps:** failing tests → implement → green → commit `PlayBlazor: intercept EventCallbacks into an event log panel (milestone 4)`.

---

## Jalon 5 — Environnement & permaliens

### Task 17: Environnement de rendu

**Files:**
- Create: `src/PlayBlazor/Rendering/PlaygroundEnvironment.cs` (+ `PlaygroundThemeContext`)
- Modify: `src/PlayBlazor/PlayBlazorOptions.cs` (ThemeWrapper)
- Modify: `src/PlayBlazor/PlaygroundView.razor` / `.razor.cs` / `.razor.css`
- Modify: `src/PlayBlazor.DemoHost/Program.cs` (ThemeWrapper → MudThemeProvider IsDarkMode)
- Test: `src/PlayBlazor.UnitTests/Shell/EnvironmentTests.cs`

**Interfaces (produced):**

```csharp
public sealed class PlaygroundEnvironment    // namespace PlayBlazor.Rendering — mutable, per view
{
    public bool Dark { get; set; }
    public bool Rtl { get; set; }
    public bool Checkerboard { get; set; }
    public int? ViewportWidth { get; set; }  // null = auto; presets 360 / 768 / 1200
}

public sealed record PlaygroundThemeContext(RenderFragment Content, PlaygroundEnvironment Environment);
// PlayBlazorOptions.ThemeWrapper: RenderFragment<PlaygroundThemeContext>? — host wraps the specimen
// (e.g. MudThemeProvider IsDarkMode="context.Environment.Dark"). Without it the specimen renders bare.
```

- Stage toolbar `.pb-stage-toolbar` (top-right): toggle buttons `.pb-env-dark`, `.pb-env-rtl`, `.pb-env-checker`, width `<select class="pb-env-width">` (Auto/360/768/1200). Active toggles get class `pb-env-on`.
- Specimen wrapper `.pb-specimen`: `dir="rtl"` when Rtl, `max-width` when ViewportWidth (+ dashed outline), cascading value of the `PlaygroundEnvironment`.
- Stage visual: `pb-stage-dark` class flips stage tokens (dark background/dots); `pb-stage-checker` swaps dot grid for a checkerboard.
- bUnit: toggling dark adds `pb-stage-dark`; rtl sets `dir`; width constrains style; ThemeWrapper from options actually wraps (fixture wrapper renders a marker div).

**Steps:** failing tests → implement → green → commit `PlayBlazor: add stage environment toolbar and host theme wrapper`.

### Task 18: Permaliens

**Files:**
- Create: `src/PlayBlazor/State/PlaygroundStateSerializer.cs`
- Modify: `src/PlayBlazor/PlaygroundView.razor` / `.razor.cs`
- Test: `src/PlayBlazor.UnitTests/State/PlaygroundStateSerializerTests.cs`, `src/PlayBlazor.UnitTests/Shell/PermalinkTests.cs`

**Interfaces (produced):**

```csharp
public static class PlaygroundStateSerializer   // namespace PlayBlazor.State
{
    // Encodes modified primitive/enum/string values (+ slot text) and non-default environment flags
    // as compact JSON → UTF8 → base64url. Unserializable values are skipped.
    public static string Encode(ComponentDescriptor descriptor, PlaygroundState state, PlaygroundEnvironment environment);
    // Tolerant decode: unknown names and mismatched types are ignored; enum by name; numbers via
    // Convert.ChangeType(invariant) to the parameter type.
    public static void Decode(string encoded, ComponentDescriptor descriptor, PlaygroundState state, PlaygroundEnvironment environment);
}
```

- Query parameter name: `pb-<DisplayName>` (several playgrounds coexist on a page).
- Share button `.pb-share` in the panel header: builds `<current uri minus query pb-...>?pb-X=<encoded>` via NavigationManager and copies it (`navigator.clipboard.writeText`).
- On first `OnParametersSet`, read the query and restore state/env.
- bUnit: round-trip encode/decode; navigating to a URI with `pb-BasicFixture=<encoded>` renders the modified preview; share button copies a URL containing the encoded state.

**Steps:** failing tests → implement → green → commit `PlayBlazor: encode playground state into shareable permalinks (milestone 5)`.

---

## Jalon 6 — Explorer

### Task 19: PlaygroundExplorer

**Files:**
- Create: `src/PlayBlazor/PlaygroundExplorer.razor` / `.razor.cs` / `.razor.css`
- Create: `src/PlayBlazor.DemoHost/Pages/Explorer.razor` (`@page "/explorer"`)
- Modify: `src/PlayBlazor.DemoHost/Pages/Index.razor` (link to explorer)
- Test: `src/PlayBlazor.UnitTests/Shell/ExplorerTests.cs`

**Interfaces (produced):**

```csharp
public partial class PlaygroundExplorer : ComponentBase
{
    [Parameter, EditorRequired] public IReadOnlyList<Assembly> Assemblies { get; set; }
}
```

- Layout: sidebar `.pb-explorer-nav` (search `input.pb-explorer-search`, groups by `Category` with eyebrow headers, one `button.pb-explorer-item` per component, selected = `.pb-explorer-selected`) + detail area hosting a `PlaygroundView` for the selection. First component selected by default.
- Search filters on `DisplayName` (ordinal, case-insensitive).
- bUnit: lists fixture components grouped; typing in search filters items; clicking an item swaps the hosted `PlaygroundView`.

**Steps:** failing tests → implement → green → commit `PlayBlazor: add multi-component explorer`.

### Task 20: README + clôture v1

**Files:**
- Create: `src/PlayBlazor/README.md` (what it is, install/AddPlayBlazor, PlaygroundView/PlaygroundExplorer, options: slots presets + ThemeWrapper, permalinks, limitations v1, roadmap v2/v3)
- Test: full suite + `dotnet build src/PlayBlazor.DemoHost`

**Steps:** write README → full suite green → commit `PlayBlazor: add package README (milestone 6 / v1 complete)`.

---

## Journal d'exécution (2026-08-26)

Tasks 14–20 exécutées le jour même, 105 tests verts, DemoHost servi en `dotnet watch` pendant toute l'implémentation. Écarts :
1. **Précédence des slots inversée** vs le plan : le texte tapé par l'utilisateur gagne sur le preset hôte (meilleure UX ; le plan disait preset toujours prioritaire).
2. Deux tests des jalons 1–3 mis à jour car leur postulat (« slot = ignoré ») a changé par design.
3. `using MudBlazor` oublié dans DemoHost/Program.cs, commité cassé puis amendé aussitôt — leçon : le grep de vérification du build était ambigu, remplacé par un `tail` explicite.
4. Une passe design (hors plan, demande utilisateur « c'est bien moche ») a précédé le jalon 4 : esthétique « établi d'atelier », accent violet #6D4AFF, mono = voix de l'API, stage à grille de points, commit `7e93e7e7d`.
