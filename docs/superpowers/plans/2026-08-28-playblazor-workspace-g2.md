# PlayBlazor Workspace (Concept G v2) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this
> plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Recreate the validated Concept G v2 mini-IDE shell in Blazor inside the PlayBlazor RCL,
with real data (reflection catalog, real snippet generator, real EventCallbacks).

**Architecture:** A new `PlaygroundWorkspace` component (chrome bar + stage + dockable panels)
becomes the explorer surface; `PlaygroundView` stays untouched as the embeddable playground
(landing page + existing tests). Layout mechanics (drag/dock/float/resize/persist/keyboard) live
in one small JS module that reports gestures to .NET; Blazor owns the layout state
(`WorkspaceLayout`) and renders panels into zones. Panel bodies reuse the existing building
blocks (ControlHost, RazorSnippetGenerator, PlaygroundState, ParameterDictionaryBuilder,
variants, scaffolds, permalinks).

**Tech Stack:** Blazor WASM RCL (net10.0), bUnit v2 + NUnit4 (Microsoft.Testing.Platform),
vanilla JS module as RCL static web asset, System.Text.Json source-gen.

**Spec:** `docs/superpowers/design/2026-08-28-playblazor-ux-concepts/HANDOFF-concept-g-v2.md`
(mirrored Claude Design handoff — high-fidelity; tokens, layout, behaviors, wiring) +
`concept-g-ide-v2.html` (reference implementation of look & interactions).

## Global Constraints

- TreatWarningsAsErrors; `$(PrimaryTargetFramework)`; SDK via `DOTNET_ROOT=$HOME/.dotnet PATH=$HOME/.dotnet:$PATH`.
- Tests: `dotnet run --project src/PlayBlazor.UnitTests` (filter: `-- --filter "FullyQualifiedName~X"`).
- bUnit v2: strict JSInterop (SetupVoid/SetupModule required), `await using` context when AddMudServices, `cut.Render(ps => …)`.
- PlaygroundView/Explorer public behavior unchanged — all 129 existing tests keep passing.
- **dotnet watch must be restarted after adding/renaming any static web asset** (known corruption).
- Design tokens/behaviors: follow HANDOFF-concept-g-v2.md exactly; keep the env toolbar
  (dark/rtl/checker/width) on the stage even though the mockup omitted it (existing product
  feature — documented deviation).

---

### Task A: Foundations — parameter groups, signatures, event log v2, layout state

**Files:**
- Modify: `src/PlayBlazor/Model/ParameterDescriptor.cs` (add optional `string Group = "General"`, `int GroupOrder = int.MaxValue`)
- Modify: `src/PlayBlazor/Discovery/ReflectionCatalogProvider.cs` (duck-typed CategoryAttribute: attr type name == "CategoryAttribute" → string prop `Name` or `Category`, int prop `Order` if present)
- Modify: `src/PlayBlazor/State/PlaygroundEventLog.cs` (Capacity 100→50 per spec; `Entry.Detail` = reflection dump of public payload properties, null when it adds nothing over Payload)
- Create: `src/PlayBlazor/State/WorkspaceLayout.cs` (zones/floats/hidden/sizes + operations + JSON)
- Create: `src/PlayBlazor/Shell/Workspace/ParameterSignature.cs` (`Format(ParameterDescriptor)` → `[Parameter] public bool Dense { get; set; } = false;` with C#-ish type names)
- Test: `src/PlayBlazor.UnitTests/Discovery/CategoryGroupTests.cs`, `src/PlayBlazor.UnitTests/State/WorkspaceLayoutTests.cs`, extend `EventLogTests`, `src/PlayBlazor.UnitTests/Shell/ParameterSignatureTests.cs`

**Interfaces (produced):**
- `WorkspaceLayout`: `IReadOnlyList<string> Zone(string zone)` for "right"/"bottom"; `FloatInfo? Float(string id)` (`record FloatInfo(double X, double Y, double? W, double? H)`); `bool IsHidden(string id)`; `double RightWidth/BottomHeight`; ops `Dock(id, zone, index)`, `SetFloat(id, x, y)`, `SetFloatSize(id, w, h)`, `ToggleHidden(id)`, `Redock(id)` (default zone map), `Resize(zone, px)` (clamped 240–560 / 120–520), `Reset()`; `string ToJson()`, `static WorkspaceLayout FromJson(string?)` (tolerant → defaults). Defaults: right = graph,parameters; bottom = razor,signals; sizes 330/235. `event Action? Changed`.
- `ParameterDescriptor.Group/GroupOrder` flow from provider; fallback "General"/int.MaxValue.
- `PlaygroundEventLog.Entry(DateTime, string Name, string Payload, string? Detail)`.

**Steps:**
- [ ] Write failing tests: MudButton descriptor has `Variant` in group "Appearance"; fixture without category → "General". WorkspaceLayout ops + JSON round-trip + tolerant FromJson(null/garbage) + Reset. EventLog cap 50; Detail lists MouseEventArgs properties; Detail null for EventArgs.Empty. Signature formatting (bool with default, nullable enum, EventCallback<T>).
- [ ] Run → red. Implement. Run → green (fix any existing test asserting Capacity 100).
- [ ] Commit `feat(playblazor): parameter groups, event detail, workspace layout state`.

### Task B: PlaygroundWorkspace shell — chrome, zones, four panels (no JS drag yet)

**Files:**
- Create: `src/PlayBlazor/Shell/Workspace/PlaygroundWorkspace.razor` + `.razor.cs` + `.razor.css`
- Create: `src/PlayBlazor/Shell/Workspace/WorkspacePanel.razor` (chrome: grip glyph, title, badge, actions incl. custom, collapse ▾/▸, close ✕; `RenderFragment Body`, `RenderFragment? Actions`, EventCallbacks)
- Modify: `src/PlayBlazor/PlayBlazorOptions.cs` (+`Related<TOther>()` on builder → `IReadOnlyList<Type> GetRelated(Type)`, exact-closing keys)
- Modify: `src/PlayBlazor.DemoHost/Pages/Explorer.razor` (swap to `<PlaygroundWorkspace Assemblies=…/>`) and `PlaygroundConfig.cs` (Related links MudDataGrid<Person> ↔ PropertyColumn<Person,string>)
- Test: `src/PlayBlazor.UnitTests/Shell/WorkspaceTests.cs` (+ `WorkspaceParametersPanelTests.cs`, `WorkspaceSignalsPanelTests.cs`, `WorkspaceGraphPanelTests.cs`)

**Interfaces (consumed):** Task A's WorkspaceLayout (rendering order), Group/Signature, EventLog Detail. Existing: ControlHost, RazorSnippetGenerator.GenerateMarkup/Generate, PlaygroundState, ParameterDictionaryBuilder.Build, Options (variants/scaffold/presets/filter), PlaygroundEnvironment + ThemeWrapper + ErrorBoundary recover pattern, permalink restore/share code (lifted from PlaygroundView/Explorer).

**Behaviors (per handoff):**
- Chrome bar: `▶ PlayBlazor` wordmark, component picker `<select>` with `<optgroup>` per category (curated via ComponentFilter/IsExcluded, permalink preselect), 4 panel pills, Play/Present segmented, Reset layout, Share ⤴ (toast `Link copied ✓`).
- Grid `"stage right" / "bottom right"`; zones render panels in layout order; `data-empty` zones hide; collapsed panels shrink; hidden panels ✕/pill round-trip.
- Parameters panel: badge = component name; toolbar filter + `Modified · n/total` chip (click filters) + fold-all ⊟/⊞ + node reset ↺ (toast `Parameters reset ✓`); groups as `<details open>` ordered by GroupOrder then name; rows = name (dot when modified, title = signature) + ControlHost; slots `◇`, events `⚡ wired`.
- Razor panel: SnippetMarkup, ⧉ copy → toast `Razor copied ✓`.
- Signals panel: badge count; entries `time · name · payload`, click unfolds Detail (▸ rotates); ⌫ clear; cleared on component switch; empty state text.
- Graph panel: nodes = scaffold label (info node when TryGetScaffold), played component (selected), related components (clickable → picker switch). Selecting the played node is default; hint line.
- Stage: dot grid, Examples chips (variants), env toolbar kept, error boundary + recover on control change/switch (same rules as PlaygroundView).
- Present mode (partial): body `data-mode`, zones hidden, stage full-bleed dark, HUD with variant filmstrip + ❚❚/▶ + autoplay 4s timer (pause on manual pick); esc arrives in Task C.

**Steps:**
- [ ] Failing bUnit tests: workspace renders 4 panels in default zones; picker optgroups + switching component resets state/log and recovers boundary; pill hides/shows panel; params filter narrows rows; Modified chip counts and filters; fold-all collapses groups; signals unfold shows Detail, clear works, switch clears; graph shows related node and clicking switches picker; snippet copy invokes clipboard (SetupVoid); permalink preselects component (register options before NavigationManager fetch).
- [ ] Implement markup/code-behind/CSS (tokens from handoff; scoped CSS with ::deep where MarkupString output is styled).
- [ ] Run new + full suite → green. Build DemoHost.
- [ ] Commit `feat(playblazor): PlaygroundWorkspace mini-IDE shell (panels, picker, present)`.

### Task C: Layout interactivity — JS module (drag/dock/float/resize/keys/persist)

**Files:**
- Create: `src/PlayBlazor/wwwroot/playground-workspace.js`
- Modify: `PlaygroundWorkspace.razor.cs` (module import in OnAfterRenderAsync(first), DotNetObjectReference, [JSInvokable] callbacks, persistence via js `saveLayout(json)` / initial `loadLayout()`), `.razor` (stable `id`/`data-panel` hooks, float style bindings), `.razor.css` (drop-target/hot/dragging/resize-handle styles)
- Test: extend `WorkspaceTests` (module mocked via `JSInterop.SetupModule`), `WorkspaceLayoutTests` already cover state ops; JSInvokable methods tested directly on component instance.

**JS contract:**
`init(root, dotnetRef)` wires: header pointer-drag (>5px → ghost clone; zones get droptarget/hot classes; drop → `OnDrop(panelId, zone|null, index, x, y)`), float resize handles (`rz-r/rz-b/rz-c`, min 250×130 → `OnFloatResized`), zone grips (`OnZoneResized(zone, px)`), header dblclick (`OnRedock`), document keys (`1–4` → `OnPanelKey(n)`, `/` → `OnFocusFilter` + JS focuses input, `Escape`/`ArrowLeft/Right` in present → `OnPresentKey`), viewport re-clamp on resize, `loadLayout()`/`saveLayout(json)` (localStorage `pb-workspace-v1`, try/catch). `dispose()`.

**Steps:**
- [ ] Failing tests for JSInvokable handlers mutating layout + persist call; module setup mocked.
- [ ] Implement JS + interop. Run suite → green.
- [ ] **Restart dotnet watch** (new static asset). Browser-verify drag/dock/float/resize/dblclick/keys/persist on http://localhost:5871/explorer with real MudBlazor components; fix; screenshot.
- [ ] Commit `feat(playblazor): workspace layout interactivity (drag-dock, floats, keyboard, persistence)`.

### Task D: Present polish + full verification

**Steps:**
- [ ] Present: esc exit + ←/→ via OnPresentKey; autoplay progress underline (CSS animation, reduced-motion off); HUD wiring test.
- [ ] Full suite green; DemoHost build clean; landing page intact (PlaygroundView untouched).
- [ ] Browser pass: MudButton + MudDataGrid<Person> through all panels/modes; permalink from landing tile; console clean.
- [ ] Update memory + NOTES; commit `feat(playblazor): present mode polish + G v2 verification`.

## Execution journal (2026-08-28)

All four tasks executed inline, TDD, four commits: `df9e5c098` (A), `6460e960a` (B),
`82ba22ab7` (C), `ee60f03ab` (closing preference + fixes). Suite 130 → **174 green**.

Deviations from the plan:
- **bUnit SetupVoid gotcha**: post-await assertions (toasts) need `.SetVoidResult()` —
  without it the interop Task never completes and the continuation never runs.
- **Razor nested-quotes**: component attribute values can't carry string literals
  (`Collapsed="x.Contains("graph")"`), fixed with panel-id consts.
- **Scoped-CSS boundary**: `.pbw-panel` chrome had to move into WorkspacePanel.razor.css;
  cross-component selectors from the workspace need `::deep` (`.pbw-present ::deep
  .pbw-panel-float`, responsive rules).
- **Unplanned but necessary — preferred generic closings**: discovery closes generics with
  string/int, so the picker served MudDataGrid<string> (empty stage, no variants, no
  Related). `For<T>()` on a constructed generic now records the host's closing and the
  workspace substitutes it. New test HostConfiguredGenericClosing_ReplacesTheDiscoveredPlaceholder.
- Graph Related targets resolve via the catalog (curated-out components stay reachable);
  the picker temporarily lists an off-catalog selection.
- EventLog cap 100 → 50 (spec): existing tests used the constant, nothing broke.
- Signals unfold needed a payload with properties — new DetailEventFixture; string payloads
  are deliberately fold-less (pbw-signal-flat).
- dotnet watch corrupted the WASM boot once after hot-reload restarts (known trap) —
  clean relaunch fixed it, as documented.

## Self-Review
- Spec coverage: chrome bar ✓ (picker=select per spec), panels ✓, drag/float/resize/dblclick ✓,
  keyboard ✓, persistence ✓ (`pb-workspace-v1` — v2 mockup key not reused: different schema),
  toasts ✓, Present ✓, signals unfold/cap/clear ✓, signature tooltips ✓, Modified chip ✓,
  fold-all ✓, per-node reset ✓. Deviations (documented): env toolbar kept; graph = scaffold
  label + played + Related navigation (the mock's intra-graph parameter re-scoping needs a
  composition engine that doesn't exist yet — Related<T> delivers the navigation honestly);
  specimen-level MultiSelection wiring is MudBlazor's own behavior, real by construction.
- Types consistent: WorkspaceLayout API used by B/C as declared in A.
