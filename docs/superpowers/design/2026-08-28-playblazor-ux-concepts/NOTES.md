# PlayBlazor UX concepts — design notes (2026-08-28)

Brief: « improve the ux, the page organization, the chrome, the use of space — make me
dream with strong concepts, first in HTML/CSS/JS, before real implementation ».

## Problems in the current shell these concepts attack

1. **Vertical stack wastes wide screens** — stage → code → events pile up; params scroll in a
   narrow right column while the stage sits half-empty.
2. **Scattered chrome** — env toolbar on the stage, Share/Reset in the params header, search in
   the nav: three homes for global actions.
3. **65-item nav is scanned, not searched** — groups + search help, but jumping is slow.

## Shared DNA (kept from the validated identity)

Violet `#6D4AFF`, mono = API voice, dot-grid stage, MudBlazor theme colors reserved for the
specimen itself (`#594AE2` primary…) so the chrome never competes with the component.

## Concept A — La Console (instrument cockpit)

- Dark bakelite chrome; the stage is the single lit island.
- Bottom **dock** (tabs Parameters / Code / Events, drag-to-resize) = one home for everything
  secondary; the stage never shrinks below the fold.
- Parameters as a **horizontal instrument bank** (segmented switches, physical toggles) — reads
  like a mixing desk, wraps instead of scrolling.
- Icon rail with hover flyout + **⌘K palette** replaces the permanent sidebar.
- Risk check: not the "black + acid green" default — accent stays violet, green is only the
  event signal tick.

## Concept B — La Fiche technique (datasheet)

- The component as an electronic **part**: masthead with part number, huge Archivo 800 title,
  "LIVE SPECIMEN" stamp, crop marks.
- Framed specimen "fig. 1" with **live width measurement** under it (updates with Size).
- Parameters = **Characteristics table** (Parameter / Type / Default / Yours) — the
  default-vs-modified story becomes explicit, ● marks touched rows.
- **Signature: SVG leader lines** from the hovered row to the specimen frame.
- Code = "Reference circuit", events = "Signal log", handling notes = the ErrorBoundary story.
- Risk check: not the cream-serif-terracotta default — grotesque + mono, datasheet apparatus
  (leader lines, fig. captions, dimension rules), violet identity kept.

## Concept C — Le Studio (organization by task)

- One shell, **three modes**: Play (stage 7 / params 5 + code drawer), Code (editor in majesty,
  v2 round-trip teaser with fake caret), Present (full-bleed dark, specimen ×1.7, floating HUD
  with auto-advancing example **filmstrip**, ← → keys, esc exits).
- The CSS grid **morphs** between modes (grid-template transition).
- Present is the shareable "demo reel" — the mode a library author would screen-record.

## Quality floor in all three

Responsive to ~mobile, `:focus-visible`, `prefers-reduced-motion` kills transitions/autoplay,
semantic buttons/tabs. All state is live JS: presets seed state, controls re-render the fake
MudButton, the snippet regenerates with defaults omitted (same rule as RazorSnippetGenerator).

## What I deliberately did NOT do

- No rebrand — Philippe validated the atelier identity; the risk budget went into layout.
- No new landing concept — the brief targets the working surface (explorer).
- Concept B's Google Fonts link (Archivo, IBM Plex Mono) has full system fallbacks; A and C are
  100 % offline.

## Round 2 (2026-08-28, after Philippe's verdict)

Verdict on round 1: **A** — the instrument controls (segments, switches) and the grip are keepers,
"sage mais pas mal du tout". **B** — assumed aesthetic, but breaks at scale: a DataGrid needs
space, and 50 characteristics would destroy the layout. **C** — more modern than A, extensible.

Round-2 rule derived from the B critique: **every concept must survive the stress case**. All
three pages share `round2-model.js` (two components: MudButton 9 params, MudDataGrid<Person>
37 params in 12 groups; live-wired specimen: Dense/Striped/Bordered/Hover/Elevation/FixedHeader/
Loading/MultiSelection actually re-render; multi-line razor with `<Columns>` block).

- **Concept D — L'Établi** (C × A): mode shell + instrument dock, plus the scale apparatus —
  parameter search, group chips, "Modified · n" filter. The safe, implementable synthesis.
- **Concept E — Palettes**: full-bleed canvas, floating draggable/collapsible dark palettes
  (Parameters / Razor / Signals), top-bar pills to summon them. Best specimen space of all.
- **Concept F — L'Inspecteur**: devtools metaphor. Scaffold graph as a selectable tree; a node
  selection shows *its* parameters (column params: Title/Hidden are wired to the specimen),
  highlights its column in the grid and its line in the razor. Directly UX-ifies the
  Scaffold/composition-graph feature.

Fixed during review: F's tree markup regex was re-matching its own injected spans (attr wrap must
happen in a single replace pass); E's Signals palette collided with Parameters at load
(max-height); D/E/F inherit the round-1 lesson of seeding a striking preset at load (D seeds via
`pbMakeState` seed field).

## Round 3 (2026-08-28, after the round-2 verdict)

Verdict on round 2: good ideas in all three. **D** — interesting but "manque de possibilité de
layout — nous sommes presque sur un mini IDE visuel mais pas de possibilité de déplacer les
panels". **E** — could be integrated into D, strong for full screen. **F** — "l'idée d'un devtool
pour composant Blazor est vraiment très impressionnante".

**Concept G — Le Mini-IDE** commits to that reading: a real panel system in vanilla JS.
- Four panels (Graph, Parameters, Razor, Signals) with headers you grab: drop on the bottom dock
  or the right dock (insertion index follows the pointer, VS Code-style), or release anywhere to
  float (E integrated). Zones show dashed drop-targets while dragging, collapse when empty, and
  resize with grips (bottom height / right width).
- Pills in the chrome summon/dismiss panels; ✕ closes; ▾ collapses; Reset layout restores the
  default. Layout persists in localStorage (`pb-g-layout-v1`, try/catch-guarded).
- F is a first-class panel: tree selection re-targets the Parameters panel (badge shows the node),
  highlights the column in the specimen and the line in the razor. Column Title/Hidden stay wired.
- Present mode kept from C/D (filmstrip HUD); Code mode dropped — Razor is a panel now.
- Browser-verified: synthetic pointer drags docked Razor between Graph and Parameters and floated
  Signals; reload restored the custom layout; Reset restored the default; search/Modified filter,
  Present, and the node-aware sheet all pass.

Sync: mockups uploaded to a Claude Design project via /design-sync (off-converter path — static
HTML cards with `@dsCard` markers, `round2-model.js` inlined into self-contained copies).

## Implementation notes (whichever wins)

- A's dock ≈ reorganizing `PlaygroundView.razor` grid areas + a resize JS interop; the
  instrument bank is a restyle of existing controls.
- B's table maps 1:1 to `ParameterDescriptor` (Name/Type/Default/Yours) — the leader lines need
  one small JS interop for geometry.
- C's modes are a `data-mode` attribute + CSS on the existing layout; Present reuses the
  variants system as filmstrip.
- Blendable: C's mode switcher can host A's dock in Play mode and B's characteristics table as
  the params presentation.
