namespace PlayBlazor.Model;

/// <summary>The kind of UI control used to drive one component parameter.</summary>
public enum ControlKind
{
    /// <summary>Booleans — rendered as a toggle.</summary>
    Bool,
    /// <summary>Enum types — rendered as a select of the declared members.</summary>
    Enum,
    /// <summary>Strings — plus char, MarkupString and string/numeric arrays (CSV-edited).</summary>
    Text,
    /// <summary>Integral and floating-point numbers — rendered as a numeric input.</summary>
    Number,
    /// <summary>Library color types recognized structurally (R/G/B properties + string constructor).</summary>
    Color,
    /// <summary>Reserved for host-registered rich mappers (icon pickers…).</summary>
    Icon,
    /// <summary>DateTime / DateOnly.</summary>
    Date,
    /// <summary>TimeSpan / TimeOnly.</summary>
    Time,
    /// <summary><see cref="Microsoft.AspNetCore.Components.RenderFragment"/> parameters — filled with editable sample content.</summary>
    Slot,
    /// <summary><see cref="Microsoft.AspNetCore.Components.EventCallback"/> parameters — intercepted and written to the event log rather than driven.</summary>
    Event,
    /// <summary>No control maps to this parameter's type; a host preset can still supply a value.</summary>
    Unsupported,
}
