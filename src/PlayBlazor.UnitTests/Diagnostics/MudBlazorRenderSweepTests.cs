using System.Diagnostics;
using System.Text;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NUnit.Framework;
using PlayBlazor.Discovery;

namespace PlayBlazor.UnitTests.Diagnostics;

/// <summary>
/// Diagnostic sweep: renders every discovered MudBlazor component through PlaygroundView
/// under explorer-like conditions (Mud services + providers, loose JS interop) and reports
/// every component that surfaces an error — either contained by the error boundary or
/// escaping the render. The test never fails; its output is the inventory used to drive fixes.
/// </summary>
public class MudBlazorRenderSweepTests
{
    [Test]
    [Explicit("Diagnostic inventory — run on demand; prints a report instead of asserting.")]
    public async Task RenderSweep_ReportsEveryComponentError()
    {
        // Debug.Assert in a component lifecycle (e.g. FooterCell asserting its parent DataGrid)
        // TERMINATES the process in Debug builds — convert asserts into catchable exceptions
        // so the sweep survives and reports them as the worst offenders they are.
        var originalListeners = Trace.Listeners.Cast<TraceListener>().ToArray();
        Trace.Listeners.Clear();
        Trace.Listeners.Add(new ThrowingTraceListener());
        try
        {
            await RunSweepAsync();
        }
        finally
        {
            Trace.Listeners.Clear();
            Trace.Listeners.AddRange(originalListeners);
        }
    }

    private static async Task RunSweepAsync()
    {
        var catalog = new ReflectionCatalogProvider();
        var components = catalog.Discover(typeof(MudButton).Assembly);
        var contained = new List<(string Name, string Error)>();
        var escaped = new List<(string Name, string Error)>();
        var healthy = 0;

        foreach (var component in components)
        {
            await using var context = new BunitContext();
            context.JSInterop.Mode = JSRuntimeMode.Loose;
            context.Services.AddMudServices();
            context.Services.AddPlayBlazor();

            try
            {
                var cut = context.Render(builder =>
                {
                    builder.OpenComponent<MudPopoverProvider>(0);
                    builder.CloseComponent();
                    builder.OpenComponent<PlaygroundView>(1);
                    builder.AddComponentParameter(2, nameof(PlaygroundView.Component), component.Type);
                    builder.CloseComponent();
                });

                var errors = cut.FindAll(".pb-error pre");
                if (errors.Count > 0)
                {
                    contained.Add((component.DisplayName, FirstLine(errors[0].TextContent)));
                }
                else
                {
                    healthy++;
                }
            }
            catch (Exception exception)
            {
                var root = Root(exception);
                escaped.Add((component.DisplayName, $"{root.GetType().Name}: {FirstLine(root.Message)}"));
            }
        }

        var report = new StringBuilder();
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
