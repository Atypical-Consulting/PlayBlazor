namespace PlayBlazor.Demo.Daisy;

/// <summary>
/// The razor text of each demo fragment, verbatim, fed to the slot presets as their
/// <c>source</c> so the generated code panel shows copy-pasteable markup instead of a
/// placeholder comment. Keep each string in sync with its fragment in DaisyDemoFragments.razor.
/// Scaffold sources carry a <c>{specimen}</c> marker where the played component sits; the
/// sample rows read <c>@_people</c>, the field a page would declare.
/// </summary>
public static class DaisyDemoFragmentSources
{
    public const string TabPanel = "<p>Ada Lovelace wrote the first published algorithm.</p>";

    public const string CarouselSlide = """<div class="bg-primary text-primary-content rounded-box flex h-40 w-64 items-center justify-center text-xl">Slide 1</div>""";

    public const string GridCell = """<div class="bg-base-200 rounded-box p-4">Grid cell</div>""";

    public const string RowCells = """
<Td>Ada Lovelace</Td>
<Td>Analyst</Td>
""";

    public const string CardHeaderText = """
<h2 class="card-title">Ada Lovelace</h2>
<p class="text-sm opacity-70">Analyst</p>
""";

    public const string CardBodyText = """
<h2 class="card-title">Analytical Engine</h2>
<p>The first design for a general-purpose computer.</p>
""";

    public const string CardButtons = """<Button Variant="Variant.Filled" Color="Color.Primary">Read more</Button>""";

    public const string InTabs = """
<Tabs ActiveIndex="1">
    <Tab Title="Overview">Born in London in 1815.</Tab>
    {specimen}
</Tabs>
""";

    public const string InSelect = """
<Select T="string" Label="Fruit" Value="@("banana")">
    <SelectItem Value="@("apple")">Apple</SelectItem>
    {specimen}
</Select>
""";

    public const string InTable = """
<Table T="Person" Items="@_people" RowsPerPage="2">
    <HeaderContent>
        <Th>Name</Th>
        <Th>Role</Th>
    </HeaderContent>
    <RowTemplate>
        <Td>@context.Name</Td>
        <Td>@context.Role</Td>
    </RowTemplate>
    <PagerContent>
        {specimen}
    </PagerContent>
</Table>
""";

    public const string InDataGridPager = """
<DataGrid T="Person" Items="@_people" RowsPerPage="2" PageSizeOptions="@(new[] { 2, 5, 10 })">
    <Columns>
        <PropertyColumn T="Person" TProperty="string" Property="p => p.Name" />
        <PropertyColumn T="Person" TProperty="string" Property="p => p.Role" />
    </Columns>
    <PagerContent>
        {specimen}
    </PagerContent>
</DataGrid>
""";

    public const string InDataGrid = """
<DataGrid T="Person" Items="@_people">
    <Columns>
        {specimen}
        <PropertyColumn T="Person" TProperty="int" Property="p => p.Age" Title="Age" />
    </Columns>
</DataGrid>
""";

    public const string InRadioGroup = """
<RadioGroup T="string" Label="Fruit" Value="@("apple")">
    {specimen}
    <Radio Value="@("banana")" Label="Banana" />
</RadioGroup>
""";

    public const string InToggleGroup = """
<ToggleGroup T="string" Value="@("grid")">
    {specimen}
    <ToggleItem Value="@("list")" Text="List" />
</ToggleGroup>
""";

    public const string InMenu = """
<div class="h-48">
    @* The transform keeps the open menu's fixed backdrop inside the menu. *@
    <Menu Label="Actions" Open="true" Style="transform: translateZ(0)">
        {specimen}
        <MenuItem Icon="share">Share</MenuItem>
    </Menu>
</div>
""";

    public const string InExpansionPanels = """
<ExpansionPanels>
    {specimen}
    <ExpansionPanel Text="Returns">Free returns within 30 days.</ExpansionPanel>
</ExpansionPanels>
""";

    public const string InTimeline = """
<Timeline>
    {specimen}
    <TimelineItem>1843: Notes on the Analytical Engine</TimelineItem>
</Timeline>
""";

    public const string InCarousel = """
<Carousel Class="rounded-box gap-4">
    {specimen}
    <CarouselItem>
        <div class="bg-secondary text-secondary-content rounded-box flex h-40 w-64 items-center justify-center text-xl">Slide 2</div>
    </CarouselItem>
</Carousel>
""";

    public const string InDock = """
<Dock Fixed="false">
    {specimen}
    <DockItem Icon="settings" Label="Settings" />
</Dock>
""";

    public const string InFab = """
<div class="relative isolate h-64">
    <Fab Fixed="false" AriaLabel="Actions">
        {specimen}
        <FabAction Icon="share" Tooltip="Share" />
    </Fab>
</div>
""";

    public const string InSteps = """
<Steps>
    {specimen}
    <Step>Choose plan</Step>
    <Step>Purchase</Step>
</Steps>
""";

    public const string InItemList = """
<ItemList>
    {specimen}
    <ListItem Icon="send" Text="Sent" />
</ItemList>
""";

    public const string InList = """
<List>
    {specimen}
    <ListRow Icon="person" Title="Grace Hopper" Subtitle="Rear Admiral" />
</List>
""";

    public const string InGrid = """
<Grid Spacing="4">
    {specimen}
    <Item xs="6"><div class="bg-base-200 rounded-box p-4">Sibling</div></Item>
</Grid>
""";

    public const string InTbody = """
<SimpleTable>
    <thead>
        <Tr><Th>Name</Th><Th>Role</Th></Tr>
    </thead>
    <tbody>
        {specimen}
        <Tr><Td>Grace Hopper</Td><Td>Rear Admiral</Td></Tr>
    </tbody>
</SimpleTable>
""";

    public const string InBodyRow = """
<SimpleTable>
    <thead>
        <Tr><Th>Name</Th><Th>Role</Th></Tr>
    </thead>
    <tbody>
        <Tr>
            {specimen}
            <Td>Analyst</Td>
        </Tr>
    </tbody>
</SimpleTable>
""";

    public const string InHeaderRow = """
<SimpleTable>
    <thead>
        <Tr>
            {specimen}
            <Th>Role</Th>
        </Tr>
    </thead>
    <tbody>
        <Tr><Td>Ada Lovelace</Td><Td>Analyst</Td></Tr>
    </tbody>
</SimpleTable>
""";

    public const string InCardTop = """
<Card Class="w-80">
    {specimen}
    <CardContent>Wrote the first published algorithm.</CardContent>
</Card>
""";

    public const string InCard = """
<Card Class="w-80">
    {specimen}
</Card>
""";

    public const string InCardBottom = """
<Card Class="w-80">
    <CardContent>Wrote the first published algorithm.</CardContent>
    {specimen}
</Card>
""";

    public const string InStats = """
<Stats Shadow="true">
    {specimen}
    <Stat Title="Users" Value="4,200" Desc="↗︎ 400 (22%)" />
</Stats>
""";

    public const string InFooter = """
<Footer Class="bg-base-200 text-base-content">
    <nav>
        {specimen}
        <a class="link link-hover">Branding</a>
        <a class="link link-hover">Design</a>
    </nav>
</Footer>
""";
}
