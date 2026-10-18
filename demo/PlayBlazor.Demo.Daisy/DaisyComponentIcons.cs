namespace PlayBlazor.Demo.Daisy;

/// <summary>
/// One glyph per component family for the landing tiles and the explorer list.
/// </summary>
/// <remarks>
/// <see cref="PlayBlazorOptions.IconResolver"/> wants SVG markup (a 24×24 viewBox), but DaisyBlazor's
/// icons are Material Symbols ligature names with no SVG behind them, so these few shapes are
/// authored here. The family is what helps a visitor scan 120-odd names; a glyph per component
/// would be noise. Unlisted names get no icon.
/// </remarks>
public static class DaisyComponentIcons
{
    private const string Action = """<rect x="2" y="7" width="20" height="10" rx="5"/>""";
    private const string Input = """<path fill-rule="evenodd" d="M2 6h20v12H2zm2 2v8h16V8z"/><rect x="6" y="10" width="2" height="4"/>""";
    private const string Choice = """<path fill-rule="evenodd" d="M3 3h18v18H3zm2 2v14h14V5z"/><path d="M10 15.4 6.6 12 8 10.6l2 2 6-6L17.4 8z"/>""";
    private const string Data = """<path fill-rule="evenodd" d="M3 4h18v16H3zm2 6v3h6v-3zm8 0v3h6v-3zm-8 5v3h6v-3zm8 0v3h6v-3z"/>""";
    private const string Feedback = """<path fill-rule="evenodd" d="M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20zm-1 5h2v6h-2zm0 8h2v2h-2z"/>""";
    private const string Overlay = """<path d="M7 3h14v14h-2V5H7z"/><path fill-rule="evenodd" d="M3 7h14v14H3zm2 4v8h10v-8z"/>""";
    private const string Layout = """<path d="M3 3h8v8H3zm10 0h8v5h-8zm0 7h8v11h-8zM3 13h8v8H3z"/>""";
    private const string Navigation = """<path d="M3 11h12.2l-4.6-4.6L12 5l7 7-7 7-1.4-1.4 4.6-4.6H3z"/>""";
    private const string Media = """<path fill-rule="evenodd" d="M3 4h18v16H3zm2 2v10.6l4-4 3 3 2-2 5 5V6zm11 1.5a2 2 0 1 1 0 4 2 2 0 0 1 0-4z"/>""";
    private const string Text = """<path d="M4 4h16v4h-2V6h-5v12h2v2H9v-2h2V6H6v2H4z"/>""";

    private static readonly Dictionary<string, string> Families = Build(
        (Action, ["Button", "ButtonGroup", "IconButton", "Fab", "FabAction", "QuickAccessButton", "Swap", "ThemeController", "Dropdown", "Menu", "MenuItem"]),
        (Input, ["TextField", "NumericField", "Field", "FieldError", "FloatingLabel", "Autocomplete", "Select", "SelectItem", "DatePicker", "ColorPicker", "FileInput", "Range", "Form", "Validator", "ValidatorHint", "ServerValidation", "FilterPanel"]),
        (Choice, ["Checkbox", "Radio", "RadioGroup", "Switch", "Rating", "ToggleGroup", "ToggleItem"]),
        (Data, ["Table", "Td", "Th", "Tr", "SimpleTable", "TablePager", "DataGrid", "DataGridPager", "DataTable", "PropertyColumn", "TemplateColumn", "List", "ListItem", "ListRow", "ItemList", "Item", "Stat", "Stats", "KpiCard", "Status", "StatusChip", "DetailRow"]),
        (Feedback, ["Alert", "ErrorAlert", "Badge", "Indicator", "Loading", "ProgressCircular", "ProgressLinear", "RadialProgress", "Skeleton", "Countdown", "Tooltip", "EmptyState", "AccessDenied"]),
        (Overlay, ["Dialog", "ConfirmDialog", "MessageBoxDialog", "Modal", "Drawer", "StackOverlay", "ExpansionPanel", "ExpansionPanels"]),
        (Layout, ["Container", "Grid", "Stack", "Spacer", "Divider", "Card", "CardHeader", "CardContent", "CardActions", "Paper", "Hero", "Footer", "FooterTitle", "SectionCard", "DetailCard", "DetailLayout", "AppCard", "FeatureHero", "FeatureHomePage", "FeatureNavCard", "PageHeader", "Join", "MockupBrowser", "MockupCode", "MockupPhone", "MockupWindow"]),
        (Navigation, ["Breadcrumbs", "Pagination", "Steps", "Step", "Tabs", "Tab", "TextLink", "Timeline", "TimelineItem", "Navbar", "NavMenuLink", "Dock", "DockItem"]),
        (Media, ["Image", "Avatar", "Carousel", "CarouselItem", "Mask", "Diff", "Icon"]),
        (Text, ["Typography", "Kbd", "ChatBubble", "Chip"]));

    /// <summary>The family glyph for <paramref name="type"/>, or null when it belongs to none.</summary>
    /// <param name="type">A discovered component type (open or closed generic).</param>
    /// <returns>The inner SVG markup of a 24×24 viewBox, or null.</returns>
    public static string? Resolve(Type type)
    {
        var name = type.Name;
        var backtick = name.IndexOf('`');
        return Families.GetValueOrDefault(backtick < 0 ? name : name[..backtick]);
    }

    private static Dictionary<string, string> Build(params (string Glyph, string[] Names)[] families)
        => families
            .SelectMany(static f => f.Names.Select(n => (n, f.Glyph)))
            .ToDictionary(static p => p.n, static p => p.Glyph, StringComparer.Ordinal);
}
