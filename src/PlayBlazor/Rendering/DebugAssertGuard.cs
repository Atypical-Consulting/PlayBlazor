using System.Diagnostics;

namespace PlayBlazor.Rendering;

/// <summary>
/// A failed <see cref="Debug.Assert(bool)"/> inside a component lifecycle terminates the whole
/// process in Debug builds — no try/catch or ErrorBoundary can contain it (MudBlazor's DataGrid
/// cells assert their parent grid, for example). A playground renders components in unusual
/// contexts on purpose, so it converts assertion failures into ordinary exceptions the error
/// boundary can show. Release builds compile asserts out; this is then a no-op in production.
/// </summary>
public static class DebugAssertGuard
{
    private static bool _installed;

    /// <summary>
    /// Removes the process-killing default trace listener, once per process. Any listener the host
    /// registered itself is left in place. Calling it repeatedly is safe.
    /// </summary>
    public static void Install()
    {
        if (_installed)
        {
            return;
        }

        _installed = true;

        // Remove only the process-killing default listener; any host-registered listener stays.
        foreach (var listener in Trace.Listeners.OfType<DefaultTraceListener>().ToArray())
        {
            Trace.Listeners.Remove(listener);
        }

        Trace.Listeners.Add(new ThrowingListener());
    }

    private sealed class ThrowingListener : TraceListener
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
}
