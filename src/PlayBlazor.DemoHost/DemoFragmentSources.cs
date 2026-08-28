namespace PlayBlazor.DemoHost;

/// <summary>
/// The razor text of each demo fragment, verbatim — fed to the slot presets as their
/// <c>source</c> so the generated code panel shows copy-pasteable markup instead of a
/// placeholder comment. Keep each string in sync with its fragment in DemoFragments.razor.
/// </summary>
public static class DemoFragmentSources
{
    public const string SelectItems = """
<MudSelectItem T="string" Value="@("Espresso")">Espresso</MudSelectItem>
<MudSelectItem T="string" Value="@("Cappuccino")">Cappuccino</MudSelectItem>
<MudSelectItem T="string" Value="@("Flat white")">Flat white</MudSelectItem>
""";

    public const string ListItems = """
<MudListItem T="string" Icon="@Icons.Material.Filled.Inbox" Text="Inbox" />
<MudListItem T="string" Icon="@Icons.Material.Filled.Send" Text="Sent" />
<MudListItem T="string" Icon="@Icons.Material.Filled.Delete" Text="Trash" />
""";

    public const string Chips = """
<MudChip T="string" Color="Color.Primary">Blazor</MudChip>
<MudChip T="string" Color="Color.Secondary">WASM</MudChip>
<MudChip T="string" Color="Color.Info">.NET</MudChip>
""";

    public const string ButtonGroupButtons = """
<MudButton>One</MudButton>
<MudButton>Two</MudButton>
<MudButton>Three</MudButton>
""";

    public const string Avatars = """
<MudAvatar Color="Color.Primary">A</MudAvatar>
<MudAvatar Color="Color.Secondary">B</MudAvatar>
<MudAvatar Color="Color.Tertiary">C</MudAvatar>
""";

    public const string CardBody = """
<MudCardContent>
    <MudText Typo="Typo.h6">Old paint</MudText>
    <MudText Typo="Typo.body2">A study of layered paint on wood, found in a Copenhagen attic.</MudText>
</MudCardContent>
<MudCardActions>
    <MudButton Variant="Variant.Text" Color="Color.Primary">Learn more</MudButton>
</MudCardActions>
""";

    public const string TabPanels = """
<MudTabPanel Text="Discover">
    <MudText Class="pa-4">Reflection finds every parameter on its own.</MudText>
</MudTabPanel>
<MudTabPanel Text="Play">
    <MudText Class="pa-4">Every control on the right is generated.</MudText>
</MudTabPanel>
<MudTabPanel Text="Share">
    <MudText Class="pa-4">The URL carries the exact configuration.</MudText>
</MudTabPanel>
""";

    public const string ExpansionPanels = """
<MudExpansionPanel Text="How does discovery work?" Expanded="true">
    <MudText Typo="Typo.body2">By reflecting over [Parameter] properties.</MudText>
</MudExpansionPanel>
<MudExpansionPanel Text="Does it need story files?">
    <MudText Typo="Typo.body2">No — that is the whole point.</MudText>
</MudExpansionPanel>
""";

    public const string MenuItems = """
<MudMenuItem Icon="@Icons.Material.Filled.ContentCopy">Copy</MudMenuItem>
<MudMenuItem Icon="@Icons.Material.Filled.ContentPaste">Paste</MudMenuItem>
<MudMenuItem Icon="@Icons.Material.Filled.Delete">Delete</MudMenuItem>
""";

    public const string NavLinks = """
<MudNavLink Icon="@Icons.Material.Filled.Dashboard">Dashboard</MudNavLink>
<MudNavLink Icon="@Icons.Material.Filled.People">Team</MudNavLink>
<MudNavLink Icon="@Icons.Material.Filled.Settings">Settings</MudNavLink>
""";

    public const string Radios = """
<MudRadio T="string" Value="@("small")">Small</MudRadio>
<MudRadio T="string" Value="@("medium")">Medium</MudRadio>
<MudRadio T="string" Value="@("large")">Large</MudRadio>
""";

    public const string ToggleItems = """
<MudToggleItem T="string" Value="@("left")" Text="Left" />
<MudToggleItem T="string" Value="@("center")" Text="Center" />
<MudToggleItem T="string" Value="@("right")" Text="Right" />
""";

    public const string CarouselItems = """
<MudCarouselItem Color="Color.Primary">
    <div class="d-flex" style="height:100%"><MudText Class="ma-auto" Typo="Typo.h5">Slide one</MudText></div>
</MudCarouselItem>
<MudCarouselItem Color="Color.Secondary">
    <div class="d-flex" style="height:100%"><MudText Class="ma-auto" Typo="Typo.h5">Slide two</MudText></div>
</MudCarouselItem>
""";

    public const string TimelineItems = """
<MudTimelineItem Color="Color.Primary">
    <MudText Typo="Typo.body2">Discovered by reflection</MudText>
</MudTimelineItem>
<MudTimelineItem Color="Color.Secondary">
    <MudText Typo="Typo.body2">Driven by generated controls</MudText>
</MudTimelineItem>
<MudTimelineItem Color="Color.Tertiary">
    <MudText Typo="Typo.body2">Shared as a permalink</MudText>
</MudTimelineItem>
""";

    public const string TreeItems = """
<MudTreeViewItem T="string" Text="src" Expanded="true">
    <MudTreeViewItem T="string" Text="PlayBlazor" />
    <MudTreeViewItem T="string" Text="PlayBlazor.DemoHost" />
</MudTreeViewItem>
<MudTreeViewItem T="string" Text="docs" />
""";

    public const string Steps = """
<MudStep Title="Point at an assembly" />
<MudStep Title="Play with parameters" />
<MudStep Title="Copy the snippet" />
""";

    public const string SimpleTableContent = """
<thead>
    <tr><th>Name</th><th>Role</th><th>Age</th></tr>
</thead>
<tbody>
    <tr><td>Ada Lovelace</td><td>Engineering</td><td>36</td></tr>
    <tr><td>Grace Hopper</td><td>R&amp;D</td><td>85</td></tr>
    <tr><td>Katherine Johnson</td><td>Science</td><td>101</td></tr>
</tbody>
""";

    public const string ToolBarContent = """
<MudIconButton Icon="@Icons.Material.Filled.Menu" />
<MudText Typo="Typo.h6">Title</MudText>
<MudSpacer />
<MudIconButton Icon="@Icons.Material.Filled.MoreVert" />
""";

    public const string TooltipChild = """
<MudButton Variant="Variant.Filled" Color="Color.Primary">Hover me</MudButton>
""";

    public const string BadgeChild = """
<MudIcon Icon="@Icons.Material.Filled.Email" Color="Color.Default" />
""";

    public const string PaperChild = """
<MudText Class="pa-4">Elevation carries depth.</MudText>
""";

    public const string StackItems = """
<MudPaper Class="pa-3">Item 1</MudPaper>
<MudPaper Class="pa-3">Item 2</MudPaper>
<MudPaper Class="pa-3">Item 3</MudPaper>
""";

    public const string GridItems = """
<MudItem xs="4"><MudPaper Class="pa-4 mud-theme-primary">xs-4</MudPaper></MudItem>
<MudItem xs="4"><MudPaper Class="pa-4 mud-theme-secondary">xs-4</MudPaper></MudItem>
<MudItem xs="4"><MudPaper Class="pa-4 mud-theme-tertiary">xs-4</MudPaper></MudItem>
""";

    public const string FieldChild = """
<MudText>Read-only value inside a field</MudText>
""";

    public const string OverlayChild = """
<MudText Class="ma-auto" Style="color:white">Overlay content</MudText>
""";
}
