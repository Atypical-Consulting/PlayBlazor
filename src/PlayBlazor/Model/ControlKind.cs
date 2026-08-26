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
