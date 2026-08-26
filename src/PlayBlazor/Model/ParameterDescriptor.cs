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
