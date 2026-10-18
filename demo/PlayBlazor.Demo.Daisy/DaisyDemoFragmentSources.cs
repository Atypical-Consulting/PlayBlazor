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

    public const string GroupButtons = """
<Button>Day</Button>
<Button>Week</Button>
<Button>Month</Button>
""";

    public const string FabActions = """
<FabAction Icon="@Icons.Material.Filled.Edit" Tooltip="Edit" />
<FabAction Icon="@Icons.Material.Filled.Share" Tooltip="Share" />
""";

    public const string SwapOn = """<Icon Name="@Icons.Material.Filled.DarkMode" Size="Size.Large" />""";

    public const string SwapOff = """<Icon Name="@Icons.Material.Filled.LightMode" Size="Size.Large" />""";

    public const string SelectOptions = """
<SelectItem Value="@("apple")">Apple</SelectItem>
<SelectItem Value="@("banana")">Banana</SelectItem>
<SelectItem Value="@("cherry")">Cherry</SelectItem>
""";

    public const string RadioOptions = """
<Radio Value="@("apple")" Label="Apple" />
<Radio Value="@("banana")" Label="Banana" />
<Radio Value="@("cherry")" Label="Cherry" />
""";

    public const string ToggleItems = """
<ToggleItem Value="@("list")" Text="List" Icon="@Icons.Material.Filled.ViewList" />
<ToggleItem Value="@("grid")" Text="Grid" Icon="@Icons.Material.Filled.GridView" />
<ToggleItem Value="@("table")" Text="Table" Icon="@Icons.Material.Filled.TableRows" />
""";

    public const string FloatingInput = """<input type="email" class="input" placeholder="ada@example.com" />""";

    public const string CardParts = """
<CardContent>
    <h2 class="card-title">Analytical Engine</h2>
    <p>The first design for a general-purpose computer.</p>
</CardContent>
<CardActions Class="justify-end">
    <Button Variant="Variant.Filled" Color="Color.Primary">Read more</Button>
</CardActions>
""";

    public const string CarouselSlides = """
<CarouselItem>
    <div class="bg-primary text-primary-content rounded-box flex h-40 w-64 items-center justify-center text-xl">Slide 1</div>
</CarouselItem>
<CarouselItem>
    <div class="bg-secondary text-secondary-content rounded-box flex h-40 w-64 items-center justify-center text-xl">Slide 2</div>
</CarouselItem>
<CarouselItem>
    <div class="bg-accent text-accent-content rounded-box flex h-40 w-64 items-center justify-center text-xl">Slide 3</div>
</CarouselItem>
""";

    public const string StatCells = """
<Stat Title="Downloads" Value="31K" Desc="Jan 1st - Feb 1st" />
<Stat Title="Users" Value="4,200" Desc="↗︎ 400 (22%)" DescColor="Color.Success" />
<Stat Title="New registers" Value="1,200" Desc="↘︎ 90 (14%)" DescColor="Color.Error" />
""";

    public const string TimelineItems = """
<TimelineItem Color="Color.Primary">1815: Ada Lovelace is born</TimelineItem>
<TimelineItem Color="Color.Primary">1843: Notes on the Analytical Engine</TimelineItem>
<TimelineItem>1953: The notes are republished</TimelineItem>
""";

    public const string ListRows = """
<ListRow Icon="person" Title="Ada Lovelace" Subtitle="Analyst" />
<ListRow Icon="person" Title="Grace Hopper" Subtitle="Rear Admiral" />
<ListRow Icon="person" Title="Alan Turing" Subtitle="Cryptanalyst" />
""";

    public const string ListItems = """
<ListItem Icon="inbox" Text="Inbox" />
<ListItem Icon="send" Text="Sent" />
<ListItem Icon="drafts" Text="Drafts" />
""";

    public const string Panels = """
<ExpansionPanel Text="Shipping" IsExpanded="true">Orders ship within two business days.</ExpansionPanel>
<ExpansionPanel Text="Returns">Free returns within 30 days.</ExpansionPanel>
<ExpansionPanel Text="Warranty">Two years on every order.</ExpansionPanel>
""";

    public const string IndicatorBadge = """<Badge Inline="true" Color="Color.Primary">12</Badge>""";

    public const string TicketsButton = """<Button Variant="Variant.Outlined">Tickets</Button>""";

    public const string InboxButton = """<Button Variant="Variant.Outlined">Inbox</Button>""";

    public const string HoverButton = """<Button Variant="Variant.Outlined">Save</Button>""";

    public const string ChatAvatar = """<Avatar Image="@_photo" Alt="Ana Silva" Size="Size.Small" />""";

    public const string MaskedPhoto = """<img class="block w-full" src="@_photo" alt="Mountains at dusk" />""";

    public const string DiffBefore = """<img src="@_photo" alt="Colour" />""";

    public const string DiffAfter = """<img src="@_photoMuted" alt="Greyscale" />""";

    public const string PaperText = """
<h3 class="text-lg font-semibold">Analytical Engine</h3>
<p class="text-sm">Designed by Charles Babbage in 1837.</p>
""";

    public const string NewProjectButton = """<Button Variant="Variant.Filled" Color="Color.Primary">New project</Button>""";

    public const string InFabFrame = """
@* The transform keeps even a fixed (default) Fab inside this box. *@
<div class="relative isolate h-64" style="transform: translateZ(0)">
    {specimen}
</div>
""";

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

    public const string ServerValidatedField = """<TextField Label="Email" FieldName="Email" Value="@("ada@example.com")" />""";

    public const string FormFields = """
<TextField Label="Name" Value="@("Ada Lovelace")" Required="true" />
<TextField Label="Email" InputType="InputType.Email" Placeholder="ada@example.com" />
<Button Color="Color.Primary" Variant="Variant.Filled">Submit</Button>
""";

    public const string EmailInput = """<input type="email" class="input validator" placeholder="ada@example.com" required />""";

    public const string DialogBody = "<p>12 services restart in all 3 regions on Friday at midnight.</p>";

    public const string DialogButtons = """
<Button Variant="Variant.Text">Later</Button>
<Button Color="Color.Primary" Variant="Variant.Filled">Deploy</Button>
""";

    public const string ModalBody = "<p>We will email Dana an invitation to join the workspace.</p>";

    public const string ModalButtons = """<Button Color="Color.Primary" Variant="Variant.Filled">Send invite</Button>""";

    public const string DrawerSide = """
<div class="bg-base-200 min-h-full w-40 p-2">
    <ItemList>
        <ListItem Icon="home" Text="Home" />
        <ListItem Icon="folder" Text="Projects" />
        <ListItem Icon="settings" Text="Settings" />
    </ItemList>
</div>
""";

    public const string DrawerMain = """<p class="p-4">Main content sits beside the open panel.</p>""";

    public const string InServerValidation = """
<ServerValidation State="@_validation">
    <fieldset class="fieldset">
        <legend class="fieldset-legend">Email</legend>
        <input type="email" class="input" value="ada@example.com" />
        {specimen}
    </fieldset>
</ServerValidation>
""";

    public const string AfterInvalidInput = """
<Validator>
    <input type="email" class="input validator" value="ada.example.com" aria-invalid="true" />
    {specimen}
</Validator>
""";

    public const string InDialogBox = """
<div class="rounded-box bg-base-100 max-w-lg p-6 shadow-xl ring-1 ring-base-300">
    {specimen}
</div>
""";

    public const string WithModalNote = """
<div>
    <p class="text-sm opacity-70">Shown while Open is true (bind it with @@bind-Open). It opens as a native modal over the whole page: Escape, the backdrop or the close button dismiss it.</p>
    {specimen}
</div>
""";

    public const string InDrawerFrame = """
<div class="rounded-box border-2 border-base-300 h-64 overflow-hidden">
    {specimen}
</div>
""";
}
