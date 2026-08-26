# PlayBlazor Milestones 1–3 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the PlayBlazor core (auto-generated component playground) through milestone 3: descriptor model + reflection discovery, first rendered playground with basic controls, and live Razor code generation.

**Architecture:** A dependency-free Razor Class Library (`PlayBlazor`) discovers Blazor components via reflection behind `IComponentCatalogProvider`, renders them through `DynamicComponent` inside an `ErrorBoundary`, drives them from auto-generated control panels, and generates the matching Razor snippet. A WASM `DemoHost` exercises it against real MudBlazor components. Tests are NUnit + bUnit against synthetic fixture components.

**Tech Stack:** .NET 10 (`$(PrimaryTargetFramework)`), Razor Class Library (`Microsoft.NET.Sdk.Razor`), Blazor WebAssembly, NUnit 4.6.1 + bUnit 2.9.0 + AwesomeAssertions 9.6.0 on Microsoft.Testing.Platform (`EnableNUnitRunner`).

**Spec:** `docs/superpowers/specs/2026-08-26-playblazor-design.md`

## Global Constraints

- All projects: `<TargetFramework>$(PrimaryTargetFramework)</TargetFramework>` (= net10.0), `<Nullable>enable</Nullable>`. `TreatWarningsAsErrors` and `ImplicitUsings` are inherited from `src/Directory.Build.props` — code must be warning-clean and nullable-clean.
- `PlayBlazor` (the core RCL) must have **zero dependency on MudBlazor** — only `Microsoft.AspNetCore.Components.Web`. MudBlazor is referenced only by `PlayBlazor.DemoHost` and `PlayBlazor.UnitTests`.
- Shell UI strings are **English** ("Copy", "Reset") — the product targets international library authors.
- All shell CSS class names are prefixed `pb-`.
- Commit message style: `PlayBlazor: <description>` + the repo's co-author trailer.
- Run all commands from the repo root (`/Users/philippe/repo/phmatray/public/MudBlazor/.claude/worktrees/cheerful-zooming-starfish`).
- Test run command (whole suite — it stays small): `dotnet run --project src/PlayBlazor.UnitTests`
- Spec deviation (validated during planning): generated Razor emits `Disabled="true"`, never a minimized bare attribute — Razor does not support minimized boolean attributes on **components**.

---

## Jalon 1 — Fondations

### Task 1: Scaffolding des projets

**Files:**
- Create: `src/PlayBlazor/PlayBlazor.csproj`
- Create: `src/PlayBlazor/_Imports.razor`
- Create: `src/PlayBlazor.UnitTests/PlayBlazor.UnitTests.csproj`
- Create: `src/PlayBlazor.UnitTests/SmokeTest.cs`
- Modify: `src/MudBlazor.slnx`

**Interfaces:**
- Produces: two buildable projects; test project runs NUnit via `dotnet run --project src/PlayBlazor.UnitTests`.

- [x] **Step 1: Create the core RCL project file**

`src/PlayBlazor/PlayBlazor.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <TargetFramework>$(PrimaryTargetFramework)</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="10.0.11" />
  </ItemGroup>

</Project>
```

- [x] **Step 2: Create `src/PlayBlazor/_Imports.razor`**

```razor
@using Microsoft.AspNetCore.Components
@using Microsoft.AspNetCore.Components.Web
```

- [x] **Step 3: Create the test project file**

`src/PlayBlazor.UnitTests/PlayBlazor.UnitTests.csproj` (Razor SDK so fixture components can be `.razor` files):

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <TargetFramework>$(PrimaryTargetFramework)</TargetFramework>
    <OutputType>Exe</OutputType>
    <EnableNUnitRunner>true</EnableNUnitRunner>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="AwesomeAssertions" Version="9.6.0" />
    <PackageReference Include="bunit" Version="2.9.0" />
    <PackageReference Include="nunit" Version="4.6.1" />
    <PackageReference Include="NUnit3TestAdapter" Version="6.2.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.9.0" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="10.0.11" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\PlayBlazor\PlayBlazor.csproj" />
    <ProjectReference Include="..\MudBlazor\MudBlazor.csproj" />
  </ItemGroup>

</Project>
```

- [x] **Step 4: Create `src/PlayBlazor.UnitTests/SmokeTest.cs`**

```csharp
using AwesomeAssertions;
using NUnit.Framework;

namespace PlayBlazor.UnitTests;

public class SmokeTest
{
    [Test]
    public void TestInfrastructure_Works()
    {
        true.Should().BeTrue();
    }
}
```

- [x] **Step 5: Register both projects in `src/MudBlazor.slnx`**

Add a `/playblazor/` folder before the `/Solution Items/` folder, and the test project inside the existing `/tests/` folder:

```xml
  <Folder Name="/playblazor/">
    <Project Path="PlayBlazor/PlayBlazor.csproj" />
  </Folder>
```

and inside `<Folder Name="/tests/">`:

```xml
    <Project Path="PlayBlazor.UnitTests/PlayBlazor.UnitTests.csproj" />
```

- [x] **Step 6: Build and run the smoke test**

Run: `dotnet run --project src/PlayBlazor.UnitTests`
Expected: build succeeds, 1 test passes.

- [x] **Step 7: Commit**

```bash
git add src/PlayBlazor src/PlayBlazor.UnitTests src/MudBlazor.slnx
git commit -m "PlayBlazor: scaffold core RCL and test projects"
```

---

### Task 2: Modèle de descripteurs + ControlKindResolver

**Files:**
- Create: `src/PlayBlazor/Model/ControlKind.cs`
- Create: `src/PlayBlazor/Model/ParameterDescriptor.cs`
- Create: `src/PlayBlazor/Model/ComponentDescriptor.cs`
- Create: `src/PlayBlazor/Discovery/ControlKindResolver.cs`
- Test: `src/PlayBlazor.UnitTests/Discovery/ControlKindResolverTests.cs`

**Interfaces:**
- Produces:
  - `enum ControlKind { Bool, Enum, Text, Number, Color, Icon, Slot, Event, Unsupported }` (namespace `PlayBlazor.Model`; `Color`/`Icon` are reserved for the milestone-4 rich mappers and never returned by the v1 resolver)
  - `sealed record ParameterDescriptor(string Name, Type Type, ControlKind Kind, bool IsNullable, object? DefaultValue, bool HasDefault, string? Summary)`
  - `sealed record ComponentDescriptor(Type Type, string DisplayName, string Category, string? Summary, IReadOnlyList<ParameterDescriptor> Parameters, string? Warning)`
  - `static class ControlKindResolver { public static (ControlKind Kind, bool IsNullable) Resolve(Type parameterType); }` (namespace `PlayBlazor.Discovery`)

- [x] **Step 1: Write the failing tests**

`src/PlayBlazor.UnitTests/Discovery/ControlKindResolverTests.cs`:

```csharp
using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using NUnit.Framework;
using PlayBlazor.Discovery;
using PlayBlazor.Model;

namespace PlayBlazor.UnitTests.Discovery;

public class ControlKindResolverTests
{
    [TestCase(typeof(bool), ControlKind.Bool, false)]
    [TestCase(typeof(bool?), ControlKind.Bool, true)]
    [TestCase(typeof(DayOfWeek), ControlKind.Enum, false)]
    [TestCase(typeof(DayOfWeek?), ControlKind.Enum, true)]
    [TestCase(typeof(string), ControlKind.Text, false)]
    [TestCase(typeof(int), ControlKind.Number, false)]
    [TestCase(typeof(int?), ControlKind.Number, true)]
    [TestCase(typeof(long), ControlKind.Number, false)]
    [TestCase(typeof(short), ControlKind.Number, false)]
    [TestCase(typeof(byte), ControlKind.Number, false)]
    [TestCase(typeof(double), ControlKind.Number, false)]
    [TestCase(typeof(float), ControlKind.Number, false)]
    [TestCase(typeof(decimal), ControlKind.Number, false)]
    [TestCase(typeof(RenderFragment), ControlKind.Slot, false)]
    [TestCase(typeof(RenderFragment<string>), ControlKind.Slot, false)]
    [TestCase(typeof(EventCallback), ControlKind.Event, false)]
    [TestCase(typeof(EventCallback<string>), ControlKind.Event, false)]
    [TestCase(typeof(Uri), ControlKind.Unsupported, false)]
    [TestCase(typeof(Dictionary<string, object>), ControlKind.Unsupported, false)]
    public void Resolve_MapsTypeToKind(Type parameterType, ControlKind expectedKind, bool expectedNullable)
    {
        var (kind, isNullable) = ControlKindResolver.Resolve(parameterType);

        kind.Should().Be(expectedKind);
        isNullable.Should().Be(expectedNullable);
    }
}
```

- [x] **Step 2: Run tests to verify they fail**

Run: `dotnet run --project src/PlayBlazor.UnitTests`
Expected: FAIL to build — `ControlKind`, `ControlKindResolver` do not exist.

- [x] **Step 3: Implement the model types**

`src/PlayBlazor/Model/ControlKind.cs`:

```csharp
namespace PlayBlazor.Model;

/// <summary>The kind of UI control used to drive one component parameter.</summary>
public enum ControlKind
{
    Bool,
    Enum,
    Text,
    Number,
    /// <summary>Reserved for host-registered rich mappers (milestone 4).</summary>
    Color,
    /// <summary>Reserved for host-registered rich mappers (milestone 4).</summary>
    Icon,
    Slot,
    Event,
    Unsupported,
}
```

`src/PlayBlazor/Model/ParameterDescriptor.cs`:

```csharp
namespace PlayBlazor.Model;

/// <summary>Describes one <c>[Parameter]</c> of a discovered component.</summary>
public sealed record ParameterDescriptor(
    string Name,
    Type Type,
    ControlKind Kind,
    bool IsNullable,
    object? DefaultValue,
    bool HasDefault,
    string? Summary);
```

`src/PlayBlazor/Model/ComponentDescriptor.cs`:

```csharp
namespace PlayBlazor.Model;

/// <summary>Describes one discovered component and its drivable parameters.</summary>
public sealed record ComponentDescriptor(
    Type Type,
    string DisplayName,
    string Category,
    string? Summary,
    IReadOnlyList<ParameterDescriptor> Parameters,
    string? Warning);
```

`src/PlayBlazor/Discovery/ControlKindResolver.cs`:

```csharp
using Microsoft.AspNetCore.Components;
using PlayBlazor.Model;

namespace PlayBlazor.Discovery;

public static class ControlKindResolver
{
    public static (ControlKind Kind, bool IsNullable) Resolve(Type parameterType)
    {
        var underlying = Nullable.GetUnderlyingType(parameterType);
        var isNullable = underlying is not null;
        var type = underlying ?? parameterType;

        if (type == typeof(bool))
        {
            return (ControlKind.Bool, isNullable);
        }
        if (type.IsEnum)
        {
            return (ControlKind.Enum, isNullable);
        }
        if (type == typeof(string))
        {
            return (ControlKind.Text, isNullable);
        }
        if (IsNumeric(type))
        {
            return (ControlKind.Number, isNullable);
        }
        if (IsRenderFragment(type))
        {
            return (ControlKind.Slot, false);
        }
        if (IsEventCallback(type))
        {
            return (ControlKind.Event, false);
        }

        return (ControlKind.Unsupported, isNullable);
    }

    private static bool IsNumeric(Type type)
        => type == typeof(int) || type == typeof(long) || type == typeof(short)
           || type == typeof(byte) || type == typeof(double) || type == typeof(float)
           || type == typeof(decimal);

    private static bool IsRenderFragment(Type type)
        => type == typeof(RenderFragment)
           || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(RenderFragment<>));

    private static bool IsEventCallback(Type type)
        => type == typeof(EventCallback)
           || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EventCallback<>));
}
```

- [x] **Step 4: Run tests to verify they pass**

Run: `dotnet run --project src/PlayBlazor.UnitTests`
Expected: PASS (all 20 cases + smoke test).

- [x] **Step 5: Commit**

```bash
git add src/PlayBlazor/Model src/PlayBlazor/Discovery src/PlayBlazor.UnitTests/Discovery
git commit -m "PlayBlazor: add descriptor model and control kind resolver"
```

---

### Task 3: Fixtures + ReflectionCatalogProvider.Describe

**Files:**
- Create: `src/PlayBlazor.UnitTests/_Imports.razor`
- Create: `src/PlayBlazor.UnitTests/Fixtures/FixtureSize.cs`
- Create: `src/PlayBlazor.UnitTests/Fixtures/BasicFixture.razor`
- Create: `src/PlayBlazor.UnitTests/Fixtures/ThrowingCtorFixture.razor`
- Create: `src/PlayBlazor/Discovery/IComponentCatalogProvider.cs`
- Create: `src/PlayBlazor/Discovery/ReflectionCatalogProvider.cs`
- Test: `src/PlayBlazor.UnitTests/Discovery/DescribeTests.cs`

**Interfaces:**
- Consumes: `ComponentDescriptor`, `ParameterDescriptor`, `ControlKind`, `ControlKindResolver` (Task 2).
- Produces:
  - `interface IComponentCatalogProvider { ComponentDescriptor Describe(Type componentType); IReadOnlyList<ComponentDescriptor> Discover(System.Reflection.Assembly assembly); }` (namespace `PlayBlazor.Discovery`)
  - `sealed class ReflectionCatalogProvider : IComponentCatalogProvider` with constructor `ReflectionCatalogProvider(XmlDocSummaryReader? xmlDocs = null)` — the `xmlDocs` parameter is added in Task 5; in this task the constructor is parameterless.
  - Fixture components in namespace `PlayBlazor.UnitTests.Fixtures`: `BasicFixture` (parameters listed below), `ThrowingCtorFixture`, `FixtureSize` enum.

- [x] **Step 1: Create the fixture components**

`src/PlayBlazor.UnitTests/_Imports.razor`:

```razor
@using Microsoft.AspNetCore.Components
@using Microsoft.AspNetCore.Components.Web
```

`src/PlayBlazor.UnitTests/Fixtures/FixtureSize.cs`:

```csharp
namespace PlayBlazor.UnitTests.Fixtures;

public enum FixtureSize
{
    Small,
    Medium,
    Large,
}
```

`src/PlayBlazor.UnitTests/Fixtures/BasicFixture.razor`:

```razor
@namespace PlayBlazor.UnitTests.Fixtures
<div class="basic-fixture">Dense=@Dense;Outlined=@Outlined;Size=@Size;Label=@Label;Count=@Count;Ratio=@Ratio</div>
@code {
    [Parameter] public bool Dense { get; set; }
    [Parameter] public bool Outlined { get; set; } = true;
    [Parameter] public FixtureSize Size { get; set; } = FixtureSize.Medium;
    [Parameter] public string? Label { get; set; }
    [Parameter] public int Count { get; set; } = 3;
    [Parameter] public double Ratio { get; set; } = 0.5;
    [Parameter] public int? MaxItems { get; set; }
    [Parameter] public EventCallback<string> OnValueChanged { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public Uri? Endpoint { get; set; }
    [CascadingParameter] public string? Cascaded { get; set; }
    public string NotAParameter { get; set; } = string.Empty;
}
```

`src/PlayBlazor.UnitTests/Fixtures/ThrowingCtorFixture.razor`:

```razor
@namespace PlayBlazor.UnitTests.Fixtures
<div>never constructed</div>
@code {
    [Parameter] public bool Dense { get; set; }

    public ThrowingCtorFixture()
    {
        throw new InvalidOperationException("ctor boom");
    }
}
```

- [x] **Step 2: Write the failing tests**

`src/PlayBlazor.UnitTests/Discovery/DescribeTests.cs`:

```csharp
using AwesomeAssertions;
using NUnit.Framework;
using PlayBlazor.Discovery;
using PlayBlazor.Model;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Discovery;

public class DescribeTests
{
    private ReflectionCatalogProvider _provider = null!;

    [SetUp]
    public void Setup()
    {
        _provider = new ReflectionCatalogProvider();
    }

    [Test]
    public void Describe_ListsAllParameterProperties()
    {
        var descriptor = _provider.Describe(typeof(BasicFixture));

        descriptor.DisplayName.Should().Be("BasicFixture");
        descriptor.Category.Should().Be("PlayBlazor.UnitTests.Fixtures");
        descriptor.Warning.Should().BeNull();
        descriptor.Parameters.Select(p => p.Name).Should().BeEquivalentTo(
            "Dense", "Outlined", "Size", "Label", "Count", "Ratio",
            "MaxItems", "OnValueChanged", "ChildContent", "Endpoint");
    }

    [Test]
    public void Describe_ResolvesKindsAndNullability()
    {
        var descriptor = _provider.Describe(typeof(BasicFixture));
        var byName = descriptor.Parameters.ToDictionary(p => p.Name);

        byName["Dense"].Kind.Should().Be(ControlKind.Bool);
        byName["Size"].Kind.Should().Be(ControlKind.Enum);
        byName["Label"].Kind.Should().Be(ControlKind.Text);
        byName["Count"].Kind.Should().Be(ControlKind.Number);
        byName["MaxItems"].Kind.Should().Be(ControlKind.Number);
        byName["MaxItems"].IsNullable.Should().BeTrue();
        byName["OnValueChanged"].Kind.Should().Be(ControlKind.Event);
        byName["ChildContent"].Kind.Should().Be(ControlKind.Slot);
        byName["Endpoint"].Kind.Should().Be(ControlKind.Unsupported);
    }

    [Test]
    public void Describe_CapturesDefaultValues()
    {
        var descriptor = _provider.Describe(typeof(BasicFixture));
        var byName = descriptor.Parameters.ToDictionary(p => p.Name);

        byName["Dense"].HasDefault.Should().BeTrue();
        byName["Dense"].DefaultValue.Should().Be(false);
        byName["Outlined"].DefaultValue.Should().Be(true);
        byName["Size"].DefaultValue.Should().Be(FixtureSize.Medium);
        byName["Count"].DefaultValue.Should().Be(3);
        byName["Ratio"].DefaultValue.Should().Be(0.5);
        byName["Label"].DefaultValue.Should().BeNull();
        byName["Label"].HasDefault.Should().BeTrue();
    }

    [Test]
    public void Describe_ThrowingConstructor_ProducesWarningWithoutDefaults()
    {
        var descriptor = _provider.Describe(typeof(ThrowingCtorFixture));

        descriptor.Warning.Should().NotBeNull();
        descriptor.Parameters.Should().NotBeEmpty();
        descriptor.Parameters.Should().OnlyContain(p => !p.HasDefault);
    }

    [Test]
    public void Describe_StripsGenericArityFromDisplayName()
    {
        var descriptor = _provider.Describe(typeof(TestGeneric<string>));

        descriptor.DisplayName.Should().Be("TestGeneric");
    }

    private class TestGeneric<T> : Microsoft.AspNetCore.Components.ComponentBase
    {
        [Microsoft.AspNetCore.Components.Parameter]
        public T? Value { get; set; }
    }
}
```

- [x] **Step 3: Run tests to verify they fail**

Run: `dotnet run --project src/PlayBlazor.UnitTests`
Expected: FAIL to build — `IComponentCatalogProvider`, `ReflectionCatalogProvider` do not exist.

- [x] **Step 4: Implement the provider**

`src/PlayBlazor/Discovery/IComponentCatalogProvider.cs`:

```csharp
using System.Reflection;
using PlayBlazor.Model;

namespace PlayBlazor.Discovery;

/// <summary>Supplies component descriptors. Reflection-based in v1; a source generator can replace it later.</summary>
public interface IComponentCatalogProvider
{
    ComponentDescriptor Describe(Type componentType);

    IReadOnlyList<ComponentDescriptor> Discover(Assembly assembly);
}
```

`src/PlayBlazor/Discovery/ReflectionCatalogProvider.cs` (the `Discover` method body comes in Task 4 — in this task it throws `NotImplementedException`):

```csharp
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using PlayBlazor.Model;

namespace PlayBlazor.Discovery;

public sealed class ReflectionCatalogProvider : IComponentCatalogProvider
{
    private readonly ConcurrentDictionary<Type, ComponentDescriptor> _cache = new();

    public ComponentDescriptor Describe(Type componentType)
        => _cache.GetOrAdd(componentType, Build);

    public IReadOnlyList<ComponentDescriptor> Discover(Assembly assembly)
        => throw new NotImplementedException(); // Task 4

    private ComponentDescriptor Build(Type type)
    {
        string? warning = null;
        object? instance = null;
        try
        {
            instance = Activator.CreateInstance(type);
        }
        catch (Exception)
        {
            warning = "Defaults not captured: the component could not be instantiated.";
        }

        var parameters = new List<ParameterDescriptor>();
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetCustomAttribute<ParameterAttribute>() is null)
            {
                continue;
            }

            var (kind, isNullable) = ControlKindResolver.Resolve(property.PropertyType);

            object? defaultValue = null;
            var hasDefault = false;
            if (instance is not null && property.CanRead)
            {
                try
                {
                    defaultValue = property.GetValue(instance);
                    hasDefault = true;
                }
                catch (Exception)
                {
                    // A throwing getter leaves this parameter without a known default.
                }
            }

            parameters.Add(new ParameterDescriptor(
                Name: property.Name,
                Type: property.PropertyType,
                Kind: kind,
                IsNullable: isNullable,
                DefaultValue: defaultValue,
                HasDefault: hasDefault,
                Summary: null));
        }

        return new ComponentDescriptor(
            Type: type,
            DisplayName: StripArity(type.Name),
            Category: type.Namespace ?? string.Empty,
            Summary: null,
            Parameters: parameters,
            Warning: warning);
    }

    private static string StripArity(string typeName)
    {
        var backtick = typeName.IndexOf('`');
        return backtick < 0 ? typeName : typeName[..backtick];
    }
}
```

- [x] **Step 5: Run tests to verify they pass**

Run: `dotnet run --project src/PlayBlazor.UnitTests`
Expected: PASS.

- [x] **Step 6: Commit**

```bash
git add src/PlayBlazor/Discovery src/PlayBlazor.UnitTests
git commit -m "PlayBlazor: add reflection-based component describer with default capture"
```

---

### Task 4: Discover(assembly) — scan, génériques, exclusions

**Files:**
- Create: `src/PlayBlazor.UnitTests/Fixtures/GenericFixture.razor`
- Create: `src/PlayBlazor.UnitTests/Fixtures/AbstractFixture.cs`
- Modify: `src/PlayBlazor/Discovery/ReflectionCatalogProvider.cs` (replace the `Discover` stub)
- Test: `src/PlayBlazor.UnitTests/Discovery/DiscoverTests.cs`

**Interfaces:**
- Consumes: `ReflectionCatalogProvider.Describe` (Task 3).
- Produces: working `IReadOnlyList<ComponentDescriptor> Discover(Assembly assembly)` — public, non-abstract, top-level `ComponentBase` types; open generics closed with `string` then `int` (skipped if neither satisfies constraints); ordered by `DisplayName` (ordinal).

- [x] **Step 1: Create the additional fixtures**

`src/PlayBlazor.UnitTests/Fixtures/GenericFixture.razor`:

```razor
@namespace PlayBlazor.UnitTests.Fixtures
@typeparam TItem
<div>@Value</div>
@code {
    [Parameter] public TItem? Value { get; set; }
}
```

`src/PlayBlazor.UnitTests/Fixtures/AbstractFixture.cs`:

```csharp
using Microsoft.AspNetCore.Components;

namespace PlayBlazor.UnitTests.Fixtures;

public abstract class AbstractFixture : ComponentBase
{
    [Parameter]
    public bool Visible { get; set; }
}
```

- [x] **Step 2: Write the failing tests**

`src/PlayBlazor.UnitTests/Discovery/DiscoverTests.cs`:

```csharp
using AwesomeAssertions;
using NUnit.Framework;
using PlayBlazor.Discovery;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Discovery;

public class DiscoverTests
{
    private ReflectionCatalogProvider _provider = null!;

    [SetUp]
    public void Setup()
    {
        _provider = new ReflectionCatalogProvider();
    }

    [Test]
    public void Discover_FindsFixtureComponents()
    {
        var components = _provider.Discover(typeof(BasicFixture).Assembly);

        components.Select(c => c.DisplayName).Should().Contain("BasicFixture");
        components.Select(c => c.DisplayName).Should().Contain("ThrowingCtorFixture");
    }

    [Test]
    public void Discover_ClosesGenericsWithString()
    {
        var components = _provider.Discover(typeof(BasicFixture).Assembly);
        var generic = components.Single(c => c.DisplayName == "GenericFixture");

        generic.Type.Should().Be(typeof(GenericFixture<string>));
    }

    [Test]
    public void Discover_ExcludesAbstractComponents()
    {
        var components = _provider.Discover(typeof(BasicFixture).Assembly);

        components.Select(c => c.DisplayName).Should().NotContain("AbstractFixture");
    }

    [Test]
    public void Discover_IsSortedByDisplayName()
    {
        var components = _provider.Discover(typeof(BasicFixture).Assembly);

        components.Select(c => c.DisplayName).Should().BeInAscendingOrder(StringComparer.Ordinal);
    }
}
```

- [x] **Step 3: Run tests to verify they fail**

Run: `dotnet run --project src/PlayBlazor.UnitTests`
Expected: `Discover` tests FAIL with `NotImplementedException`.

- [x] **Step 4: Implement `Discover`**

In `src/PlayBlazor/Discovery/ReflectionCatalogProvider.cs`, replace the stub with:

```csharp
    public IReadOnlyList<ComponentDescriptor> Discover(Assembly assembly)
        => assembly.GetTypes()
            .Where(static t => t.IsPublic && !t.IsAbstract && typeof(ComponentBase).IsAssignableFrom(t))
            .Select(static t => TryCloseGeneric(t))
            .OfType<Type>()
            .Select(Describe)
            .OrderBy(static d => d.DisplayName, StringComparer.Ordinal)
            .ToArray();

    private static Type? TryCloseGeneric(Type type)
    {
        if (!type.IsGenericTypeDefinition)
        {
            return type;
        }

        foreach (var candidate in new[] { typeof(string), typeof(int) })
        {
            try
            {
                var arguments = new Type[type.GetGenericArguments().Length];
                Array.Fill(arguments, candidate);
                return type.MakeGenericType(arguments);
            }
            catch (ArgumentException)
            {
                // Constraint not satisfied by this candidate — try the next one.
            }
        }

        return null;
    }
```

- [x] **Step 5: Run tests to verify they pass**

Run: `dotnet run --project src/PlayBlazor.UnitTests`
Expected: PASS.

- [x] **Step 6: Commit**

```bash
git add src/PlayBlazor/Discovery src/PlayBlazor.UnitTests
git commit -m "PlayBlazor: add assembly discovery with generic closing and exclusions"
```

---

### Task 5: XmlDocSummaryReader + intégration provider

**Files:**
- Create: `src/PlayBlazor/Discovery/XmlDocSummaryReader.cs`
- Modify: `src/PlayBlazor/Discovery/ReflectionCatalogProvider.cs` (constructor + summary lookup)
- Test: `src/PlayBlazor.UnitTests/Discovery/XmlDocSummaryReaderTests.cs`

**Interfaces:**
- Consumes: `ReflectionCatalogProvider.Build` (Task 3).
- Produces:
  - `sealed class XmlDocSummaryReader` (namespace `PlayBlazor.Discovery`) with `static XmlDocSummaryReader FromStream(Stream stream)`, `string? GetTypeSummary(Type type)`, `string? GetPropertySummary(System.Reflection.PropertyInfo property)`.
  - `ReflectionCatalogProvider` constructor becomes `ReflectionCatalogProvider(XmlDocSummaryReader? xmlDocs = null)`; descriptors carry summaries when a reader is provided.

- [x] **Step 1: Write the failing tests**

`src/PlayBlazor.UnitTests/Discovery/XmlDocSummaryReaderTests.cs`:

```csharp
using System.Text;
using AwesomeAssertions;
using NUnit.Framework;
using PlayBlazor.Discovery;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Discovery;

public class XmlDocSummaryReaderTests
{
    private const string Xml = """
        <?xml version="1.0"?>
        <doc>
          <assembly><name>Fixture</name></assembly>
          <members>
            <member name="T:PlayBlazor.UnitTests.Fixtures.BasicFixture">
              <summary>A basic fixture.</summary>
            </member>
            <member name="P:PlayBlazor.UnitTests.Fixtures.BasicFixture.Dense">
              <summary>
                Renders with <see cref="T:PlayBlazor.UnitTests.Fixtures.FixtureSize"/> compact spacing.
              </summary>
            </member>
            <member name="P:PlayBlazor.UnitTests.Fixtures.GenericFixture`1.Value">
              <summary>The bound value.</summary>
            </member>
          </members>
        </doc>
        """;

    private static XmlDocSummaryReader CreateReader()
        => XmlDocSummaryReader.FromStream(new MemoryStream(Encoding.UTF8.GetBytes(Xml)));

    [Test]
    public void GetTypeSummary_ReturnsSummary()
    {
        CreateReader().GetTypeSummary(typeof(BasicFixture)).Should().Be("A basic fixture.");
    }

    [Test]
    public void GetPropertySummary_NormalizesWhitespaceAndSeeRefs()
    {
        var property = typeof(BasicFixture).GetProperty(nameof(BasicFixture.Dense))!;

        CreateReader().GetPropertySummary(property)
            .Should().Be("Renders with FixtureSize compact spacing.");
    }

    [Test]
    public void GetPropertySummary_ResolvesClosedGenericsToOpenDefinition()
    {
        var property = typeof(GenericFixture<string>).GetProperty("Value")!;

        CreateReader().GetPropertySummary(property).Should().Be("The bound value.");
    }

    [Test]
    public void GetTypeSummary_UnknownType_ReturnsNull()
    {
        CreateReader().GetTypeSummary(typeof(ThrowingCtorFixture)).Should().BeNull();
    }

    [Test]
    public void Describe_WithReader_PopulatesSummaries()
    {
        var provider = new ReflectionCatalogProvider(CreateReader());

        var descriptor = provider.Describe(typeof(BasicFixture));

        descriptor.Summary.Should().Be("A basic fixture.");
        descriptor.Parameters.Single(p => p.Name == "Dense").Summary
            .Should().Be("Renders with FixtureSize compact spacing.");
        descriptor.Parameters.Single(p => p.Name == "Label").Summary.Should().BeNull();
    }
}
```

- [x] **Step 2: Run tests to verify they fail**

Run: `dotnet run --project src/PlayBlazor.UnitTests`
Expected: FAIL to build — `XmlDocSummaryReader` does not exist.

- [x] **Step 3: Implement the reader**

`src/PlayBlazor/Discovery/XmlDocSummaryReader.cs`:

```csharp
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace PlayBlazor.Discovery;

/// <summary>Reads member summaries from a compiler-generated XML documentation file.</summary>
public sealed partial class XmlDocSummaryReader
{
    private readonly Dictionary<string, string> _summaries;

    private XmlDocSummaryReader(Dictionary<string, string> summaries)
        => _summaries = summaries;

    public static XmlDocSummaryReader FromStream(Stream stream)
    {
        var summaries = new Dictionary<string, string>(StringComparer.Ordinal);
        var document = XDocument.Load(stream);
        foreach (var member in document.Descendants("member"))
        {
            var name = member.Attribute("name")?.Value;
            var summary = member.Element("summary");
            if (name is null || summary is null)
            {
                continue;
            }

            var text = FlattenSummary(summary);
            if (text.Length > 0)
            {
                summaries[name] = text;
            }
        }

        return new XmlDocSummaryReader(summaries);
    }

    public string? GetTypeSummary(Type type)
        => _summaries.GetValueOrDefault($"T:{XmlId(type)}");

    public string? GetPropertySummary(PropertyInfo property)
        => property.DeclaringType is { } declaringType
            ? _summaries.GetValueOrDefault($"P:{XmlId(declaringType)}.{property.Name}")
            : null;

    private static string XmlId(Type type)
    {
        var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        return (definition.FullName ?? definition.Name).Replace('+', '.');
    }

    private static string FlattenSummary(XElement summary)
    {
        var builder = new StringBuilder();
        foreach (var node in summary.Nodes())
        {
            switch (node)
            {
                case XText text:
                    builder.Append(text.Value);
                    break;
                case XElement { Name.LocalName: "see" } see:
                    builder.Append(SeeText(see));
                    break;
                case XElement element:
                    builder.Append(element.Value);
                    break;
            }
        }

        return WhitespaceRun().Replace(builder.ToString(), " ").Trim();
    }

    private static string SeeText(XElement see)
    {
        if (!string.IsNullOrEmpty(see.Value))
        {
            return see.Value;
        }

        var cref = see.Attribute("cref")?.Value ?? see.Attribute("href")?.Value ?? string.Empty;
        var lastDot = cref.LastIndexOf('.');
        return lastDot < 0 ? cref : cref[(lastDot + 1)..];
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRun();
}
```

- [x] **Step 4: Integrate into `ReflectionCatalogProvider`**

In `src/PlayBlazor/Discovery/ReflectionCatalogProvider.cs`:

1. Add the field and constructor:

```csharp
    private readonly XmlDocSummaryReader? _xmlDocs;

    public ReflectionCatalogProvider(XmlDocSummaryReader? xmlDocs = null)
        => _xmlDocs = xmlDocs;
```

2. In `Build`, replace `Summary: null` for the parameter with `Summary: _xmlDocs?.GetPropertySummary(property)`, and `Summary: null` for the component with `Summary: _xmlDocs?.GetTypeSummary(type)`.

- [x] **Step 5: Run tests to verify they pass**

Run: `dotnet run --project src/PlayBlazor.UnitTests`
Expected: PASS.

- [x] **Step 6: Commit**

```bash
git add src/PlayBlazor/Discovery src/PlayBlazor.UnitTests/Discovery
git commit -m "PlayBlazor: read XML doc summaries into descriptors"
```

---

### Task 6: Test de généralité MudBlazor + enregistrement DI

**Files:**
- Create: `src/PlayBlazor/PlayBlazorServiceCollectionExtensions.cs`
- Test: `src/PlayBlazor.UnitTests/Discovery/MudBlazorGeneralityTests.cs`
- Test: `src/PlayBlazor.UnitTests/ServiceRegistrationTests.cs`

**Interfaces:**
- Consumes: `ReflectionCatalogProvider` (Tasks 3–5).
- Produces: `public static IServiceCollection AddPlayBlazor(this IServiceCollection services)` (namespace `PlayBlazor`) registering `IComponentCatalogProvider` as a singleton `ReflectionCatalogProvider`.

- [x] **Step 1: Write the failing tests**

`src/PlayBlazor.UnitTests/Discovery/MudBlazorGeneralityTests.cs`:

```csharp
using AwesomeAssertions;
using MudBlazor;
using NUnit.Framework;
using PlayBlazor.Discovery;

namespace PlayBlazor.UnitTests.Discovery;

/// <summary>
/// Anti-regression guard for the "generalized" positioning: scanning a real,
/// large component library must never throw and must find a substantial catalog.
/// </summary>
public class MudBlazorGeneralityTests
{
    [Test]
    public void Discover_MudBlazorAssembly_SucceedsWithSubstantialCatalog()
    {
        var provider = new ReflectionCatalogProvider();

        var components = provider.Discover(typeof(MudButton).Assembly);

        components.Should().HaveCountGreaterThan(50);
        components.Should().OnlyContain(c => c.Parameters != null);
        components.Select(c => c.DisplayName).Should().Contain("MudButton");
        components.Select(c => c.DisplayName).Should().Contain("MudSelect");
    }
}
```

`src/PlayBlazor.UnitTests/ServiceRegistrationTests.cs`:

```csharp
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PlayBlazor.Discovery;

namespace PlayBlazor.UnitTests;

public class ServiceRegistrationTests
{
    [Test]
    public void AddPlayBlazor_RegistersSingletonCatalogProvider()
    {
        var services = new ServiceCollection().AddPlayBlazor().BuildServiceProvider();

        var first = services.GetRequiredService<IComponentCatalogProvider>();
        var second = services.GetRequiredService<IComponentCatalogProvider>();

        first.Should().BeOfType<ReflectionCatalogProvider>();
        first.Should().BeSameAs(second);
    }
}
```

- [x] **Step 2: Run tests to verify they fail**

Run: `dotnet run --project src/PlayBlazor.UnitTests`
Expected: FAIL to build — `AddPlayBlazor` does not exist. (The generality test may already pass — that is fine.)

- [x] **Step 3: Implement the DI extension**

`src/PlayBlazor/PlayBlazorServiceCollectionExtensions.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlayBlazor.Discovery;

namespace PlayBlazor;

public static class PlayBlazorServiceCollectionExtensions
{
    public static IServiceCollection AddPlayBlazor(this IServiceCollection services)
    {
        services.TryAddSingleton<IComponentCatalogProvider>(static _ => new ReflectionCatalogProvider());
        return services;
    }
}
```

- [x] **Step 4: Run tests to verify they pass**

Run: `dotnet run --project src/PlayBlazor.UnitTests`
Expected: PASS — including the MudBlazor scan (zero exceptions, > 50 components).

- [x] **Step 5: Commit — milestone 1 complete**

```bash
git add src/PlayBlazor src/PlayBlazor.UnitTests
git commit -m "PlayBlazor: add DI registration and MudBlazor generality guard (milestone 1)"
```

---

## Jalon 2 — Premier rendu

### Task 7: PlaygroundState

**Files:**
- Create: `src/PlayBlazor/State/PlaygroundState.cs`
- Test: `src/PlayBlazor.UnitTests/State/PlaygroundStateTests.cs`

**Interfaces:**
- Consumes: `ParameterDescriptor` (Task 2).
- Produces (namespace `PlayBlazor.State`):

```csharp
public sealed class PlaygroundState
{
    public event Action? Changed;
    public int InstanceKey { get; }                                 // bumped only when values are REMOVED (reset)
    public IReadOnlyDictionary<string, object?> ModifiedValues { get; }
    public bool IsModified(string parameterName);
    public object? GetValue(ParameterDescriptor parameter);         // modified value, else parameter.DefaultValue
    public void Set(string parameterName, object? value);
    public void Reset(string parameterName);
    public void ResetAll();
}
```

- [x] **Step 1: Write the failing tests**

`src/PlayBlazor.UnitTests/State/PlaygroundStateTests.cs`:

```csharp
using AwesomeAssertions;
using NUnit.Framework;
using PlayBlazor.Model;
using PlayBlazor.State;

namespace PlayBlazor.UnitTests.State;

public class PlaygroundStateTests
{
    private static readonly ParameterDescriptor Dense = new(
        "Dense", typeof(bool), ControlKind.Bool, IsNullable: false,
        DefaultValue: false, HasDefault: true, Summary: null);

    [Test]
    public void GetValue_Unmodified_ReturnsDefault()
    {
        new PlaygroundState().GetValue(Dense).Should().Be(false);
    }

    [Test]
    public void Set_ThenGetValue_ReturnsModifiedValue()
    {
        var state = new PlaygroundState();

        state.Set("Dense", true);

        state.GetValue(Dense).Should().Be(true);
        state.IsModified("Dense").Should().BeTrue();
        state.ModifiedValues.Should().ContainKey("Dense");
    }

    [Test]
    public void Set_RaisesChanged_ButKeepsInstanceKey()
    {
        var state = new PlaygroundState();
        var raised = 0;
        state.Changed += () => raised++;
        var keyBefore = state.InstanceKey;

        state.Set("Dense", true);

        raised.Should().Be(1);
        state.InstanceKey.Should().Be(keyBefore);
    }

    [Test]
    public void Reset_RemovesValue_AndBumpsInstanceKey()
    {
        var state = new PlaygroundState();
        state.Set("Dense", true);
        var keyBefore = state.InstanceKey;

        state.Reset("Dense");

        state.IsModified("Dense").Should().BeFalse();
        state.GetValue(Dense).Should().Be(false);
        state.InstanceKey.Should().BeGreaterThan(keyBefore);
    }

    [Test]
    public void Reset_UnknownName_DoesNothing()
    {
        var state = new PlaygroundState();
        var raised = 0;
        state.Changed += () => raised++;
        var keyBefore = state.InstanceKey;

        state.Reset("Nope");

        raised.Should().Be(0);
        state.InstanceKey.Should().Be(keyBefore);
    }

    [Test]
    public void ResetAll_ClearsEverything_AndBumpsInstanceKey()
    {
        var state = new PlaygroundState();
        state.Set("Dense", true);
        state.Set("Label", "x");
        var keyBefore = state.InstanceKey;

        state.ResetAll();

        state.ModifiedValues.Should().BeEmpty();
        state.InstanceKey.Should().BeGreaterThan(keyBefore);
    }
}
```

- [x] **Step 2: Run tests to verify they fail**

Run: `dotnet run --project src/PlayBlazor.UnitTests`
Expected: FAIL to build — `PlaygroundState` does not exist.

- [x] **Step 3: Implement**

`src/PlayBlazor/State/PlaygroundState.cs`:

```csharp
using PlayBlazor.Model;

namespace PlayBlazor.State;

/// <summary>
/// Holds only the parameters the user has modified; everything else falls back to the
/// component's own defaults. <see cref="InstanceKey"/> changes only when values are
/// removed, so the preview can force a fresh component instance on reset (Blazor never
/// un-sets a previously supplied parameter on a live instance).
/// </summary>
public sealed class PlaygroundState
{
    private readonly Dictionary<string, object?> _modified = new(StringComparer.Ordinal);

    public event Action? Changed;

    public int InstanceKey { get; private set; }

    public IReadOnlyDictionary<string, object?> ModifiedValues => _modified;

    public bool IsModified(string parameterName)
        => _modified.ContainsKey(parameterName);

    public object? GetValue(ParameterDescriptor parameter)
        => _modified.TryGetValue(parameter.Name, out var value) ? value : parameter.DefaultValue;

    public void Set(string parameterName, object? value)
    {
        _modified[parameterName] = value;
        Changed?.Invoke();
    }

    public void Reset(string parameterName)
    {
        if (_modified.Remove(parameterName))
        {
            InstanceKey++;
            Changed?.Invoke();
        }
    }

    public void ResetAll()
    {
        if (_modified.Count == 0)
        {
            return;
        }

        _modified.Clear();
        InstanceKey++;
        Changed?.Invoke();
    }
}
```

- [x] **Step 4: Run tests to verify they pass**

Run: `dotnet run --project src/PlayBlazor.UnitTests`
Expected: PASS.

- [x] **Step 5: Commit**

```bash
git add src/PlayBlazor/State src/PlayBlazor.UnitTests/State
git commit -m "PlayBlazor: add playground state with reset-aware instance key"
```

---

### Task 8: ParameterDictionaryBuilder

**Files:**
- Create: `src/PlayBlazor/Rendering/ParameterDictionaryBuilder.cs`
- Test: `src/PlayBlazor.UnitTests/Rendering/ParameterDictionaryBuilderTests.cs`

**Interfaces:**
- Consumes: `ComponentDescriptor`, `PlaygroundState` (Tasks 2, 7).
- Produces: `static class ParameterDictionaryBuilder { public static Dictionary<string, object> Build(ComponentDescriptor component, PlaygroundState state); }` (namespace `PlayBlazor.Rendering`) — only **modified**, non-null values of drivable kinds (`Bool`, `Enum`, `Text`, `Number`); `Slot`/`Event`/`Unsupported` and unmodified parameters are omitted.

- [x] **Step 1: Write the failing tests**

`src/PlayBlazor.UnitTests/Rendering/ParameterDictionaryBuilderTests.cs`:

```csharp
using AwesomeAssertions;
using NUnit.Framework;
using PlayBlazor.Discovery;
using PlayBlazor.Rendering;
using PlayBlazor.State;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Rendering;

public class ParameterDictionaryBuilderTests
{
    [Test]
    public void Build_EmptyState_ReturnsEmptyDictionary()
    {
        var descriptor = new ReflectionCatalogProvider().Describe(typeof(BasicFixture));

        ParameterDictionaryBuilder.Build(descriptor, new PlaygroundState()).Should().BeEmpty();
    }

    [Test]
    public void Build_IncludesOnlyModifiedDrivableParameters()
    {
        var descriptor = new ReflectionCatalogProvider().Describe(typeof(BasicFixture));
        var state = new PlaygroundState();
        state.Set("Dense", true);
        state.Set("Label", "hello");
        state.Set("ChildContent", "ignored");   // Slot — must be skipped
        state.Set("OnValueChanged", "ignored"); // Event — must be skipped
        state.Set("Endpoint", "ignored");       // Unsupported — must be skipped

        var parameters = ParameterDictionaryBuilder.Build(descriptor, state);

        parameters.Should().BeEquivalentTo(new Dictionary<string, object>
        {
            ["Dense"] = true,
            ["Label"] = "hello",
        });
    }

    [Test]
    public void Build_SkipsNullModifiedValues()
    {
        var descriptor = new ReflectionCatalogProvider().Describe(typeof(BasicFixture));
        var state = new PlaygroundState();
        state.Set("Label", null);

        ParameterDictionaryBuilder.Build(descriptor, state).Should().BeEmpty();
    }
}
```

- [x] **Step 2: Run tests to verify they fail**

Run: `dotnet run --project src/PlayBlazor.UnitTests`
Expected: FAIL to build — `ParameterDictionaryBuilder` does not exist.

- [x] **Step 3: Implement**

`src/PlayBlazor/Rendering/ParameterDictionaryBuilder.cs`:

```csharp
using PlayBlazor.Model;
using PlayBlazor.State;

namespace PlayBlazor.Rendering;

public static class ParameterDictionaryBuilder
{
    public static Dictionary<string, object> Build(ComponentDescriptor component, PlaygroundState state)
    {
        var result = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var parameter in component.Parameters)
        {
            if (parameter.Kind is ControlKind.Slot or ControlKind.Event or ControlKind.Unsupported)
            {
                continue;
            }
            if (!state.IsModified(parameter.Name))
            {
                continue;
            }

            if (state.GetValue(parameter) is { } value)
            {
                result[parameter.Name] = value;
            }
        }

        return result;
    }
}
```

- [x] **Step 4: Run tests to verify they pass**

Run: `dotnet run --project src/PlayBlazor.UnitTests`
Expected: PASS.

- [x] **Step 5: Commit**

```bash
git add src/PlayBlazor/Rendering src/PlayBlazor.UnitTests/Rendering
git commit -m "PlayBlazor: build DynamicComponent parameter dictionaries from state"
```

---

### Task 9: Contrôles de base + ControlHost

**Files:**
- Create: `src/PlayBlazor/Shell/Controls/BoolControl.razor`
- Create: `src/PlayBlazor/Shell/Controls/EnumControl.razor`
- Create: `src/PlayBlazor/Shell/Controls/TextControl.razor`
- Create: `src/PlayBlazor/Shell/Controls/NumberControl.razor`
- Create: `src/PlayBlazor/Shell/ControlHost.razor`
- Modify: `src/PlayBlazor/_Imports.razor`
- Test: `src/PlayBlazor.UnitTests/Shell/ControlTests.cs`

**Interfaces:**
- Consumes: `ParameterDescriptor`, `ControlKind` (Task 2).
- Produces: five Razor components in namespace `PlayBlazor.Shell`, all sharing the same parameter contract:

```csharp
[Parameter, EditorRequired] public ParameterDescriptor Parameter { get; set; }
[Parameter] public object? Value { get; set; }
[Parameter] public EventCallback<object?> ValueChanged { get; set; }
```

`ControlHost` dispatches on `Parameter.Kind` to the matching control and renders nothing for other kinds. Value semantics: `TextControl` reports empty input as `null`; `NumberControl` reports blank as `null` and swallows unparsable input; `EnumControl` parses the selected name into the enum type.

- [x] **Step 1: Extend `src/PlayBlazor/_Imports.razor`**

```razor
@using Microsoft.AspNetCore.Components
@using Microsoft.AspNetCore.Components.Web
@using PlayBlazor.Model
```

- [x] **Step 2: Write the failing tests**

`src/PlayBlazor.UnitTests/Shell/ControlTests.cs`:

```csharp
using AwesomeAssertions;
using Bunit;
using NUnit.Framework;
using PlayBlazor.Model;
using PlayBlazor.Shell;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Shell;

public class ControlTests
{
    private BunitContext _context = null!;

    [SetUp]
    public void Setup()
    {
        _context = new BunitContext();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    private static ParameterDescriptor Descriptor(string name, Type type, ControlKind kind, object? defaultValue = null)
        => new(name, type, kind, IsNullable: false, DefaultValue: defaultValue, HasDefault: true, Summary: "the docs");

    [Test]
    public void BoolControl_Change_ReportsBoolean()
    {
        object? reported = "unset";
        var cut = _context.Render<BoolControl>(ps => ps
            .Add(c => c.Parameter, Descriptor("Dense", typeof(bool), ControlKind.Bool, false))
            .Add(c => c.Value, false)
            .Add(c => c.ValueChanged, v => reported = v));

        cut.Find("input[type=checkbox]").Change(true);

        reported.Should().Be(true);
    }

    [Test]
    public void EnumControl_ListsNamesAndReportsEnumValue()
    {
        object? reported = null;
        var cut = _context.Render<EnumControl>(ps => ps
            .Add(c => c.Parameter, Descriptor("Size", typeof(FixtureSize), ControlKind.Enum, FixtureSize.Medium))
            .Add(c => c.Value, FixtureSize.Medium)
            .Add(c => c.ValueChanged, v => reported = v));

        cut.FindAll("option").Count.Should().Be(3);
        cut.Find("select").Change("Large");

        reported.Should().Be(FixtureSize.Large);
    }

    [Test]
    public void TextControl_EmptyInput_ReportsNull()
    {
        object? reported = "unset";
        var cut = _context.Render<TextControl>(ps => ps
            .Add(c => c.Parameter, Descriptor("Label", typeof(string), ControlKind.Text))
            .Add(c => c.Value, "hello")
            .Add(c => c.ValueChanged, v => reported = v));

        cut.Find("input[type=text]").Change("");

        reported.Should().BeNull();
    }

    [Test]
    public void NumberControl_ParsesWithParameterType()
    {
        object? reported = null;
        var cut = _context.Render<NumberControl>(ps => ps
            .Add(c => c.Parameter, Descriptor("Ratio", typeof(double), ControlKind.Number, 0.5))
            .Add(c => c.Value, 0.5)
            .Add(c => c.ValueChanged, v => reported = v));

        cut.Find("input[type=number]").Change("2.75");

        reported.Should().Be(2.75);
    }

    [Test]
    public void NumberControl_UnparsableInput_ReportsNothing()
    {
        object? reported = "unset";
        var cut = _context.Render<NumberControl>(ps => ps
            .Add(c => c.Parameter, Descriptor("Count", typeof(int), ControlKind.Number, 3))
            .Add(c => c.Value, 3)
            .Add(c => c.ValueChanged, v => reported = v));

        cut.Find("input[type=number]").Change("abc");

        reported.Should().Be("unset");
    }

    [Test]
    public void ControlHost_DispatchesOnKind()
    {
        var cut = _context.Render<ControlHost>(ps => ps
            .Add(c => c.Parameter, Descriptor("Dense", typeof(bool), ControlKind.Bool, false)));

        cut.FindAll("input[type=checkbox]").Count.Should().Be(1);
    }

    [Test]
    public void ControlHost_UnsupportedKind_RendersNothing()
    {
        var cut = _context.Render<ControlHost>(ps => ps
            .Add(c => c.Parameter, Descriptor("Endpoint", typeof(Uri), ControlKind.Unsupported)));

        cut.Markup.Trim().Should().BeEmpty();
    }
}
```

- [x] **Step 3: Run tests to verify they fail**

Run: `dotnet run --project src/PlayBlazor.UnitTests`
Expected: FAIL to build — the control components do not exist.

- [x] **Step 4: Implement the controls**

`src/PlayBlazor/Shell/Controls/BoolControl.razor`:

```razor
@namespace PlayBlazor.Shell
<label class="pb-control pb-control-bool">
    <span class="pb-control-label" title="@Parameter.Summary">@Parameter.Name</span>
    <input type="checkbox" checked="@(Value is true)" @onchange="OnChanged" />
</label>
@code {
    [Parameter, EditorRequired] public ParameterDescriptor Parameter { get; set; } = default!;
    [Parameter] public object? Value { get; set; }
    [Parameter] public EventCallback<object?> ValueChanged { get; set; }

    private Task OnChanged(ChangeEventArgs e) => ValueChanged.InvokeAsync(e.Value is true);
}
```

`src/PlayBlazor/Shell/Controls/EnumControl.razor`:

```razor
@namespace PlayBlazor.Shell
<label class="pb-control pb-control-enum">
    <span class="pb-control-label" title="@Parameter.Summary">@Parameter.Name</span>
    <select value="@(Value?.ToString() ?? string.Empty)" @onchange="OnChanged">
        @foreach (var name in Enum.GetNames(EnumType))
        {
            <option value="@name">@name</option>
        }
    </select>
</label>
@code {
    [Parameter, EditorRequired] public ParameterDescriptor Parameter { get; set; } = default!;
    [Parameter] public object? Value { get; set; }
    [Parameter] public EventCallback<object?> ValueChanged { get; set; }

    private Type EnumType => Nullable.GetUnderlyingType(Parameter.Type) ?? Parameter.Type;

    private Task OnChanged(ChangeEventArgs e) => ValueChanged.InvokeAsync(Enum.Parse(EnumType, (string)e.Value!));
}
```

`src/PlayBlazor/Shell/Controls/TextControl.razor`:

```razor
@namespace PlayBlazor.Shell
<label class="pb-control pb-control-text">
    <span class="pb-control-label" title="@Parameter.Summary">@Parameter.Name</span>
    <input type="text" value="@(Value as string)" @onchange="OnChanged" />
</label>
@code {
    [Parameter, EditorRequired] public ParameterDescriptor Parameter { get; set; } = default!;
    [Parameter] public object? Value { get; set; }
    [Parameter] public EventCallback<object?> ValueChanged { get; set; }

    private Task OnChanged(ChangeEventArgs e)
        => ValueChanged.InvokeAsync((string?)e.Value is { Length: > 0 } text ? text : null);
}
```

`src/PlayBlazor/Shell/Controls/NumberControl.razor`:

```razor
@namespace PlayBlazor.Shell
@using System.Globalization
<label class="pb-control pb-control-number">
    <span class="pb-control-label" title="@Parameter.Summary">@Parameter.Name</span>
    <input type="number" value="@FormattedValue" step="@Step" @onchange="OnChanged" />
</label>
@code {
    [Parameter, EditorRequired] public ParameterDescriptor Parameter { get; set; } = default!;
    [Parameter] public object? Value { get; set; }
    [Parameter] public EventCallback<object?> ValueChanged { get; set; }

    private Type NumberType => Nullable.GetUnderlyingType(Parameter.Type) ?? Parameter.Type;

    private string Step
        => NumberType == typeof(double) || NumberType == typeof(float) || NumberType == typeof(decimal)
            ? "any"
            : "1";

    private string FormattedValue
        => Value is IFormattable formattable ? formattable.ToString(null, CultureInfo.InvariantCulture) : string.Empty;

    private Task OnChanged(ChangeEventArgs e)
    {
        var raw = (string?)e.Value;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return ValueChanged.InvokeAsync(null);
        }

        try
        {
            return ValueChanged.InvokeAsync(Convert.ChangeType(raw, NumberType, CultureInfo.InvariantCulture));
        }
        catch (Exception exception) when (exception is FormatException or OverflowException or InvalidCastException)
        {
            return Task.CompletedTask;
        }
    }
}
```

`src/PlayBlazor/Shell/ControlHost.razor`:

```razor
@namespace PlayBlazor.Shell
@switch (Parameter.Kind)
{
    case ControlKind.Bool:
        <BoolControl Parameter="Parameter" Value="Value" ValueChanged="ValueChanged" />
        break;
    case ControlKind.Enum:
        <EnumControl Parameter="Parameter" Value="Value" ValueChanged="ValueChanged" />
        break;
    case ControlKind.Text:
        <TextControl Parameter="Parameter" Value="Value" ValueChanged="ValueChanged" />
        break;
    case ControlKind.Number:
        <NumberControl Parameter="Parameter" Value="Value" ValueChanged="ValueChanged" />
        break;
    default:
        break;
}
@code {
    [Parameter, EditorRequired] public ParameterDescriptor Parameter { get; set; } = default!;
    [Parameter] public object? Value { get; set; }
    [Parameter] public EventCallback<object?> ValueChanged { get; set; }
}
```

- [x] **Step 5: Run tests to verify they pass**

Run: `dotnet run --project src/PlayBlazor.UnitTests`
Expected: PASS.

- [x] **Step 6: Commit**

```bash
git add src/PlayBlazor/Shell src/PlayBlazor/_Imports.razor src/PlayBlazor.UnitTests/Shell
git commit -m "PlayBlazor: add basic parameter controls and control host"
```

---

### Task 10: PlaygroundView

**Files:**
- Create: `src/PlayBlazor/PlaygroundView.razor`
- Create: `src/PlayBlazor/PlaygroundView.razor.cs`
- Create: `src/PlayBlazor/PlaygroundView.razor.css`
- Create: `src/PlayBlazor.UnitTests/Fixtures/ThrowingRenderFixture.razor`
- Test: `src/PlayBlazor.UnitTests/Shell/PlaygroundViewTests.cs`

**Interfaces:**
- Consumes: everything from Tasks 2–9.
- Produces: `PlaygroundView` (namespace `PlayBlazor`) with `[Parameter, EditorRequired] public Type Component { get; set; }`. Renders: preview (DynamicComponent in ErrorBoundary), control rows with per-row reset, header with display name + warning badge + "Reset" button, collapsed "Uncontrolled" list. Test hooks: preview container has class `pb-preview`, error container `pb-error`, header reset button `pb-reset`, per-row reset `pb-row-reset`.

- [x] **Step 1: Create the throwing-render fixture**

`src/PlayBlazor.UnitTests/Fixtures/ThrowingRenderFixture.razor`:

```razor
@namespace PlayBlazor.UnitTests.Fixtures
<div>never rendered</div>
@code {
    [Parameter] public bool Dense { get; set; }

    protected override void OnParametersSet() => throw new InvalidOperationException("render boom");
}
```

- [x] **Step 2: Write the failing tests**

`src/PlayBlazor.UnitTests/Shell/PlaygroundViewTests.cs`:

```csharp
using AwesomeAssertions;
using Bunit;
using NUnit.Framework;
using PlayBlazor;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Shell;

public class PlaygroundViewTests
{
    private BunitContext _context = null!;

    [SetUp]
    public void Setup()
    {
        _context = new BunitContext();
        _context.Services.AddPlayBlazor();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    private IRenderedComponent<PlaygroundView> RenderView(Type componentType)
        => _context.Render<PlaygroundView>(ps => ps.Add(v => v.Component, componentType));

    [Test]
    public void RendersPreviewWithComponentDefaults()
    {
        var cut = RenderView(typeof(BasicFixture));

        cut.Find(".pb-preview .basic-fixture").TextContent
            .Should().Contain("Size=Medium").And.Contain("Count=3");
    }

    [Test]
    public void RendersOneControlPerDrivableParameter()
    {
        var cut = RenderView(typeof(BasicFixture));

        // Dense, Outlined (bool) + Size (enum) + Label (text) + Count, Ratio, MaxItems (number) = 7
        cut.FindAll(".pb-control").Count.Should().Be(7);
    }

    [Test]
    public void TogglingControl_UpdatesPreview()
    {
        var cut = RenderView(typeof(BasicFixture));

        cut.FindAll("input[type=checkbox]")[0].Change(true); // Dense

        cut.Find(".basic-fixture").TextContent.Should().Contain("Dense=True");
    }

    [Test]
    public void RowReset_RestoresDefault_OnFreshInstance()
    {
        var cut = RenderView(typeof(BasicFixture));
        cut.FindAll("input[type=checkbox]")[0].Change(true);
        cut.Find(".basic-fixture").TextContent.Should().Contain("Dense=True");

        cut.Find(".pb-row-reset").Click();

        cut.Find(".basic-fixture").TextContent.Should().Contain("Dense=False");
    }

    [Test]
    public void ListsUncontrolledParameters()
    {
        var cut = RenderView(typeof(BasicFixture));

        var uncontrolled = cut.Find(".pb-uncontrolled").TextContent;
        uncontrolled.Should().Contain("ChildContent").And.Contain("OnValueChanged").And.Contain("Endpoint");
    }

    [Test]
    public void ThrowingComponent_ShowsErrorInsteadOfCrashing()
    {
        var cut = RenderView(typeof(ThrowingRenderFixture));

        cut.Find(".pb-error").TextContent.Should().Contain("render boom");
    }

    [Test]
    public void WarningBadge_ShownForUninstantiableComponent()
    {
        var cut = RenderView(typeof(ThrowingCtorFixture));

        cut.FindAll(".pb-warning").Count.Should().Be(1);
    }
}
```

- [x] **Step 3: Run tests to verify they fail**

Run: `dotnet run --project src/PlayBlazor.UnitTests`
Expected: FAIL to build — `PlaygroundView` does not exist.

- [x] **Step 4: Implement the view**

`src/PlayBlazor/PlaygroundView.razor`:

```razor
@namespace PlayBlazor
@using PlayBlazor.Rendering
@using PlayBlazor.Shell

<div class="pb-playground">
    <div class="pb-preview">
        <ErrorBoundary @ref="_errorBoundary">
            <ChildContent>
                <DynamicComponent @key="@_state.InstanceKey"
                                  Type="@_descriptor.Type"
                                  Parameters="@ParameterDictionaryBuilder.Build(_descriptor, _state)" />
            </ChildContent>
            <ErrorContent Context="exception">
                <div class="pb-error">
                    <strong>The component threw an exception.</strong>
                    <pre>@exception.Message</pre>
                    <button type="button" @onclick="RecoverFromError">Reset</button>
                </div>
            </ErrorContent>
        </ErrorBoundary>
    </div>
    <div class="pb-panel">
        <div class="pb-panel-header">
            <span class="pb-title" title="@_descriptor.Summary">@_descriptor.DisplayName</span>
            @if (_descriptor.Warning is not null)
            {
                <span class="pb-warning" title="@_descriptor.Warning">&#9888;</span>
            }
            <button type="button" class="pb-reset" @onclick="ResetAll">Reset</button>
        </div>
        @foreach (var parameter in Controllable)
        {
            <div class="pb-row" @key="parameter.Name">
                <ControlHost Parameter="parameter"
                             Value="_state.GetValue(parameter)"
                             ValueChanged="value => OnControlChanged(parameter, value)" />
                @if (_state.IsModified(parameter.Name))
                {
                    <button type="button" class="pb-row-reset" title="Restore default"
                            @onclick="() => _state.Reset(parameter.Name)">&times;</button>
                }
            </div>
        }
        @if (Uncontrollable.Any())
        {
            <details class="pb-uncontrolled">
                <summary>Uncontrolled (@Uncontrollable.Count())</summary>
                <ul>
                    @foreach (var parameter in Uncontrollable)
                    {
                        <li @key="parameter.Name" title="@parameter.Summary">@parameter.Name — @parameter.Kind</li>
                    }
                </ul>
            </details>
        }
    </div>
</div>
```

`src/PlayBlazor/PlaygroundView.razor.cs`:

```csharp
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PlayBlazor.Discovery;
using PlayBlazor.Model;
using PlayBlazor.State;

namespace PlayBlazor;

/// <summary>An auto-generated playground for a single component type.</summary>
public partial class PlaygroundView : ComponentBase, IDisposable
{
    private readonly PlaygroundState _state = new();
    private ComponentDescriptor _descriptor = default!;
    private ErrorBoundary? _errorBoundary;

    [Inject]
    private IComponentCatalogProvider Catalog { get; set; } = default!;

    [Parameter, EditorRequired]
    public Type Component { get; set; } = default!;

    private IEnumerable<ParameterDescriptor> Controllable
        => _descriptor.Parameters.Where(static p => p.Kind
            is ControlKind.Bool or ControlKind.Enum or ControlKind.Text or ControlKind.Number);

    private IEnumerable<ParameterDescriptor> Uncontrollable
        => _descriptor.Parameters.Where(static p => p.Kind
            is ControlKind.Slot or ControlKind.Event or ControlKind.Unsupported);

    protected override void OnInitialized()
        => _state.Changed += OnStateChanged;

    protected override void OnParametersSet()
    {
        if (_descriptor?.Type != Component)
        {
            _descriptor = Catalog.Describe(Component);
            _state.ResetAll();
        }
    }

    public void Dispose()
        => _state.Changed -= OnStateChanged;

    private void OnStateChanged()
        => _ = InvokeAsync(StateHasChanged);

    private void OnControlChanged(ParameterDescriptor parameter, object? value)
    {
        if (value is null)
        {
            _state.Reset(parameter.Name);
        }
        else
        {
            _state.Set(parameter.Name, value);
        }
    }

    private void ResetAll()
        => _state.ResetAll();

    private void RecoverFromError()
    {
        _state.ResetAll();
        _errorBoundary?.Recover();
    }
}
```

(The `_descriptor = default!` initialization is the standard Blazor idiom: the field is assigned in the first `OnParametersSet`, which runs before the first render; `_descriptor?.Type != Component` is true on that first call because `?.` yields null.)

`src/PlayBlazor/PlaygroundView.razor.css`:

```css
.pb-playground {
    display: grid;
    grid-template-columns: 1fr 320px;
    border: 1px solid #d0d0d0;
    border-radius: 8px;
    overflow: hidden;
    font-family: system-ui, -apple-system, "Segoe UI", Roboto, sans-serif;
    font-size: 0.875rem;
}

.pb-preview {
    display: flex;
    align-items: center;
    justify-content: center;
    min-height: 200px;
    padding: 1.5rem;
    background: #fafafa;
}

.pb-error {
    color: #b00020;
}

.pb-error pre {
    white-space: pre-wrap;
    font-size: 0.75rem;
}

.pb-panel {
    display: flex;
    flex-direction: column;
    gap: 0.125rem;
    max-height: 480px;
    padding: 0.75rem;
    overflow-y: auto;
    border-left: 1px solid #d0d0d0;
}

.pb-panel-header {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 0.5rem;
}

.pb-title {
    font-weight: 600;
}

.pb-warning {
    cursor: help;
}

.pb-reset {
    margin-left: auto;
}

.pb-row {
    display: flex;
    align-items: center;
    gap: 0.25rem;
}

.pb-row ::deep .pb-control {
    display: flex;
    flex: 1;
    align-items: center;
    justify-content: space-between;
    gap: 0.5rem;
    padding: 0.25rem 0;
}

.pb-row ::deep .pb-control-label {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.pb-row ::deep input[type="text"],
.pb-row ::deep input[type="number"],
.pb-row ::deep select {
    max-width: 160px;
}

.pb-row-reset {
    border: none;
    background: none;
    cursor: pointer;
    color: #888;
}

.pb-uncontrolled {
    margin-top: 0.5rem;
    color: #777;
    font-size: 0.8125rem;
}
```

- [x] **Step 5: Run tests to verify they pass**

Run: `dotnet run --project src/PlayBlazor.UnitTests`
Expected: PASS.

- [x] **Step 6: Commit**

```bash
git add src/PlayBlazor src/PlayBlazor.UnitTests
git commit -m "PlayBlazor: add PlaygroundView with auto-generated controls and error boundary"
```

---

### Task 11: DemoHost WASM

**Files:**
- Create: `src/PlayBlazor.DemoHost/PlayBlazor.DemoHost.csproj`
- Create: `src/PlayBlazor.DemoHost/Program.cs`
- Create: `src/PlayBlazor.DemoHost/App.razor`
- Create: `src/PlayBlazor.DemoHost/MainLayout.razor`
- Create: `src/PlayBlazor.DemoHost/Pages/Index.razor`
- Create: `src/PlayBlazor.DemoHost/_Imports.razor`
- Create: `src/PlayBlazor.DemoHost/wwwroot/index.html`
- Modify: `src/MudBlazor.slnx`

**Interfaces:**
- Consumes: `PlaygroundView`, `AddPlayBlazor` (Tasks 6, 10).
- Produces: a runnable WASM demo site (`dotnet run --project src/PlayBlazor.DemoHost`) showing MudBlazor components inside PlayBlazor playgrounds.

- [x] **Step 1: Create the project**

`src/PlayBlazor.DemoHost/PlayBlazor.DemoHost.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">

  <PropertyGroup>
    <TargetFramework>$(PrimaryTargetFramework)</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="10.0.11" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" Version="10.0.11" PrivateAssets="all" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\MudBlazor\MudBlazor.csproj" />
    <ProjectReference Include="..\PlayBlazor\PlayBlazor.csproj" />
  </ItemGroup>

</Project>
```

`src/PlayBlazor.DemoHost/_Imports.razor`:

```razor
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using MudBlazor
@using PlayBlazor
@using PlayBlazor.DemoHost
```

`src/PlayBlazor.DemoHost/Program.cs`:

```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using PlayBlazor;
using PlayBlazor.DemoHost;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();
builder.Services.AddPlayBlazor();

await builder.Build().RunAsync();
```

`src/PlayBlazor.DemoHost/App.razor`:

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

`src/PlayBlazor.DemoHost/MainLayout.razor`:

```razor
@inherits LayoutComponentBase
<MudThemeProvider />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />
<div class="demo-host">
    @Body
</div>
```

`src/PlayBlazor.DemoHost/Pages/Index.razor`:

```razor
@page "/"
<h1>PlayBlazor — DemoHost</h1>
<p>Auto-generated playgrounds for real MudBlazor components. No hand-written wiring below.</p>

<h2>MudButton</h2>
<PlaygroundView Component="typeof(MudButton)" />

<h2>MudProgressCircular</h2>
<PlaygroundView Component="typeof(MudProgressCircular)" />

<h2>MudAlert</h2>
<PlaygroundView Component="typeof(MudAlert)" />
```

`src/PlayBlazor.DemoHost/wwwroot/index.html`:

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>PlayBlazor DemoHost</title>
    <base href="/" />
    <link href="https://fonts.googleapis.com/css2?family=Roboto:wght@300;400;500;700&display=swap" rel="stylesheet" />
    <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
    <link href="PlayBlazor.DemoHost.styles.css" rel="stylesheet" />
</head>
<body>
    <div id="app">Loading…</div>
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>
    <script src="_framework/blazor.webassembly.js"></script>
</body>
</html>
```

- [x] **Step 2: Register in `src/MudBlazor.slnx`**

Inside `<Folder Name="/playblazor/">` add:

```xml
    <Project Path="PlayBlazor.DemoHost/PlayBlazor.DemoHost.csproj" />
```

- [x] **Step 3: Build and verify trimmed publish**

Run: `dotnet build src/PlayBlazor.DemoHost`
Expected: build succeeds.

Run: `dotnet publish src/PlayBlazor.DemoHost -c Release -o /private/tmp/claude-501/-Users-philippe-repo-phmatray-public-MudBlazor/8d6118c8-0bb1-4693-9fab-cbe7fad30472/scratchpad/demohost-publish`
Expected: publish succeeds (WASM publish trims by default; this is the early trimming guard from the spec).

- [x] **Step 4: Manual smoke run**

Run: `dotnet run --project src/PlayBlazor.DemoHost` (background), then load the printed localhost URL and verify: three playgrounds render; toggling MudButton's `Disabled`, `Variant`, `Color` changes the button live; `MudProgressCircular` `Indeterminate` animates. Stop the server afterwards.

- [x] **Step 5: Run the full test suite (regression)**

Run: `dotnet run --project src/PlayBlazor.UnitTests`
Expected: PASS.

- [x] **Step 6: Commit — milestone 2 complete**

```bash
git add src/PlayBlazor.DemoHost src/MudBlazor.slnx
git commit -m "PlayBlazor: add WASM DemoHost exercising MudBlazor components (milestone 2)"
```

---

## Jalon 3 — CodeGen

### Task 12: RazorSnippetGenerator

**Files:**
- Create: `src/PlayBlazor/CodeGen/RazorSnippetGenerator.cs`
- Test: `src/PlayBlazor.UnitTests/CodeGen/RazorSnippetGeneratorTests.cs`

**Interfaces:**
- Consumes: `ComponentDescriptor`, `PlaygroundState` (Tasks 2, 7).
- Produces: `static class RazorSnippetGenerator { public static string Generate(ComponentDescriptor component, PlaygroundState state); }` (namespace `PlayBlazor.CodeGen`). Rules: only modified, non-null, drivable parameters, in declaration order; bool → `Name="true"/"false"`; enum → `Name="EnumType.Member"`; string → `Name="value"` with `"` escaped as `&quot;`; number → invariant culture; 0 attributes → `<Name />`; 1–2 attributes → single line; 3+ → one attribute per line aligned under the first.

- [x] **Step 1: Write the failing tests**

`src/PlayBlazor.UnitTests/CodeGen/RazorSnippetGeneratorTests.cs`:

```csharp
using AwesomeAssertions;
using NUnit.Framework;
using PlayBlazor.CodeGen;
using PlayBlazor.Discovery;
using PlayBlazor.Model;
using PlayBlazor.State;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.CodeGen;

public class RazorSnippetGeneratorTests
{
    private ComponentDescriptor _descriptor = null!;

    [SetUp]
    public void Setup()
    {
        _descriptor = new ReflectionCatalogProvider().Describe(typeof(BasicFixture));
    }

    [Test]
    public void Generate_NoModifications_EmitsSelfClosingTag()
    {
        RazorSnippetGenerator.Generate(_descriptor, new PlaygroundState())
            .Should().Be("<BasicFixture />");
    }

    [Test]
    public void Generate_TwoAttributes_SingleLine()
    {
        var state = new PlaygroundState();
        state.Set("Dense", true);
        state.Set("Label", "Hello");

        RazorSnippetGenerator.Generate(_descriptor, state)
            .Should().Be("""<BasicFixture Dense="true" Label="Hello" />""");
    }

    [Test]
    public void Generate_ThreeAttributes_MultiLineAligned()
    {
        var state = new PlaygroundState();
        state.Set("Dense", true);
        state.Set("Size", FixtureSize.Large);
        state.Set("Count", 7);

        RazorSnippetGenerator.Generate(_descriptor, state).Should().Be(
            "<BasicFixture Dense=\"true\"\n" +
            "              Size=\"FixtureSize.Large\"\n" +
            "              Count=\"7\" />");
    }

    [Test]
    public void Generate_UsesDeclarationOrder_NotModificationOrder()
    {
        var state = new PlaygroundState();
        state.Set("Label", "x");
        state.Set("Dense", true);

        RazorSnippetGenerator.Generate(_descriptor, state)
            .Should().Be("""<BasicFixture Dense="true" Label="x" />""");
    }

    [Test]
    public void Generate_FormatsNumbersWithInvariantCulture()
    {
        var state = new PlaygroundState();
        state.Set("Ratio", 2.75);

        RazorSnippetGenerator.Generate(_descriptor, state)
            .Should().Be("""<BasicFixture Ratio="2.75" />""");
    }

    [Test]
    public void Generate_EscapesQuotesInStrings()
    {
        var state = new PlaygroundState();
        state.Set("Label", "say \"hi\"");

        RazorSnippetGenerator.Generate(_descriptor, state)
            .Should().Be("""<BasicFixture Label="say &quot;hi&quot;" />""");
    }

    [Test]
    public void Generate_SkipsNonDrivableAndNullValues()
    {
        var state = new PlaygroundState();
        state.Set("ChildContent", "ignored");
        state.Set("Label", null);

        RazorSnippetGenerator.Generate(_descriptor, state)
            .Should().Be("<BasicFixture />");
    }
}
```

- [x] **Step 2: Run tests to verify they fail**

Run: `dotnet run --project src/PlayBlazor.UnitTests`
Expected: FAIL to build — `RazorSnippetGenerator` does not exist.

- [x] **Step 3: Implement**

`src/PlayBlazor/CodeGen/RazorSnippetGenerator.cs`:

```csharp
using System.Globalization;
using System.Text;
using PlayBlazor.Model;
using PlayBlazor.State;

namespace PlayBlazor.CodeGen;

/// <summary>Generates the Razor snippet matching the current playground state.</summary>
public static class RazorSnippetGenerator
{
    public static string Generate(ComponentDescriptor component, PlaygroundState state)
    {
        var attributes = new List<string>();
        foreach (var parameter in component.Parameters)
        {
            if (parameter.Kind is ControlKind.Slot or ControlKind.Event or ControlKind.Unsupported)
            {
                continue;
            }
            if (!state.IsModified(parameter.Name))
            {
                continue;
            }
            if (state.GetValue(parameter) is not { } value)
            {
                continue;
            }

            attributes.Add($"{parameter.Name}=\"{FormatValue(value)}\"");
        }

        if (attributes.Count == 0)
        {
            return $"<{component.DisplayName} />";
        }

        if (attributes.Count <= 2)
        {
            return $"<{component.DisplayName} {string.Join(" ", attributes)} />";
        }

        var indent = new string(' ', component.DisplayName.Length + 2);
        var builder = new StringBuilder();
        builder.Append('<').Append(component.DisplayName).Append(' ').Append(attributes[0]);
        foreach (var attribute in attributes.Skip(1))
        {
            builder.Append('\n').Append(indent).Append(attribute);
        }

        builder.Append(" />");
        return builder.ToString();
    }

    private static string FormatValue(object value)
        => value switch
        {
            bool boolean => boolean ? "true" : "false",
            Enum enumValue => $"{enumValue.GetType().Name}.{enumValue}",
            string text => text.Replace("\"", "&quot;"),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };
}
```

- [x] **Step 4: Run tests to verify they pass**

Run: `dotnet run --project src/PlayBlazor.UnitTests`
Expected: PASS.

- [x] **Step 5: Commit**

```bash
git add src/PlayBlazor/CodeGen src/PlayBlazor.UnitTests/CodeGen
git commit -m "PlayBlazor: generate idiomatic Razor snippets from playground state"
```

---

### Task 13: Panneau code + bouton copier

**Files:**
- Modify: `src/PlayBlazor/PlaygroundView.razor` (add the code panel below the grid)
- Modify: `src/PlayBlazor/PlaygroundView.razor.cs` (snippet property + copy)
- Modify: `src/PlayBlazor/PlaygroundView.razor.css` (code panel styles)
- Test: `src/PlayBlazor.UnitTests/Shell/CodePanelTests.cs`

**Interfaces:**
- Consumes: `RazorSnippetGenerator` (Task 12), `PlaygroundView` (Task 10).
- Produces: a `.pb-code` section inside `PlaygroundView` containing `<pre><code>` with the live snippet and a `.pb-copy` button calling `navigator.clipboard.writeText` via JS interop.

- [x] **Step 1: Write the failing tests**

`src/PlayBlazor.UnitTests/Shell/CodePanelTests.cs`:

```csharp
using AwesomeAssertions;
using Bunit;
using NUnit.Framework;
using PlayBlazor;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Shell;

public class CodePanelTests
{
    private BunitContext _context = null!;

    [SetUp]
    public void Setup()
    {
        _context = new BunitContext();
        _context.Services.AddPlayBlazor();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    private IRenderedComponent<PlaygroundView> RenderView()
        => _context.Render<PlaygroundView>(ps => ps.Add(v => v.Component, typeof(BasicFixture)));

    [Test]
    public void CodePanel_ShowsDefaultSnippet()
    {
        RenderView().Find(".pb-code code").TextContent.Should().Be("<BasicFixture />");
    }

    [Test]
    public void CodePanel_UpdatesLiveWithControls()
    {
        var cut = RenderView();

        cut.FindAll("input[type=checkbox]")[0].Change(true); // Dense

        cut.Find(".pb-code code").TextContent.Should().Be("""<BasicFixture Dense="true" />""");
    }

    [Test]
    public void CopyButton_WritesSnippetToClipboard()
    {
        var cut = RenderView();
        cut.FindAll("input[type=checkbox]")[0].Change(true);

        cut.Find(".pb-copy").Click();

        var invocation = _context.JSInterop.Invocations
            .Single(i => i.Identifier == "navigator.clipboard.writeText");
        invocation.Arguments[0].Should().Be("""<BasicFixture Dense="true" />""");
    }
}
```

- [x] **Step 2: Run tests to verify they fail**

Run: `dotnet run --project src/PlayBlazor.UnitTests`
Expected: FAIL — `.pb-code` not found.

- [x] **Step 3: Implement the code panel**

In `src/PlayBlazor/PlaygroundView.razor`, add `@using PlayBlazor.CodeGen` to the top and insert after the closing `</div>` of `pb-panel` but before the closing `</div>` of `pb-playground`:

```razor
    <div class="pb-code">
        <pre><code>@Snippet</code></pre>
        <button type="button" class="pb-copy" title="Copy to clipboard" @onclick="CopySnippet">Copy</button>
    </div>
```

In `src/PlayBlazor/PlaygroundView.razor.cs`:

1. Add `using Microsoft.JSInterop;` and `using PlayBlazor.CodeGen;`.
2. Add the injection, property and handler:

```csharp
    [Inject]
    private IJSRuntime Js { get; set; } = default!;

    private string Snippet => RazorSnippetGenerator.Generate(_descriptor, _state);

    private async Task CopySnippet()
        => await Js.InvokeVoidAsync("navigator.clipboard.writeText", Snippet);
```

In `src/PlayBlazor/PlaygroundView.razor.css`, append:

```css
.pb-code {
    position: relative;
    grid-column: 1 / -1;
    margin: 0;
    padding: 0.75rem 1rem;
    border-top: 1px solid #d0d0d0;
    background: #1e1e2e;
    color: #e6e6ef;
    overflow-x: auto;
}

.pb-code pre {
    margin: 0;
    font-family: ui-monospace, "Cascadia Code", "SF Mono", Menlo, Consolas, monospace;
    font-size: 0.8125rem;
    line-height: 1.5;
}

.pb-copy {
    position: absolute;
    top: 0.5rem;
    right: 0.5rem;
    padding: 0.25rem 0.75rem;
    border: 1px solid #555;
    border-radius: 4px;
    background: transparent;
    color: #e6e6ef;
    cursor: pointer;
}

.pb-copy:hover {
    background: #333;
}
```

- [x] **Step 4: Run tests to verify they pass**

Run: `dotnet run --project src/PlayBlazor.UnitTests`
Expected: PASS (full suite).

- [x] **Step 5: DemoHost visual check**

Run: `dotnet run --project src/PlayBlazor.DemoHost` (background), load the URL, verify the code panel shows under each playground and updates live; stop the server.

- [x] **Step 6: Commit — milestone 3 complete**

```bash
git add src/PlayBlazor src/PlayBlazor.UnitTests
git commit -m "PlayBlazor: add live Razor code panel with copy button (milestone 3)"
```

---

## Journal d'exécution (2026-08-26) — écarts vs plan

Exécuté intégralement le 2026-08-26 ; 72 tests verts. Quatre écarts, tous documentés dans les commits :

1. **`ComponentDescriptor.CanInstantiate` ajouté** (Task 10). Une exception de *constructeur* n'est pas interceptable par `ErrorBoundary` (l'instanciation se fait dans la component factory, hors cycle de vie). Quand la découverte sait que l'instanciation échoue, `PlaygroundView` affiche l'erreur directement au lieu de tenter le rendu.
2. **bUnit 2.x est en JSInterop Strict par défaut** (Task 13). Le test du bouton Copy requiert `_context.JSInterop.SetupVoid("navigator.clipboard.writeText", _ => true)`.
3. **Vérification navigateur du DemoHost remplacée** (Tasks 11/13). Les deux canaux navigateur étaient indisponibles dans la session (profil chrome-devtools verrouillé par une autre instance, extension Claude in Chrome déconnectée). Couverture équivalente : `MudBlazorIntegrationTests.cs` (bUnit rend `PlaygroundView` sur les vrais `MudButton`/`MudProgressCircular`, le select `Variant` auto-généré change les classes rendues), check du contenu servi par le dev server, publish Release trimmed OK. La passe visuelle reste à faire à la main : `dotnet run --project src/PlayBlazor.DemoHost`.
4. **SDK .NET** : `global.json` exige 10.0.400 ; seul `~/.dotnet` le contient sur cette machine — préfixer les commandes avec `DOTNET_ROOT=$HOME/.dotnet` et `PATH=$HOME/.dotnet:$PATH`.
