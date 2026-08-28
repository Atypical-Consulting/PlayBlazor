namespace PlayBlazor.Model;

/// <summary>The kind of UI control used to drive one component parameter.</summary>
public enum ControlKind
{
    Bool,
    Enum,
    /// <summary>Strings — plus char, MarkupString and string/numeric arrays (CSV-edited).</summary>
    Text,
    Number,
    /// <summary>Library color types recognized structurally (R/G/B properties + string constructor).</summary>
    Color,
    /// <summary>Reserved for host-registered rich mappers (icon pickers…).</summary>
    Icon,
    /// <summary>DateTime / DateOnly.</summary>
    Date,
    /// <summary>TimeSpan / TimeOnly.</summary>
    Time,
    Slot,
    Event,
    Unsupported,
}
