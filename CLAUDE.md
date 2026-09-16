# CLAUDE.md

Guidance for Claude Code when working in this repository.

## What this is

PlayBlazor generates component playgrounds by reflection: point it at an assembly and every public
`[Parameter]` becomes a typed control, next to a live specimen and the Razor markup that produces
it. It ships as one NuGet package with **no UI dependencies** — that constraint is load-bearing,
do not introduce a component library, CSS framework or JS library into `src/PlayBlazor`.

MudBlazor appears only in `demo/` and `tests/`, as the library being *pointed at*.

## Commands

```bash
dotnet build -c Release
dotnet test -c Release                                     # 367 tests, 4 skipped, ~2s
dotnet test -c Release -- --filter "FullyQualifiedName~X"  # single suite (MTP, note the `--`)
dotnet run --project demo/PlayBlazor.Demo.MudBlazor         # showcase on / and /explorer
```

Tests run on Microsoft.Testing.Platform (see `global.json`), not VSTest — VSTest-era flags such as
`--collect:"XPlat Code Coverage"` are silently ignored. Use `--coverage` and friends.

**Never pass `--nologo` to `dotnet test`.** MTP rejects unrecognised arguments by exiting 5 and
printing `Zero tests ran` — which reads like a discovery failure and sends you hunting the test
project. The migration guide claims `--nologo` still works; it does not. Exit code 5 means
*invalid arguments*, never *no tests found*.

Two `[Explicit]` suites (`RenderSweep`, `ListUnsupportedParameterTypes`) are diagnostic inventories
that print a report instead of asserting; run them on demand when auditing a component library.
They are parametrized per explored library (currently MudBlazor and Fluent UI), so a normal run
discovers all 367 tests but skips these 4 (2 suites × 2 libraries) rather than executing them.

**Filter an `[Explicit]` suite by its METHOD name, not its class name.** `--filter
"FullyQualifiedName~RenderSweepTests"` does not run the sweep: the NUnit adapter treats a
class-name match as a non-explicit run, skips both `[Explicit]` cases and reports `total: 1`,
which reads exactly like success. `--filter "FullyQualifiedName~RenderSweep_ReportsEveryComponentError"`
runs it. Same family as the `--nologo` trap above — the failure mode here is a green-looking
run that measured nothing.

## Layout

| Path | Role |
|------|------|
| `src/PlayBlazor` | The package. `Discovery/` (reflection → descriptors), `Model/`, `Rendering/` (specimen, interception, scaffolds), `CodeGen/` (Razor snippet), `State/` (playground + workspace state, permalink serialization), `Shell/` (UI: `Workspace/`, `Controls/`). |
| `tests/PlayBlazor.UnitTests` | bUnit + NUnit + AwesomeAssertions. |
| `demo/PlayBlazor.Demo.Shared` | The library-agnostic demo chrome (`DemoLanding`, `LibrarySwitcher`) shared by every showcase app. Zero UI dependencies — same constraint as `src/PlayBlazor`. |
| `demo/PlayBlazor.Demo.MudBlazor` | `PlaygroundConfig.cs` holds every preset, scaffold, variant and exclusion for the MudBlazor showcase. Namespace stays `PlayBlazor.DemoHost` even though the project and assembly are `PlayBlazor.Demo.MudBlazor`. |
| `demo/PlayBlazor.Demo.FluentUI` | Fluent UI Blazor showcase, curated to the same depth as MudBlazor's. `FluentPlaygroundConfig.cs` holds every preset, scaffold, variant and the `Infrastructure` deny-list, plus the hand-written `FluentIconCatalogue` (24 SVGs, no dependency on the 23 MB icons package). A few components are deliberately left imperfect rather than faked — see the comments beside them in that file. |
| `docs/superpowers` | Incubation-era design spec, milestone plans, and the UX concept prototypes (A→G) whose concept G v2 is the shell that exists today. Paths quoted inside them predate the `src`/`tests`/`demo` split. |

## Traps learned the hard way

- **The IL trimmer decapitates reflection.** A Release WASM publish strips constructors and
  `[Parameter]` properties from every component not referenced statically, so benches report
  "could not be instantiated" with four base parameters — but only for components the demo does
  not spell out in markup, which reads like a per-component bug. `demo/` roots the explored
  assembly with `<TrimmerRootAssembly Include="MudBlazor" />`. Any consumer needs the same.
- **Static web assets + `dotnet watch`** — any change to a `wwwroot` asset corrupts the manifest;
  CSS/JS then serve without a Content-Type behind `nosniff`, and the page boots blank. Restart the
  server after touching assets, never rely on hot reload for them.
- **`Debug.Assert` in a component lifecycle kills the process** in Debug builds — no `try`/`catch`
  or `ErrorBoundary` contains it. `DebugAssertGuard` converts assertion failures into catchable
  exceptions; leave it installed.
- **A host stylesheet can reach into the specimen.** `MudBlazor.min.css` centers bare `<button>`
  content — this has broken alignment three separate times. Set the property explicitly in
  workspace chrome rather than relying on the browser default.
- **Blazor scoped CSS has a hard component boundary.** Panel chrome belongs in
  `WorkspacePanel.razor.css`; use `::deep` to cross into a child. Keyframe names are rewritten by
  the scoping pass — verify an animation by reading `getComputedStyle(...).animationName`, not by
  eye.
- **bUnit assertions after an `await`** need `SetupVoid(...).SetVoidResult()` on the JS interop
  mock, otherwise the continuation never runs.
- **Razor attributes cannot nest quotes** — hoist the value into a `const`.
- Do not judge a thin dark chrome's luminance from a screenshot by eye; decode the PNG and probe
  the pixels.
- **A preset can compile, pass the full suite and the render sweep, and still render nothing
  visible or teach invalid markup.** Happened nine times curating Fluent. The component's own XML
  docs are not a reliable source — they caused one of the nine. The only check that held up was
  reading the library's own shipped implementation: decompiled `BuildRenderTree`, `lib.module.js`,
  `bundle.scp.css`.

## Conventions

- Central Package Management: versions live in `Directory.Packages.props`, never in a `.csproj`.
- Versioning is MinVer over `v`-prefixed git tags; do not hand-edit a `<Version>`.
- `TreatWarningsAsErrors` is on, and `GenerateDocumentationFile` is on for the package, so
  `CS1591` makes an undocumented public member a build error. Document what you expose.
