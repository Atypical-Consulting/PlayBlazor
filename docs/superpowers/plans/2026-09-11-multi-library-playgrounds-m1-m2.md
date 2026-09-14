# Multi-Library Playgrounds — Implementation Plan (Milestones 1 & 2)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give the PlayBlazor package a host-supplied value catalogue so any library's icons become a real picker, then restructure the demo into three isolated WASM apps published under one GitHub Pages site.

**Architecture:** The package grows exactly one extension point — `PlayBlazorOptions.Catalogue<T>(named, preview)` — which fills `ControlKind.Icon` for parameter types reflection cannot drive (Fluent UI's `Icon` object) and upgrades the control for types it already could (MudBlazor's SVG-markup strings). A catalogue never widens a `ControlKind`: it only rescues a type that resolved to `Unsupported`. The demo splits into a UI-dependency-free shared RCL plus one app per library, each with its own stylesheet, its own trimmer root, and its own subpath on the Pages site.

**Tech Stack:** .NET 10, Blazor WebAssembly, NUnit + bUnit + AwesomeAssertions on Microsoft.Testing.Platform, Central Package Management, MinVer, GitHub Actions → GitHub Pages.

**Spec:** [`docs/superpowers/specs/2026-09-07-multi-library-playgrounds-design.md`](../specs/2026-09-07-multi-library-playgrounds-design.md)

## Global Constraints

- **`src/PlayBlazor` has zero UI dependencies.** No component library, CSS framework or JS library may be added to it. This is load-bearing — see `CLAUDE.md`.
- **`demo/PlayBlazor.Demo.Shared` has zero UI dependencies either.** It is the library-agnostic chrome; a `PackageReference` to MudBlazor, Fluent or DaisyBlazor in it is a plan failure.
- **Central Package Management:** every version lives in `Directory.Packages.props`. A `<PackageReference>` in a `.csproj` carries **no** `Version` attribute.
- **`TreatWarningsAsErrors` is on**, and `GenerateDocumentationFile` is on for `src/PlayBlazor`: every public member you add there needs an XML doc comment or `CS1591` fails the build.
- **Versioning is MinVer over `v`-prefixed git tags.** Never hand-edit a `<Version>`.
- **Tests run on Microsoft.Testing.Platform**, not VSTest. Filter syntax is `dotnet test -c Release -- --filter "..."` — note the bare `--`.
- **Do not `cd` out of the worktree.** All commands run from the repository root.

**Commands used throughout:**

```bash
dotnet build -c Release
dotnet test -c Release
dotnet test -c Release -- --filter "FullyQualifiedName~CatalogueTests"
```

## File Structure

### Milestone 1 — the package

| File | Responsibility |
|---|---|
| `src/PlayBlazor/CatalogueDefinition.cs` | **Create.** One type's named values, its optional preview renderer, and the value→name reverse lookup a permalink needs. |
| `src/PlayBlazor/PlayBlazorOptions.cs` | **Modify.** Add `Catalogue<T>` (host-facing) and `TryGetCatalogue` (consumed by discovery, the control and the serializer). |
| `src/PlayBlazor/Discovery/ReflectionCatalogProvider.cs` | **Modify.** Learn the options; a type with a catalogue that resolved to `Unsupported` becomes `ControlKind.Icon`. |
| `src/PlayBlazor/PlayBlazorServiceCollectionExtensions.cs` | **Modify.** Hand the options to the provider it registers. |
| `src/PlayBlazor/Shell/Controls/IconControl.razor` | **Modify.** Catalogue-driven searchable picker, falling back to today's text box when no catalogue is registered. |
| `src/PlayBlazor/State/ParameterValueConverter.cs` | **Modify.** A catalogued value serializes as its *name*; parsing accepts a name or the legacy raw text. |
| `src/PlayBlazor/State/PlaygroundStateSerializer.cs` | **Modify.** Thread the options through so each parameter gets its catalogue. |
| `src/PlayBlazor/PlaygroundView.razor.cs` | **Modify.** Pass `Options` at both serializer call sites. |
| `src/PlayBlazor/Shell/Workspace/PlaygroundWorkspace.razor.cs` | **Modify.** Same, at both call sites. |
| `demo/…/MudIconCatalogue.cs` | **Create.** Every `Icons.Material.Filled` constant by name — the end-to-end proof. |

### Milestone 2 — the demo

| File | Responsibility |
|---|---|
| `demo/PlayBlazor.Demo.Shared/` | **Create.** RCL: `DemoLanding.razor`, `LibrarySwitcher.razor`, `DemoChrome.razor.css`, `wwwroot/css/demo.css`. Zero UI dependencies. |
| `demo/PlayBlazor.Demo.MudBlazor/` | **Move** from `demo/PlayBlazor.DemoHost/`. Keeps `PlaygroundConfig.cs`, `ComponentIcons.cs`, `DemoFragments*`, `Person.cs`. |
| `demo/PlayBlazor.Demo.FluentUI/` | **Create.** Scaffold only — bare workspace, no curation yet (Milestone 3). |
| `demo/PlayBlazor.Demo.Daisy/` | **Create.** Scaffold plus the npm/Tailwind build. Blocked on `phmatray/blazor-tailwind-ui#20`. |
| `demo/landing/index.html` | **Create.** Static site root — three cards, three links, no WASM. |
| `.github/workflows/deploy-demo.yml` | **Modify.** Matrix over the three apps, then one assembling job. |
| `PlayBlazor.slnx` | **Modify.** Four demo projects instead of one. |

---

### Task 1: The catalogue type and its registration

**Files:**
- Create: `src/PlayBlazor/CatalogueDefinition.cs`
- Modify: `src/PlayBlazor/PlayBlazorOptions.cs`
- Test: `tests/PlayBlazor.UnitTests/CatalogueTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces:
  - `public sealed class PlayBlazor.CatalogueDefinition` with `IReadOnlyDictionary<string, object?> Named`, `Func<object?, RenderFragment>? Preview`, and `bool TryGetName(object? value, out string name)`.
  - `public PlayBlazorOptions Catalogue<T>(IReadOnlyDictionary<string, T> named, Func<T, RenderFragment>? preview = null) where T : class`
  - `public bool TryGetCatalogue(Type type, out CatalogueDefinition catalogue)`

- [ ] **Step 1: Write the failing test**

Create `tests/PlayBlazor.UnitTests/CatalogueTests.cs`:

```csharp
using AwesomeAssertions;
using NUnit.Framework;

namespace PlayBlazor.UnitTests;

public class CatalogueTests
{
    private static readonly Dictionary<string, string> Icons = new(StringComparer.Ordinal)
    {
        ["Save"] = "<path d='save' />",
        ["Delete"] = "<path d='delete' />",
    };

    [Test]
    public void Catalogue_IsFoundByType()
    {
        var options = new PlayBlazorOptions().Catalogue(Icons);

        options.TryGetCatalogue(typeof(string), out var catalogue).Should().BeTrue();
        catalogue.Named.Should().ContainKey("Save");
        catalogue.Named["Save"].Should().Be("<path d='save' />");
    }

    [Test]
    public void Catalogue_UnregisteredType_IsNotFound()
    {
        var options = new PlayBlazorOptions().Catalogue(Icons);

        options.TryGetCatalogue(typeof(int), out _).Should().BeFalse();
    }

    [Test]
    public void TryGetName_MapsAValueBackToItsName()
    {
        new PlayBlazorOptions().Catalogue(Icons).TryGetCatalogue(typeof(string), out var catalogue);

        catalogue.TryGetName("<path d='delete' />", out var name).Should().BeTrue();
        name.Should().Be("Delete");
    }

    [Test]
    public void TryGetName_UnknownValue_ReportsFalse()
    {
        new PlayBlazorOptions().Catalogue(Icons).TryGetCatalogue(typeof(string), out var catalogue);

        catalogue.TryGetName("<path d='unknown' />", out _).Should().BeFalse();
    }

    [Test]
    public void TryGetName_Null_ReportsFalse()
    {
        new PlayBlazorOptions().Catalogue(Icons).TryGetCatalogue(typeof(string), out var catalogue);

        catalogue.TryGetName(null, out _).Should().BeFalse();
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test -c Release -- --filter "FullyQualifiedName~CatalogueTests"`

Expected: compile failure — `PlayBlazorOptions` does not contain `Catalogue` / `TryGetCatalogue`.

- [ ] **Step 3: Create `CatalogueDefinition`**

Create `src/PlayBlazor/CatalogueDefinition.cs`:

```csharp
using Microsoft.AspNetCore.Components;

namespace PlayBlazor;

/// <summary>
/// The named values a host offers for one parameter type — the entries of an icon picker.
/// </summary>
/// <remarks>
/// The registered values are the very instances handed to the specimen, so
/// <see cref="TryGetName" /> normally matches the same object back. A host registering
/// values whose type overrides neither <see cref="object.Equals(object)" /> nor
/// <see cref="object.GetHashCode" /> therefore still round-trips.
/// </remarks>
public sealed class CatalogueDefinition
{
    private readonly Dictionary<object, string> _names = [];

    internal CatalogueDefinition(
        IReadOnlyDictionary<string, object?> named,
        Func<object?, RenderFragment>? preview)
    {
        Named = named;
        Preview = preview;
        foreach (var (name, value) in named)
        {
            if (value is not null)
            {
                // First name wins: two names sharing one value would otherwise flip-flop
                // the permalink depending on dictionary order.
                _names.TryAdd(value, name);
            }
        }
    }

    /// <summary>The offered values, by the name the picker shows.</summary>
    public IReadOnlyDictionary<string, object?> Named { get; }

    /// <summary>Renders one value as a thumbnail. Null lists names alone.</summary>
    public Func<object?, RenderFragment>? Preview { get; }

    /// <summary>The name a value is listed under — what a permalink carries instead of the value.</summary>
    /// <param name="value">The value to look up.</param>
    /// <param name="name">The name it is listed under, when the catalogue holds it.</param>
    /// <returns><c>false</c> for null and for values the catalogue does not offer.</returns>
    public bool TryGetName(object? value, out string name)
    {
        if (value is null)
        {
            name = string.Empty;
            return false;
        }

        return _names.TryGetValue(value, out name!);
    }
}
```

- [ ] **Step 4: Add `Catalogue<T>` and `TryGetCatalogue` to the options**

In `src/PlayBlazor/PlayBlazorOptions.cs`, add the backing field next to the other dictionaries at the top of the class:

```csharp
    private readonly Dictionary<Type, CatalogueDefinition> _catalogues = new();
```

Add the two members after `ResolvePreferredClosing`:

```csharp
    /// <summary>
    /// Named values the host offers for every parameter of type <typeparamref name="T" /> —
    /// the entries of an icon picker.
    /// </summary>
    /// <remarks>
    /// A catalogue FILLS <see cref="Model.ControlKind.Icon" />, it never widens it. Registering
    /// one for <c>string</c> reaches only the string parameters discovery already recognized as
    /// icons, never every text field in the library.
    /// </remarks>
    /// <param name="named">The offered values, by the name the picker shows.</param>
    /// <param name="preview">Renders one value as a thumbnail. Omit it to list names alone.</param>
    /// <typeparam name="T">The parameter type this catalogue drives.</typeparam>
    /// <returns>The same options, for chaining.</returns>
    public PlayBlazorOptions Catalogue<T>(
        IReadOnlyDictionary<string, T> named,
        Func<T, RenderFragment>? preview = null)
        where T : class
    {
        _catalogues[typeof(T)] = new CatalogueDefinition(
            named.ToDictionary(static entry => entry.Key, static entry => (object?)entry.Value, StringComparer.Ordinal),
            preview is null ? null : value => preview((T)value!));
        return this;
    }

    /// <summary>The catalogue a host registered for a parameter type, if any.</summary>
    /// <param name="type">The parameter's declared type.</param>
    /// <param name="catalogue">The registered catalogue, when one exists.</param>
    /// <returns><c>true</c> when the host registered a catalogue for this exact type.</returns>
    public bool TryGetCatalogue(Type type, out CatalogueDefinition catalogue)
        => _catalogues.TryGetValue(type, out catalogue!);
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test -c Release -- --filter "FullyQualifiedName~CatalogueTests"`

Expected: 5 tests PASS.

- [ ] **Step 6: Run the whole suite to verify nothing regressed**

Run: `dotnet test -c Release`

Expected: all pre-existing tests still pass (220 before this task, 225 after).

- [ ] **Step 7: Commit**

```bash
git add src/PlayBlazor/CatalogueDefinition.cs src/PlayBlazor/PlayBlazorOptions.cs tests/PlayBlazor.UnitTests/CatalogueTests.cs
git commit -m "Feat: let a host register named values for a parameter type"
```

---

### Task 2: Discovery honours a catalogue, without widening

**Files:**
- Modify: `src/PlayBlazor/Discovery/ReflectionCatalogProvider.cs:24` (constructor), `:91-97` (kind decision)
- Modify: `src/PlayBlazor/PlayBlazorServiceCollectionExtensions.cs:26`
- Test: `tests/PlayBlazor.UnitTests/Discovery/CatalogueKindTests.cs`
- Test: `tests/PlayBlazor.UnitTests/Fixtures/CatalogueFixture.razor`

**Interfaces:**
- Consumes: `PlayBlazorOptions.TryGetCatalogue` (Task 1).
- Produces: `ReflectionCatalogProvider(XmlDocSummaryReader? xmlDocs = null, PlayBlazorOptions? options = null)` — `options` is the **second** parameter so the existing positional call `new ReflectionCatalogProvider(reader)` in `XmlDocSummaryReaderTests.cs:66` keeps compiling.

- [ ] **Step 1: Write the fixture the test needs**

Create `tests/PlayBlazor.UnitTests/Fixtures/CatalogueFixture.razor`:

```razor
@namespace PlayBlazor.UnitTests.Fixtures
<span>@Label</span>
@code {
    /// <summary>A library type reflection cannot drive on its own.</summary>
    public sealed record Glyph(string Markup);

    [Parameter] public Glyph? Icon { get; set; }
    [Parameter] public string? Label { get; set; }
    [Parameter] public string? TrailingIcon { get; set; }
}
```

- [ ] **Step 2: Write the failing test**

Create `tests/PlayBlazor.UnitTests/Discovery/CatalogueKindTests.cs`:

```csharp
using AwesomeAssertions;
using NUnit.Framework;
using PlayBlazor.Discovery;
using PlayBlazor.Model;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Discovery;

public class CatalogueKindTests
{
    private static PlayBlazorOptions WithGlyphCatalogue()
        => new PlayBlazorOptions().Catalogue(new Dictionary<string, CatalogueFixture.Glyph>(StringComparer.Ordinal)
        {
            ["Save"] = new("<path d='save' />"),
        });

    private static ParameterDescriptor Parameter(PlayBlazorOptions? options, string name)
        => new ReflectionCatalogProvider(options: options)
            .Describe(typeof(CatalogueFixture))
            .Parameters.Single(p => p.Name == name);

    [Test]
    public void CataloguedType_BecomesAnIconControl()
        => Parameter(WithGlyphCatalogue(), "Icon").Kind.Should().Be(ControlKind.Icon);

    [Test]
    public void CataloguedType_WithoutACatalogue_StaysUnsupported()
        => Parameter(null, "Icon").Kind.Should().Be(ControlKind.Unsupported);

    [Test]
    public void OrdinaryString_IsNotWidenedByAStringCatalogue()
    {
        var options = new PlayBlazorOptions().Catalogue(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Save"] = "<path d='save' />",
        });

        Parameter(options, "Label").Kind.Should().Be(ControlKind.Text);
    }

    [Test]
    public void StringNamedIcon_StaysAnIconControl()
        => Parameter(null, "TrailingIcon").Kind.Should().Be(ControlKind.Icon);
}
```

- [ ] **Step 3: Run the test to verify it fails**

Run: `dotnet test -c Release -- --filter "FullyQualifiedName~CatalogueKindTests"`

Expected: compile failure — `ReflectionCatalogProvider` has no `options` parameter.

- [ ] **Step 4: Teach the provider about the options**

In `src/PlayBlazor/Discovery/ReflectionCatalogProvider.cs`, replace the field and constructor (around line 17-25):

```csharp
    private readonly ConcurrentDictionary<Type, ComponentDescriptor> _cache = new();
    private readonly XmlDocSummaryReader? _xmlDocs;
    private readonly PlayBlazorOptions? _options;

    /// <summary>Creates a provider, optionally enriching descriptors with XML doc summaries.</summary>
    /// <param name="xmlDocs">
    /// Summaries for the scanned library, used as component and parameter tooltips. Omit it and
    /// descriptors simply carry no summary.
    /// </param>
    /// <param name="options">
    /// The host configuration, consulted for value catalogues: a parameter whose type no control
    /// fits becomes an icon picker when the host registered a catalogue for that type. Omit it
    /// and no catalogue is ever consulted.
    /// </param>
    public ReflectionCatalogProvider(XmlDocSummaryReader? xmlDocs = null, PlayBlazorOptions? options = null)
    {
        _xmlDocs = xmlDocs;
        _options = options;
    }
```

- [ ] **Step 5: Add the second rule to the kind decision**

In the same file, replace the block at lines 91-97:

```csharp
            var (kind, isNullable) = ControlKindResolver.Resolve(property.PropertyType);
            if (kind == ControlKind.Text && property.PropertyType == typeof(string)
                && property.Name.EndsWith("Icon", StringComparison.Ordinal))
            {
                // Icon strings (SVG markup in MudBlazor) deserve a preview, not a blob field.
                kind = ControlKind.Icon;
            }
            else if (kind == ControlKind.Unsupported
                     && _options?.TryGetCatalogue(property.PropertyType, out _) == true)
            {
                // A host catalogue is the only thing that can drive a library's own value type
                // (Fluent UI's Icon object). Gated on Unsupported so a catalogue never widens a
                // kind a control already fits — a string catalogue must not swallow every text field.
                kind = ControlKind.Icon;
            }
```

- [ ] **Step 6: Hand the options to the registered provider**

In `src/PlayBlazor/PlayBlazorServiceCollectionExtensions.cs`, replace line 26:

```csharp
        // Not `static`: the provider needs the options this call just configured.
        services.TryAddSingleton<IComponentCatalogProvider>(_ => new ReflectionCatalogProvider(options: options));
```

- [ ] **Step 7: Run the tests to verify they pass**

Run: `dotnet test -c Release -- --filter "FullyQualifiedName~CatalogueKindTests"`

Expected: 4 tests PASS.

- [ ] **Step 8: Run the whole suite**

Run: `dotnet test -c Release`

Expected: all pass. `CategoryGroupTests.cs:29` (which asserts `ControlKind.Icon` through a provider built with no options) still passes on the unchanged first rule.

- [ ] **Step 9: Commit**

```bash
git add src/PlayBlazor/Discovery/ReflectionCatalogProvider.cs src/PlayBlazor/PlayBlazorServiceCollectionExtensions.cs tests/PlayBlazor.UnitTests/Discovery/CatalogueKindTests.cs tests/PlayBlazor.UnitTests/Fixtures/CatalogueFixture.razor
git commit -m "Feat: a catalogued parameter type becomes an icon control"
```

---

### Task 3: The icon control becomes a searchable picker

**Files:**
- Modify: `src/PlayBlazor/Shell/Controls/IconControl.razor`
- Test: `tests/PlayBlazor.UnitTests/Shell/IconPickerTests.cs`

**Interfaces:**
- Consumes: `CatalogueDefinition` and `PlayBlazorOptions.TryGetCatalogue` (Task 1).
- Produces: no new public API. `IconControl` now requires `PlayBlazorOptions` in DI — every bUnit test rendering it must call `_context.Services.AddPlayBlazor(...)` or register the options directly.

The picker is an `<input list>` bound to a `<datalist>`: the browser filters as you type, with no JS and no dependency. A registered catalogue replaces the text box entirely; with no catalogue the control is byte-for-byte the one that shipped.

- [ ] **Step 1: Write the failing test**

Create `tests/PlayBlazor.UnitTests/Shell/IconPickerTests.cs`:

```csharp
using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PlayBlazor.Model;
using PlayBlazor.Shell;

namespace PlayBlazor.UnitTests.Shell;

public class IconPickerTests
{
    private BunitContext _context = null!;

    [SetUp]
    public void Setup() => _context = new BunitContext();

    [TearDown]
    public void TearDown() => _context.Dispose();

    private static readonly Dictionary<string, string> Icons = new(StringComparer.Ordinal)
    {
        ["Save"] = "<path d='save' />",
        ["Delete"] = "<path d='delete' />",
    };

    private static ParameterDescriptor IconParameter()
        => new("Icon", typeof(string), ControlKind.Icon, IsNullable: true,
            DefaultValue: null, HasDefault: true, Summary: "the docs");

    private void Register(PlayBlazorOptions options)
        => _context.Services.AddSingleton(options);

    [Test]
    public void WithCatalogue_ListsEveryNameAndReportsTheValue()
    {
        Register(new PlayBlazorOptions().Catalogue(Icons));
        object? reported = null;

        var cut = _context.Render<IconControl>(ps => ps
            .Add(c => c.Parameter, IconParameter())
            .Add(c => c.Value, null)
            .Add(c => c.ValueChanged, v => reported = v));

        cut.FindAll("datalist option").Count.Should().Be(2);
        cut.Find("input[list]").Change("Delete");

        reported.Should().Be("<path d='delete' />");
    }

    [Test]
    public void WithCatalogue_ShowsTheNameOfTheCurrentValue()
    {
        Register(new PlayBlazorOptions().Catalogue(Icons));

        var cut = _context.Render<IconControl>(ps => ps
            .Add(c => c.Parameter, IconParameter())
            .Add(c => c.Value, "<path d='save' />"));

        cut.Find("input[list]").GetAttribute("value").Should().Be("Save");
    }

    [Test]
    public void WithCatalogue_UnknownName_ReportsNull()
    {
        Register(new PlayBlazorOptions().Catalogue(Icons));
        object? reported = "unset";

        var cut = _context.Render<IconControl>(ps => ps
            .Add(c => c.Parameter, IconParameter())
            .Add(c => c.Value, null)
            .Add(c => c.ValueChanged, v => reported = v));

        cut.Find("input[list]").Change("NotAnIcon");

        reported.Should().BeNull();
    }

    [Test]
    public void WithoutCatalogue_KeepsThePlainTextBoxAndTheSvgPreview()
    {
        Register(new PlayBlazorOptions());

        var cut = _context.Render<IconControl>(ps => ps
            .Add(c => c.Parameter, IconParameter())
            .Add(c => c.Value, "<path d='save' />"));

        cut.FindAll("datalist").Should().BeEmpty();
        cut.FindAll("svg.pb-icon-preview").Count.Should().Be(1);
        cut.Find("input[type=text]").GetAttribute("value").Should().Be("<path d='save' />");
    }

    [Test]
    public void WithCataloguePreview_RendersTheThumbnail()
    {
        Register(new PlayBlazorOptions().Catalogue(
            Icons,
            static markup => builder =>
            {
                builder.OpenElement(0, "i");
                builder.AddAttribute(1, "class", "thumb");
                builder.AddContent(2, markup);
                builder.CloseElement();
            }));

        var cut = _context.Render<IconControl>(ps => ps
            .Add(c => c.Parameter, IconParameter())
            .Add(c => c.Value, "<path d='save' />"));

        cut.FindAll("i.thumb").Count.Should().Be(1);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test -c Release -- --filter "FullyQualifiedName~IconPickerTests"`

Expected: FAIL — no `datalist` is rendered, and the catalogue branch does not exist.

- [ ] **Step 3: Rewrite the control**

Replace the whole of `src/PlayBlazor/Shell/Controls/IconControl.razor`:

```razor
@namespace PlayBlazor.Shell
@inject PlayBlazorOptions Options
<label class="pb-control pb-control-icon">
    <span class="pb-control-label" title="@Parameter.Summary">@Parameter.Name</span>
    <span class="pb-icon-pair">
        @if (Catalogue is { } catalogue)
        {
            @* A host catalogue names the icons: type to filter, no JS, no dependency. *@
            @if (catalogue.Preview is { } preview && Value is not null)
            {
                <span class="pb-icon-preview">@preview(Value)</span>
            }
            <input type="text" list="@_listId" value="@SelectedName" title="@SelectedName" @onchange="OnPicked" />
            <datalist id="@_listId">
                @foreach (var name in catalogue.Named.Keys.Order(StringComparer.Ordinal))
                {
                    <option value="@name"></option>
                }
            </datalist>
        }
        else
        {
            @if (Value is string { Length: > 0 } markup && markup.TrimStart().StartsWith('<'))
            {
                @* MudBlazor icon strings ARE svg inner markup — preview them live. *@
                <svg class="pb-icon-preview" viewBox="0 0 24 24" aria-hidden="true">@((MarkupString)markup)</svg>
            }
            <input type="text" value="@(Value as string)" title="@(Value as string)" @onchange="OnChanged" />
        }
    </span>
</label>
@code {
    // Two pickers on one page would otherwise share a datalist and offer each other's entries.
    private readonly string _listId = $"pb-cat-{Guid.NewGuid():N}";

    [Parameter, EditorRequired] public ParameterDescriptor Parameter { get; set; } = default!;
    [Parameter] public object? Value { get; set; }
    [Parameter] public EventCallback<object?> ValueChanged { get; set; }

    private CatalogueDefinition? Catalogue
        => Options.TryGetCatalogue(Parameter.Type, out var catalogue) ? catalogue : null;

    private string SelectedName
        => Catalogue is { } catalogue && catalogue.TryGetName(Value, out var name) ? name : string.Empty;

    private Task OnPicked(ChangeEventArgs e)
        => ValueChanged.InvokeAsync(
            (string?)e.Value is { Length: > 0 } name
            && Catalogue is { } catalogue
            && catalogue.Named.TryGetValue(name, out var value)
                ? value
                : null);

    private Task OnChanged(ChangeEventArgs e)
        => ValueChanged.InvokeAsync((string?)e.Value is { Length: > 0 } text ? text : null);
}
```

- [ ] **Step 4: Add the picker styling**

The icon preview is styled in exactly one place: `src/PlayBlazor/Shell/Workspace/PlaygroundWorkspace.razor.css:212-213`. Those rules reach the control from the workspace's own scope through `::deep` — Blazor's scoped CSS has a hard component boundary, so a rule written anywhere else will not apply (`CLAUDE.md`).

Today's rule assumes the preview *is* an `<svg>` with width/height. A catalogue preview is an arbitrary fragment, so add a sibling rule immediately after line 213:

```css
.pbw-prow ::deep .pb-icon-pair > .pb-icon-preview { display: inline-flex; align-items: center; justify-content: center; }
```

Keep the existing two lines untouched — the no-catalogue `<svg>` path still depends on them.

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test -c Release -- --filter "FullyQualifiedName~IconPickerTests"`

Expected: 5 tests PASS.

- [ ] **Step 6: Run the whole suite**

Run: `dotnet test -c Release`

Expected: all pass. If any bUnit test now fails with "Cannot provide a value for property 'Options'", that test renders `IconControl` transitively — add `_context.Services.AddPlayBlazor();` to its `Setup`.

- [ ] **Step 7: Commit**

```bash
git add src/PlayBlazor/Shell/Controls/IconControl.razor src/PlayBlazor/Shell tests/PlayBlazor.UnitTests/Shell/IconPickerTests.cs
git commit -m "Feat: drive an icon parameter from a host catalogue"
```

---

### Task 4: A catalogued value permalinks as its name

**Files:**
- Modify: `src/PlayBlazor/State/ParameterValueConverter.cs:19` (`Format`), `:44` (`TryParse`)
- Modify: `src/PlayBlazor/State/PlaygroundStateSerializer.cs:23` (`Encode`), `:61` (`Decode`)
- Modify: `src/PlayBlazor/PlaygroundView.razor.cs:117,125`
- Modify: `src/PlayBlazor/Shell/Workspace/PlaygroundWorkspace.razor.cs:343,377`
- Test: `tests/PlayBlazor.UnitTests/State/CataloguePermalinkTests.cs`

**Interfaces:**
- Consumes: `CatalogueDefinition` (Task 1).
- Produces:
  - `ParameterValueConverter.Format(ParameterDescriptor parameter, object? value, CatalogueDefinition? catalogue = null)`
  - `ParameterValueConverter.TryParse(ParameterDescriptor parameter, string text, out object? value, CatalogueDefinition? catalogue = null)`
  - `PlaygroundStateSerializer.Encode(ComponentDescriptor descriptor, PlaygroundState state, PlaygroundEnvironment environment, PlayBlazorOptions? options = null)`
  - `PlaygroundStateSerializer.Decode(string encoded, ComponentDescriptor descriptor, PlaygroundState state, PlaygroundEnvironment environment, PlayBlazorOptions? options = null)`

Every new parameter is optional and last, so every existing call site keeps compiling unchanged.

**Backward compatibility that must hold:** today a MudBlazor icon permalinks as its *raw SVG markup*. After this task it permalinks as its *name*. `TryParse` therefore tries the catalogue first and falls back to the existing text parsing, so links shared before this change still resolve.

- [ ] **Step 1: Write the failing test**

Create `tests/PlayBlazor.UnitTests/State/CataloguePermalinkTests.cs`:

```csharp
using AwesomeAssertions;
using NUnit.Framework;
using PlayBlazor.Model;
using PlayBlazor.State;

namespace PlayBlazor.UnitTests.State;

public class CataloguePermalinkTests
{
    private const string SaveMarkup = "<path d='save' />";

    private static CatalogueDefinition Catalogue()
    {
        new PlayBlazorOptions()
            .Catalogue(new Dictionary<string, string>(StringComparer.Ordinal) { ["Save"] = SaveMarkup })
            .TryGetCatalogue(typeof(string), out var catalogue);
        return catalogue;
    }

    private static ParameterDescriptor IconParameter()
        => new("Icon", typeof(string), ControlKind.Icon, IsNullable: true,
            DefaultValue: null, HasDefault: true, Summary: null);

    [Test]
    public void Format_CataloguedValue_YieldsItsName()
        => ParameterValueConverter.Format(IconParameter(), SaveMarkup, Catalogue())
            .Should().Be("Save");

    [Test]
    public void Format_ValueOutsideTheCatalogue_FallsBackToTheRawText()
        => ParameterValueConverter.Format(IconParameter(), "<path d='other' />", Catalogue())
            .Should().Be("<path d='other' />");

    [Test]
    public void TryParse_AName_YieldsTheCataloguedValue()
    {
        ParameterValueConverter.TryParse(IconParameter(), "Save", out var value, Catalogue())
            .Should().BeTrue();

        value.Should().Be(SaveMarkup);
    }

    [Test]
    public void TryParse_LegacyRawMarkup_StillResolves()
    {
        ParameterValueConverter.TryParse(IconParameter(), "<path d='legacy' />", out var value, Catalogue())
            .Should().BeTrue();

        value.Should().Be("<path d='legacy' />");
    }

    [Test]
    public void RoundTrip_ThroughTheCatalogue_IsStable()
    {
        var catalogue = Catalogue();
        var text = ParameterValueConverter.Format(IconParameter(), SaveMarkup, catalogue)!;

        ParameterValueConverter.TryParse(IconParameter(), text, out var value, catalogue).Should().BeTrue();

        value.Should().Be(SaveMarkup);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test -c Release -- --filter "FullyQualifiedName~CataloguePermalinkTests"`

Expected: compile failure — `Format` and `TryParse` take no catalogue.

- [ ] **Step 3: Give the converter its catalogue**

In `src/PlayBlazor/State/ParameterValueConverter.cs`, replace the `Format` signature and add the catalogue shortcut as its first statement:

```csharp
    /// <summary>Formats a value as the text a control shows and a permalink carries.</summary>
    /// <param name="parameter">The parameter, whose kind disambiguates types recognized structurally.</param>
    /// <param name="value">The value to format.</param>
    /// <param name="catalogue">
    /// The host catalogue for this parameter's type, when one exists. A catalogued value is
    /// carried as its NAME, which keeps a permalink short and survives the value changing shape.
    /// </param>
    /// <returns>The invariant-culture text, or <c>null</c> when the type has no text form.</returns>
    public static string? Format(ParameterDescriptor parameter, object? value, CatalogueDefinition? catalogue = null)
    {
        if (catalogue is not null && catalogue.TryGetName(value, out var name))
        {
            return name;
        }

        return value switch
        {
            // … the existing switch arms, unchanged …
        };
    }
```

Then replace the `TryParse` signature and add the catalogue lookup ahead of the kind switch:

```csharp
    /// <summary>Parses text back into a parameter value — the exact inverse of <see cref="Format" />.</summary>
    /// <param name="parameter">The parameter whose type and kind drive the conversion.</param>
    /// <param name="text">The text to parse, in invariant culture.</param>
    /// <param name="value">The parsed value, when parsing succeeded.</param>
    /// <param name="catalogue">
    /// The host catalogue for this parameter's type, when one exists. A catalogue name wins;
    /// anything else falls through to the type-based parsing, so permalinks written before a
    /// catalogue existed still resolve.
    /// </param>
    /// <returns><c>false</c> for text that does not convert, leaving the caller's value untouched.</returns>
    public static bool TryParse(
        ParameterDescriptor parameter,
        string text,
        out object? value,
        CatalogueDefinition? catalogue = null)
    {
        value = null;

        if (catalogue is not null && catalogue.Named.TryGetValue(text, out var catalogued) && catalogued is not null)
        {
            value = catalogued;
            return true;
        }

        var type = Nullable.GetUnderlyingType(parameter.Type) ?? parameter.Type;
        // … the existing try/catch and switch, unchanged …
    }
```

- [ ] **Step 4: Run the converter tests to verify they pass**

Run: `dotnet test -c Release -- --filter "FullyQualifiedName~CataloguePermalinkTests"`

Expected: 5 tests PASS.

- [ ] **Step 5: Thread the options through the serializer**

In `src/PlayBlazor/State/PlaygroundStateSerializer.cs`, add an optional trailing parameter to both entry points and a private helper, then use it at the two converter call sites (`:33` and `:101`):

```csharp
    // A catalogue fills a ControlKind, it never widens one — the same rule discovery applies in
    // ReflectionCatalogProvider. Without the Kind gate a string catalogue would reach EVERY text
    // parameter in the library, and decoding `Label=Save` would yield the Save icon's markup.
    private static CatalogueDefinition? CatalogueFor(PlayBlazorOptions? options, ParameterDescriptor parameter)
        => parameter.Kind == ControlKind.Icon
           && options is not null
           && options.TryGetCatalogue(parameter.Type, out var catalogue)
            ? catalogue
            : null;
```

The gate belongs here and **only** here — `Format` and `TryParse` receive an explicit catalogue, so
their caller has already decided; second-guessing it inside them would be wrong. Their
`<param name="catalogue">` docs instead state that the caller is responsible for supplying a
catalogue only for a parameter that catalogue actually drives.

`Encode` gains `PlayBlazorOptions? options = null` as its last parameter, and line 33 becomes:

```csharp
            var text = ParameterValueConverter.Format(parameter, state.GetValue(parameter), CatalogueFor(options, parameter));
```

`Decode` gains `PlayBlazorOptions? options = null` as its last parameter, and line 101 becomes:

```csharp
            if (ParameterValueConverter.TryParse(parameter, text, out var value, CatalogueFor(options, parameter)))
```

Document both new parameters with `<param name="options">The host configuration, consulted for value catalogues. Omit it and values are carried in their raw text form.</param>` — `CS1591` will fail the build otherwise.

- [ ] **Step 6: Pass the options at all four call sites**

- `src/PlayBlazor/PlaygroundView.razor.cs:117` — add `Options` as the last argument to `Decode`.
- `src/PlayBlazor/PlaygroundView.razor.cs:125` — add `Options` as the last argument to `Encode`.
- `src/PlayBlazor/Shell/Workspace/PlaygroundWorkspace.razor.cs:343` — add `Options` as the last argument to `Encode`.
- `src/PlayBlazor/Shell/Workspace/PlaygroundWorkspace.razor.cs:377` — add `Options` as the last argument to `Decode`.

Both components already `[Inject]` `PlayBlazorOptions Options`; no new injection is needed.

- [ ] **Step 7: Run the whole suite**

Run: `dotnet test -c Release`

Expected: all pass, `PermalinkTests` and `PlaygroundStateSerializerTests` included — they call the serializer without options, which is the unchanged path.

- [ ] **Step 8: Commit**

```bash
git add src/PlayBlazor/State src/PlayBlazor/PlaygroundView.razor.cs src/PlayBlazor/Shell/Workspace/PlaygroundWorkspace.razor.cs tests/PlayBlazor.UnitTests/State/CataloguePermalinkTests.cs
git commit -m "Feat: carry a catalogued parameter value as its name in permalinks"
```

---

### Task 5: The MudBlazor demo registers an icon catalogue

This is Milestone 1's end-to-end proof: the chain options → discovery → control → permalink, exercised against a real library. It also removes the demo's worst piece of UX — pasting raw SVG to change an icon.

**Files:**
- Create: `demo/PlayBlazor.DemoHost/MudIconCatalogue.cs`
- Modify: `demo/PlayBlazor.DemoHost/PlaygroundConfig.cs:45` (inside `Configure`)
- Test: `tests/PlayBlazor.UnitTests/Shell/MudIconCatalogueTests.cs`

**Interfaces:**
- Consumes: `PlayBlazorOptions.Catalogue<T>` (Task 1), the picker (Task 3).
- Produces: `PlayBlazor.DemoHost.MudIconCatalogue.Filled` — `IReadOnlyDictionary<string, string>`, every `Icons.Material.Filled` constant by its C# name.

- [ ] **Step 1: Write the failing test**

Create `tests/PlayBlazor.UnitTests/Shell/MudIconCatalogueTests.cs`:

```csharp
using AwesomeAssertions;
using MudBlazor;
using NUnit.Framework;
using PlayBlazor.DemoHost;

namespace PlayBlazor.UnitTests.Shell;

public class MudIconCatalogueTests
{
    [Test]
    public void Filled_HoldsTheMaterialConstants()
    {
        MudIconCatalogue.Filled.Should().ContainKey("Delete");
        MudIconCatalogue.Filled["Delete"].Should().Be(Icons.Material.Filled.Delete);
    }

    [Test]
    public void Filled_IsLargeEnoughToBeWorthAPicker()
        => MudIconCatalogue.Filled.Count.Should().BeGreaterThan(100);

    [Test]
    public void Filled_ValuesAreSvgMarkup()
        => MudIconCatalogue.Filled["Delete"].TrimStart().Should().StartWith("<");
}
```

The test project must reference the demo. In `tests/PlayBlazor.UnitTests/PlayBlazor.UnitTests.csproj`, add to the `ProjectReference` item group:

```xml
    <ProjectReference Include="..\..\demo\PlayBlazor.DemoHost\PlayBlazor.DemoHost.csproj" />
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test -c Release -- --filter "FullyQualifiedName~MudIconCatalogueTests"`

Expected: compile failure — `MudIconCatalogue` does not exist.

- [ ] **Step 3: Build the catalogue by reflection**

Create `demo/PlayBlazor.DemoHost/MudIconCatalogue.cs`:

```csharp
using System.Reflection;
using MudBlazor;

namespace PlayBlazor.DemoHost;

/// <summary>
/// Every <c>Icons.Material.Filled</c> constant, by its C# name — the entries of the
/// playground's icon picker.
/// </summary>
/// <remarks>
/// These are <c>const</c> fields, which the compiler inlines: nothing in the demo's own markup
/// keeps the declaring type alive, and a trimmed publish would drop it. The demo's
/// <c>TrimmerRootAssembly Include="MudBlazor"</c> is what keeps this reflection working.
/// </remarks>
public static class MudIconCatalogue
{
    /// <summary>The filled Material set, by constant name.</summary>
    public static IReadOnlyDictionary<string, string> Filled { get; } =
        typeof(Icons.Material.Filled)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(static field => field.IsLiteral && field.FieldType == typeof(string))
            .ToDictionary(
                static field => field.Name,
                static field => (string)field.GetRawConstantValue()!,
                StringComparer.Ordinal);
}
```

- [ ] **Step 4: Register it in the demo configuration**

In `demo/PlayBlazor.DemoHost/PlaygroundConfig.cs`, inside `Configure`, immediately after the `options.IconResolver = ComponentIcons.Resolve;` line:

```csharp
        // Icon parameters are MudBlazor SVG markup strings. Naming them turns a blob field into
        // a picker — and shortens every permalink that carries an icon.
        options.Catalogue(MudIconCatalogue.Filled, static markup => builder =>
        {
            builder.OpenElement(0, "svg");
            builder.AddAttribute(1, "viewBox", "0 0 24 24");
            builder.AddAttribute(2, "aria-hidden", "true");
            builder.AddMarkupContent(3, markup);
            builder.CloseElement();
        });
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test -c Release -- --filter "FullyQualifiedName~MudIconCatalogueTests"`

Expected: 3 tests PASS.

- [ ] **Step 6: Verify it end-to-end in the browser**

Run: `dotnet run --project demo/PlayBlazor.DemoHost`

Open `/` and find the `MudIconButton` bench. Expected: the `Icon` parameter is a text box with a dropdown arrow; typing `fav` filters to `Favorite`, `FavoriteBorder`, …; picking one re-renders the specimen and the address bar's `pb-` parameter now carries `Favorite`, not a wall of SVG.

Stop the server before continuing — **never rely on hot reload after touching a `wwwroot` asset** (`CLAUDE.md`).

- [ ] **Step 7: Run the whole suite**

Run: `dotnet test -c Release`

Expected: all pass.

- [ ] **Step 8: Commit**

```bash
git add demo/PlayBlazor.DemoHost/MudIconCatalogue.cs demo/PlayBlazor.DemoHost/PlaygroundConfig.cs tests/PlayBlazor.UnitTests/PlayBlazor.UnitTests.csproj tests/PlayBlazor.UnitTests/Shell/MudIconCatalogueTests.cs
git commit -m "Feat: give the MudBlazor demo a named icon catalogue"
```

**Milestone 1 is complete here.** The package can drive any library's icons, and nothing about the demo's behaviour changed except that icons got better.

---

### Task 6: Extract the shared demo chrome and rename the MudBlazor app

Pure restructuring: **no visible behaviour may change.** That is what makes it safely reviewable.

**Files:**
- Create: `demo/PlayBlazor.Demo.Shared/PlayBlazor.Demo.Shared.csproj`
- Create: `demo/PlayBlazor.Demo.Shared/_Imports.razor`
- Create: `demo/PlayBlazor.Demo.Shared/DemoLanding.razor`
- Create: `demo/PlayBlazor.Demo.Shared/LibrarySwitcher.razor`
- Move: `demo/PlayBlazor.DemoHost/` → `demo/PlayBlazor.Demo.MudBlazor/` (project renamed with it)
- Move: `demo/PlayBlazor.DemoHost/wwwroot/css/demo.css` → `demo/PlayBlazor.Demo.Shared/wwwroot/css/demo.css`
- Modify: `PlayBlazor.slnx`, `.github/workflows/deploy-demo.yml`, `README.md`, `CLAUDE.md`
- Test: `tests/PlayBlazor.UnitTests/Shell/DemoLandingTests.cs`

**Interfaces:**
- Consumes: `IComponentCatalogProvider`, `PlayBlazorOptions` (both already public).
- Produces:
  - `PlayBlazor.Demo.Shared.DemoLanding` — parameters `Assembly Assembly`, `RenderFragment Flagships`, `string LibraryName`, `string DocsUrl`, `string Current`.
  - `PlayBlazor.Demo.Shared.LibrarySwitcher` — parameter `string Current` (`"mud"`, `"fluent"` or `"daisy"`).

- [ ] **Step 1: Create the shared project**

Create `demo/PlayBlazor.Demo.Shared/PlayBlazor.Demo.Shared.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <!--
    The library-agnostic demo chrome. It must NEVER reference a component library:
    that is the constraint PlayBlazor itself is built on, and the demo is its proof.
  -->
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.Web" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\PlayBlazor\PlayBlazor.csproj" />
  </ItemGroup>

</Project>
```

Create `demo/PlayBlazor.Demo.Shared/_Imports.razor`:

```razor
@using Microsoft.AspNetCore.Components
@using Microsoft.AspNetCore.Components.Web
@using PlayBlazor
@using PlayBlazor.Demo.Shared
```

- [ ] **Step 2: Write the failing test**

Create `tests/PlayBlazor.UnitTests/Shell/DemoLandingTests.cs`:

```csharp
using System.Reflection;
using AwesomeAssertions;
using Bunit;
using NUnit.Framework;
using PlayBlazor.Demo.Shared;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Shell;

public class DemoLandingTests
{
    private BunitContext _context = null!;

    [SetUp]
    public void Setup()
    {
        _context = new BunitContext();
        _context.Services.AddPlayBlazor();
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    [Test]
    public void Landing_TilesEveryDiscoveredComponent()
    {
        var cut = _context.Render<DemoLanding>(ps => ps
            .Add(c => c.Assembly, typeof(BasicFixture).Assembly)
            .Add(c => c.LibraryName, "Fixtures")
            .Add(c => c.DocsUrl, "https://example.test/")
            .Add(c => c.Current, "mud"));

        cut.FindAll("a.demo-tile").Count.Should().BeGreaterThan(0);
        cut.Markup.Should().Contain("Fixtures");
    }

    [Test]
    public void Switcher_MarksTheCurrentLibraryAndLinksTheOthers()
    {
        var cut = _context.Render<LibrarySwitcher>(ps => ps.Add(c => c.Current, "fluent"));

        cut.FindAll("a.demo-switch").Count.Should().Be(2);
        cut.FindAll("span.demo-switch-current").Count.Should().Be(1);
        cut.Markup.Should().Contain("../mud/");
        cut.Markup.Should().Contain("../daisy/");
    }
}
```

- [ ] **Step 3: Run the test to verify it fails**

Run: `dotnet test -c Release -- --filter "FullyQualifiedName~DemoLandingTests"`

Expected: compile failure — `PlayBlazor.Demo.Shared` does not exist.

- [ ] **Step 4: Write the switcher**

Create `demo/PlayBlazor.Demo.Shared/LibrarySwitcher.razor`:

```razor
<nav class="demo-switcher" aria-label="Component library">
    @foreach (var (slug, label) in Libraries)
    {
        @if (slug == Current)
        {
            <span class="demo-switch demo-switch-current" aria-current="page">@label</span>
        }
        else
        {
            @* Relative, never rooted: under a project subpath a leading slash walks past
               <base href> straight to the domain root. *@
            <a class="demo-switch" href="@($"../{slug}/")">@label</a>
        }
    }
</nav>
@code {
    private static readonly (string Slug, string Label)[] Libraries =
    [
        ("mud", "MudBlazor"),
        ("fluent", "Fluent UI"),
        ("daisy", "DaisyBlazor"),
    ];

    /// <summary>The slug of the library this app shows: <c>mud</c>, <c>fluent</c> or <c>daisy</c>.</summary>
    [Parameter, EditorRequired] public string Current { get; set; } = default!;
}
```

- [ ] **Step 5: Write the landing component**

Create `demo/PlayBlazor.Demo.Shared/DemoLanding.razor`. This is `demo/PlayBlazor.DemoHost/Pages/Index.razor` with the MudBlazor types lifted into parameters:

```razor
@using System.Reflection
@using PlayBlazor.Discovery
@inject IComponentCatalogProvider Catalog
@inject PlayBlazorOptions Options

<LibrarySwitcher Current="Current" />

<header class="demo-hero">
    <div class="demo-wordmark"><span class="demo-mark">▶</span> PlayBlazor</div>
    <p class="demo-tagline">
        Auto-generated playgrounds for any Blazor component library.
        Point it at an assembly — every parameter becomes a control, every
        <code>EventCallback</code> a log entry, every configuration a shareable link.
        No story files. Nothing below is hand-written.
    </p>
    @* Base-relative, never "/explorer": under a project subpath (GitHub Pages) a leading slash
       walks past <base href> straight to the domain root. *@
    <a class="demo-cta" href="explorer">Open the explorer &rarr;</a>
</header>

<main class="demo-main">
    @Flagships

    <section class="demo-section">
        <h2 class="demo-covered-title">The documented surface, discovered</h2>
        <p class="demo-note">@_covered.Count @LibraryName components pass the render sweep — each tile opens its playground.</p>
        <div class="demo-tiles">
            @foreach (var component in _covered)
            {
                <a class="demo-tile" href="@($"explorer?pb-{component.DisplayName}=e30")">
                    @if (Options.IconResolver?.Invoke(component.Type) is { } icon)
                    {
                        <svg viewBox="0 0 24 24" aria-hidden="true">@((MarkupString)icon)</svg>
                    }
                    <span>@component.DisplayName</span>
                </a>
            }
        </div>
    </section>
</main>
<footer class="demo-footer">
    <a href="@DocsUrl">@LibraryName</a> is the library this playground is pointed at, not a dependency of it.
</footer>

@code {
    private IReadOnlyList<PlayBlazor.Model.ComponentDescriptor> _covered = [];

    /// <summary>The assembly whose components this app explores.</summary>
    [Parameter, EditorRequired] public Assembly Assembly { get; set; } = default!;

    /// <summary>The hand-picked benches shown above the tile grid.</summary>
    [Parameter] public RenderFragment? Flagships { get; set; }

    /// <summary>The explored library's display name.</summary>
    [Parameter, EditorRequired] public string LibraryName { get; set; } = default!;

    /// <summary>The explored library's documentation site.</summary>
    [Parameter, EditorRequired] public string DocsUrl { get; set; } = default!;

    /// <summary>This app's slug, for the library switcher.</summary>
    [Parameter, EditorRequired] public string Current { get; set; } = default!;

    protected override void OnInitialized()
        => _covered = Catalog.Discover(Assembly)
            .Where(c => !Options.IsExcluded(c.Type) && (Options.ComponentFilter?.Invoke(c.Type) ?? true))
            .OrderBy(c => c.DisplayName, StringComparer.Ordinal)
            .ToArray();
}
```

Note the tile `href`: the original used a Razor attribute containing an interpolation with nested quotes, which Razor cannot nest — the `@(...)` form above is the fix.

- [ ] **Step 6: Move the MudBlazor app**

```bash
git mv demo/PlayBlazor.DemoHost demo/PlayBlazor.Demo.MudBlazor
git mv demo/PlayBlazor.Demo.MudBlazor/PlayBlazor.DemoHost.csproj demo/PlayBlazor.Demo.MudBlazor/PlayBlazor.Demo.MudBlazor.csproj
mkdir -p demo/PlayBlazor.Demo.Shared/wwwroot/css
git mv demo/PlayBlazor.Demo.MudBlazor/wwwroot/css/demo.css demo/PlayBlazor.Demo.Shared/wwwroot/css/demo.css
```

In `demo/PlayBlazor.Demo.MudBlazor/PlayBlazor.Demo.MudBlazor.csproj`, add the shared project next to the existing `PlayBlazor` reference:

```xml
    <ProjectReference Include="..\PlayBlazor.Demo.Shared\PlayBlazor.Demo.Shared.csproj" />
```

In `demo/PlayBlazor.Demo.MudBlazor/wwwroot/index.html`, the stylesheet now comes from the shared RCL — replace the `css/demo.css` link and the app-styles link:

```html
    <link href="_content/PlayBlazor.Demo.Shared/css/demo.css" rel="stylesheet" />
    <link href="PlayBlazor.Demo.MudBlazor.styles.css" rel="stylesheet" />
```

The namespace stays `PlayBlazor.DemoHost` (renaming it too would churn every file for no gain) — but the assembly name follows the project, so `PlayBlazor.Demo.MudBlazor.styles.css` is correct above.

- [ ] **Step 7: Point the MudBlazor index page at the shared landing**

Replace `demo/PlayBlazor.Demo.MudBlazor/Pages/Index.razor` entirely:

```razor
@page "/"
@using PlayBlazor.Demo.Shared
<DemoLanding Assembly="typeof(MudButton).Assembly"
             LibraryName="MudBlazor"
             DocsUrl="https://mudblazor.com/docs/overview"
             Current="mud">
    <Flagships>
        <section class="demo-section">
            <h2 class="demo-tag">MudButton</h2>
            <p class="demo-note">The flagship demo — try the example chips, flip parameters, watch the snippet follow.</p>
            <PlaygroundView Component="typeof(MudButton)" />
        </section>

        <section class="demo-section">
            <h2 class="demo-tag">MudDataGrid&lt;Person&gt;</h2>
            <p class="demo-note">Sample rows and columns come from host presets — Dense, Striped, Hover… stay yours to play with.</p>
            <PlaygroundView Component="typeof(MudDataGrid<Person>)" />
        </section>

        <section class="demo-section">
            <h2 class="demo-tag">PropertyColumn&lt;Person, string&gt;</h2>
            <p class="demo-note">A column cannot live outside its grid: the host scaffold renders the played column inside a real MudDataGrid.</p>
            <PlaygroundView Component="typeof(PropertyColumn<Person, string>)" />
        </section>
    </Flagships>
</DemoLanding>
```

- [ ] **Step 8: Update the solution and the workflow path filter**

In `PlayBlazor.slnx`, replace the `/demo/` folder contents:

```xml
  <Folder Name="/demo/">
    <Project Path="demo/PlayBlazor.Demo.Shared/PlayBlazor.Demo.Shared.csproj" />
    <Project Path="demo/PlayBlazor.Demo.MudBlazor/PlayBlazor.Demo.MudBlazor.csproj" />
  </Folder>
```

In `.github/workflows/deploy-demo.yml`, change the publish step's project path to `demo/PlayBlazor.Demo.MudBlazor/PlayBlazor.Demo.MudBlazor.csproj`. (The full matrix rewrite lands in Task 9; this keeps `main` deployable in between.)

In `tests/PlayBlazor.UnitTests/PlayBlazor.UnitTests.csproj`, update the reference added in Task 5 to the new path, and add the shared project:

```xml
    <ProjectReference Include="..\..\demo\PlayBlazor.Demo.MudBlazor\PlayBlazor.Demo.MudBlazor.csproj" />
    <ProjectReference Include="..\..\demo\PlayBlazor.Demo.Shared\PlayBlazor.Demo.Shared.csproj" />
```

- [ ] **Step 9: Update the docs that name the old path**

In `README.md` and `CLAUDE.md`, replace every `demo/PlayBlazor.DemoHost` with `demo/PlayBlazor.Demo.MudBlazor`, and update the layout tables to list the four demo projects. Verify none is left:

```bash
grep -rn "PlayBlazor.DemoHost" README.md CLAUDE.md .github src demo tests
```

Expected: only the `namespace PlayBlazor.DemoHost;` declarations in C# files, which are intentionally unchanged.

- [ ] **Step 10: Run the tests to verify they pass**

Run: `dotnet test -c Release -- --filter "FullyQualifiedName~DemoLandingTests"`

Expected: 2 tests PASS.

- [ ] **Step 11: Verify the app is visually unchanged**

Run: `dotnet run --project demo/PlayBlazor.Demo.MudBlazor`

Expected: `/` and `/explorer` look exactly as before, plus a three-item switcher strip at the top whose other two entries 404 for now. Stop the server afterwards.

- [ ] **Step 12: Run the whole suite**

Run: `dotnet test -c Release`

Expected: all pass.

- [ ] **Step 13: Commit**

```bash
git add -A
git commit -m "Refactor: split the demo into a shared chrome and a MudBlazor app"
```

---

### Task 7: Scaffold the Fluent UI app

**Files:**
- Create: `demo/PlayBlazor.Demo.FluentUI/` — `PlayBlazor.Demo.FluentUI.csproj`, `Program.cs`, `App.razor`, `MainLayout.razor`, `_Imports.razor`, `Pages/Index.razor`, `Pages/Explorer.razor`, `wwwroot/index.html`
- Modify: `Directory.Packages.props`, `PlayBlazor.slnx`

**Interfaces:**
- Consumes: `DemoLanding`, `LibrarySwitcher` (Task 6); `PlayBlazorOptions.Catalogue<T>` (Task 1).
- Produces: nothing other tasks consume. Curation is Milestone 3's own plan.

- [ ] **Step 1: Add the package version**

In `Directory.Packages.props`, inside the "Demo & Test Dependencies" group:

```xml
    <PackageVersion Include="Microsoft.FluentUI.AspNetCore.Components" Version="5.0.0-rc.5-26219.1" />
```

The exact RC is pinned deliberately: the spec accepts churn from a moving pre-release, but not silent churn.

- [ ] **Step 2: Create the project**

Create `demo/PlayBlazor.Demo.FluentUI/PlayBlazor.Demo.FluentUI.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <!-- The showcased library is a release candidate; NU5104 would otherwise fail the build. -->
    <NoWarn>$(NoWarn);NU5104</NoWarn>
  </PropertyGroup>

  <!--
    PlayBlazor discovers components by reflection, and the IL trimmer only keeps what is
    statically referenced. Rooting the explored assembly keeps its constructors and
    [Parameter] properties whole; the framework is still trimmed. See CLAUDE.md.
  -->
  <ItemGroup>
    <TrimmerRootAssembly Include="Microsoft.FluentUI.AspNetCore.Components" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" PrivateAssets="all" />
    <PackageReference Include="Microsoft.FluentUI.AspNetCore.Components" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\PlayBlazor\PlayBlazor.csproj" />
    <ProjectReference Include="..\PlayBlazor.Demo.Shared\PlayBlazor.Demo.Shared.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Create the app files**

`demo/PlayBlazor.Demo.FluentUI/_Imports.razor`:

```razor
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.FluentUI.AspNetCore.Components
@using PlayBlazor
@using PlayBlazor.Demo.Shared
@using PlayBlazor.Demo.FluentUI
```

`demo/PlayBlazor.Demo.FluentUI/App.razor` — identical to the MudBlazor app's:

```razor
<Router AppAssembly="typeof(App).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="routeData" DefaultLayout="typeof(MainLayout)" />
    </Found>
    <NotFound>
        <p>Page not found.</p>
    </NotFound>
</Router>
```

`demo/PlayBlazor.Demo.FluentUI/MainLayout.razor`:

> **v5 is not v4 here.** `FluentDesignTheme` and `FluentMenuProvider` do **not** exist in
> `5.0.0-rc.5-26219.1` — theming moved from a component to a service (`IThemeService`,
> `ThemeService`, `ThemeSettings`, `ThemeMode`), and a single `FluentProviders` replaces v4's stack.
> A scaffold needs no custom theme, so none is configured. Verified: `FluentProviders` activates as
> plain markup. Do not reach for `FluentMessageBarProvider` as a substitute without supplying its
> required `Section` parameter.

```razor
@inherits LayoutComponentBase
<FluentProviders />
<div class="demo-host">
    @Body
</div>
```

`demo/PlayBlazor.Demo.FluentUI/Program.cs`:

```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.FluentUI.AspNetCore.Components;
using PlayBlazor;
using PlayBlazor.Demo.FluentUI;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddFluentUIComponents();
builder.Services.AddPlayBlazor(FluentPlaygroundConfig.Configure);

await builder.Build().RunAsync();
```

`demo/PlayBlazor.Demo.FluentUI/FluentPlaygroundConfig.cs` — the seed Milestone 3 will grow:

```csharp
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
        // ToMarkup() returns a MarkupString, so this is AddContent's MarkupString overload —
        // AddMarkupContent takes a string and does not compile here.
        options.Catalogue(FluentIconCatalogue.All, static icon => builder
            => builder.AddContent(0, icon.ToMarkup()));
    }
}
```

`demo/PlayBlazor.Demo.FluentUI/FluentIconCatalogue.cs`:

> **Why these are hand-written and not Fluent's own icon set.** The concrete Fluent icons ship in a
> separate package, `Microsoft.FluentUI.AspNetCore.Components.Icons` — **23 MB of assemblies**
> (`Regular.dll` 10.6 MB, `Filled.dll` 9.0 MB). The Components package this app references contains
> **zero** icon types, so reflecting over it would yield a silently empty catalogue; referencing the
> Icons package instead would bloat a WASM demo whose whole pitch is that it loads fast. What this
> milestone has to prove is that an `Icon`-**typed** parameter becomes drivable at all, and a couple
> of dozen icons prove that exactly as well as ten thousand. Milestone 3 revisits the full set with
> the payload measured.
>
> `DemoIcon` derives from `Icon` rather than calling its constructor directly, which works whether
> that constructor is public or protected — no guess about visibility is involved.

```csharp
using Microsoft.FluentUI.AspNetCore.Components;

namespace PlayBlazor.Demo.FluentUI;

/// <summary>
/// The Fluent icons the picker offers. Each entry is an <see cref="Icon" /> INSTANCE — the same
/// object handed to the specimen, which is what lets a permalink map it back to its name.
/// </summary>
public static class FluentIconCatalogue
{
    /// <summary>An icon carrying hand-written SVG, so the demo needs no icon package.</summary>
    private sealed class DemoIcon(string name, string content)
        : Icon(name, IconVariant.Regular, IconSize.Size24, content);

    private const string Stroke = "fill='none' stroke='currentColor' stroke-width='2' "
                                  + "stroke-linecap='round' stroke-linejoin='round'";

    private static readonly (string Name, string Content)[] Shapes =
    [
        ("Circle", "<circle cx='12' cy='12' r='8' fill='currentColor' />"),
        ("CircleOutline", $"<circle cx='12' cy='12' r='8' {Stroke} />"),
        ("Square", "<rect x='5' y='5' width='14' height='14' rx='2' fill='currentColor' />"),
        ("SquareOutline", $"<rect x='5' y='5' width='14' height='14' rx='2' {Stroke} />"),
        ("Triangle", "<path d='M12 4 L20 19 H4 Z' fill='currentColor' />"),
        ("Checkmark", $"<path d='M4 13 l5 5 L20 7' {Stroke} />"),
        ("Dismiss", $"<path d='M6 6 L18 18 M18 6 L6 18' {Stroke} />"),
        ("Add", $"<path d='M12 5 V19 M5 12 H19' {Stroke} />"),
        ("Subtract", $"<path d='M5 12 H19' {Stroke} />"),
        ("ArrowUp", $"<path d='M12 19 V5 M6 11 l6-6 6 6' {Stroke} />"),
        ("ArrowDown", $"<path d='M12 5 V19 M6 13 l6 6 6-6' {Stroke} />"),
        ("ArrowLeft", $"<path d='M19 12 H5 M11 6 l-6 6 6 6' {Stroke} />"),
        ("ArrowRight", $"<path d='M5 12 H19 M13 6 l6 6-6 6' {Stroke} />"),
        ("ChevronUp", $"<path d='M6 15 l6-6 6 6' {Stroke} />"),
        ("ChevronDown", $"<path d='M6 9 l6 6 6-6' {Stroke} />"),
        ("Play", "<path d='M8 5 L19 12 L8 19 Z' fill='currentColor' />"),
        ("Pause", "<path d='M8 5 h3 v14 h-3 Z M13 5 h3 v14 h-3 Z' fill='currentColor' />"),
        ("Star", "<path d='M12 3 l2.6 6.3 6.8 .5 -5.2 4.4 1.6 6.6 -5.8-3.6 -5.8 3.6 1.6-6.6 "
                 + "-5.2-4.4 6.8-.5 Z' fill='currentColor' />"),
        ("Heart", "<path d='M12 20 C6 16 3 12.5 3 9.2 A4.2 4.2 0 0 1 12 7 A4.2 4.2 0 0 1 21 9.2 "
                  + "C21 12.5 18 16 12 20 Z' fill='currentColor' />"),
        ("Search", $"<circle cx='11' cy='11' r='6' {Stroke} /><path d='M15.5 15.5 L20 20' {Stroke} />"),
        ("Settings", $"<circle cx='12' cy='12' r='3' {Stroke} /><circle cx='12' cy='12' r='8' {Stroke} />"),
        ("Delete", $"<path d='M5 7 h14 M10 7 V5 h4 v2 M7 7 l1 13 h8 l1-13' {Stroke} />"),
        ("Edit", $"<path d='M4 20 h4 L19 9 l-4-4 -11 11 Z' {Stroke} />"),
        ("Warning", $"<path d='M12 4 L21 20 H3 Z M12 10 v4 M12 17 v.5' {Stroke} />"),
    ];

    /// <summary>The offered icons, by name.</summary>
    public static IReadOnlyDictionary<string, Icon> All { get; } =
        Shapes.ToDictionary(
            static shape => shape.Name,
            static shape => (Icon)new DemoIcon(shape.Name, shape.Content),
            StringComparer.Ordinal);
}
```

`demo/PlayBlazor.Demo.FluentUI/Pages/Index.razor`:

```razor
@page "/"
<DemoLanding Assembly="typeof(FluentButton).Assembly"
             LibraryName="Fluent UI Blazor"
             DocsUrl="https://www.fluentui-blazor.net/"
             Current="fluent" />
```

`demo/PlayBlazor.Demo.FluentUI/Pages/Explorer.razor`:

```razor
@page "/explorer"
@using PlayBlazor.Shell.Workspace
<PlaygroundWorkspace Assemblies="@(new[] { typeof(FluentButton).Assembly })" />
```

- [ ] **Step 4: Create the host page**

Create `demo/PlayBlazor.Demo.FluentUI/wwwroot/index.html` by copying the MudBlazor app's and swapping the library assets:

```bash
cp demo/PlayBlazor.Demo.MudBlazor/wwwroot/index.html demo/PlayBlazor.Demo.FluentUI/wwwroot/index.html
cp demo/PlayBlazor.Demo.MudBlazor/wwwroot/favicon.png demo/PlayBlazor.Demo.FluentUI/wwwroot/favicon.png
cp demo/PlayBlazor.Demo.MudBlazor/wwwroot/apple-touch-icon.png demo/PlayBlazor.Demo.FluentUI/wwwroot/apple-touch-icon.png
```

Then in the copied `index.html`:
- change the `<title>` to `PlayBlazor — Fluent UI`;
- **remove** the Google Fonts Roboto link, the `_content/MudBlazor/MudBlazor.min.css` link and the `_content/MudBlazor/MudBlazor.min.js` script;
- change the app stylesheet link to `PlayBlazor.Demo.FluentUI.styles.css`;
- keep the `_content/PlayBlazor.Demo.Shared/css/demo.css` link and the boot-screen `<style>` block as-is.

Fluent UI v5 loads its own JS through a Blazor JS initializer — do **not** add a script tag for it.

- [ ] **Step 5: Add the project to the solution**

In `PlayBlazor.slnx`, inside the `/demo/` folder:

```xml
    <Project Path="demo/PlayBlazor.Demo.FluentUI/PlayBlazor.Demo.FluentUI.csproj" />
```

- [ ] **Step 6: Build and run it**

Run: `dotnet build -c Release`

Expected: SUCCESS.

Run: `dotnet run --project demo/PlayBlazor.Demo.FluentUI`

Expected: `/` lists the Fluent components as tiles (uncurated, so the list is long and includes providers and base types — that is Milestone 3's job); `/explorer` opens the workspace; picking a `FluentButton` and opening its `IconStart` parameter shows the **icon picker**, which is the whole point of Milestone 1. Stop the server afterwards.

- [ ] **Step 7: Run the whole suite**

Run: `dotnet test -c Release`

Expected: all pass.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "Feat: scaffold the Fluent UI demo app"
```

---

### Task 8: Scaffold the DaisyBlazor app

> **BLOCKED.** This task needs `DaisyBlazor.Components` 1.0.0 on NuGet and `@daisyblazor/tailwind` on npm, both published by merging the release-please PR `phmatray/blazor-tailwind-ui#20`. Verify before starting:
>
> ```bash
> curl -s "https://api.nuget.org/v3-flatcontainer/daisyblazor.components/index.json" | grep -o '"1\.0\.0"'
> curl -s -o /dev/null -w "%{http_code}\n" "https://registry.npmjs.org/@daisyblazor%2Ftailwind"
> ```
>
> Expected: `"1.0.0"` and `200`. If either is missing, stop and report — do not fall back to 0.2.3, which is two breaking changes stale.

**Files:**
- Create: `demo/PlayBlazor.Demo.Daisy/` — project, app files, `Styles/main.css`, `package.json`, `wwwroot/index.html`
- Modify: `Directory.Packages.props`, `PlayBlazor.slnx`, `.gitignore`

**Interfaces:**
- Consumes: `DemoLanding`, `LibrarySwitcher` (Task 6).
- Produces: nothing other tasks consume.

- [ ] **Step 1: Add the package version**

In `Directory.Packages.props`, in the "Demo & Test Dependencies" group:

```xml
    <PackageVersion Include="DaisyBlazor.Components" Version="1.0.0" />
```

- [ ] **Step 2: Create the project**

Create `demo/PlayBlazor.Demo.Daisy/PlayBlazor.Demo.Daisy.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <!-- See CLAUDE.md: a trimmed publish decapitates reflection unless the explored assembly is rooted. -->
  <ItemGroup>
    <TrimmerRootAssembly Include="DaisyBlazor.Components" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" PrivateAssets="all" />
    <PackageReference Include="DaisyBlazor.Components" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\PlayBlazor\PlayBlazor.csproj" />
    <ProjectReference Include="..\PlayBlazor.Demo.Shared\PlayBlazor.Demo.Shared.csproj" />
  </ItemGroup>

  <!-- The generated Tailwind stylesheet is a build output, never committed. -->
  <ItemGroup>
    <Content Remove="wwwroot/css/app.css" />
    <Content Include="wwwroot/css/app.css" Condition="Exists('wwwroot/css/app.css')" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Wire the Tailwind build**

Create `demo/PlayBlazor.Demo.Daisy/package.json`:

```json
{
  "name": "playblazor-demo-daisy",
  "version": "0.1.0",
  "private": true,
  "description": "Tailwind CSS v4 + daisyUI build for the PlayBlazor DaisyBlazor showcase",
  "scripts": {
    "build:css": "tailwindcss -i ./Styles/main.css -o ./wwwroot/css/app.css --minify",
    "watch:css": "tailwindcss -i ./Styles/main.css -o ./wwwroot/css/app.css --watch"
  },
  "devDependencies": {
    "@daisyblazor/tailwind": "^1.0.0",
    "@tailwindcss/cli": "^4.1.0",
    "tailwindcss": "^4.1.0"
  }
}
```

Create `demo/PlayBlazor.Demo.Daisy/Styles/main.css`:

```css
@import "tailwindcss";

/* The shippable DaisyBlazor preset: daisyUI plugin + themes + the C#-class safelist.
   The safelist is what keeps the playground working at all: PlayBlazor composes classes
   at RUNTIME (btn-accent, badge-xl) that Tailwind's static analysis never sees. */
@import "@daisyblazor/tailwind/preset.css";

/* Our own markup. The kit's classes come from the safelist, not from scanning the package. */
@source "../Pages/**/*.razor";
@source "../*.razor";
```

Add to `.gitignore`:

```
demo/PlayBlazor.Demo.Daisy/wwwroot/css/app.css
```

- [ ] **Step 4: Create the app files**

`_Imports.razor`:

```razor
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using DaisyBlazor
@using PlayBlazor
@using PlayBlazor.Demo.Shared
@using PlayBlazor.Demo.Daisy
```

`App.razor` — identical to the other two apps (copy it verbatim from `demo/PlayBlazor.Demo.FluentUI/App.razor`).

`MainLayout.razor`:

```razor
@inherits LayoutComponentBase
<ThemeProvider />
<div class="demo-host">
    @Body
</div>
```

`Program.cs`:

```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PlayBlazor;
using PlayBlazor.Demo.Daisy;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddDaisyBlazor();
builder.Services.AddPlayBlazor(DaisyPlaygroundConfig.Configure);

await builder.Build().RunAsync();
```

`DaisyPlaygroundConfig.cs`:

```csharp
namespace PlayBlazor.Demo.Daisy;

/// <summary>
/// Curation for the DaisyBlazor showcase. A seed only: the curated surface, presets, scaffolds
/// and variants land in milestone 4, driven by the [Explicit] sweep's inventory.
/// </summary>
public static class DaisyPlaygroundConfig
{
    /// <summary>Applies the DaisyBlazor curation to the playground options.</summary>
    /// <param name="options">The options to configure.</param>
    public static void Configure(PlayBlazorOptions options)
    {
        // DaisyBlazor icons are Material LIGATURE NAMES ("ac_unit"), not SVG markup: the
        // shipped preview would render nothing, so the catalogue supplies a font span instead.
        options.Catalogue(DaisyIconCatalogue.Material, static name => builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "material-symbols-outlined");
            builder.AddContent(2, name);
            builder.CloseElement();
        });
    }
}
```

`DaisyIconCatalogue.cs`:

```csharp
using System.Reflection;
using DaisyBlazor;

namespace PlayBlazor.Demo.Daisy;

/// <summary>Every <c>Icons.Material.Filled</c> ligature name DaisyBlazor exposes, by constant name.</summary>
public static class DaisyIconCatalogue
{
    /// <summary>The Material ligature names, by constant name.</summary>
    public static IReadOnlyDictionary<string, string> Material { get; } =
        typeof(Icons.Material.Filled)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(static field => field.IsLiteral && field.FieldType == typeof(string))
            .ToDictionary(
                static field => field.Name,
                static field => (string)field.GetRawConstantValue()!,
                StringComparer.Ordinal);
}
```

> If `DaisyBlazor.Icons.Material.Filled` is not the exact shape at 1.0.0, run
> `dotnet build -c Release` and read the compiler error; the kit's `Compat/Icons.cs` is the
> authority. Adjust the type reference only — the reflection shape is identical to MudBlazor's.

`Pages/Index.razor`:

```razor
@page "/"
<DemoLanding Assembly="typeof(Button).Assembly"
             LibraryName="DaisyBlazor"
             DocsUrl="https://phmatray.github.io/blazor-tailwind-ui/"
             Current="daisy" />
```

`Pages/Explorer.razor`:

```razor
@page "/explorer"
@using PlayBlazor.Shell.Workspace
<PlaygroundWorkspace Assemblies="@(new[] { typeof(Button).Assembly })" />
```

- [ ] **Step 5: Create the host page**

```bash
cp demo/PlayBlazor.Demo.FluentUI/wwwroot/index.html demo/PlayBlazor.Demo.Daisy/wwwroot/index.html
cp demo/PlayBlazor.Demo.FluentUI/wwwroot/favicon.png demo/PlayBlazor.Demo.Daisy/wwwroot/favicon.png
cp demo/PlayBlazor.Demo.FluentUI/wwwroot/apple-touch-icon.png demo/PlayBlazor.Demo.Daisy/wwwroot/apple-touch-icon.png
```

In the copied `index.html`:
- change the `<title>` to `PlayBlazor — DaisyBlazor`;
- change the app stylesheet link to `PlayBlazor.Demo.Daisy.styles.css`;
- add the generated Tailwind sheet **before** the shared demo stylesheet, so the demo chrome wins over Tailwind's preflight:

```html
    <link href="css/app.css" rel="stylesheet" />
    <link href="_content/PlayBlazor.Demo.Shared/css/demo.css" rel="stylesheet" />
```

- add the Material Symbols font the icon preview needs, next to the other `<link>` tags:

```html
    <link href="https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined&display=swap" rel="stylesheet" />
```

- [ ] **Step 6: Add the project to the solution**

In `PlayBlazor.slnx`, inside the `/demo/` folder:

```xml
    <Project Path="demo/PlayBlazor.Demo.Daisy/PlayBlazor.Demo.Daisy.csproj" />
```

- [ ] **Step 7: Build the CSS, then the app**

```bash
npm --prefix demo/PlayBlazor.Demo.Daisy install
npm --prefix demo/PlayBlazor.Demo.Daisy run build:css
dotnet build -c Release
```

Expected: `demo/PlayBlazor.Demo.Daisy/wwwroot/css/app.css` exists and the solution builds.

- [ ] **Step 8: Run it and look for safelist holes**

Run: `dotnet run --project demo/PlayBlazor.Demo.Daisy`

Open `/explorer`, pick `Button`, and cycle its `Color` through every value. Expected: each value visibly restyles the specimen. **A value that changes nothing is a hole in DaisyBlazor's safelist, not a PlayBlazor bug** — the spec anticipates this. Record each one; they are reported upstream to `blazor-tailwind-ui`, not patched here.

Stop the server afterwards.

- [ ] **Step 9: Run the whole suite**

Run: `dotnet test -c Release`

Expected: all pass.

- [ ] **Step 10: Commit**

```bash
git add -A
git commit -m "Feat: scaffold the DaisyBlazor demo app"
```

---

### Task 9: The static landing and the deployment matrix

**Files:**
- Create: `demo/landing/index.html`
- Modify: `.github/workflows/deploy-demo.yml`
- Modify: `README.md`

**Interfaces:**
- Consumes: the three apps (Tasks 6-8).
- Produces: the published site layout `/`, `/mud/`, `/fluent/`, `/daisy/`.

- [ ] **Step 1: Write the static landing**

Create `demo/landing/index.html`. It is deliberately plain HTML — no WASM, so the site root opens instantly and choosing a library never costs a .NET runtime:

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>PlayBlazor — pick a component library</title>
    <link rel="icon" type="image/png" href="favicon.png" />
    <style>
        :root { color-scheme: light; }
        body {
            margin: 0;
            min-height: 100vh;
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            gap: 2rem;
            background: #ffffff radial-gradient(#e4e4ec 1px, transparent 1px) 0 0 / 16px 16px;
            font-family: system-ui, -apple-system, "Segoe UI", Roboto, sans-serif;
            color: #1b1b22;
        }
        h1 { margin: 0; font-size: 1.75rem; letter-spacing: -0.01em; }
        h1 .mark { color: #6d4aff; }
        p { max-width: 34rem; margin: 0 1.5rem; text-align: center; color: #6e6e7a; line-height: 1.6; }
        .cards { display: flex; flex-wrap: wrap; gap: 1rem; justify-content: center; padding: 0 1.5rem; }
        .card {
            display: block;
            width: 15rem;
            padding: 1.25rem;
            border: 1px solid #e6e6ee;
            border-radius: 0.75rem;
            background: #fbfbfd;
            color: inherit;
            text-decoration: none;
            transition: border-color 150ms ease, transform 150ms ease;
        }
        .card:hover { border-color: #6d4aff; transform: translateY(-2px); }
        .card strong { display: block; margin-bottom: 0.35rem; font-size: 1.0625rem; }
        .card span { color: #6e6e7a; font-size: 0.875rem; line-height: 1.5; }
        footer { color: #6e6e7a; font-size: 0.8125rem; text-align: center; margin: 0 1.5rem; }
        footer a { color: #6d4aff; }
        @media (prefers-reduced-motion: reduce) { .card { transition: none; } }
    </style>
</head>
<body>
    <h1><span class="mark">▶</span> PlayBlazor</h1>
    <p>
        The same playground, pointed at three different component libraries.
        Every control below was generated by reflection — no story files, nothing hand-written.
    </p>
    <div class="cards">
        <a class="card" href="mud/">
            <strong>MudBlazor</strong>
            <span>65 curated components, Material Design.</span>
        </a>
        <a class="card" href="fluent/">
            <strong>Fluent UI Blazor</strong>
            <span>Microsoft's Fluent Design System, v5.</span>
        </a>
        <a class="card" href="daisy/">
            <strong>DaisyBlazor</strong>
            <span>Tailwind CSS v4 and daisyUI v5.</span>
        </a>
    </div>
    <footer>
        None of these libraries is a dependency of PlayBlazor —
        <a href="https://github.com/Atypical-Consulting/PlayBlazor">see the source</a>.
    </footer>
</body>
</html>
```

Copy a favicon next to it:

```bash
cp demo/PlayBlazor.Demo.MudBlazor/wwwroot/favicon.png demo/landing/favicon.png
```

- [ ] **Step 2: Rewrite the build job as a matrix**

In `.github/workflows/deploy-demo.yml`, replace the `build` job (keep the `on:`, `permissions:`, `concurrency:` and `deploy:` blocks untouched):

```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    strategy:
      matrix:
        include:
          - app: mud
            project: demo/PlayBlazor.Demo.MudBlazor/PlayBlazor.Demo.MudBlazor.csproj
            npm: false
          - app: fluent
            project: demo/PlayBlazor.Demo.FluentUI/PlayBlazor.Demo.FluentUI.csproj
            npm: false
          - app: daisy
            project: demo/PlayBlazor.Demo.Daisy/PlayBlazor.Demo.Daisy.csproj
            npm: true
    steps:
      - uses: actions/checkout@v6

      - uses: actions/setup-dotnet@v5
        with:
          dotnet-version: 10.x

      # Only the DaisyBlazor app has a front-end build: daisyUI is a Tailwind plugin, so its
      # stylesheet is generated, not shipped.
      - uses: actions/setup-node@v6
        if: matrix.npm
        with:
          node-version: 22.x
          cache: npm
          cache-dependency-path: demo/PlayBlazor.Demo.Daisy/package-lock.json

      - name: Build the Tailwind stylesheet
        if: matrix.npm
        run: |
          npm --prefix demo/PlayBlazor.Demo.Daisy ci
          npm --prefix demo/PlayBlazor.Demo.Daisy run build:css

      - name: Publish the app
        run: dotnet publish ${{ matrix.project }} -c Release -o publish

      # The site is served from https://atypical-consulting.github.io/PlayBlazor/<app>/, not from
      # a domain root, so every asset URL Blazor resolves — _framework, _content, the scoped CSS —
      # has to be rooted at that prefix.
      - name: Rewrite the base href for the project subpath
        run: sed -i 's|<base href="/" />|<base href="/PlayBlazor/${{ matrix.app }}/" />|' publish/wwwroot/index.html

      # sed reports success when it matches nothing, which would ship a site whose every asset
      # 404s and whose failure only shows up in a browser. Fail the build here instead.
      - name: Verify the base href was rewritten
        run: |
          grep -q '<base href="/PlayBlazor/${{ matrix.app }}/" />' publish/wwwroot/index.html \
            || { echo "::error::base href was not rewritten — did index.html change shape?"; exit 1; }

      # Pages serves 404.html for any path it holds no file for. Each app routes on the client,
      # and a PlayBlazor permalink IS a deep link, so an unknown path must return the app shell
      # and let the Blazor router take it from there.
      - name: Install the SPA fallback
        run: cp publish/wwwroot/index.html publish/wwwroot/404.html

      - uses: actions/upload-artifact@v5
        with:
          name: app-${{ matrix.app }}
          path: publish/wwwroot
          retention-days: 1

  assemble:
    needs: build
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v6

      - uses: actions/download-artifact@v5
        with:
          pattern: app-*
          path: staging

      - name: Lay the site out
        run: |
          mkdir -p site
          cp demo/landing/index.html demo/landing/favicon.png site/
          for app in mud fluent daisy; do
            mv "staging/app-$app" "site/$app"
          done

      # Belt and braces: the Pages artifact is served as-is today, but should this ever be served
      # through Jekyll it would strip _framework/ and _content/ for starting with an underscore.
      - name: Disable Jekyll processing
        run: touch site/.nojekyll

      - name: Verify all three apps landed
        run: |
          for app in mud fluent daisy; do
            test -f "site/$app/index.html" \
              || { echo "::error::$app is missing from the assembled site"; exit 1; }
          done

      - uses: actions/upload-pages-artifact@v5
        with:
          path: site
```

Then change the `deploy` job's `needs: build` to `needs: assemble`.

- [ ] **Step 3: Validate the workflow locally**

Run: `python3 -c "import yaml,sys; yaml.safe_load(open('.github/workflows/deploy-demo.yml')); print('ok')"`

Expected: `ok`.

- [ ] **Step 4: Update the README**

In `README.md`, replace the "Try it" section's single link with the site layout:

```markdown
**[atypical-consulting.github.io/PlayBlazor](https://atypical-consulting.github.io/PlayBlazor/)** — the
same playground pointed at three libraries, running in your browser:
[MudBlazor](https://atypical-consulting.github.io/PlayBlazor/mud/),
[Fluent UI](https://atypical-consulting.github.io/PlayBlazor/fluent/),
[DaisyBlazor](https://atypical-consulting.github.io/PlayBlazor/daisy/).
```

and update the repository-layout table to list the four demo projects.

- [ ] **Step 5: Full build and test**

```bash
dotnet build -c Release
dotnet test -c Release
```

Expected: both succeed.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "Build: publish the three demo apps under one Pages site"
```

- [ ] **Step 7: Verify the deployment**

Push the branch and open a PR. After it merges, the workflow runs; then check each path returns 200 and that the switcher links work:

```bash
for p in "" mud/ fluent/ daisy/; do
  printf "%-10s " "/$p"
  curl -s -o /dev/null -w "%{http_code}\n" "https://atypical-consulting.github.io/PlayBlazor/$p"
done
```

Expected: four `200`s.

---

### Task 10: Parameterize the diagnostic sweeps per library

The spec makes this the gate on Milestones 3 and 4: *"le sweep d'abord, la config ensuite"*. Today both `[Explicit]` suites hardcode MudBlazor, so there is no way to inventory Fluent or Daisy — and no evidence to write their plans from. This task ends Milestone 2 by producing that evidence.

**Files:**
- Modify: `tests/PlayBlazor.UnitTests/Diagnostics/UnsupportedParameterInventory.cs`
- Modify: `tests/PlayBlazor.UnitTests/Diagnostics/MudBlazorRenderSweepTests.cs` → renamed `RenderSweepTests.cs`
- Create: `tests/PlayBlazor.UnitTests/Diagnostics/ExploredLibraries.cs`

**Interfaces:**
- Consumes: the three demo projects (Tasks 6-8), already referenced by the test project.
- Produces: `ExploredLibraries.All` — `IEnumerable<TestCaseData>` of `(string Name, Assembly Assembly, Action<PlayBlazorOptions> Configure)`, the NUnit source both suites iterate.

- [ ] **Step 1: Write the shared library source**

Create `tests/PlayBlazor.UnitTests/Diagnostics/ExploredLibraries.cs`:

```csharp
using System.Reflection;
using NUnit.Framework;

namespace PlayBlazor.UnitTests.Diagnostics;

/// <summary>
/// The libraries the diagnostic sweeps inventory — one case per demo app, so a sweep reports
/// on exactly the configuration that app ships.
/// </summary>
public static class ExploredLibraries
{
    /// <summary>One NUnit case per explored library: display name, assembly, host configuration.</summary>
    public static IEnumerable<TestCaseData> All
    {
        get
        {
            yield return new TestCaseData(
                typeof(MudBlazor.MudButton).Assembly,
                (Action<PlayBlazorOptions>)PlayBlazor.DemoHost.PlaygroundConfig.Configure)
                { TestName = "MudBlazor" };

            yield return new TestCaseData(
                typeof(Microsoft.FluentUI.AspNetCore.Components.FluentButton).Assembly,
                (Action<PlayBlazorOptions>)PlayBlazor.Demo.FluentUI.FluentPlaygroundConfig.Configure)
                { TestName = "FluentUI" };

            yield return new TestCaseData(
                typeof(DaisyBlazor.Button).Assembly,
                (Action<PlayBlazorOptions>)PlayBlazor.Demo.Daisy.DaisyPlaygroundConfig.Configure)
                { TestName = "DaisyBlazor" };
        }
    }
}
```

> If Task 8 is still blocked, comment out the DaisyBlazor case with a note pointing at `blazor-tailwind-ui#20` rather than deleting it.

- [ ] **Step 2: Reference the two new demo apps from the test project**

In `tests/PlayBlazor.UnitTests/PlayBlazor.UnitTests.csproj`, alongside the references added in Tasks 5 and 6:

```xml
    <ProjectReference Include="..\..\demo\PlayBlazor.Demo.FluentUI\PlayBlazor.Demo.FluentUI.csproj" />
    <ProjectReference Include="..\..\demo\PlayBlazor.Demo.Daisy\PlayBlazor.Demo.Daisy.csproj" />
```

- [ ] **Step 3: Turn the unsupported-parameter inventory into a per-library case**

Read `tests/PlayBlazor.UnitTests/Diagnostics/UnsupportedParameterInventory.cs` first — it currently builds `new ReflectionCatalogProvider()` at line 14 and walks the MudBlazor assembly. Change three things and nothing else:

1. Put `[TestCaseSource(typeof(ExploredLibraries), nameof(ExploredLibraries.All))]` on the `[Explicit]` test method, and give it the parameters `(Assembly assembly, Action<PlayBlazorOptions> configure)`.
2. Build the provider **with** the app's options, so a catalogued type is no longer reported as unsupported:

```csharp
        var options = new PlayBlazorOptions();
        configure(options);
        var provider = new ReflectionCatalogProvider(options: options);
```

3. Replace the hardcoded MudBlazor assembly with the `assembly` parameter, and prefix the printed report with the library name so three runs are distinguishable.

- [ ] **Step 4: Do the same to the render sweep**

```bash
git mv tests/PlayBlazor.UnitTests/Diagnostics/MudBlazorRenderSweepTests.cs tests/PlayBlazor.UnitTests/Diagnostics/RenderSweepTests.cs
```

Rename the class to `RenderSweepTests`, then apply the same three changes as Step 3 (the provider is built at line 44).

The sweep renders real components: each library's services must be registered on the bUnit context before the sweep runs. Add a switch on the assembly's name in `Setup`, so each library gets what it needs:

```csharp
        // Each library needs its own service registrations before its components will render.
        if (assembly.GetName().Name == "MudBlazor") { _context.Services.AddMudServices(); }
        else if (assembly.GetName().Name == "Microsoft.FluentUI.AspNetCore.Components") { _context.Services.AddFluentUIComponents(); }
        else if (assembly.GetName().Name == "DaisyBlazor.Components") { _context.Services.AddDaisyBlazor(); }
```

- [ ] **Step 5: Verify both suites are still skipped by default**

Run: `dotnet test -c Release`

Expected: all pass; the two `[Explicit]` suites report as skipped, not run. A diagnostic inventory must never become part of the normal gate.

- [ ] **Step 6: Run the sweeps and capture the evidence**

```bash
mkdir -p docs/superpowers/plans/sweeps
dotnet test -c Release -- --filter "FullyQualifiedName~UnsupportedParameterInventory" > docs/superpowers/plans/sweeps/2026-unsupported.txt
dotnet test -c Release -- --filter "FullyQualifiedName~RenderSweepTests" > docs/superpowers/plans/sweeps/2026-render.txt
```

Expected: both files list one section per library. These two files are the input to the Milestone 3 and 4 plans — without them those plans would be guesswork.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "Test: inventory every explored library, not just MudBlazor"
```

**Milestone 2 is complete here.** The site serves three apps, and the next two milestones have their evidence.

---

## What this plan does NOT cover

Deliberately out of scope, each needing its own plan written against evidence that does not exist yet:

- **Milestone 3 — Fluent UI curation.** ~60 components with presets, scaffolds and variants. Write it from `docs/superpowers/plans/sweeps/` (Task 10), not from guesswork.
- **Milestone 4 — DaisyBlazor curation.** Same, plus the safelist holes recorded in Task 8 Step 8.
- **Everything in the spec's §9**: satellite profile packages, wiring `XmlDocSummaryReader`, and the archived-repo docs duplicate.
