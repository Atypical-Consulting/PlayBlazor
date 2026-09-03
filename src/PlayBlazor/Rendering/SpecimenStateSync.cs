using PlayBlazor.Model;
using PlayBlazor.State;

namespace PlayBlazor.Rendering;

/// <summary>
/// Interacting with the specimen must not lie to the bench: an intercepted
/// <c>XxxChanged</c> event whose payload fits the drivable <c>Xxx</c> parameter flows back
/// into the playground state — the control, the modified marker and the razor follow, and
/// two-way-bound components stop snapping back to their pre-interaction value.
/// </summary>
public static class SpecimenStateSync
{
    private const string Suffix = "Changed";

    /// <summary>Writes an intercepted change event back into the playground state, when it matches a drivable parameter.</summary>
    /// <param name="component">The played component's descriptor.</param>
    /// <param name="state">The state to update.</param>
    /// <param name="eventName">The intercepted callback name; only <c>XxxChanged</c> is considered.</param>
    /// <param name="payload">The callback argument, which must fit the <c>Xxx</c> parameter's type.</param>
    /// <returns>
    /// <c>true</c> when the state was updated. A value equal to the default on an untouched parameter
    /// is deliberately not recorded, so merely rendering does not mark the bench as modified.
    /// </returns>
    public static bool TryApply(ComponentDescriptor component, PlaygroundState state, string eventName, object? payload)
    {
        if (!eventName.EndsWith(Suffix, StringComparison.Ordinal)
            || eventName.Length == Suffix.Length
            || payload is null)
        {
            return false;
        }

        var target = eventName[..^Suffix.Length];
        var parameter = component.Parameters.FirstOrDefault(p => p.Name == target);
        if (parameter is null || !IsSyncable(parameter))
        {
            return false;
        }

        var type = Nullable.GetUnderlyingType(parameter.Type) ?? parameter.Type;
        if (!type.IsInstanceOfType(payload))
        {
            return false;
        }

        // A pristine parameter reporting its own default (OpenChanged(false)…) is not a
        // modification; once the user or a previous event touched it, every change counts.
        if (!state.IsModified(target) && parameter.HasDefault && Equals(parameter.DefaultValue, payload))
        {
            return false;
        }

        state.Set(target, payload);
        return true;
    }

    private static bool IsSyncable(ParameterDescriptor parameter)
        => parameter.Kind is ControlKind.Bool or ControlKind.Enum or ControlKind.Text
            or ControlKind.Number or ControlKind.Date or ControlKind.Time or ControlKind.Color or ControlKind.Icon;
}
