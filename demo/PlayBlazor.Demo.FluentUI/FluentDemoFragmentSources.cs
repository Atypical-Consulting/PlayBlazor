namespace PlayBlazor.Demo.FluentUI;

/// <summary>
/// The razor text of each demo fragment, verbatim — fed to the slot presets as their
/// <c>source</c> so the generated code panel shows copy-pasteable markup instead of a
/// placeholder comment. Keep each string in sync with its fragment in FluentDemoFragments.razor.
/// </summary>
public static class FluentDemoFragmentSources
{
    public const string RadioOptions = """
<FluentRadio Label="Apple" />
<FluentRadio Label="Banana" />
<FluentRadio Label="Orange" Disabled="true" />
<FluentRadio Label="Kiwi" />
""";

    public const string CardBody = """
<FluentBadge Appearance="BadgeAppearance.Tint" Color="BadgeColor.Brand">New</FluentBadge>
<FluentText Weight="TextWeight.Semibold" Block="true">Old paint</FluentText>
<FluentText Size="TextSize.Size200">A study of layered paint on wood, found in a Copenhagen attic.</FluentText>
""";

    public const string GridItems = """
<FluentGridItem Xs="4"><FluentCard>Column 1</FluentCard></FluentGridItem>
<FluentGridItem Xs="4"><FluentCard>Column 2</FluentCard></FluentGridItem>
<FluentGridItem Xs="4"><FluentCard>Column 3</FluentCard></FluentGridItem>
""";

    public const string LayoutAreas = """
<FluentLayoutItem Area="LayoutArea.Header">
    <FluentText Weight="TextWeight.Semibold" Block="true">Header</FluentText>
</FluentLayoutItem>
<FluentLayoutItem Area="LayoutArea.Navigation" Width="160px">
    <FluentText Block="true">Navigation</FluentText>
</FluentLayoutItem>
<FluentLayoutItem Area="LayoutArea.Content">
    <FluentText Block="true">Content</FluentText>
</FluentLayoutItem>
<FluentLayoutItem Area="LayoutArea.Footer">
    <FluentText Block="true">Footer</FluentText>
</FluentLayoutItem>
""";

    public const string StackItems = """
<FluentCard Width="120px">Item 1</FluentCard>
<FluentCard Width="120px">Item 2</FluentCard>
<FluentCard Width="120px">Item 3</FluentCard>
""";

    public const string SplitterPanes = """
<FluentMultiSplitterPane>
    <FluentText Block="true">Left pane</FluentText>
</FluentMultiSplitterPane>
<FluentMultiSplitterPane>
    <FluentText Block="true">Right pane</FluentText>
</FluentMultiSplitterPane>
""";

    // --- Navigation and overlays ---

    public const string NavContent = """
<FluentNavSectionHeader Title="General" />
<FluentNavItem>Dashboard</FluentNavItem>
<FluentNavItem>Reports</FluentNavItem>
<FluentNavCategory Title="Settings" Expanded="true">
    <FluentNavItem>Profile</FluentNavItem>
    <FluentNavItem>Security</FluentNavItem>
</FluentNavCategory>
""";

    public const string NavCategoryItems = """
<FluentNavItem>Profile</FluentNavItem>
<FluentNavItem>Security</FluentNavItem>
""";

    public const string TabsContent = """
<FluentTab Header="Home">
    <FluentText Block="true">Welcome back! Here's what's new today.</FluentText>
</FluentTab>
<FluentTab Header="Profile">
    <FluentText Block="true">Manage your account details and preferences.</FluentText>
</FluentTab>
<FluentTab Header="Settings">
    <FluentText Block="true">Configure notifications and privacy options.</FluentText>
</FluentTab>
""";

    public const string MenuContent = """
<FluentMenuButton>Options</FluentMenuButton>
<FluentMenuList>
    <FluentMenuItem Label="Cut" />
    <FluentMenuItem Label="Copy" />
    <FluentMenuItem Label="Paste" />
</FluentMenuList>
""";

    public const string MenuListItems = """
<FluentMenuItem Label="Sort by name" />
<FluentMenuItem Label="Sort by date" />
<FluentMenuItem Label="Sort by size" />
""";

    public const string SplitButtonItems = """
<FluentMenuList>
    <FluentMenuItem Label="Save As..." />
    <FluentMenuItem Label="Save a Copy" />
    <FluentMenuItem Label="Save as Template" />
</FluentMenuList>
""";

    public const string AccordionItems = """
<FluentAccordionItem Header="Shipping details" Expanded="true">
    <FluentText Block="true">Ships within 2 business days via standard courier.</FluentText>
</FluentAccordionItem>
<FluentAccordionItem Header="Return policy">
    <FluentText Block="true">Items can be returned within 30 days of delivery.</FluentText>
</FluentAccordionItem>
""";

    public const string DialogContent = """
<FluentDialogBody>
    <FluentText Block="true">This action permanently deletes the selected file. It cannot be undone.</FluentText>
</FluentDialogBody>
""";

    public const string PopoverBody = """
<FluentText Weight="TextWeight.Semibold" Block="true">Storage details</FluentText>
<FluentText Size="TextSize.Size200">64.2 GB used of 100 GB available.</FluentText>
""";

    public const string WizardSteps = """
<FluentWizardStep Label="Account" Summary="Create your account">
    <FluentText Block="true">Enter your name and email address.</FluentText>
</FluentWizardStep>
<FluentWizardStep Label="Profile" Summary="Tell us about you">
    <FluentText Block="true">Add a photo and a short bio.</FluentText>
</FluentWizardStep>
<FluentWizardStep Label="Review" Summary="Confirm and finish">
    <FluentText Block="true">Review your details before finishing.</FluentText>
</FluentWizardStep>
""";

    public const string TreeViewItems = """
<FluentTreeItem Text="Documents" Expanded="true">
    <FluentTreeItem Text="Resume.docx" />
    <FluentTreeItem Text="CoverLetter.docx" />
</FluentTreeItem>
<FluentTreeItem Text="Pictures" />
""";

    public const string TreeItemChildren = """
<FluentTreeItem Text="Resume.docx" />
<FluentTreeItem Text="CoverLetter.docx" />
""";

    public const string AppBarItems = """
<FluentAppBarItem Text="Home" IconRest="@HomeIcon" />
<FluentAppBarItem Text="Search" IconRest="@SearchIcon" />
<FluentAppBarItem Text="Settings" IconRest="@SettingsIcon" />
""";
}
