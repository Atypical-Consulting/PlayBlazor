using System.Text.RegularExpressions;
using AwesomeAssertions;
using Bunit;
using DaisyBlazor;
using DaisyBlazor.Feedback;
using NUnit.Framework;
using PlayBlazor.Demo.Daisy;
using PlayBlazor.Discovery;
using PlayBlazor.Rendering;
using PlayBlazor.State;

namespace PlayBlazor.UnitTests.Shell;

/// <summary>
/// DaisyBlazor's actions, form inputs, data display and feedback, rendered on the bench as the explorer renders them. Each
/// selector names the daisyUI control the preset should produce (an input.toggle, a select with its
/// options, an open .join...), so a preset that compiles but renders nothing visible fails here.
/// </summary>
public partial class DaisyPresetTests
{
    private static readonly Type[] Curated =
    [
        typeof(Button), typeof(ButtonGroup), typeof(IconButton), typeof(Fab), typeof(Swap),
        typeof(TextField), typeof(NumericField<int>), typeof(Autocomplete<string>), typeof(Select<string>),
        typeof(RadioGroup<string>), typeof(ToggleGroup<string>), typeof(Checkbox), typeof(Switch),
        typeof(DaisyBlazor.Range), typeof(Rating), typeof(ColorPicker), typeof(DatePicker), typeof(FileInput),
        typeof(FloatingLabel),
        // Data display and feedback.
        typeof(Alert), typeof(Avatar), typeof(Badge), typeof(Card), typeof(Carousel), typeof(ChatBubble),
        typeof(Chip), typeof(Countdown), typeof(Diff), typeof(Divider), typeof(Icon), typeof(Image),
        typeof(Indicator), typeof(Kbd), typeof(Mask), typeof(Paper), typeof(Skeleton), typeof(Stats),
        typeof(Status), typeof(Tooltip), typeof(Typography), typeof(Timeline), typeof(List), typeof(ItemList),
        typeof(ExpansionPanels), typeof(Loading), typeof(ProgressCircular), typeof(ProgressLinear),
        typeof(RadialProgress), typeof(EmptyState), typeof(ErrorAlert), typeof(AccessDenied),
    ];

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

    [TestCase(typeof(Button), "button.btn:not(.btn-ghost)", "Click me")]
    [TestCase(typeof(ButtonGroup), "div.join > button.btn:not(.btn-ghost)", "Week")]
    [TestCase(typeof(IconButton), "button.btn.btn-circle[title='Add to favourites'] .material-symbols-outlined", "favorite")]
    [TestCase(typeof(Fab), "div.relative > div.fab.absolute > div[role=button][aria-label=Actions].btn-primary", null)]
    [TestCase(typeof(Fab), "div.fab button.btn-circle[aria-label=Share]", null)]
    [TestCase(typeof(Swap), "label.swap > input[type=checkbox][aria-label='Dark mode'] ~ div.swap-on", "dark_mode")]
    [TestCase(typeof(Swap), "label.swap > div.swap-off", "light_mode")]
    [TestCase(typeof(TextField), "div.fieldset > input.input[type=email][placeholder='ada@example.com']", null)]
    [TestCase(typeof(TextField), "div.fieldset > span", "We never share")]
    [TestCase(typeof(NumericField<int>), "input.input[type=number][value='42'][min='0'][max='100'][step='2']", null)]
    [TestCase(typeof(Autocomplete<string>), "div.dropdown input[role=combobox][placeholder^='Type two letters']", null)]
    [TestCase(typeof(Select<string>), "select.select option[selected]", "Banana")]
    [TestCase(typeof(RadioGroup<string>), "div[role=radiogroup] input.radio[name^=radiogroup-][checked]", null)]
    [TestCase(typeof(ToggleGroup<string>), "div.join[role=group] > button.join-item.btn-primary", "Grid")]
    [TestCase(typeof(Checkbox), "label > input.checkbox[type=checkbox][checked] + span", "Remember me")]
    [TestCase(typeof(Switch), "label > input.toggle[role=switch][checked] + span", "Email notifications")]
    [TestCase(typeof(DaisyBlazor.Range), "input.range[type=range][value='40'][aria-label=Volume]", null)]
    [TestCase(typeof(Rating), "div.rating > input.mask.mask-star-2.bg-warning[value='3'][checked]", null)]
    [TestCase(typeof(ColorPicker), "div.fieldset input[type=color][value='#605dff']", null)]
    [TestCase(typeof(DatePicker), "div.fieldset > div.input > input[type=text][value='12/10/1815']", null)]
    [TestCase(typeof(DatePicker), "span.dz-date__pick > input[type=date][value='1815-12-10']", null)]
    [TestCase(typeof(FileInput), "fieldset.fieldset > input.file-input[type=file][accept='image/*']", null)]
    [TestCase(typeof(FloatingLabel), "label.floating-label > span + input.input[placeholder]", null)]
    public void Preset_RendersItsDaisyControl(Type component, string selector, string? text)
    {
        using var context = NewContext();
        var cut = Bench(context, component);

        cut.FindAll(".pb-specimen .pb-error").Should().BeEmpty();
        var matches = cut.FindAll($".pb-specimen {selector}");
        matches.Should().NotBeEmpty($"{component.Name} should render `{selector}`");
        if (text is not null)
        {
            matches.Select(m => m.TextContent).Should().Contain(t => t.Contains(text));
        }
    }

    [TestCase(typeof(Select<string>), "select.select > option", 3)]
    [TestCase(typeof(RadioGroup<string>), "input.radio", 3)]
    [TestCase(typeof(ToggleGroup<string>), "button.join-item", 3)]
    [TestCase(typeof(Rating), "input.mask", 5)]
    [TestCase(typeof(ButtonGroup), "button.btn", 3)]
    public void Preset_RendersEveryOption(Type component, string selector, int count)
    {
        using var context = NewContext();
        var cut = Bench(context, component);

        cut.FindAll($".pb-specimen {selector}").Should().HaveCount(count);
    }

    [Test]
    public void Autocomplete_SearchFunc_FindsTheMatchingOptions()
    {
        // Without the SearchFunc preset the list never opens: the component would be inert.
        using var context = NewContext();
        var cut = Bench(context, typeof(Autocomplete<string>));
        var input = cut.Find(".pb-specimen input[role=combobox]");

        input.Focus();
        input.Input("an");

        cut.WaitForAssertion(
            () => cut.FindAll(".pb-specimen ul[role=listbox] button[role=option]").Select(o => o.TextContent.Trim())
                .Should().Equal("Banana", "Mango", "Orange"),
            TimeSpan.FromSeconds(3));
    }

    [Test]
    public void Select_MultipleVariant_SelectsThePresetValues()
    {
        using var context = NewContext();
        var cut = Bench(context, typeof(Select<string>));

        Variant(cut, "Multiple");

        var select = cut.Find(".pb-specimen select[multiple]");
        select.QuerySelectorAll("option[selected]").Select(o => o.TextContent).Should().Equal("Apple", "Cherry");
    }

    [Test]
    public void ToggleGroup_MultipleVariant_SelectsThePresetValues()
    {
        using var context = NewContext();
        var cut = Bench(context, typeof(ToggleGroup<string>));

        Variant(cut, "Multiple");

        // Each item's text follows its icon's ligature (view_list, table_rows).
        cut.FindAll(".pb-specimen button.join-item.btn-primary").Select(b => b.TextContent.Trim())
            .Should().SatisfyRespectively(b => b.Should().EndWith("List"), b => b.Should().EndWith("Table"));
    }

    [Test]
    public void Checkbox_IndeterminateVariant_SetsTheDomPropertyThroughJs()
    {
        // indeterminate has no attribute: the markup cannot show it, the interop call is the effect.
        using var context = NewContext();
        var cut = Bench(context, typeof(Checkbox));
        context.JSInterop.Invocations.Should().NotContain(i => i.Identifier == "Reflect.set");

        Variant(cut, "Indeterminate");

        context.JSInterop.Invocations.Should().Contain(i => i.Identifier == "Reflect.set" && Equals(i.Arguments[1], "indeterminate") && Equals(i.Arguments[2], true));
    }

    [Test]
    public void RadioGroup_ValueSetFromOutside_DoesNotMoveTheCheck_Upstream()
    {
        // At 0.6.0 nothing re-renders the radios when the group's Value changes from outside. When
        // this fails, the comment beside RadioGroup's curation can go and a variant can set Value.
        using var context = NewContext();
        var cut = context.Render<RadioGroup<string>>(ps => ps
            .Add(g => g.Value, "banana")
            .Add(g => g.ChildContent, DaisyDemoFragments.RadioOptions));

        cut.Render(ps => ps.Add(g => g.Value, "cherry"));

        var radios = cut.FindAll("input.radio");
        radios[1].HasAttribute("checked").Should().BeTrue("Banana keeps the stale check");
        radios[2].HasAttribute("checked").Should().BeFalse("Cherry is not checked though it is the Value");
    }

    [Test]
    public void Fab_InTheFrame_IsNotPinnedToTheViewport()
    {
        using var context = NewContext();
        var cut = Bench(context, typeof(Fab));

        cut.Find(".pb-specimen div.fab").ClassList.Should().Contain("absolute");
        cut.Find(".pb-specimen div.relative").GetAttribute("style").Should().Contain("translateZ(0)");
    }

    /// <summary>Indeterminate changes a DOM property, not the markup; pinned above instead.</summary>
    private static readonly HashSet<(Type, string)> MarkupInvisible = [(typeof(Checkbox), "Indeterminate")];

    [TestCaseSource(nameof(Curated))]
    public void EveryVariant_ChangesTheRenderedMarkup(Type component)
    {
        var variants = Configured().GetVariants(component);
        variants.Count.Should().BeInRange(3, 6);

        foreach (var variant in variants.Where(v => !MarkupInvisible.Contains((component, v.Name))))
        {
            using var context = NewContext();
            var cut = Bench(context, component);
            var before = Normalized(cut.Find(".pb-specimen").InnerHtml);

            Variant(cut, variant.Name);

            Normalized(cut.Find(".pb-specimen").InnerHtml).Should().NotBe(
                before, $"{component.Name} / \"{variant.Name}\" should change what the bench shows");
        }
    }

    // --- Data display and feedback ---------------------------------------------------------

    [TestCase(typeof(Alert), "div.alert.alert-info[role=status] > span", "12 unread messages")]
    [TestCase(typeof(Avatar), "div.avatar > div.w-12.rounded-full > img[alt='Ada Lovelace'][src^='data:image/svg+xml']", null)]
    [TestCase(typeof(Badge), "span.indicator > span.indicator-item.badge.badge-primary.badge-sm", "8")]
    [TestCase(typeof(Badge), "span.indicator > button.btn.btn-outline", "Inbox")]
    [TestCase(typeof(Card), "div.card.shadow-sm.w-80 > div.card-body > h2.card-title", "Analytical Engine")]
    [TestCase(typeof(Card), "div.card > div.card-actions.justify-end > button.btn-primary", "Read more")]
    [TestCase(typeof(Carousel), "div.carousel.w-80 > div.carousel-item > div.bg-primary", "Slide 1")]
    [TestCase(typeof(ChatBubble), "div.chat.chat-start.w-full > div.chat-image > div.avatar img[src^='data:image/svg+xml']", null)]
    [TestCase(typeof(ChatBubble), "div.chat > div.chat-bubble", "The hotfix is on staging")]
    [TestCase(typeof(ChatBubble), "div.chat > div.chat-footer", "Delivered")]
    [TestCase(typeof(Chip), "span.badge.badge-primary > button", "Blazor")]
    [TestCase(typeof(Chip), "span.badge > button.btn-circle[aria-label='Remove Blazor']", null)]
    [TestCase(typeof(Countdown), "span.countdown.font-mono.text-5xl > span[style='--value:42;']", null)]
    [TestCase(typeof(Diff), "div.diff.aspect-16\\/9.w-96 > div.diff-item-1[aria-label=Colour] > img[src^='data:image/svg+xml']", null)]
    [TestCase(typeof(Diff), "div.diff > div.diff-item-2[aria-label=Greyscale] > img[src^='data:image/svg+xml']", null)]
    [TestCase(typeof(Divider), "div.divider.w-64", "OR")]
    [TestCase(typeof(Icon), "span.material-symbols-outlined[aria-hidden=true]", "favorite")]
    [TestCase(typeof(Image), "img[src^='data:image/svg+xml'][alt='Mountains at dusk'][style='width:192px;']", null)]
    [TestCase(typeof(Indicator), "div.indicator > span.indicator-item.indicator-top.indicator-end > span.badge.badge-primary", "12")]
    [TestCase(typeof(Indicator), "div.indicator > button.btn-outline", "Tickets")]
    [TestCase(typeof(Kbd), "kbd.kbd.kbd-md", "Ctrl")]
    [TestCase(typeof(Mask), "div.mask.mask-squircle.w-24 > img.block.w-full[src^='data:image/svg+xml']", null)]
    [TestCase(typeof(Paper), "div.bg-base-100.rounded-box.shadow-xs.p-6 > h3", "Analytical Engine")]
    [TestCase(typeof(Skeleton), "div.skeleton.rounded-full[style='width:12rem;height:1rem;']", null)]
    [TestCase(typeof(Stats), "div.stats.stats-horizontal.shadow > div.stat > div.stat-value", "4,200")]
    [TestCase(typeof(Status), "span.status.status-success[role=img][aria-label=Online]", null)]
    [TestCase(typeof(Tooltip), "div.tooltip.tooltip-top[data-tip='Saves a draft'] > button.btn-outline", "Save")]
    [TestCase(typeof(Typography), "p.text-base", "The Analytical Engine")]
    [TestCase(typeof(Timeline), "ul.timeline.timeline-vertical > li .timeline-box", "1843")]
    [TestCase(typeof(List), "ul.list.bg-base-100.rounded-box > li.list-row", "Grace Hopper")]
    [TestCase(typeof(ItemList), "ul.menu.bg-base-200.rounded-box > li", "Drafts")]
    [TestCase(typeof(ExpansionPanels), "div.flex-col.w-96 > details.collapse[open] > summary", "Shipping")]
    [TestCase(typeof(Loading), "span.loading.loading-spinner.loading-md[role=status][aria-label=Loading]", null)]
    [TestCase(typeof(ProgressCircular), "div.radial-progress[role=progressbar][aria-valuenow='65'][aria-label='Upload progress']", null)]
    [TestCase(typeof(ProgressLinear), "progress.progress.w-56[value='40'][max='100']", null)]
    [TestCase(typeof(RadialProgress), "div.radial-progress[role=progressbar][aria-valuenow='70'] > span", "70%")]
    [TestCase(typeof(EmptyState), "span.material-symbols-outlined", "folder_open")]
    [TestCase(typeof(EmptyState), "p.font-medium", "No projects yet")]
    [TestCase(typeof(EmptyState), "div.justify-center > button.btn-primary", "New project")]
    [TestCase(typeof(ErrorAlert), "div.alert.alert-error[role=alert] > span", "the server is unreachable")]
    [TestCase(typeof(AccessDenied), "div.max-w-screen-sm[style*='min-height: 16rem'] span.text-error", "lock")]
    [TestCase(typeof(AccessDenied), "a.btn.btn-outline.btn-primary[href='#']", "Back to home")]
    public void DisplayPreset_RendersItsDaisyClass(Type component, string selector, string? text)
        => Preset_RendersItsDaisyControl(component, selector, text);

    [TestCase(typeof(Carousel), "div.carousel-item", 3)]
    [TestCase(typeof(Stats), "div.stat", 3)]
    [TestCase(typeof(Timeline), "ul.timeline > li", 3)]
    [TestCase(typeof(List), "li.list-row", 3)]
    [TestCase(typeof(ItemList), "ul.menu > li", 3)]
    [TestCase(typeof(ExpansionPanels), "details.collapse", 3)]
    public void DisplayContainer_RendersEveryChild(Type component, string selector, int count)
        => Preset_RendersEveryOption(component, selector, count);

    [TestCase(typeof(Alert), "Soft", "div.alert.alert-soft.text-base-content")]
    [TestCase(typeof(Alert), "Error", "div.alert.alert-error[role=alert]")]
    [TestCase(typeof(Avatar), "Initials", "div.avatar.avatar-placeholder > div.bg-primary.text-primary-content > span")]
    [TestCase(typeof(Avatar), "Outlined initials", "div.avatar-placeholder > div.border-2.text-secondary")]
    [TestCase(typeof(Badge), "Dot", "span.indicator-item.badge.badge-xs")]
    [TestCase(typeof(Badge), "Secondary", "span.indicator-item.badge.badge-secondary")]
    [TestCase(typeof(Card), "Floating", "div.card.shadow-xl")]
    [TestCase(typeof(Card), "Outlined", "div.card.card-border")]
    [TestCase(typeof(Carousel), "Snap to center", "div.carousel.carousel-center")]
    [TestCase(typeof(Carousel), "Vertical", "div.carousel.carousel-vertical.h-40")]
    [TestCase(typeof(ChatBubble), "End", "div.chat.chat-end")]
    [TestCase(typeof(ChatBubble), "End, secondary", "div.chat.chat-end > div.chat-bubble.chat-bubble-secondary")]
    [TestCase(typeof(Chip), "Large", "span.badge.badge-lg")]
    [TestCase(typeof(Chip), "Small", "span.badge.badge-sm")]
    [TestCase(typeof(Chip), "Soft", "span.badge.badge-soft")]
    [TestCase(typeof(Chip), "Success", "span.badge.badge-success")]
    [TestCase(typeof(Countdown), "Two digits", "span.countdown > span[style='--value:7;--digits:2;']")]
    [TestCase(typeof(Diff), "Square", "div.diff.aspect-square")]
    [TestCase(typeof(Diff), "4:3", "div.diff.aspect-4\\/3")]
    [TestCase(typeof(Divider), "Vertical", "div.divider.divider-horizontal.h-24")]
    [TestCase(typeof(Icon), "Rocket", "span.text-primary")]
    [TestCase(typeof(Icon), "Large", "span.material-symbols-outlined[style^='font-size:2.25rem']")]
    [TestCase(typeof(Image), "Cover, wide", "img.object-cover[style='width:288px;height:120px;']")]
    [TestCase(typeof(Indicator), "Bottom end", "span.indicator-item.indicator-bottom.indicator-end")]
    [TestCase(typeof(ItemList), "Dense", "ul.menu.menu-sm")]
    [TestCase(typeof(ItemList), "Horizontal", "ul.menu.menu-horizontal")]
    [TestCase(typeof(Kbd), "Large", "kbd.kbd.kbd-lg")]
    [TestCase(typeof(Loading), "Dots", "span.loading.loading-dots")]
    [TestCase(typeof(Loading), "Infinity", "span.loading.loading-infinity")]
    [TestCase(typeof(Loading), "Large primary", "span.loading.loading-lg.text-primary")]
    [TestCase(typeof(Mask), "Heart", "div.mask.mask-heart")]
    [TestCase(typeof(Mask), "Decagon", "div.mask.mask-decagon")]
    [TestCase(typeof(Mask), "Left half", "div.mask.mask-squircle.mask-half-1")]
    [TestCase(typeof(Paper), "Elevation 3", "div.rounded-box.shadow-sm")]
    [TestCase(typeof(Paper), "Elevation 6", "div.rounded-box.shadow-md")]
    [TestCase(typeof(Paper), "Elevation 12", "div.rounded-box.shadow-lg")]
    [TestCase(typeof(Paper), "Elevation 24", "div.rounded-box.shadow-2xl")]
    [TestCase(typeof(Paper), "Outlined", "div.rounded-box.border.border-base-300")]
    [TestCase(typeof(ProgressCircular), "Indeterminate", "span.loading.loading-spinner[role=status]")]
    [TestCase(typeof(ProgressCircular), "Out of 8", "div.radial-progress[aria-valuemax='8'][aria-valuenow='3']")]
    [TestCase(typeof(ProgressLinear), "Primary", "progress.progress.progress-primary")]
    [TestCase(typeof(ProgressLinear), "Thick", "progress.progress.h-4")]
    [TestCase(typeof(ProgressLinear), "Indeterminate", "progress.progress:not([value])")]
    [TestCase(typeof(RadialProgress), "Success, full", "div.radial-progress.text-success[aria-valuenow='100']")]
    [TestCase(typeof(Skeleton), "Rectangle", "div.skeleton:not(.rounded-full)[style='width:8rem;height:5rem;']")]
    [TestCase(typeof(Stats), "Vertical", "div.stats.stats-vertical")]
    [TestCase(typeof(Status), "Extra large", "span.status.status-success.status-xl")]
    [TestCase(typeof(Status), "Error", "span.status.status-error[aria-label=Offline]")]
    [TestCase(typeof(Status), "Animated", "span.inline-grid > span.status.status-success.animate-ping + span.status")]
    [TestCase(typeof(Timeline), "Compact", "ul.timeline.timeline-compact")]
    [TestCase(typeof(Tooltip), "Primary", "div.tooltip.tooltip-primary.tooltip-open")]
    [TestCase(typeof(Tooltip), "Below", "div.tooltip.tooltip-bottom.tooltip-open")]
    [TestCase(typeof(Typography), "Heading 1", "h1.text-4xl.font-bold")]
    [TestCase(typeof(Typography), "Overline", "p.uppercase.tracking-widest")]
    public void DisplayVariant_RendersItsDaisyModifier(Type component, string variant, string selector)
    {
        using var context = NewContext();
        var cut = Bench(context, component);

        Variant(cut, variant);

        cut.FindAll(".pb-specimen .pb-error").Should().BeEmpty();
        cut.FindAll($".pb-specimen {selector}").Should().NotBeEmpty($"{component.Name} / \"{variant}\" should render `{selector}`");
    }

    /// <summary>
    /// The classes these presets make DaisyBlazor compose at runtime must exist in the compiled
    /// app.css (Tailwind emits only what the safelist or this project's sources name). The last
    /// case pins the known upstream hole: when daisyUI defines tooltip-neutral, Tooltip can offer
    /// Color.Dark.
    /// </summary>
    [Test]
    public void DisplayClasses_AreCompiledIntoAppCss()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "PlayBlazor.slnx")))
        {
            root = root.Parent;
        }

        root.Should().NotBeNull("the tests run inside the repository");
        var css = File.ReadAllText(Path.Combine(root!.FullName, "demo", "PlayBlazor.Demo.Daisy", "wwwroot", "css", "app.css"));
        string[] composed =
        [
            "shadow-xs", "shadow-sm", "shadow", "shadow-md", "shadow-lg", "shadow-xl", "shadow-2xl",
            "status-success", "status-error", "status-warning", "status-xs", "status-xl",
            "badge-primary", "badge-secondary", "badge-error", "badge-success", "badge-xs", "badge-sm", "badge-lg", "badge-outline", "badge-soft",
            "alert-info", "alert-success", "alert-warning", "alert-error", "alert-outline", "alert-soft",
            "chat-start", "chat-end", "chat-bubble-primary", "chat-bubble-secondary", "chat-bubble-error",
            "tooltip-open", "tooltip-primary", "tooltip-error", "tooltip-bottom", "tooltip-right",
            "loading-dots", "loading-ring", "loading-ball", "loading-bars", "loading-infinity",
            "mask-squircle", "mask-circle", "mask-hexagon", "mask-heart", "mask-star-2", "mask-decagon", "mask-half-1",
            "kbd-sm", "kbd-md", "kbd-lg", "kbd-xl", "progress-primary", "progress-success", "radial-progress",
            "carousel-center", "carousel-end", "carousel-vertical", "stats-vertical", "timeline-compact",
            "indicator-bottom", "indicator-start", "indicator-center", "indicator-middle", "divider-horizontal",
            "menu-sm", "menu-horizontal", "avatar-placeholder", "skeleton", "countdown", "diff-resizer",
            "aspect-16\\/9", "aspect-4\\/3", "aspect-square", "object-cover", "object-contain",
        ];

        composed.Where(c => !Regex.IsMatch(css, $@"\.{Regex.Escape(c)}[\s{{:,.>\[]"))
            .Should().BeEmpty("each class must be emitted for the variant that composes it to restyle anything");
        Regex.IsMatch(css, @"\.tooltip-neutral\b").Should().BeFalse("upstream hole at 0.6.0: safelisted but undefined in daisyUI 5");
    }

    private static void Variant(IRenderedComponent<PlaygroundView> cut, string name)
        => cut.FindAll("button.pb-variant").Single(b => b.TextContent == name).Click();

    // Field ids are minted per instance, and Blazor renumbers its event-handler attributes.
    private static string Normalized(string html) => Noise().Replace(html, "{noise}");

    [GeneratedRegex("[0-9a-f]{32}|[0-9a-f]{8}(?=-)|(?<=dz-field-)[0-9a-f]+|(?<=dz-date-)[0-9a-f]{8}|blazor:\\w+=\"\\d+\"")]
    private static partial Regex Noise();
}
