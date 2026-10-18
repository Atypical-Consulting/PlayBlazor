using System.Text.RegularExpressions;
using AwesomeAssertions;
using Bunit;
using DaisyBlazor;
using DaisyBlazor.Data;
using DaisyBlazor.Feedback;
using DaisyBlazor.Validation;
using NUnit.Framework;
using PlayBlazor.Demo.Daisy;

namespace PlayBlazor.UnitTests.Shell;

/// <summary>
/// DaisyBlazor's child components, rendered on the bench exactly as the explorer renders them:
/// through PlaygroundView, so through their scaffold. The render sweep calls almost all of them
/// healthy because alone they throw nothing, yet Tab, SelectItem, TablePager, DataGridPager and the
/// two column types render NO markup alone, and the rest render degraded markup. Every selector
/// below names markup only the parent produces (the tab button, the option, the radio's group name,
/// the .join around a join-item...), so each case fails if its scaffold is removed.
/// </summary>
public partial class DaisyScaffoldTests
{
    private static readonly Type[] Scaffolded =
    [
        typeof(Tab), typeof(SelectItem<string>), typeof(Radio<string>), typeof(ToggleItem<string>),
        typeof(MenuItem), typeof(ExpansionPanel), typeof(TimelineItem), typeof(CarouselItem),
        typeof(DockItem), typeof(FabAction), typeof(Step), typeof(ListItem), typeof(ListRow),
        typeof(Item), typeof(Tr), typeof(Td), typeof(Th), typeof(CardHeader), typeof(CardContent),
        typeof(CardActions), typeof(Stat), typeof(FooterTitle), typeof(TablePager),
        typeof(DataGridPager<Person>), typeof(PropertyColumn<Person, string>), typeof(TemplateColumn<Person>),
        typeof(FieldError), typeof(ValidatorHint), typeof(ConfirmDialog), typeof(MessageBoxDialog),
        typeof(Modal), typeof(Drawer),
    ];

    /// <summary>Curated without a scaffold, but variant-checked with the children above.</summary>
    private static readonly Type[] Unscaffolded =
        [typeof(Form), typeof(Field), typeof(Validator), typeof(Dialog), typeof(ThemeController)];

    // ServerValidation is absent: its one non-slot parameter, State, has no control to vary.
    private static IEnumerable<Type> WithVariants => Scaffolded.Concat(Unscaffolded);

    private static PlayBlazorOptions Configured()
    {
        var options = new PlayBlazorOptions();
        DaisyPlaygroundConfig.Configure(options);
        return options;
    }

    private static BunitContext NewContext()
    {
        var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddDaisyBlazor();
        context.Services.AddPlayBlazor(DaisyPlaygroundConfig.Configure);
        return context;
    }

    private static IRenderedComponent<PlaygroundView> Bench(BunitContext context, Type component)
        => context.Render<PlaygroundView>(ps => ps.Add(c => c.Component, component));

    [TestCaseSource(nameof(Scaffolded))]
    public void EveryChild_HasAScaffoldWithSource_AndLinksToItsParent(Type component)
    {
        var options = Configured();

        options.TryGetScaffold(component, out _).Should().BeTrue();
        options.TryGetScaffoldSource(component, out var source).Should().BeTrue();
        source.Should().Contain("{specimen}");
        options.GetRelated(component).Should().NotBeEmpty();
    }

    // Ghosts: alone, these render nothing at all.
    [TestCase(typeof(Tab), "div[role=tablist] button.tab.tab-active", "Biography")]
    [TestCase(typeof(SelectItem<string>), "select.select option[selected]", "Banana")]
    [TestCase(typeof(TablePager), "div.overflow-x-auto + div .join > button.join-item[aria-label='Next page']", null)]
    [TestCase(typeof(DataGridPager<Person>), "div.overflow-x-auto + div .join > button.join-item[aria-label='Next page']", null)]
    [TestCase(typeof(PropertyColumn<Person, string>), "table.table tbody tr td", "Ada Lovelace")]
    [TestCase(typeof(TemplateColumn<Person>), "table.table thead th", "Role")]
    // Degraded alone: they render, but without what their parent gives them.
    [TestCase(typeof(Radio<string>), "div[role=radiogroup] input.radio[name^=radiogroup-][checked]", null)]
    [TestCase(typeof(ToggleItem<string>), "div.join > button.join-item.btn.btn-primary", "Grid")]
    [TestCase(typeof(MenuItem), "ul[role=menu] > li[role=none] > button[role=menuitem]", "Edit")]
    [TestCase(typeof(ExpansionPanel), "details.collapse[name^=xp-][open] .collapse-title", "Shipping")]
    [TestCase(typeof(TimelineItem), "ul.timeline > li > .timeline-box", "1815")]
    [TestCase(typeof(CarouselItem), "div.carousel > div.carousel-item", "Slide 1")]
    [TestCase(typeof(DockItem), "div.dock.relative > button .dock-label", "Home")]
    [TestCase(typeof(FabAction), "div.relative > div.fab.absolute button.btn-circle[aria-label=Edit]", null)]
    [TestCase(typeof(Step), "ul.steps > li.step.step-primary[data-content]", "Register")]
    [TestCase(typeof(ListItem), "ul.menu > li", "Inbox")]
    [TestCase(typeof(ListRow), "ul.list > li.list-row", "Ada Lovelace")]
    [TestCase(typeof(Item), "div.grid.grid-cols-12 > div.col-span-6", "Grid cell")]
    [TestCase(typeof(Tr), "table.table > tbody > tr > td", "Ada Lovelace")]
    [TestCase(typeof(Td), "table.table > tbody > tr > td", "Ada Lovelace")]
    [TestCase(typeof(Th), "table.table > thead > tr > th[scope=col]", "Name")]
    [TestCase(typeof(CardHeader), "div.card > div.card-title-area .card-title", "Ada Lovelace")]
    [TestCase(typeof(CardContent), "div.card > div.card-body .card-title", "Analytical Engine")]
    [TestCase(typeof(CardActions), "div.card > div.card-actions > button.btn", "Read more")]
    [TestCase(typeof(Stat), "div.stats > div.stat > .stat-value", "31K")]
    [TestCase(typeof(FooterTitle), "footer.footer > nav > h6.footer-title", "Services")]
    // Cascades, required objects and overlays.
    [TestCase(typeof(ServerValidation), "div.fieldset input[aria-invalid=true]", null)]
    [TestCase(typeof(ServerValidation), "div.fieldset", "This email is already registered.")]
    [TestCase(typeof(FieldError), "fieldset.fieldset > input.input + span.text-error", "This email is already registered.")]
    [TestCase(typeof(Form), "div.flex.flex-col.gap-3 > button.btn.btn-primary", "Submit")]
    [TestCase(typeof(Form), "div.flex.flex-col.gap-3 input[required]", null)]
    [TestCase(typeof(Field), "div.fieldset > div[role=group][aria-labelledby]", "INV-2041")]
    [TestCase(typeof(Validator), "div.validator > input.input.validator[type=email][required]", null)]
    [TestCase(typeof(Validator), "div.validator + p.validator-hint", "Enter a valid email address.")]
    [TestCase(typeof(ValidatorHint), "input.validator[aria-invalid=true] ~ p.validator-hint", "Enter a valid email address.")]
    [TestCase(typeof(Dialog), "div.rounded-box.bg-base-100 > h3", "Deploy v2.4.0")]
    [TestCase(typeof(Dialog), "div.rounded-box.bg-base-100 > div.modal-action > button.btn-primary", "Deploy")]
    [TestCase(typeof(ConfirmDialog), "div.rounded-box.bg-base-100 div.modal-action > button.btn-error", "Delete")]
    [TestCase(typeof(MessageBoxDialog), "div.rounded-box.bg-base-100 div.modal-action > button.btn-outline", "Discard")]
    [TestCase(typeof(Modal), "div > p", "Open is true")]
    [TestCase(typeof(Drawer), "div.h-64.overflow-hidden > div.drawer.drawer-open.h-full > div.drawer-side ul.menu > li", "Projects")]
    [TestCase(typeof(ThemeController), "label.cursor-pointer > input.theme-controller.toggle[type=checkbox][value=dark]", null)]
    public void Child_RendersItsDaisyMarkup_InsideItsParent(Type component, string selector, string? text)
    {
        using var context = NewContext();
        var cut = Bench(context, component);

        cut.FindAll(".pb-specimen .pb-error").Should().BeEmpty();
        var matches = cut.FindAll($".pb-specimen {selector}");
        matches.Should().NotBeEmpty($"{component.Name} should render `{selector}` through its scaffold");
        if (text is not null)
        {
            matches.Select(m => m.TextContent).Should().Contain(t => t.Contains(text));
        }
    }

    [Test]
    public void Dock_InTheScaffold_IsNotPinnedToTheViewport()
    {
        // Dock defaults to Fixed=true: daisyUI's .dock is position:fixed at the viewport bottom,
        // and only the `relative` utility (Fixed=false) takes it back into the flow.
        using var context = NewContext();
        var dock = Bench(context, typeof(DockItem)).Find(".pb-specimen div.dock");

        dock.ClassList.Should().Contain("relative");
    }

    [Test]
    public void FieldError_ForAFieldWithoutAnError_RendersNothing()
    {
        using var context = NewContext();
        var cut = Bench(context, typeof(FieldError));

        cut.FindAll("button.pb-variant").Single(b => b.TextContent == "Field without an error").Click();

        cut.FindAll(".pb-specimen span.text-error").Should().BeEmpty();
    }

    [Test]
    public void FieldError_PlainFor_FindsAQualifiedServerKey()
    {
        // The fallback matches the server key's last segment against For, one way only: caught in
        // a real browser, where the earlier For="request.Email" variant rendered nothing.
        using var context = NewContext();
        var cut = Bench(context, typeof(FieldError));

        cut.FindAll("button.pb-variant").Single(b => b.TextContent == "Qualified server key").Click();

        cut.Find(".pb-specimen span.text-error").TextContent.Should().Contain("cannot receive texts");
    }

    [Test]
    public void Modal_StaysClosedUntilAVariantOpensIt()
    {
        // An open Modal is a native top-layer modal over the whole explorer: never by default.
        using var context = NewContext();
        var cut = Bench(context, typeof(Modal));
        cut.FindAll(".pb-specimen dialog").Should().BeEmpty();

        cut.FindAll("button.pb-variant").Single(b => b.TextContent == "Open").Click();

        cut.Find(".pb-specimen dialog.modal.modal-open[open] .modal-box button[aria-label=Close]").Should().NotBeNull();
    }

    [Test]
    public void ThemeController_RadioVariant_JoinsANamedGroup_AndStartsUnchecked()
    {
        // Checked re-themes the whole page (:root:has(...:checked)), so no preset checks it.
        using var context = NewContext();
        var cut = Bench(context, typeof(ThemeController));
        cut.Find(".pb-specimen input.theme-controller").HasAttribute("checked").Should().BeFalse();

        cut.FindAll("button.pb-variant").Single(b => b.TextContent == "Radio").Click();

        var radio = cut.Find(".pb-specimen input.theme-controller.radio[type=radio]");
        radio.GetAttribute("name").Should().Be("theme");
        radio.HasAttribute("checked").Should().BeFalse();
    }

    [Test]
    public void Pagers_ShowTheRangeOfTheirHost()
    {
        using var context = NewContext();

        Bench(context, typeof(TablePager)).Find(".pb-specimen").TextContent.Should().Contain("1-2 / 3");
        Bench(context, typeof(DataGridPager<Person>)).Find(".pb-specimen").TextContent.Should().Contain("1-2 of 3");
    }

    /// <summary>
    /// Every variant must change the specimen's markup when clicked on a live bench, which is how the
    /// explorer applies the first variant (no reset, so no fresh instance). This sees what the
    /// parameter-dictionary liveness gate cannot: a parent that renders its child's state and is not
    /// told it changed.
    /// </summary>
    [TestCaseSource(nameof(WithVariants))]
    public void EveryVariant_ChangesTheRenderedChild(Type component)
    {
        var variants = Configured().GetVariants(component);
        variants.Should().NotBeEmpty();

        foreach (var variant in variants)
        {
            using var context = NewContext();
            var cut = Bench(context, component);
            var before = Normalized(cut.Find(".pb-specimen").InnerHtml);

            cut.FindAll("button.pb-variant").Single(b => b.TextContent == variant.Name).Click();
            if (LagsOneRender.Contains(component))
            {
                cut.Render();
            }

            Normalized(cut.Find(".pb-specimen").InnerHtml).Should().NotBe(
                before, $"{component.Name} / \"{variant.Name}\" should change what the bench shows");
        }
    }

    /// <summary>
    /// Upstream at 0.6.0: Tab, SelectItem and the DataGrid columns only register with their parent,
    /// which renders their state; none tells the parent when its own parameters change. The parent
    /// re-renders BEFORE the child receives its new values, so the bench shows the previous state
    /// until the next render. Pinned here so the lag stays visible: when this list can be emptied,
    /// the comments beside their curation go too.
    /// </summary>
    private static readonly HashSet<Type> LagsOneRender =
        [typeof(Tab), typeof(SelectItem<string>), typeof(PropertyColumn<Person, string>), typeof(TemplateColumn<Person>)];

    [Test]
    public void RegisteringChildren_StillLagOneRender_Upstream()
    {
        foreach (var (component, variant, expected) in new[]
                 {
                     (typeof(Tab), "Rich title", "★ Biography"),
                     (typeof(SelectItem<string>), "Longer label", "Banana (ripe)"),
                     (typeof(PropertyColumn<Person, string>), "Custom title", "Full name"),
                     (typeof(TemplateColumn<Person>), "Custom title", "Position"),
                 })
        {
            using var context = NewContext();
            var cut = Bench(context, component);

            cut.FindAll("button.pb-variant").Single(b => b.TextContent == variant).Click();
            cut.Find(".pb-specimen").TextContent.Should().NotContain(expected, "the parent renders before the child updates");

            cut.Render();
            cut.Find(".pb-specimen").TextContent.Should().Contain(expected);
        }
    }

    // Tabs, RadioGroup, ExpansionPanels and the form fields mint a GUID per instance for their ids,
    // and Blazor renumbers its event-handler attributes on every render.
    private static string Normalized(string html) => Noise().Replace(html, "{noise}");

    [GeneratedRegex("[0-9a-f]{32}|[0-9a-f]{8}(?=-)|(?<=dz-field-)[0-9a-f]+|blazor:\\w+=\"\\d+\"")]
    private static partial Regex Noise();
}
