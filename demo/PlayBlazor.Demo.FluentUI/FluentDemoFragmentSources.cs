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
}
