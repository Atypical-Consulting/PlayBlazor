using System.Diagnostics;
using System.Reflection;
using System.Text;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Services;
using NUnit.Framework;
using PlayBlazor.Discovery;
using PlayBlazor.Model;

namespace PlayBlazor.UnitTests.Diagnostics;

/// <summary>
/// Diagnostic sweep: renders every discovered component of an explored library through
/// PlaygroundView under explorer-like conditions (the library's own services + providers, loose
/// JS interop) and reports every component that surfaces an error — either contained by the
/// error boundary or escaping the render. The test never fails; its output is the inventory used
/// to drive fixes.
/// </summary>
public class RenderSweepTests
{
    [Test]
    [Explicit("Diagnostic inventory — run on demand; prints a report instead of asserting.")]
    [TestCaseSource(typeof(ExploredLibraries), nameof(ExploredLibraries.All))]
    public async Task RenderSweep_ReportsEveryComponentError(Assembly assembly, Action<PlayBlazorOptions> configure)
    {
        // Debug.Assert in a component lifecycle (e.g. FooterCell asserting its parent DataGrid)
        // TERMINATES the process in Debug builds — convert asserts into catchable exceptions
        // so the sweep survives and reports them as the worst offenders they are.
        var originalListeners = Trace.Listeners.Cast<TraceListener>().ToArray();
        Trace.Listeners.Clear();
        Trace.Listeners.Add(new ThrowingTraceListener());
        try
        {
            await RunSweepAsync(assembly, configure);
        }
        finally
        {
            Trace.Listeners.Clear();
            Trace.Listeners.AddRange(originalListeners);
        }
    }

    [Test]
    public void SweptName_NamesTheClosingActuallyRendered()
    {
        // A generic reports its closing, so a transcript never reads "FluentCalendar" for both
        // the placeholder Discovery picked and a closing the host declared with options.For<T>().
        SweptName("FluentCalendar", typeof(FluentCalendar<>).MakeGenericType(typeof(string)))
            .Should().Be("FluentCalendar<string>");
        SweptName("FluentCalendar", typeof(FluentCalendar<DateTime?>)).Should().Be("FluentCalendar<DateTime?>");
        SweptName("FluentNumberInput", typeof(FluentNumberInput<int>)).Should().Be("FluentNumberInput<int>");
        SweptName("MudChart", typeof(MudChart<int>)).Should().Be("MudChart<int>");

        // A non-generic reports bare — nothing to disambiguate.
        SweptName("FluentButton", typeof(FluentButton)).Should().Be("FluentButton");
    }

    /// <summary>
    /// Registers the explored library's own services. DaisyBlazor is not yet a sweep case: a third
    /// branch will register its services once the DaisyBlazor demo app exists — see the note in
    /// ExploredLibraries for the blocker.
    /// </summary>
    private static void AddLibraryServices(IServiceCollection services, Assembly assembly)
    {
        switch (assembly.GetName().Name)
        {
            case "MudBlazor":
                services.AddMudServices();
                break;
            case "Microsoft.FluentUI.AspNetCore.Components":
                services.AddFluentUIComponents();
                break;
        }
    }

    /// <summary>The container the catalog constructs from, matching what a real host provides.</summary>
    private static IServiceProvider ContainerFor(Assembly assembly)
    {
        var services = new ServiceCollection();
        AddLibraryServices(services, assembly);
        return services.BuildServiceProvider();
    }

    private static async Task RunSweepAsync(Assembly assembly, Action<PlayBlazorOptions> configure)
    {
        var options = new PlayBlazorOptions();
        configure(options);

        // Capturing defaults means constructing each component, and a library may demand its own
        // services through the constructor (Fluent UI v5 wants a LibraryConfiguration). Give the
        // catalog the same container a real host would, or every such component reports
        // "could not be instantiated" and the sweep measures the harness instead of the library.
        var catalog = new ReflectionCatalogProvider(options: options, services: ContainerFor(assembly));

        // Discovery closes an open generic with string, then int — but the host may have declared
        // a different closing worth playing (options.For<T>()). Resolve each to the same closing
        // PlaygroundWorkspace.OnParametersSet would pick, or the sweep measures a type the real
        // explorer never renders, and a host closing (Task 2's FluentCalendar<DateTime?>, say)
        // never moves the numbers no matter how correct it is.
        IReadOnlyList<ComponentDescriptor> components = catalog.Discover(assembly)
            .Select(c => options.ResolvePreferredClosing(c.Type) is var preferred && preferred != c.Type
                ? catalog.Describe(preferred)
                : c)
            .ToArray();

        var contained = new List<(string Name, string Error)>();
        var escaped = new List<(string Name, string Error)>();
        var healthy = 0;

        foreach (var component in components)
        {
            await using var context = new BunitContext();
            context.JSInterop.Mode = JSRuntimeMode.Loose;

            // Each library needs its own service registrations before its components will render.
            AddLibraryServices(context.Services, assembly);

            // With the host's own configuration, so presets, scaffolds and catalogues apply —
            // without it the sweep reports failures that curation would already have fixed.
            context.Services.AddPlayBlazor(configure);

            // Bare DisplayName is ambiguous once a generic can be swept under more than one
            // closing (the placeholder Discovery picked, or one the host declared) — name the
            // closing actually rendered, not just the open generic's name.
            var sweptAs = SweptName(component.DisplayName, component.Type);

            try
            {
                var cut = context.Render(builder =>
                {
                    if (assembly.GetName().Name == "MudBlazor")
                    {
                        builder.OpenComponent<MudPopoverProvider>(0);
                        builder.CloseComponent();
                    }

                    builder.OpenComponent<PlaygroundView>(1);
                    builder.AddComponentParameter(2, nameof(PlaygroundView.Component), component.Type);
                    builder.CloseComponent();
                });

                var errors = cut.FindAll(".pb-error pre");
                if (errors.Count > 0)
                {
                    contained.Add((sweptAs, FirstLine(errors[0].TextContent)));
                }
                else
                {
                    healthy++;
                }
            }
            catch (Exception exception)
            {
                var root = Root(exception);
                escaped.Add((sweptAs, $"{root.GetType().Name}: {FirstLine(root.Message)}"));
            }
        }

        var report = new StringBuilder();
        report.AppendLine($"=== {assembly.GetName().Name} ===");
        report.AppendLine($"SWEEP {components.Count} components: {healthy} healthy, {contained.Count} contained errors, {escaped.Count} escaped exceptions");
        report.AppendLine("--- ESCAPED (would take down more than the preview) ---");
        foreach (var (name, error) in escaped)
        {
            report.AppendLine($"[ESCAPED] {name} :: {error}");
        }

        report.AppendLine("--- CONTAINED (shown in the pb-error box) ---");
        foreach (var (name, error) in contained)
        {
            report.AppendLine($"[CONTAINED] {name} :: {error}");
        }

        NUnit.Framework.TestContext.Out.WriteLine(report.ToString());
        Assert.Pass($"{healthy}/{components.Count} healthy — see output for the inventory.");
    }

    private sealed class ThrowingTraceListener : TraceListener
    {
        public override void Fail(string? message)
            => throw new InvalidOperationException($"Debug.Assert failed: {message}");

        public override void Fail(string? message, string? detailMessage)
            => throw new InvalidOperationException($"Debug.Assert failed: {message} {detailMessage}");

        public override void Write(string? message)
        {
        }

        public override void WriteLine(string? message)
        {
        }
    }

    /// <summary>Names the closing actually rendered — bare for a non-generic, closed for a generic.</summary>
    private static string SweptName(string displayName, Type type)
        => type.IsConstructedGenericType
            ? $"{displayName}<{string.Join(", ", type.GetGenericArguments().Select(FriendlyArgumentName))}>"
            : displayName;

    private static string FriendlyArgumentName(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            return FriendlyArgumentName(underlying) + "?";
        }

        return type switch
        {
            _ when type == typeof(bool) => "bool",
            _ when type == typeof(int) => "int",
            _ when type == typeof(long) => "long",
            _ when type == typeof(short) => "short",
            _ when type == typeof(byte) => "byte",
            _ when type == typeof(double) => "double",
            _ when type == typeof(float) => "float",
            _ when type == typeof(decimal) => "decimal",
            _ when type == typeof(string) => "string",
            _ => type.Name,
        };
    }

    private static Exception Root(Exception exception)
    {
        while (exception.InnerException is { } inner)
        {
            exception = inner;
        }

        return exception;
    }

    private static string FirstLine(string text)
    {
        var line = text.AsSpan().Trim();
        var newline = line.IndexOf('\n');
        return (newline < 0 ? line : line[..newline]).ToString();
    }
}
