# Milestone 3 — Fluent UI Curation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the Fluent UI demo from an uncurated scaffold into a showcase at parity with MudBlazor's, and stop the package from offering controls for parameters no playground can drive.

**Architecture:** One package change first — a structural filter for Blazor's splatting parameter and for opaque `object` payloads, which together account for 195 of Fluent's 382 undrivable parameters and for MudBlazor's two largest groups. Then host curation in `FluentPlaygroundConfig`, ordered by what the render sweep says is broken: exclusions, generic closings, parent scaffolds, cascading context, and finally presets and variants by component family.

**Tech Stack:** .NET 10, Blazor WebAssembly, `Microsoft.FluentUI.AspNetCore.Components` 5.0.0-rc.5-26219.1, NUnit + bUnit + AwesomeAssertions on Microsoft.Testing.Platform.

**Spec:** [`docs/superpowers/specs/2026-09-07-multi-library-playgrounds-design.md`](../specs/2026-09-07-multi-library-playgrounds-design.md) — §2 sets the ambition (*parité complète avec MudBlazor*), §8 names this milestone.

**Evidence:** [`docs/superpowers/plans/sweeps/2026-render.txt`](sweeps/2026-render.txt) and [`sweeps/2026-unsupported.txt`](sweeps/2026-unsupported.txt), regenerated at commit `af31885`. Every target in this plan comes from those two files, not from guesswork — that ordering is the spec's own rule (§6, *"le sweep d'abord, la config ensuite"*).

## Global Constraints

- **`src/PlayBlazor` has zero UI dependencies.** No component library, CSS framework or JS library. Task 1 is the only task that touches it.
- **`demo/PlayBlazor.Demo.Shared` has zero UI dependencies either** — it is the library-agnostic chrome and no task here touches it.
- **Central Package Management:** versions live in `Directory.Packages.props`, never in a `.csproj`. No task here adds a package.
- **`TreatWarningsAsErrors` and `GenerateDocumentationFile` are on for `src/PlayBlazor`**: every public member needs an XML doc comment, `<param>` entries included, or `CS1591` fails the build.
- **The pinned Fluent package is a release candidate.** Its API moved between v4 and v5 in ways that already cost this project a blocked task: `FluentDesignTheme` and `FluentMenuProvider` do not exist, theming is a service (`IThemeService`, `ThemeSettings`), and `Icon.ToMarkup()` returns `MarkupString`. **Check the assembly before assuming a v4-era API exists.**
- **The starting measurement, from `af31885`:** Fluent 108 components — 83 healthy, 20 contained errors, 5 escaped exceptions; 47 distinct undrivable parameter types over 382 occurrences. MudBlazor 169 components — 158 healthy, 11 contained, 0 escaped.

**Commands used throughout.** The pinned SDK is shadowed by a Homebrew shim on at least one developer machine, so every dotnet command carries the prefix below. **Never pass `--nologo`:** Microsoft.Testing.Platform rejects it and reports the rejection as `Zero tests ran` with exit code 5, which reads like a discovery failure and is not one. Either invocation works:

```bash
PATH="$HOME/.dotnet:$PATH" dotnet build -c Release
PATH="$HOME/.dotnet:$PATH" dotnet test --configuration Release
PATH="$HOME/.dotnet:$PATH" ./tests/PlayBlazor.UnitTests/bin/Release/net10.0/PlayBlazor.UnitTests --filter "FullyQualifiedName~X"
```

Baseline at the start of this plan: **259 total, 255 succeeded, 4 skipped**. The 4 skipped are the two `[Explicit]` diagnostic suites × two library cases; they must never run in a normal run.

**Never modify `global.json` or `Directory.Build.props` to make a tool work.** A previous implementer silently downgraded the pinned SDK and left the tree dirty; it was caught in review and reverted. A tooling problem is a BLOCKED report, not a workaround.

## File Structure

| File | Responsibility |
|---|---|
| `src/PlayBlazor/Discovery/ControlKindResolver.cs` | **Modify.** Gains the structural test for an undrivable parameter shape. |
| `src/PlayBlazor/Discovery/ReflectionCatalogProvider.cs` | **Modify.** Marks such parameters so the shell can leave them out. |
| `src/PlayBlazor/Model/ControlKind.cs` | **Modify.** One new member, documented. |
| `demo/PlayBlazor.Demo.FluentUI/FluentPlaygroundConfig.cs` | **Modify throughout.** Today ~20 lines holding only the icon catalogue; grows into the curation, mirroring `demo/PlayBlazor.Demo.MudBlazor/PlaygroundConfig.cs` (431 lines) in shape. |
| `demo/PlayBlazor.Demo.FluentUI/FluentDemoFragments.razor` | **Create.** Reusable `RenderFragment`s for slot presets, mirroring the MudBlazor app's `DemoFragments.razor`. |
| `demo/PlayBlazor.Demo.FluentUI/FluentDemoFragmentSources.cs` | **Create.** The razor text of those fragments, so the generated snippet shows something runnable — mirrors `DemoFragmentSources.cs`. |
| `demo/PlayBlazor.Demo.FluentUI/Person.cs` | **Create.** Sample row type for the data-grid presets, mirroring the MudBlazor app's. |
| `demo/PlayBlazor.Demo.FluentUI/Pages/Index.razor` | **Modify.** Gains flagship benches, as the MudBlazor app has. |
| `docs/superpowers/plans/sweeps/*.txt` | **Modify.** Regenerated at the end; the numbers are the deliverable's proof. |

---

### Task 1: Stop offering controls for parameters nothing can drive

Two parameter shapes appear on nearly every component of every library and can never be driven from a generated control. They are 195 of Fluent's 382 undrivable-parameter occurrences, and MudBlazor's two largest groups. Filtering them in the package fixes all three libraries at once and makes the remaining inventory readable.

**Files:**
- Modify: `src/PlayBlazor/Model/ControlKind.cs`
- Modify: `src/PlayBlazor/Discovery/ControlKindResolver.cs`
- Modify: `src/PlayBlazor/Discovery/ReflectionCatalogProvider.cs`
- Test: `tests/PlayBlazor.UnitTests/Discovery/UndrivableParameterTests.cs`
- Test: `tests/PlayBlazor.UnitTests/Fixtures/SplattingFixture.razor`

**Interfaces:**
- Consumes: nothing.
- Produces: `ControlKind.Undrivable`, returned by `ControlKindResolver.Resolve` for a parameter whose type is exactly `object`, and set by `ReflectionCatalogProvider` for a parameter declared `[Parameter(CaptureUnmatchedValues = true)]`.

**The signals are structural, not nominal.** Verified by reflection against both pinned libraries:

| | Fluent | MudBlazor | Signal |
|---|---|---|---|
| splatting | `FluentButton.AdditionalAttributes` (`IReadOnlyDictionary<string,object>`) | `MudButton.UserAttributes` (`Dictionary<string,object>`) | `ParameterAttribute.CaptureUnmatchedValues == true` |
| payload | `FluentButton.Data` | `MudButton.Tag` | `PropertyType == typeof(object)` |

Different names, different dictionary types, same signal. **Do not match on property names** — that is the mistake the catalogue rule deliberately avoided, and it would break on the third library.

- [ ] **Step 1: Write the fixture**

Create `tests/PlayBlazor.UnitTests/Fixtures/SplattingFixture.razor`:

```razor
@namespace PlayBlazor.UnitTests.Fixtures
<div @attributes="Extra">@Label</div>
@code {
    /// <summary>Blazor's splatting parameter — every library has one, under a different name.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? Extra { get; set; }

    /// <summary>An opaque payload a host attaches to a component; nothing can type one in.</summary>
    [Parameter] public object? Payload { get; set; }

    [Parameter] public string? Label { get; set; }
}
```

- [ ] **Step 2: Write the failing test**

Create `tests/PlayBlazor.UnitTests/Discovery/UndrivableParameterTests.cs`:

```csharp
using AwesomeAssertions;
using NUnit.Framework;
using PlayBlazor.Discovery;
using PlayBlazor.Model;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Discovery;

/// <summary>
/// Two parameter shapes exist on nearly every component of every library and can never be driven
/// from a generated control: Blazor's splatting dictionary, and an opaque object payload. They are
/// recognised structurally, so the rule holds for a library this repo has never seen.
/// </summary>
public class UndrivableParameterTests
{
    private static ParameterDescriptor Parameter(string name)
        => new ReflectionCatalogProvider()
            .Describe(typeof(SplattingFixture))
            .Parameters.Single(p => p.Name == name);

    [Test]
    public void SplattingParameter_IsUndrivable()
        => Parameter("Extra").Kind.Should().Be(ControlKind.Undrivable);

    [Test]
    public void OpaqueObjectPayload_IsUndrivable()
        => Parameter("Payload").Kind.Should().Be(ControlKind.Undrivable);

    [Test]
    public void AnOrdinaryStringIsUntouched()
        => Parameter("Label").Kind.Should().Be(ControlKind.Text);

    [Test]
    public void SplattingIsRecognisedByItsAttribute_NotItsName()
    {
        // MudBlazor calls it UserAttributes and types it Dictionary<,>; Fluent calls it
        // AdditionalAttributes and types it IReadOnlyDictionary<,>. Only the attribute is common.
        var property = typeof(SplattingFixture).GetProperty("Extra")!;
        var attribute = property.GetCustomAttribute<Microsoft.AspNetCore.Components.ParameterAttribute>()!;

        attribute.CaptureUnmatchedValues.Should().BeTrue();
        property.Name.Should().NotBe("AdditionalAttributes");
    }
}
```

Add `using System.Reflection;` at the top for `GetCustomAttribute`.

- [ ] **Step 3: Run the test to verify it fails**

Run: `PATH="$HOME/.dotnet:$PATH" dotnet build -c Release`

Expected: compile failure, `CS0117: 'ControlKind' does not contain a definition for 'Undrivable'`. Capture the real output.

- [ ] **Step 4: Add the enum member**

In `src/PlayBlazor/Model/ControlKind.cs`, add after `Unsupported`:

```csharp
    /// <summary>
    /// A parameter no generated control can ever drive, whatever the library: Blazor's splatting
    /// dictionary (<c>CaptureUnmatchedValues</c>), or an opaque <see cref="object" /> payload a
    /// host attaches in code. Distinct from <see cref="Unsupported" />, which means "no control
    /// fits this type yet" and which a host catalogue can still rescue.
    /// </summary>
    Undrivable,
```

- [ ] **Step 5: Recognise the opaque payload in the resolver**

In `src/PlayBlazor/Discovery/ControlKindResolver.cs`, inside `Resolve`, immediately before the final `return (ControlKind.Unsupported, isNullable);`:

```csharp
        if (type == typeof(object))
        {
            // A bare object carries no shape to build a control from — MudBlazor's Tag and
            // Fluent's Data are both this. Not "unsupported yet": undrivable by construction.
            return (ControlKind.Undrivable, isNullable);
        }
```

- [ ] **Step 6: Recognise splatting in the provider**

In `src/PlayBlazor/Discovery/ReflectionCatalogProvider.cs`, in `Build`'s property loop, immediately after the `var (kind, isNullable) = ControlKindResolver.Resolve(...)` line and **before** the icon heuristics:

```csharp
            if (property.GetCustomAttribute<ParameterAttribute>()?.CaptureUnmatchedValues == true)
            {
                // The splatting parameter: MudBlazor's UserAttributes, Fluent's AdditionalAttributes.
                // The attribute is the only thing they have in common — never match on the name.
                kind = ControlKind.Undrivable;
            }
```

Note the loop already fetched the attribute to decide whether the property is a parameter at all; reuse that value rather than calling `GetCustomAttribute` twice if the surrounding code makes that natural.

- [ ] **Step 7: Run the tests to verify they pass**

Run: `PATH="$HOME/.dotnet:$PATH" ./tests/PlayBlazor.UnitTests/bin/Release/net10.0/PlayBlazor.UnitTests --filter "FullyQualifiedName~UndrivableParameterTests"`

Expected: 4 PASS.

- [ ] **Step 8: Check the shell leaves them out**

`PlaygroundView.razor.cs` and `PlaygroundWorkspace.razor.cs` each split parameters into a controllable set and an "not controlled here" list, keyed on `ControlKind`. Read both and confirm `Undrivable` lands in the uncontrolled list, not the controllable one — it will, if the split is a whitelist of drivable kinds, but verify rather than assume. If either uses a blacklist, add `Undrivable` to it.

- [ ] **Step 9: Run the whole suite**

Run: `PATH="$HOME/.dotnet:$PATH" ./tests/PlayBlazor.UnitTests/bin/Release/net10.0/PlayBlazor.UnitTests`

Expected: all pass. Existing tests that assert `ControlKind.Unsupported` for an `object`-typed parameter will now see `Undrivable` — if one fails, that is this change working; update the assertion and say so in your report.

- [ ] **Step 10: Commit**

```bash
git add src/PlayBlazor tests/PlayBlazor.UnitTests
git commit -m "Feat: stop offering controls for splatting and opaque payload parameters"
```

---

### Task 2: Curate the Fluent surface and close its generics

**Files:**
- Modify: `demo/PlayBlazor.Demo.FluentUI/FluentPlaygroundConfig.cs`
- Test: `tests/PlayBlazor.UnitTests/Shell/FluentCurationTests.cs`

**Interfaces:**
- Consumes: `PlayBlazorOptions.ComponentFilter`, `PlayBlazorOptions.For<TComponent>()` (both existing).
- Produces: a `FluentPlaygroundConfig` whose `Configure` filters the explorer and declares four generic closings. Later tasks add to the same method.

**Why a deny-list here when MudBlazor uses an allow-list.** MudBlazor's `Curated` set names 65 of 169 discovered types because most of what MudBlazor exposes is internal. Fluent exposes 108 of which ~94 are genuine components; enumerating 94 names would be noise that rots on every package bump, while the 14 to remove are stable categories. Say so in a comment — a reviewer will otherwise read the divergence as carelessness.

- [ ] **Step 1: Write the failing test**

Create `tests/PlayBlazor.UnitTests/Shell/FluentCurationTests.cs`:

```csharp
using AwesomeAssertions;
using Microsoft.FluentUI.AspNetCore.Components;
using NUnit.Framework;
using PlayBlazor.Demo.FluentUI;
using PlayBlazor.Discovery;

namespace PlayBlazor.UnitTests.Shell;

public class FluentCurationTests
{
    private static (PlayBlazorOptions Options, IReadOnlyList<PlayBlazor.Model.ComponentDescriptor> Listed) Curated()
    {
        var options = new PlayBlazorOptions();
        FluentPlaygroundConfig.Configure(options);
        var listed = new ReflectionCatalogProvider(options: options)
            .Discover(typeof(FluentButton).Assembly)
            .Where(c => !options.IsExcluded(c.Type) && (options.ComponentFilter?.Invoke(c.Type) ?? true))
            .ToList();
        return (options, listed);
    }

    [Test]
    public void ProvidersAndOptionsObjects_AreNotListed()
    {
        var names = Curated().Listed.Select(c => c.DisplayName).ToList();

        names.Should().NotContain("FluentToastProvider");
        names.Should().NotContain("FluentTooltipProvider");
        names.Should().NotContain("FluentProviders");
        names.Should().NotContain("ColumnReorderOptions");
        names.Should().NotContain("ColumnResizeOptions");
        names.Should().NotContain("Defer");
    }

    [Test]
    public void TheRealComponentsSurvive()
    {
        var names = Curated().Listed.Select(c => c.DisplayName).ToList();

        names.Should().Contain("FluentButton");
        names.Should().Contain("FluentDataGrid");
        names.Should().Contain("FluentNav");
        names.Count.Should().BeGreaterThan(80);
    }

    [Test]
    public void GenericsAreClosedWithATypeTheyAccept()
    {
        var options = Curated().Options;

        // Discovery closes an open generic with string first; all four reject it at construction.
        options.ResolvePreferredClosing(typeof(FluentCalendar<>).MakeGenericType(typeof(string)))
            .Should().Be(typeof(FluentCalendar<DateTime?>));
        options.ResolvePreferredClosing(typeof(FluentNumberInput<>).MakeGenericType(typeof(string)))
            .Should().Be(typeof(FluentNumberInput<int>));
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `PATH="$HOME/.dotnet:$PATH" ./tests/PlayBlazor.UnitTests/bin/Release/net10.0/PlayBlazor.UnitTests --filter "FullyQualifiedName~FluentCurationTests"`

Expected: FAIL — providers are still listed and the closings are not declared.

- [ ] **Step 3: Add the exclusions and the closings**

In `demo/PlayBlazor.Demo.FluentUI/FluentPlaygroundConfig.cs`, inside `Configure`, before the existing catalogue registration:

```csharp
        // Fluent exposes 108 public components, of which ~94 are things a user actually plays with.
        // MudBlazor's config uses an allow-list because most of ITS surface is internal; here the
        // reverse holds, so name what to drop. These are stable categories, not a list that rots:
        // service providers, internal render helpers, and settings objects that are ComponentBase
        // by inheritance rather than by intent.
        options.ComponentFilter = type => !Infrastructure.Contains(StripArity(type.Name));

        // Discovery closes an open generic with string, then int. All four of these reject string
        // at construction, naming the types they accept — so declare the closing worth playing.
        options.For<FluentCalendar<DateTime?>>();
        options.For<FluentDatePicker<DateTime?>>();
        options.For<FluentTimePicker<DateTime?>>();
        options.For<FluentNumberInput<int>>();
```

And at class scope:

```csharp
    /// <summary>Not components a user plays with: providers, internal helpers, settings objects.</summary>
    private static readonly HashSet<string> Infrastructure =
    [
        "ColumnReorderOptions", "ColumnResizeOptions", "Defer", "FluentDialogProvider",
        "FluentKeyCodeProvider", "FluentMessageBarProvider", "FluentOptionString",
        "FluentProviders", "FluentToastProvider", "FluentTooltipProvider", "FreeOptionOutput",
    ];

    private static string StripArity(string name)
    {
        var backtick = name.IndexOf('`');
        return backtick < 0 ? name : name[..backtick];
    }
```

`StripArity` is duplicated from the MudBlazor config on purpose: the two apps share no code by design, and a shared helper would need a home in `Demo.Shared`, which must stay free of library knowledge. Note that in a comment.

- [ ] **Step 4: Run the tests to verify they pass**

Run: `PATH="$HOME/.dotnet:$PATH" ./tests/PlayBlazor.UnitTests/bin/Release/net10.0/PlayBlazor.UnitTests --filter "FullyQualifiedName~FluentCurationTests"`

Expected: 3 PASS.

- [ ] **Step 5: Confirm the four generics now instantiate**

Run the render sweep and read the Fluent section:

```
PATH="$HOME/.dotnet:$PATH" ./tests/PlayBlazor.UnitTests/bin/Release/net10.0/PlayBlazor.UnitTests --filter "FullyQualifiedName~RenderSweepTests"
```

Expected: `FluentCalendar`, `FluentDatePicker`, `FluentNumberInput` and `FluentTimePicker` no longer report "Defaults not captured". Paste the Fluent `SWEEP` line into your report — the count is the evidence.

- [ ] **Step 6: Run the whole suite and commit**

```bash
PATH="$HOME/.dotnet:$PATH" ./tests/PlayBlazor.UnitTests/bin/Release/net10.0/PlayBlazor.UnitTests
git add demo/PlayBlazor.Demo.FluentUI tests/PlayBlazor.UnitTests
git commit -m "Feat: curate the Fluent surface and close its date and number generics"
```

---

### Task 3: Scaffold the components that cannot live alone

Eleven components fail because they require a parent. Five of them **escape** the error boundary, which in the running app takes down more than the preview — they are the most urgent items in the whole milestone.

**Files:**
- Modify: `demo/PlayBlazor.Demo.FluentUI/FluentPlaygroundConfig.cs`
- Create: `demo/PlayBlazor.Demo.FluentUI/Person.cs`
- Test: `tests/PlayBlazor.UnitTests/Shell/FluentScaffoldTests.cs`

**Interfaces:**
- Consumes: `ComponentOptionsBuilder<T>.Scaffold(Func<RenderFragment, RenderFragment> scaffold, string? source = null)`, `.Related<TOther>()`.
- Produces: scaffolds registered for the eleven types listed below.

**The targets, from `sweeps/2026-render.txt`:**

| Component | Needs | Sweep status |
|---|---|---|
| `FluentAppBarItem` | `FluentAppBar` | **escaped** |
| `FluentDataGridCell` | `FluentDataGrid` | **escaped** |
| `FluentDataGridRow` | `FluentDataGrid` | **escaped** |
| `FluentNavCategory` | `FluentNav` | **escaped** |
| `FluentNavItem` | `FluentNav` | **escaped** |
| `FluentNavSectionHeader` | `FluentNav` | contained |
| `FluentRadio` | `FluentRadioGroup` | contained |
| `FluentWizardStep` | `FluentWizard` | contained |
| `FluentWizardStepValidator` | `FluentWizardStep` | contained |
| `PropertyColumn`, `SelectColumn`, `TemplateColumn`, `HierarchicalSelectColumn` | `FluentDataGrid` | contained |
| `FluentPaginator` | a `PaginationState` | contained |

- [ ] **Step 1: Create the sample row type**

Create `demo/PlayBlazor.Demo.FluentUI/Person.cs`, mirroring the MudBlazor app's:

```csharp
namespace PlayBlazor.Demo.FluentUI;

/// <summary>Sample rows for the data-grid presets, so a grid has something to show.</summary>
public sealed record Person(string Name, string Role, int Age);
```

- [ ] **Step 2: Write the failing test**

Create `tests/PlayBlazor.UnitTests/Shell/FluentScaffoldTests.cs`:

```csharp
using AwesomeAssertions;
using Microsoft.FluentUI.AspNetCore.Components;
using NUnit.Framework;
using PlayBlazor.Demo.FluentUI;

namespace PlayBlazor.UnitTests.Shell;

public class FluentScaffoldTests
{
    private static PlayBlazorOptions Configured()
    {
        var options = new PlayBlazorOptions();
        FluentPlaygroundConfig.Configure(options);
        return options;
    }

    [TestCase(typeof(FluentAppBarItem))]
    [TestCase(typeof(FluentNavItem))]
    [TestCase(typeof(FluentNavCategory))]
    [TestCase(typeof(FluentNavSectionHeader))]
    [TestCase(typeof(FluentWizardStep))]
    public void ComponentsThatNeedAParent_HaveOne(Type component)
        => Configured().TryGetScaffold(component, out _).Should().BeTrue();

    [Test]
    public void EveryScaffoldCarriesItsRazorSource()
    {
        // The generated snippet wraps the specimen in the scaffold's text; a scaffold without
        // source produces a snippet that does not reproduce what the bench shows.
        var options = Configured();

        options.TryGetScaffold(typeof(FluentNavItem), out _).Should().BeTrue();
        options.TryGetScaffoldSource(typeof(FluentNavItem), out var source).Should().BeTrue();
        source.Should().Contain("{specimen}");
    }
}
```

- [ ] **Step 3: Run the test to verify it fails**

Run: `PATH="$HOME/.dotnet:$PATH" ./tests/PlayBlazor.UnitTests/bin/Release/net10.0/PlayBlazor.UnitTests --filter "FullyQualifiedName~FluentScaffoldTests"`

Expected: FAIL — no scaffolds registered.

- [ ] **Step 4: Register the scaffolds**

In `FluentPlaygroundConfig.Configure`. The shape for every one is the same — wrap the specimen in the parent the library demands. Worked example for two, then apply the same shape to the rest of the table above:

```csharp
        options.For<FluentNavItem>()
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<FluentNav>(0);
                builder.AddAttribute(1, nameof(FluentNav.ChildContent), specimen);
                builder.CloseComponent();
            },
            "<FluentNav>\n    {specimen}\n</FluentNav>")
            .Related<FluentNav>();

        options.For<PropertyColumn<Person, string>>()
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<FluentDataGrid<Person>>(0);
                builder.AddAttribute(1, nameof(FluentDataGrid<Person>.Items), SampleRows.AsQueryable());
                builder.AddAttribute(2, nameof(FluentDataGrid<Person>.ChildContent), specimen);
                builder.CloseComponent();
            },
            "<FluentDataGrid Items=\"@_people\">\n    {specimen}\n</FluentDataGrid>")
            .Related<FluentDataGrid<Person>>();
```

with, at class scope:

```csharp
    private static readonly List<Person> SampleRows =
    [
        new("Ada Lovelace", "Analyst", 36),
        new("Grace Hopper", "Rear Admiral", 45),
        new("Alan Turing", "Cryptanalyst", 41),
    ];
```

**Before writing each one, check the parent's actual API on the pinned RC** — the parameter that takes children may not be called `ChildContent`, and `FluentDataGrid`'s items parameter may want `IQueryable<T>` rather than `IEnumerable<T>`. The compiler will tell you; `nameof(...)` keeps a rename honest. For `FluentPaginator`, the missing piece is a `PaginationState` instance rather than a parent component — give it a `Parameter` preset instead of a scaffold.

- [ ] **Step 5: Run the tests to verify they pass**

Run: `PATH="$HOME/.dotnet:$PATH" ./tests/PlayBlazor.UnitTests/bin/Release/net10.0/PlayBlazor.UnitTests --filter "FullyQualifiedName~FluentScaffoldTests"`

Expected: 6 PASS.

- [ ] **Step 6: Confirm the escapes are gone**

Run the render sweep. **Expected: 0 escaped exceptions for Fluent** — that is this task's real success criterion, and MudBlazor's number to match. Paste the `SWEEP` line into your report. If any escape remains, name it and say what parent it wanted; do not move on.

- [ ] **Step 7: Run the whole suite and commit**

```bash
PATH="$HOME/.dotnet:$PATH" ./tests/PlayBlazor.UnitTests/bin/Release/net10.0/PlayBlazor.UnitTests
git add demo/PlayBlazor.Demo.FluentUI tests/PlayBlazor.UnitTests
git commit -m "Feat: scaffold the Fluent components that need a parent"
```

---

### Task 4: The cascading-context and required-parameter cases

Four components fail for reasons a scaffold alone does not fix.

**Files:**
- Modify: `demo/PlayBlazor.Demo.FluentUI/FluentPlaygroundConfig.cs`
- Test: extend `tests/PlayBlazor.UnitTests/Shell/FluentScaffoldTests.cs`

**Interfaces:**
- Consumes: `ComponentOptionsBuilder<T>.Scaffold(...)`, `.Parameter(string, object?, string?)`.
- Produces: scaffolds for the two validation components; parameter presets for the two with required values.

**The targets, from `sweeps/2026-render.txt`:**

| Component | Sweep message | Fix |
|---|---|---|
| `FluentValidationMessage` | *requires a cascading parameter of type EditContext* | scaffold inside an `EditForm` |
| `FluentValidationSummary` | *ValidationSummary requires a cascading parameter of type EditContext* | same |
| `AddTag` | *Name property is required* | `Parameter` preset for `Name` |
| `FluentKeyCode` | *The Anchor parameter must be set to the ID of an element. Or the ChildContent must be set* | `Slot` preset for `ChildContent` |

- [ ] **Step 1: Write the failing test**

Append to `tests/PlayBlazor.UnitTests/Shell/FluentScaffoldTests.cs`:

```csharp
    [Test]
    public void ValidationComponents_RenderInsideAnEditForm()
    {
        var options = Configured();

        options.TryGetScaffoldSource(typeof(FluentValidationSummary), out var source).Should().BeTrue();
        source.Should().Contain("EditForm");
    }

    [Test]
    public void ComponentsWithARequiredValue_GetOne()
    {
        var options = Configured();

        options.TryGetParameterPreset(typeof(AddTag), "Name", out var name).Should().BeTrue();
        name.Should().NotBeNull();
        options.TryGetSlotPreset(typeof(FluentKeyCode), "ChildContent", out _).Should().BeTrue();
    }
```

- [ ] **Step 2: Run it to verify it fails**

Run: `PATH="$HOME/.dotnet:$PATH" ./tests/PlayBlazor.UnitTests/bin/Release/net10.0/PlayBlazor.UnitTests --filter "FullyQualifiedName~FluentScaffoldTests"`

Expected: the two new tests FAIL; the existing six still pass.

- [ ] **Step 3: Register them**

In `FluentPlaygroundConfig.Configure`. The validation pair needs a real `EditContext`, which means a model instance and an `EditForm` around the specimen:

```csharp
        // Both validation components demand a cascading EditContext and say so by name. An
        // EditForm over a throwaway model is the smallest thing that supplies one.
        options.For<FluentValidationSummary>()
            .Scaffold(specimen => builder =>
            {
                builder.OpenComponent<EditForm>(0);
                builder.AddAttribute(1, nameof(EditForm.Model), ValidationModel);
                builder.AddAttribute(2, nameof(EditForm.ChildContent),
                    (RenderFragment<EditContext>)(_ => specimen));
                builder.CloseComponent();
            },
            "<EditForm Model=\"@_model\">\n    {specimen}\n</EditForm>");

        options.For<AddTag>()
            .Parameter(nameof(AddTag.Name), "priority");
```

with, at class scope:

```csharp
    private static readonly Person ValidationModel = new("Ada Lovelace", "Analyst", 36);
```

`EditForm` lives in `Microsoft.AspNetCore.Components.Forms`; add the `using`. Repeat the same scaffold shape for `FluentValidationMessage`, and give `FluentKeyCode` a `Slot` preset for `ChildContent` in the style Task 5 uses for every other slot.

- [ ] **Step 4: Run the tests to verify they pass**

Run: `PATH="$HOME/.dotnet:$PATH" ./tests/PlayBlazor.UnitTests/bin/Release/net10.0/PlayBlazor.UnitTests --filter "FullyQualifiedName~FluentScaffoldTests"`

Expected: 8 PASS.

- [ ] **Step 5: Confirm the sweep and commit**

Run the render sweep. Expected: these four no longer appear under CONTAINED. Paste the `SWEEP` line.

```bash
PATH="$HOME/.dotnet:$PATH" ./tests/PlayBlazor.UnitTests/bin/Release/net10.0/PlayBlazor.UnitTests
git add demo/PlayBlazor.Demo.FluentUI tests/PlayBlazor.UnitTests
git commit -m "Feat: give the Fluent validation and required-value components what they need"
```

---

### Task 5: Presets for the form inputs

**Files:**
- Modify: `demo/PlayBlazor.Demo.FluentUI/FluentPlaygroundConfig.cs`
- Create or modify: `demo/PlayBlazor.Demo.FluentUI/FluentDemoFragments.razor`, `FluentDemoFragmentSources.cs`
- Test: `tests/PlayBlazor.UnitTests/Shell/FluentPresetTests.cs`

**Interfaces:**
- Consumes: `ComponentOptionsBuilder<T>.Slot(string, RenderFragment, string?)`, `.Parameter(string, object?, string?)`, `.Variant(string, Action<PlaygroundVariantBuilder>)`; the `Person` record and `SampleRows` list from Task 3.
- Produces: presets for the components listed below. The other preset tasks add to the same `Configure` method and the same test file.

**The shape, per component:**

1. A **slot preset** for its content parameter, with realistic sample text and its `source` string — so the bench shows something and the generated snippet reproduces it. Mirror `demo/PlayBlazor.Demo.MudBlazor/PlaygroundConfig.cs`'s use of `.Slot(name, fragment, source)`, and its `DemoFragments.razor` / `DemoFragmentSources.cs` pair for anything longer than a line of text.
2. A **parameter preset** for any value the component needs to look like itself (an icon, a label, a count).
3. **Two to four variants**, each named after a real configuration from Fluent's own documentation, via `.Variant(name, v => v.Set(...))`.

Worked example, in the style the MudBlazor config already uses:

```csharp
        options.For<FluentButton>()
            .Slot(nameof(FluentButton.ChildContent), b => b.AddContent(0, "Click me"), "Click me")
            .Variant("Accent", v => v.Set(nameof(FluentButton.Appearance), Appearance.Accent))
            .Variant("Lightweight", v => v.Set(nameof(FluentButton.Appearance), Appearance.Lightweight))
            .Variant("Disabled", v => v.Set(nameof(FluentButton.Disabled), true));
```

**Check each component's real API on the pinned RC before writing its entry.** `Appearance` above is illustrative; v5 may name it differently, and a v4-era guess is exactly what blocked an earlier task in this project — `FluentDesignTheme` and `FluentMenuProvider` turned out not to exist at all.

**The components:** `FluentTextInput`, `FluentTextArea`, `FluentNumberInput<int>`, `FluentSelect`, `FluentCombobox`, `FluentAutocomplete`, `FluentListbox`, `FluentOption`, `FluentCheckbox`, `FluentSwitch`, `FluentRadio`, `FluentRadioGroup`, `FluentSlider`, `FluentDatePicker<DateTime?>`, `FluentTimePicker<DateTime?>`, `FluentCalendar<DateTime?>`, `FluentColorPicker`, `FluentColorPickerInput`, `FluentField`, `FluentLabel`, `FluentInputFile`.

Fifteen of these take an `IEnumerable<string>` of items, per `sweeps/2026-unsupported.txt` — give them one shared sample list at class scope rather than repeating a literal per component. The four generic closings come from Task 2; use those exact closings, never new ones.

- [ ] **Step 1: Write the failing test**

In `tests/PlayBlazor.UnitTests/Shell/FluentPresetTests.cs`, add one `[TestCase]` per component this task names, driving a single assertion that the component carries at least one slot, parameter preset, variant or scaffold. One case per component, so a missing one is named in the failure rather than hidden in a count:

```csharp
    [TestCase(typeof(FluentButton))]
    // … one line per component in this task's list …
    public void ComponentHasCuration(Type component)
    {
        var options = new PlayBlazorOptions();
        FluentPlaygroundConfig.Configure(options);

        var curated = options.GetVariants(component).Count > 0
            || options.TryGetSlotPreset(component, "ChildContent", out _)
            || options.TryGetScaffold(component, out _);

        curated.Should().BeTrue($"{component.Name} should carry a preset, slot or variant");
    }
```

If the file does not exist yet, create it with `using AwesomeAssertions; using Microsoft.FluentUI.AspNetCore.Components; using NUnit.Framework; using PlayBlazor.Demo.FluentUI;` and `namespace PlayBlazor.UnitTests.Shell;`. If a sibling preset task already created it, add your cases to it — never a second file.

- [ ] **Step 2: Run it and watch it fail**

Run: `PATH="$HOME/.dotnet:$PATH" ./tests/PlayBlazor.UnitTests/bin/Release/net10.0/PlayBlazor.UnitTests --filter "FullyQualifiedName~FluentPresetTests"`

Expected: this task's cases FAIL, each naming its component. Capture the output.

- [ ] **Step 3: Add the entries**

Add them to `FluentPlaygroundConfig.Configure`, grouped under a comment naming the family (form input).

- [ ] **Step 4: Run the filtered test, then the whole suite**

```bash
PATH="$HOME/.dotnet:$PATH" ./tests/PlayBlazor.UnitTests/bin/Release/net10.0/PlayBlazor.UnitTests --filter "FullyQualifiedName~FluentPresetTests"
PATH="$HOME/.dotnet:$PATH" ./tests/PlayBlazor.UnitTests/bin/Release/net10.0/PlayBlazor.UnitTests
```

Expected: the new cases PASS and nothing else regressed.

- [ ] **Step 5: Commit**

```bash
git add demo/PlayBlazor.Demo.FluentUI tests/PlayBlazor.UnitTests
git commit -m "Feat: preset the Fluent form input components"
```

---

### Task 6: Presets for display and layout

**Files:**
- Modify: `demo/PlayBlazor.Demo.FluentUI/FluentPlaygroundConfig.cs`
- Create or modify: `demo/PlayBlazor.Demo.FluentUI/FluentDemoFragments.razor`, `FluentDemoFragmentSources.cs`
- Test: `tests/PlayBlazor.UnitTests/Shell/FluentPresetTests.cs`

**Interfaces:**
- Consumes: `ComponentOptionsBuilder<T>.Slot(string, RenderFragment, string?)`, `.Parameter(string, object?, string?)`, `.Variant(string, Action<PlaygroundVariantBuilder>)`; the `Person` record and `SampleRows` list from Task 3.
- Produces: presets for the components listed below. The other preset tasks add to the same `Configure` method and the same test file.

**The shape, per component:**

1. A **slot preset** for its content parameter, with realistic sample text and its `source` string — so the bench shows something and the generated snippet reproduces it. Mirror `demo/PlayBlazor.Demo.MudBlazor/PlaygroundConfig.cs`'s use of `.Slot(name, fragment, source)`, and its `DemoFragments.razor` / `DemoFragmentSources.cs` pair for anything longer than a line of text.
2. A **parameter preset** for any value the component needs to look like itself (an icon, a label, a count).
3. **Two to four variants**, each named after a real configuration from Fluent's own documentation, via `.Variant(name, v => v.Set(...))`.

Worked example, in the style the MudBlazor config already uses:

```csharp
        options.For<FluentButton>()
            .Slot(nameof(FluentButton.ChildContent), b => b.AddContent(0, "Click me"), "Click me")
            .Variant("Accent", v => v.Set(nameof(FluentButton.Appearance), Appearance.Accent))
            .Variant("Lightweight", v => v.Set(nameof(FluentButton.Appearance), Appearance.Lightweight))
            .Variant("Disabled", v => v.Set(nameof(FluentButton.Disabled), true));
```

**Check each component's real API on the pinned RC before writing its entry.** `Appearance` above is illustrative; v5 may name it differently, and a v4-era guess is exactly what blocked an earlier task in this project — `FluentDesignTheme` and `FluentMenuProvider` turned out not to exist at all.

**The components:** `FluentBadge`, `FluentCounterBadge`, `FluentPresenceBadge`, `FluentAvatar`, `FluentCard`, `FluentDivider`, `FluentGrid`, `FluentGridItem`, `FluentStack`, `FluentSpacer`, `FluentLayout`, `FluentLayoutItem`, `FluentText`, `FluentHighlighter`, `FluentImage`, `FluentSkeleton`, `FluentProgress`, `FluentProgressBar`, `FluentProgressRing`, `FluentSpinner`, `FluentRatingDisplay`, `FluentMultiSplitter`, `FluentMultiSplitterPane`.

`FluentBadge` and `FluentCounterBadge` each carry `OffsetX`/`OffsetY` typed `SByte?`, which the sweep lists as undrivable — leave them alone; a numeric control for `sbyte` is outside this task.

- [ ] **Step 1: Write the failing test**

In `tests/PlayBlazor.UnitTests/Shell/FluentPresetTests.cs`, add one `[TestCase]` per component this task names, driving a single assertion that the component carries at least one slot, parameter preset, variant or scaffold. One case per component, so a missing one is named in the failure rather than hidden in a count:

```csharp
    [TestCase(typeof(FluentButton))]
    // … one line per component in this task's list …
    public void ComponentHasCuration(Type component)
    {
        var options = new PlayBlazorOptions();
        FluentPlaygroundConfig.Configure(options);

        var curated = options.GetVariants(component).Count > 0
            || options.TryGetSlotPreset(component, "ChildContent", out _)
            || options.TryGetScaffold(component, out _);

        curated.Should().BeTrue($"{component.Name} should carry a preset, slot or variant");
    }
```

If the file does not exist yet, create it with `using AwesomeAssertions; using Microsoft.FluentUI.AspNetCore.Components; using NUnit.Framework; using PlayBlazor.Demo.FluentUI;` and `namespace PlayBlazor.UnitTests.Shell;`. If a sibling preset task already created it, add your cases to it — never a second file.

- [ ] **Step 2: Run it and watch it fail**

Run: `PATH="$HOME/.dotnet:$PATH" ./tests/PlayBlazor.UnitTests/bin/Release/net10.0/PlayBlazor.UnitTests --filter "FullyQualifiedName~FluentPresetTests"`

Expected: this task's cases FAIL, each naming its component. Capture the output.

- [ ] **Step 3: Add the entries**

Add them to `FluentPlaygroundConfig.Configure`, grouped under a comment naming the family (display and layout).

- [ ] **Step 4: Run the filtered test, then the whole suite**

```bash
PATH="$HOME/.dotnet:$PATH" ./tests/PlayBlazor.UnitTests/bin/Release/net10.0/PlayBlazor.UnitTests --filter "FullyQualifiedName~FluentPresetTests"
PATH="$HOME/.dotnet:$PATH" ./tests/PlayBlazor.UnitTests/bin/Release/net10.0/PlayBlazor.UnitTests
```

Expected: the new cases PASS and nothing else regressed.

- [ ] **Step 5: Commit**

```bash
git add demo/PlayBlazor.Demo.FluentUI tests/PlayBlazor.UnitTests
git commit -m "Feat: preset the Fluent display and layout components"
```

---

### Task 7: Presets for navigation and overlays

**Files:**
- Modify: `demo/PlayBlazor.Demo.FluentUI/FluentPlaygroundConfig.cs`
- Create or modify: `demo/PlayBlazor.Demo.FluentUI/FluentDemoFragments.razor`, `FluentDemoFragmentSources.cs`
- Test: `tests/PlayBlazor.UnitTests/Shell/FluentPresetTests.cs`

**Interfaces:**
- Consumes: `ComponentOptionsBuilder<T>.Slot(string, RenderFragment, string?)`, `.Parameter(string, object?, string?)`, `.Variant(string, Action<PlaygroundVariantBuilder>)`; the `Person` record and `SampleRows` list from Task 3.
- Produces: presets for the components listed below. The other preset tasks add to the same `Configure` method and the same test file.

**The shape, per component:**

1. A **slot preset** for its content parameter, with realistic sample text and its `source` string — so the bench shows something and the generated snippet reproduces it. Mirror `demo/PlayBlazor.Demo.MudBlazor/PlaygroundConfig.cs`'s use of `.Slot(name, fragment, source)`, and its `DemoFragments.razor` / `DemoFragmentSources.cs` pair for anything longer than a line of text.
2. A **parameter preset** for any value the component needs to look like itself (an icon, a label, a count).
3. **Two to four variants**, each named after a real configuration from Fluent's own documentation, via `.Variant(name, v => v.Set(...))`.

Worked example, in the style the MudBlazor config already uses:

```csharp
        options.For<FluentButton>()
            .Slot(nameof(FluentButton.ChildContent), b => b.AddContent(0, "Click me"), "Click me")
            .Variant("Accent", v => v.Set(nameof(FluentButton.Appearance), Appearance.Accent))
            .Variant("Lightweight", v => v.Set(nameof(FluentButton.Appearance), Appearance.Lightweight))
            .Variant("Disabled", v => v.Set(nameof(FluentButton.Disabled), true));
```

**Check each component's real API on the pinned RC before writing its entry.** `Appearance` above is illustrative; v5 may name it differently, and a v4-era guess is exactly what blocked an earlier task in this project — `FluentDesignTheme` and `FluentMenuProvider` turned out not to exist at all.

**The components:** `FluentNav`, `FluentNavItem`, `FluentNavCategory`, `FluentNavSectionHeader`, `FluentTabs`, `FluentTab`, `FluentMenu`, `FluentMenuItem`, `FluentMenuList`, `FluentMenuButton`, `FluentSplitButton`, `FluentToggleButton`, `FluentCompoundButton`, `FluentAnchorButton`, `FluentLink`, `FluentAccordion`, `FluentAccordionItem`, `FluentDialog`, `FluentDialogBody`, `FluentMessageBox`, `FluentMessageBar`, `FluentToast`, `FluentTooltip`, `FluentPopover`, `FluentOverlay`, `FluentWizard`, `FluentWizardStep`, `FluentTreeView`, `FluentTreeItem`, `FluentAppBar`, `FluentAppBarItem`.

Five of these already have a scaffold from Task 3 — `FluentNavItem`, `FluentNavCategory`, `FluentNavSectionHeader`, `FluentWizardStep`, `FluentAppBarItem`. Add presets to the SAME `options.For<T>()` chain rather than opening a second one for the same type.

`FluentTooltip` reported *"&lt;FluentTooltipProvider /&gt; needs to be added to the main layout"* in the sweep, but the demo's `MainLayout` renders `<FluentProviders />`, which composes that provider — check whether it already works in the running app before adding anything for it, and record which you found in your report.

- [ ] **Step 1: Write the failing test**

In `tests/PlayBlazor.UnitTests/Shell/FluentPresetTests.cs`, add one `[TestCase]` per component this task names, driving a single assertion that the component carries at least one slot, parameter preset, variant or scaffold. One case per component, so a missing one is named in the failure rather than hidden in a count:

```csharp
    [TestCase(typeof(FluentButton))]
    // … one line per component in this task's list …
    public void ComponentHasCuration(Type component)
    {
        var options = new PlayBlazorOptions();
        FluentPlaygroundConfig.Configure(options);

        var curated = options.GetVariants(component).Count > 0
            || options.TryGetSlotPreset(component, "ChildContent", out _)
            || options.TryGetScaffold(component, out _);

        curated.Should().BeTrue($"{component.Name} should carry a preset, slot or variant");
    }
```

If the file does not exist yet, create it with `using AwesomeAssertions; using Microsoft.FluentUI.AspNetCore.Components; using NUnit.Framework; using PlayBlazor.Demo.FluentUI;` and `namespace PlayBlazor.UnitTests.Shell;`. If a sibling preset task already created it, add your cases to it — never a second file.

- [ ] **Step 2: Run it and watch it fail**

Run: `PATH="$HOME/.dotnet:$PATH" ./tests/PlayBlazor.UnitTests/bin/Release/net10.0/PlayBlazor.UnitTests --filter "FullyQualifiedName~FluentPresetTests"`

Expected: this task's cases FAIL, each naming its component. Capture the output.

- [ ] **Step 3: Add the entries**

Add them to `FluentPlaygroundConfig.Configure`, grouped under a comment naming the family (navigation and overlay).

- [ ] **Step 4: Run the filtered test, then the whole suite**

```bash
PATH="$HOME/.dotnet:$PATH" ./tests/PlayBlazor.UnitTests/bin/Release/net10.0/PlayBlazor.UnitTests --filter "FullyQualifiedName~FluentPresetTests"
PATH="$HOME/.dotnet:$PATH" ./tests/PlayBlazor.UnitTests/bin/Release/net10.0/PlayBlazor.UnitTests
```

Expected: the new cases PASS and nothing else regressed.

- [ ] **Step 5: Commit**

```bash
git add demo/PlayBlazor.Demo.FluentUI tests/PlayBlazor.UnitTests
git commit -m "Feat: preset the Fluent navigation and overlay components"
```

---

### Task 8: Presets for the data components

**Files:**
- Modify: `demo/PlayBlazor.Demo.FluentUI/FluentPlaygroundConfig.cs`
- Create or modify: `demo/PlayBlazor.Demo.FluentUI/FluentDemoFragments.razor`, `FluentDemoFragmentSources.cs`
- Test: `tests/PlayBlazor.UnitTests/Shell/FluentPresetTests.cs`

**Interfaces:**
- Consumes: `ComponentOptionsBuilder<T>.Slot(string, RenderFragment, string?)`, `.Parameter(string, object?, string?)`, `.Variant(string, Action<PlaygroundVariantBuilder>)`; the `Person` record and `SampleRows` list from Task 3.
- Produces: presets for the components listed below. The other preset tasks add to the same `Configure` method and the same test file.

**The shape, per component:**

1. A **slot preset** for its content parameter, with realistic sample text and its `source` string — so the bench shows something and the generated snippet reproduces it. Mirror `demo/PlayBlazor.Demo.MudBlazor/PlaygroundConfig.cs`'s use of `.Slot(name, fragment, source)`, and its `DemoFragments.razor` / `DemoFragmentSources.cs` pair for anything longer than a line of text.
2. A **parameter preset** for any value the component needs to look like itself (an icon, a label, a count).
3. **Two to four variants**, each named after a real configuration from Fluent's own documentation, via `.Variant(name, v => v.Set(...))`.

Worked example, in the style the MudBlazor config already uses:

```csharp
        options.For<FluentButton>()
            .Slot(nameof(FluentButton.ChildContent), b => b.AddContent(0, "Click me"), "Click me")
            .Variant("Accent", v => v.Set(nameof(FluentButton.Appearance), Appearance.Accent))
            .Variant("Lightweight", v => v.Set(nameof(FluentButton.Appearance), Appearance.Lightweight))
            .Variant("Disabled", v => v.Set(nameof(FluentButton.Disabled), true));
```

**Check each component's real API on the pinned RC before writing its entry.** `Appearance` above is illustrative; v5 may name it differently, and a v4-era guess is exactly what blocked an earlier task in this project — `FluentDesignTheme` and `FluentMenuProvider` turned out not to exist at all.

**The components:** `FluentDataGrid<Person>`, `FluentDataGridRow`, `FluentDataGridCell`, `PropertyColumn<Person, string>`, `SelectColumn<Person>`, `TemplateColumn<Person>`, `HierarchicalSelectColumn<Person>`, `FluentPaginator`, `FluentSortableList`, `FluentDragContainer`, `FluentDropZone`, `FluentOverflow`, `FluentPullToRefresh`, `FluentKeyCode`.

Reuse the `Person` record and the `SampleRows` list Task 3 created — never define new ones. The grid and the four column types already have scaffolds from Task 3; add presets to the same `options.For<T>()` chains.

`Defer` is deliberately absent: Task 2 puts it in the `Infrastructure` exclusion set, so it is never listed and needs no presets. `FluentErrorBoundary` IS listed — it looked like infrastructure while writing this plan, but it is a documented component with real parameters, so it belongs in a preset task's scope.

- [ ] **Step 1: Write the failing test**

In `tests/PlayBlazor.UnitTests/Shell/FluentPresetTests.cs`, add one `[TestCase]` per component this task names, driving a single assertion that the component carries at least one slot, parameter preset, variant or scaffold. One case per component, so a missing one is named in the failure rather than hidden in a count:

```csharp
    [TestCase(typeof(FluentButton))]
    // … one line per component in this task's list …
    public void ComponentHasCuration(Type component)
    {
        var options = new PlayBlazorOptions();
        FluentPlaygroundConfig.Configure(options);

        var curated = options.GetVariants(component).Count > 0
            || options.TryGetSlotPreset(component, "ChildContent", out _)
            || options.TryGetScaffold(component, out _);

        curated.Should().BeTrue($"{component.Name} should carry a preset, slot or variant");
    }
```

If the file does not exist yet, create it with `using AwesomeAssertions; using Microsoft.FluentUI.AspNetCore.Components; using NUnit.Framework; using PlayBlazor.Demo.FluentUI;` and `namespace PlayBlazor.UnitTests.Shell;`. If a sibling preset task already created it, add your cases to it — never a second file.

- [ ] **Step 2: Run it and watch it fail**

Run: `PATH="$HOME/.dotnet:$PATH" ./tests/PlayBlazor.UnitTests/bin/Release/net10.0/PlayBlazor.UnitTests --filter "FullyQualifiedName~FluentPresetTests"`

Expected: this task's cases FAIL, each naming its component. Capture the output.

- [ ] **Step 3: Add the entries**

Add them to `FluentPlaygroundConfig.Configure`, grouped under a comment naming the family (data).

- [ ] **Step 4: Run the filtered test, then the whole suite**

```bash
PATH="$HOME/.dotnet:$PATH" ./tests/PlayBlazor.UnitTests/bin/Release/net10.0/PlayBlazor.UnitTests --filter "FullyQualifiedName~FluentPresetTests"
PATH="$HOME/.dotnet:$PATH" ./tests/PlayBlazor.UnitTests/bin/Release/net10.0/PlayBlazor.UnitTests
```

Expected: the new cases PASS and nothing else regressed.

- [ ] **Step 5: Commit**

```bash
git add demo/PlayBlazor.Demo.FluentUI tests/PlayBlazor.UnitTests
git commit -m "Feat: preset the Fluent data components"
```

---

### Task 9: Flagship benches, re-measure, and document

**Files:**
- Modify: `demo/PlayBlazor.Demo.FluentUI/Pages/Index.razor`
- Modify: `docs/superpowers/plans/sweeps/2026-render.txt`, `sweeps/2026-unsupported.txt`
- Modify: `README.md`, `CLAUDE.md`

**Interfaces:**
- Consumes: `DemoLanding`'s `Flagships` render fragment (from Milestone 2).
- Produces: nothing later tasks need.

- [ ] **Step 1: Add the flagship benches**

`demo/PlayBlazor.Demo.FluentUI/Pages/Index.razor` currently passes no `Flagships`. Give it three, mirroring the MudBlazor app's `Index.razor`: `FluentButton`, `FluentDataGrid<Person>`, and `PropertyColumn<Person, string>` — the last one specifically because it demonstrates a scaffold, as MudBlazor's does. Copy the surrounding `<section class="demo-section">` markup and note text style exactly.

- [ ] **Step 2: Verify in the browser**

Run `PATH="$HOME/.dotnet:$PATH" dotnet run --project demo/PlayBlazor.Demo.FluentUI` and open `/` and `/explorer`. Confirm: the three benches render specimens rather than error boxes; the tile grid lists the curated surface; picking a variant chip changes the specimen; and the generated snippet for an icon parameter is **not** `IconStart="…DemoIcon"` — that symptom is a known open follow-up recorded in the spec's §9, so if you see it, say so rather than fixing it here.

Stop the server afterwards. Never rely on hot reload after touching a `wwwroot` asset — the static-asset manifest corrupts and the page boots blank.

- [ ] **Step 3: Regenerate both inventories**

```bash
PATH="$HOME/.dotnet:$PATH" ./tests/PlayBlazor.UnitTests/bin/Release/net10.0/PlayBlazor.UnitTests --filter "FullyQualifiedName~RenderSweepTests" > docs/superpowers/plans/sweeps/2026-render.txt 2>&1
PATH="$HOME/.dotnet:$PATH" ./tests/PlayBlazor.UnitTests/bin/Release/net10.0/PlayBlazor.UnitTests --filter "FullyQualifiedName~UnsupportedParameterInventory" > docs/superpowers/plans/sweeps/2026-unsupported.txt 2>&1
```

- [ ] **Step 4: Report the measurement**

Put the before/after in your report, with the starting numbers from this plan's Global Constraints:

| | Before | After |
|---|---|---|
| Fluent healthy | 83 / 108 | ? |
| Fluent escaped | 5 | ? |
| Fluent undrivable-parameter occurrences | 382 | ? |

**The escaped count must be 0.** If it is not, that is a failure of Task 3 and this task cannot close.

- [ ] **Step 5: Update the docs**

`README.md` and `CLAUDE.md` both describe the Fluent app as an uncurated scaffold pending Milestone 3. Reword both to match what now ships, and refresh the test count — measure it, do not copy a number from this plan.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "Feat: complete the Fluent UI showcase"
```

---

## What this plan does NOT cover

- **Milestone 4, the DaisyBlazor curation** — still blocked on `phmatray/blazor-tailwind-ui#20`; `DaisyBlazor.Components` 1.0.0 is not on NuGet and `@daisyblazor/tailwind` is not on npm. Its plan gets written from its own sweep once the app exists.
- **The generated-snippet gap for catalogued icons** (spec §9): `RazorSnippetGenerator.FormatValue` omits the attribute rather than emitting uncompilable Razor, and the proper fix — an optional source expression on `Catalogue<T>` — is a deliberate follow-up, not this milestone's work.
- **The `--mud-palette-text-primary` leak** in the shared demo CSS (spec §9).
