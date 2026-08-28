using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PlayBlazor.CodeGen;
using PlayBlazor.Discovery;
using PlayBlazor.Model;
using PlayBlazor.Rendering;
using PlayBlazor.State;

namespace PlayBlazor.Shell.Workspace;

/// <summary>
/// The mini-IDE shell (Concept G v2): a specimen stage surrounded by four dockable panels —
/// Graph, Parameters, Razor, Signals — with a Present mode for demos.
/// </summary>
public partial class PlaygroundWorkspace : ComponentBase, IDisposable
{
    internal sealed record PanelDef(string Id, string Title);

    internal const string GraphId = "graph";
    internal const string ParametersId = "parameters";
    internal const string RazorId = "razor";
    internal const string SignalsId = "signals";

    internal static readonly PanelDef[] Panels =
    [
        new(GraphId, "Graph"),
        new(ParametersId, "Parameters"),
        new(RazorId, "Razor"),
        new(SignalsId, "Signals"),
    ];

    private readonly PlaygroundState _state = new();
    private readonly PlaygroundEventLog _eventLog = new();
    private readonly PlaygroundEnvironment _environment = new();
    private readonly WorkspaceLayout _layout = new();
    private readonly HashSet<string> _collapsed = new(StringComparer.Ordinal);
    private readonly Dictionary<string, bool> _groupOpen = new(StringComparer.Ordinal);
    private readonly HashSet<PlaygroundEventLog.Entry> _unfolded = [];
    private readonly CancellationTokenSource _disposeCts = new();

    private IReadOnlyList<ComponentDescriptor> _components = [];
    private ComponentDescriptor? _selected;
    private ErrorBoundary? _errorBoundary;
    private string? _activeVariant;
    private string _paramFilter = string.Empty;
    private bool _modifiedOnly;
    private bool _allFolded;
    private string? _toast;
    private CancellationTokenSource? _toastCts;
    private bool _present;
    private bool _autoPlaying;
    private int _autoIndex;
    private CancellationTokenSource? _autoCts;

    [Inject]
    private IComponentCatalogProvider Catalog { get; set; } = default!;

    [Inject]
    private PlayBlazorOptions Options { get; set; } = default!;

    [Inject]
    private IJSRuntime Js { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Parameter, EditorRequired]
    public IReadOnlyList<Assembly> Assemblies { get; set; } = default!;

    protected override void OnInitialized()
    {
        _state.Changed += OnBenchChanged;
        _eventLog.Changed += OnBenchChanged;
        _layout.Changed += OnBenchChanged;
        if (Options.GuardDebugAsserts)
        {
            DebugAssertGuard.Install();
        }
    }

    protected override void OnParametersSet()
    {
        _components = Assemblies
            .SelectMany(assembly => Catalog.Discover(assembly))
            .Where(c => !Options.IsExcluded(c.Type) && (Options.ComponentFilter?.Invoke(c.Type) ?? true))
            .OrderBy(static c => c.DisplayName, StringComparer.Ordinal)
            .ToArray();
        if (_selected is null && _components.Count > 0)
        {
            SelectComponent(FindPermalinkTarget() ?? _components[0], restorePermalink: true);
        }
    }

    public void Dispose()
    {
        _state.Changed -= OnBenchChanged;
        _eventLog.Changed -= OnBenchChanged;
        _layout.Changed -= OnBenchChanged;
        _disposeCts.Cancel();
        _disposeCts.Dispose();
    }

    private void OnBenchChanged()
        => _ = InvokeAsync(StateHasChanged);

    /* ── Component selection ─────────────────────────── */

    private IEnumerable<IGrouping<string, ComponentDescriptor>> PickerGroups
        => _components.GroupBy(static c => c.Category).OrderBy(static g => g.Key, StringComparer.Ordinal);

    private void OnPickerChanged(ChangeEventArgs e)
    {
        if (_components.FirstOrDefault(c => c.DisplayName == (string?)e.Value) is { } match)
        {
            SelectComponent(match);
        }
    }

    private void SelectComponent(ComponentDescriptor descriptor, bool restorePermalink = false)
    {
        _selected = descriptor;
        _state.ResetAll();
        _eventLog.Clear();
        _unfolded.Clear();
        _activeVariant = null;
        _paramFilter = string.Empty;
        _modifiedOnly = false;
        _allFolded = false;
        _groupOpen.Clear();
        // A previous specimen's failure must not poison the next one.
        _errorBoundary?.Recover();
        if (restorePermalink)
        {
            RestoreFromPermalink();
        }
    }

    private string PermalinkParameterName => $"pb-{_selected!.DisplayName}";

    private ComponentDescriptor? FindPermalinkTarget()
    {
        foreach (var (key, _) in QueryPairs())
        {
            if (key.StartsWith("pb-", StringComparison.Ordinal)
                && _components.FirstOrDefault(c => c.DisplayName == key["pb-".Length..]) is { } match)
            {
                return match;
            }
        }

        return null;
    }

    private void RestoreFromPermalink()
    {
        foreach (var (key, value) in QueryPairs())
        {
            if (key == PermalinkParameterName)
            {
                PlaygroundStateSerializer.Decode(value, _selected!, _state, _environment);
                return;
            }
        }
    }

    private IEnumerable<(string Key, string Value)> QueryPairs()
    {
        var query = new Uri(Navigation.Uri).Query.TrimStart('?');
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            yield return (
                Uri.UnescapeDataString(pair[..separator]),
                Uri.UnescapeDataString(pair[(separator + 1)..]));
        }
    }

    /* ── Parameters panel ────────────────────────────── */

    private static bool IsTextSlot(ParameterDescriptor parameter)
        => parameter.Kind == ControlKind.Slot && parameter.Type == typeof(RenderFragment);

    private static bool IsDrivable(ParameterDescriptor parameter)
        => parameter.Kind is ControlKind.Bool or ControlKind.Enum or ControlKind.Text or ControlKind.Number
           || IsTextSlot(parameter);

    private IEnumerable<IGrouping<string, ParameterDescriptor>> ParameterGroups
        => _selected!.Parameters
            .OrderBy(static p => p.GroupOrder)
            .ThenBy(static p => p.Group, StringComparer.Ordinal)
            .GroupBy(static p => p.Group);

    private IReadOnlyList<ParameterDescriptor> VisibleRows(IEnumerable<ParameterDescriptor> group)
        => group
            .Where(p => _paramFilter.Length == 0
                        || p.Name.Contains(_paramFilter, StringComparison.OrdinalIgnoreCase))
            .Where(p => !_modifiedOnly || _state.IsModified(p.Name))
            .ToArray();

    private int ModifiedCount => _selected!.Parameters.Count(p => _state.IsModified(p.Name));

    private bool IsGroupOpen(string group)
        => _groupOpen.TryGetValue(group, out var open) ? open : !_allFolded;

    private void ToggleGroup(string group)
        => _groupOpen[group] = !IsGroupOpen(group);

    private void ToggleFoldAll()
    {
        _allFolded = !_allFolded;
        _groupOpen.Clear();
    }

    /// <summary>Resolution order: user modification &gt; host preset &gt; component default.</summary>
    private object? EffectiveValue(ParameterDescriptor parameter)
    {
        if (_state.IsModified(parameter.Name))
        {
            return _state.GetValue(parameter);
        }

        return Options.TryGetParameterPreset(_selected!.Type, parameter.Name, out var preset)
            ? preset
            : parameter.DefaultValue;
    }

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

        // Changing an input is the natural way to retry a specimen that crashed.
        _activeVariant = null;
        _errorBoundary?.Recover();
    }

    private void ResetNode()
    {
        _state.ResetAll();
        _activeVariant = null;
        _errorBoundary?.Recover();
        ShowToast("Parameters reset ✓");
    }

    /* ── Razor / share ───────────────────────────────── */

    private MarkupString SnippetMarkup => RazorSnippetGenerator.GenerateMarkup(_selected!, _state);

    private async Task CopyRazor()
    {
        await Js.InvokeVoidAsync("navigator.clipboard.writeText", RazorSnippetGenerator.Generate(_selected!, _state));
        ShowToast("Razor copied ✓");
    }

    private async Task CopyShareLink()
    {
        var encoded = PlaygroundStateSerializer.Encode(_selected!, _state, _environment);
        var uri = Navigation.GetUriWithQueryParameter(PermalinkParameterName, encoded);
        await Js.InvokeVoidAsync("navigator.clipboard.writeText", uri);
        ShowToast("Link copied ✓");
    }

    private void ShowToast(string message)
    {
        _toastCts?.Cancel();
        var cts = _toastCts = CancellationTokenSource.CreateLinkedTokenSource(_disposeCts.Token);
        _toast = message;
        StateHasChanged();
        _ = HideToastLater(cts.Token);
    }

    private async Task HideToastLater(CancellationToken token)
    {
        try
        {
            await Task.Delay(1600, token);
            await InvokeAsync(() =>
            {
                _toast = null;
                StateHasChanged();
            });
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer toast, or the workspace is going away.
        }
    }

    /* ── Signals panel ───────────────────────────────── */

    private void ToggleUnfold(PlaygroundEventLog.Entry entry)
    {
        if (!_unfolded.Remove(entry))
        {
            _unfolded.Add(entry);
        }
    }

    private IEnumerable<ParameterDescriptor> EventParameters
        => _selected!.Parameters.Where(static p => p.Kind is ControlKind.Event);

    /* ── Graph panel ─────────────────────────────────── */

    internal sealed record GraphNode(string Label, ComponentDescriptor? Target, bool Selected, int Depth);

    private IReadOnlyList<GraphNode> GraphNodes()
    {
        var nodes = new List<GraphNode>();
        var depth = 0;
        if (Options.TryGetScaffold(_selected!.Type, out _))
        {
            nodes.Add(new GraphNode("host scaffold", null, false, depth++));
        }

        nodes.Add(new GraphNode(_selected.DisplayName, _selected, true, depth));
        foreach (var relatedType in Options.GetRelated(_selected.Type))
        {
            if (_components.FirstOrDefault(c => c.Type == relatedType) is { } related)
            {
                nodes.Add(new GraphNode(related.DisplayName, related, false, depth + 1));
            }
        }

        return nodes;
    }

    /* ── Layout / panels ─────────────────────────────── */

    private void TogglePanel(string id)
        => _layout.ToggleHidden(id);

    private void ToggleCollapse(string id)
    {
        if (!_collapsed.Remove(id))
        {
            _collapsed.Add(id);
        }
    }

    private void ResetLayout()
        => _layout.Reset();

    private bool ZoneEmpty(string zone)
        => _layout.Zone(zone).All(id => _layout.IsHidden(id));

    /* ── Variants / present ──────────────────────────── */

    private IReadOnlyList<PlaygroundVariantDefinition> Variants
        => Options.GetVariants(_selected!.Type);

    private void ApplyVariant(PlaygroundVariantDefinition variant)
    {
        _state.ResetAll();
        foreach (var (name, value) in variant.Values)
        {
            if (value is not null)
            {
                _state.Set(name, value);
            }
        }

        _activeVariant = variant.Name;
        _errorBoundary?.Recover();
    }

    private void SetPresent(bool present)
    {
        _present = present;
        if (present && Variants.Count > 0)
        {
            StartAutoplay();
        }
        else
        {
            StopAutoplay();
        }
    }

    private void StartAutoplay()
    {
        StopAutoplay();
        _autoPlaying = true;
        _autoIndex = 0;
        ApplyVariant(Variants[0]);
        var cts = _autoCts = CancellationTokenSource.CreateLinkedTokenSource(_disposeCts.Token);
        _ = AutoplayLoop(cts.Token);
    }

    private async Task AutoplayLoop(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(4000, token);
                await InvokeAsync(() =>
                {
                    _autoIndex = (_autoIndex + 1) % Variants.Count;
                    ApplyVariant(Variants[_autoIndex]);
                    StateHasChanged();
                });
            }
        }
        catch (OperationCanceledException)
        {
            // Autoplay stopped (manual pick, mode exit, or dispose).
        }
    }

    private void StopAutoplay()
    {
        _autoCts?.Cancel();
        _autoPlaying = false;
    }

    private void ToggleAutoplay()
    {
        if (_autoPlaying)
        {
            StopAutoplay();
        }
        else if (Variants.Count > 0)
        {
            StartAutoplay();
        }
    }

    private void PickVariantManually(PlaygroundVariantDefinition variant)
    {
        StopAutoplay();
        ApplyVariant(variant);
    }

    /* ── Stage (mirrors PlaygroundView) ───────────────── */

    private static bool LooksLikeMissingParent(Exception exception)
    {
        if (exception is NullReferenceException)
        {
            return true;
        }

        var message = exception.Message;
        return message.StartsWith("Debug.Assert failed", StringComparison.Ordinal)
               || message.Contains("must be used", StringComparison.OrdinalIgnoreCase)
               || message.Contains("must be placed", StringComparison.OrdinalIgnoreCase)
               || message.Contains("inside a", StringComparison.OrdinalIgnoreCase)
               || message.Contains("within a", StringComparison.OrdinalIgnoreCase);
    }

    private void RecoverFromError()
    {
        _state.ResetAll();
        _errorBoundary?.Recover();
    }

    private void OnViewportChanged(ChangeEventArgs e)
        => _environment.ViewportWidth = int.TryParse((string?)e.Value, out var width) ? width : null;
}
