namespace PlayBlazor.Model;

/// <summary>Describes one discovered component and its drivable parameters.</summary>
public sealed record ComponentDescriptor(
    Type Type,
    string DisplayName,
    string Category,
    string? Summary,
    IReadOnlyList<ParameterDescriptor> Parameters,
    string? Warning,
    bool CanInstantiate);
