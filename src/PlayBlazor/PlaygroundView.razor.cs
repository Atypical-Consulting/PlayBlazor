using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using PlayBlazor.CodeGen;
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

    [Inject]
    private IJSRuntime Js { get; set; } = default!;

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

    private string Snippet => RazorSnippetGenerator.Generate(_descriptor, _state);

    private async Task CopySnippet()
        => await Js.InvokeVoidAsync("navigator.clipboard.writeText", Snippet);

    private void RecoverFromError()
    {
        _state.ResetAll();
        _errorBoundary?.Recover();
    }
}
