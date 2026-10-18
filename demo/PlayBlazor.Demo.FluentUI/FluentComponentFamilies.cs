namespace PlayBlazor.Demo.FluentUI;

/// <summary>
/// The listed Fluent UI surface by family: the same ten families as the MudBlazor and DaisyBlazor
/// showcases, so the explorer rail and the home wall group every library alike.
/// </summary>
public static class FluentComponentFamilies
{
    private static readonly Dictionary<string, string> ByName = new (string Family, string[] Names)[]
    {
        ("Actions", ["FluentButton", "FluentAnchorButton", "FluentCompoundButton", "FluentMenuButton", "FluentSplitButton", "FluentToggleButton", "FluentMenu", "FluentMenuItem", "FluentMenuList"]),
        ("Inputs", ["FluentAutocomplete", "FluentCalendar", "FluentColorPicker", "FluentColorPickerInput", "FluentCombobox", "FluentDatePicker", "FluentField", "FluentInputFile", "FluentKeyCode", "FluentListbox", "FluentNumberInput", "FluentOption", "FluentSelect", "FluentSlider", "FluentTextArea", "FluentTextInput", "FluentTimePicker", "FluentValidationMessage", "FluentValidationSummary"]),
        ("Choices", ["FluentCheckbox", "FluentRadio", "FluentRadioGroup", "FluentRatingDisplay", "FluentSwitch"]),
        ("Data", ["FluentDataGrid", "FluentDataGridCell", "FluentDataGridRow", "PropertyColumn", "TemplateColumn", "SelectColumn", "HierarchicalSelectColumn", "FluentPaginator", "FluentSortableList", "FluentTreeView", "FluentTreeItem"]),
        ("Feedback", ["FluentBadge", "FluentCounterBadge", "FluentPresenceBadge", "FluentErrorBoundary", "FluentMessageBar", "FluentProgress", "FluentProgressBar", "FluentProgressRing", "FluentSkeleton", "FluentSpinner", "FluentToast", "FluentTooltip"]),
        ("Overlays", ["FluentAccordion", "FluentAccordionItem", "FluentDialog", "FluentDialogBody", "FluentMessageBox", "FluentOverlay", "FluentPopover"]),
        ("Layout", ["FluentCard", "FluentDivider", "FluentDragContainer", "FluentDropZone", "FluentGrid", "FluentGridItem", "FluentLayout", "FluentLayoutHamburger", "FluentLayoutItem", "FluentMultiSplitter", "FluentMultiSplitterPane", "FluentOverflow", "FluentPullToRefresh", "FluentSpacer", "FluentStack"]),
        ("Navigation", ["FluentAppBar", "FluentAppBarItem", "FluentLink", "FluentNav", "FluentNavCategory", "FluentNavItem", "FluentNavSectionHeader", "FluentTab", "FluentTabs", "FluentWizard", "FluentWizardStep", "FluentWizardStepValidator"]),
        ("Media", ["FluentAvatar", "FluentImage"]),
        ("Text", ["FluentHighlighter", "FluentLabel", "FluentLabelInfo", "FluentText"]),
    }.SelectMany(static f => f.Names.Select(n => (n, f.Family))).ToDictionary(static p => p.n, static p => p.Family, StringComparer.Ordinal);

    /// <summary>The family a component belongs to, for <see cref="PlayBlazorOptions.CategoryResolver"/>.</summary>
    /// <param name="componentType">A discovered component type (open or closed generic).</param>
    /// <returns>The family label, or null when it belongs to none (it lists under "Other").</returns>
    public static string? FamilyOf(Type componentType)
    {
        var backtick = componentType.Name.IndexOf('`');
        return ByName.GetValueOrDefault(backtick < 0 ? componentType.Name : componentType.Name[..backtick]);
    }
}
