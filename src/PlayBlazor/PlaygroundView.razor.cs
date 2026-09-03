using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PlayBlazor.CodeGen;
using PlayBlazor.Discovery;
using PlayBlazor.Model;
using PlayBlazor.Rendering;
using PlayBlazor.State;

namespace PlayBlazor;

/// <summary>An auto-generated playground for a single component type.</summary>
public partial class PlaygroundView : ComponentBase, IDisposable
{
    private readonly PlaygroundState _state = new();
    private readonly PlaygroundEventLog _eventLog = new();
    private readonly PlaygroundEnvironment _environment = new();
    private ComponentDescriptor _descriptor = default!;
    private ErrorBoundary? _errorBoundary;
    private string? _activeVariant;

    [Inject]
    private IComponentCatalogProvider Catalog { get; set; } = default!;

    [Inject]
    private IJSRuntime Js { get; set; } = default!;

    [Inject]
    private PlayBlazorOptions Options { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// The component to play. Must be a closed type — <c>typeof(MudSelect&lt;string&gt;)</c>, never an
    /// open generic. Assigning a different type resets the state and reloads from the permalink.
    /// </summary>
    [Parameter, EditorRequired]
    public Type Component { get; set; } = default!;

    private static bool IsTextSlot(ParameterDescriptor parameter)
        => parameter.Kind == ControlKind.Slot && parameter.Type == typeof(RenderFragment);

    private IEnumerable<ParameterDescriptor> Controllable
        => _descriptor.Parameters.Where(static p => p.Kind
            is ControlKind.Bool or ControlKind.Enum or ControlKind.Text or ControlKind.Number
                or ControlKind.Date or ControlKind.Time or ControlKind.Color or ControlKind.Icon
            || IsTextSlot(p));

    private IEnumerable<ParameterDescriptor> Uncontrollable
        => _descriptor.Parameters.Where(static p =>
            (p.Kind is ControlKind.Slot && !IsTextSlot(p)) || p.Kind is ControlKind.Unsupported);

    private IEnumerable<ParameterDescriptor> Events
        => _descriptor.Parameters.Where(static p => p.Kind is ControlKind.Event);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _state.Changed += OnStateChanged;
        _eventLog.Changed += OnStateChanged;
        _eventLog.Recorded += OnSpecimenEvent;
        if (Options.GuardDebugAsserts)
        {
            DebugAssertGuard.Install();
        }
    }

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

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (_descriptor?.Type != Component)
        {
            _descriptor = Catalog.Describe(Component);
            _state.ResetAll();
            _activeVariant = null;
            // A previous specimen's failure must not poison the next one.
            _errorBoundary?.Recover();
            RestoreFromPermalink();
        }
    }

    private string PermalinkParameterName => $"pb-{_descriptor.DisplayName}";

    private void RestoreFromPermalink()
    {
        var query = new Uri(Navigation.Uri).Query.TrimStart('?');
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = Uri.UnescapeDataString(pair[..separator]);
            if (key != PermalinkParameterName)
            {
                continue;
            }

            PlaygroundStateSerializer.Decode(
                Uri.UnescapeDataString(pair[(separator + 1)..]), _descriptor, _state, _environment);
            return;
        }
    }

    private async Task CopyShareLink()
    {
        var encoded = PlaygroundStateSerializer.Encode(_descriptor, _state, _environment);
        var uri = Navigation.GetUriWithQueryParameter(PermalinkParameterName, encoded);
        await Js.InvokeVoidAsync("navigator.clipboard.writeText", uri);
    }

    /// <summary>Unsubscribes from the playground state and event log.</summary>
    public void Dispose()
    {
        _state.Changed -= OnStateChanged;
        _eventLog.Changed -= OnStateChanged;
        _eventLog.Recorded -= OnSpecimenEvent;
    }

    private void OnSpecimenEvent(string name, object? payload)
    {
        if (SpecimenStateSync.TryApply(_descriptor, _state, name, payload))
        {
            _activeVariant = null;
        }
    }

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

        // Changing an input is the natural way to retry a specimen that crashed.
        _activeVariant = null;
        _errorBoundary?.Recover();
    }

    private void ResetAll()
    {
        _state.ResetAll();
        _activeVariant = null;
        _errorBoundary?.Recover();
    }

    private IReadOnlyList<PlaygroundVariantDefinition> VariantDefinitions
        => Options.GetVariants(_descriptor.Type);

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

    private void OnViewportChanged(ChangeEventArgs e)
        => _environment.ViewportWidth = int.TryParse((string?)e.Value, out var width) ? width : null;

    /// <summary>Resolution order: user modification &gt; host preset &gt; component default.</summary>
    private object? EffectiveValue(ParameterDescriptor parameter)
    {
        if (_state.IsModified(parameter.Name))
        {
            return _state.GetValue(parameter);
        }

        return Options.TryGetParameterPreset(_descriptor.Type, parameter.Name, out var preset)
            ? preset
            : parameter.DefaultValue;
    }

    private string Snippet => RazorSnippetGenerator.Generate(_descriptor, _state, Options);

    private MarkupString SnippetMarkup => RazorSnippetGenerator.GenerateMarkup(_descriptor, _state, Options);

    private async Task CopySnippet()
        => await Js.InvokeVoidAsync("navigator.clipboard.writeText", Snippet);

    private void RecoverFromError()
    {
        _state.ResetAll();
        _errorBoundary?.Recover();
    }
}
