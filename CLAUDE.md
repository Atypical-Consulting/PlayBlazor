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
dotnet test -c Release                                     # 220 tests, ~2s
dotnet test -c Release -- --filter "FullyQualifiedName~X"  # single suite (MTP, note the `--`)
dotnet run --project demo/PlayBlazor.DemoHost              # showcase on / and /explorer
```

Tests run on Microsoft.Testing.Platform (see `global.json`), not VSTest — VSTest-era flags such as
`--collect:"XPlat Code Coverage"` are silently ignored. Use `--coverage` and friends.

Two `[Explicit]` suites (`RenderSweep`, `ListUnsupportedParameterTypes`) are diagnostic inventories
that print a report instead of asserting; run them on demand when auditing a component library.

## Layout

| Path | Role |
|------|------|
| `src/PlayBlazor` | The package. `Discovery/` (reflection → descriptors), `Model/`, `Rendering/` (specimen, interception, scaffolds), `CodeGen/` (Razor snippet), `State/` (playground + workspace state, permalink serialization), `Shell/` (UI: `Workspace/`, `Controls/`). |
| `tests/PlayBlazor.UnitTests` | bUnit + NUnit + AwesomeAssertions. |
| `demo/PlayBlazor.DemoHost` | `PlaygroundConfig.cs` holds every preset, scaffold, variant and exclusion for the MudBlazor showcase. |
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

## Conventions

- Central Package Management: versions live in `Directory.Packages.props`, never in a `.csproj`.
- Versioning is MinVer over `v`-prefixed git tags; do not hand-edit a `<Version>`.
- `TreatWarningsAsErrors` is on, and `GenerateDocumentationFile` is on for the package, so
  `CS1591` makes an undocumented public member a build error. Document what you expose.
