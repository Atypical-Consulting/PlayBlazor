using System.Text.RegularExpressions;
using AwesomeAssertions;
using Bunit;
using DaisyBlazor;
using DaisyBlazor.Layout;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PlayBlazor.Demo.Daisy;
using PlayBlazor.Discovery;
using PlayBlazor.Rendering;
using PlayBlazor.State;

namespace PlayBlazor.UnitTests.Shell;

/// <summary>
/// Layout, navigation, mockups and the app-level helpers (DaisyPlaygroundConfig.LayoutAndNavigation),
/// on the bench as the explorer renders them. Same contract as the rest of this class: each selector
/// names the daisyUI markup the preset must produce, so a preset that renders nothing visible fails.
/// </summary>
public partial class DaisyPresetTests
{
    private static readonly Type[] LayoutCurated =
    [
        typeof(Container), typeof(Grid), typeof(Stack), typeof(StackOverlay), typeof(Spacer), typeof(Hero),
        typeof(Footer), typeof(Navbar), typeof(Breadcrumbs), typeof(Dock), typeof(Dropdown), typeof(Menu),
        typeof(NavMenuLink), typeof(Pagination), typeof(Steps), typeof(Tabs), typeof(TextLink),
        typeof(MockupBrowser), typeof(MockupCode), typeof(MockupPhone), typeof(MockupWindow),
        typeof(PageHeader), typeof(SectionCard), typeof(DetailCard), typeof(DetailRow), typeof(DetailLayout),
        typeof(AppCard), typeof(FeatureHero), typeof(FeatureHomePage), typeof(FeatureNavCard), typeof(QuickAccessButton),
    ];

    [TestCase(typeof(Grid), "div.grid.grid-cols-12.gap-6.w-96 > div.col-span-4 > div.p-4", "xs=4")]
    [TestCase(typeof(Stack), "div.flex.flex-col.gap-4 > div.px-6", "Two, wider")]
    [TestCase(typeof(StackOverlay), "div.stack > div.size-28", "3")]
    [TestCase(typeof(Container), "div.max-w-full > div.mx-auto.w-full.px-4.max-w-screen-lg > div.bg-base-200", "max width")]
    [TestCase(typeof(Spacer), "div.flex-row > span + div.flex-1 + span", "5 GB")]
    [TestCase(typeof(Hero), "div.hero.min-h-64.bg-base-200 > div.hero-content h2", "Ship it.")]
    [TestCase(typeof(Footer), "footer.footer.p-10.rounded-box > nav > h6.footer-title", "Company")]
    [TestCase(typeof(Navbar), "div.navbar.bg-base-100.rounded-box > div.navbar-start > a.btn", "Acme")]
    [TestCase(typeof(Navbar), "div.navbar > div.navbar-center > ul.menu.menu-horizontal > li", "Pricing")]
    [TestCase(typeof(Navbar), "div.navbar > div.navbar-end > button.btn-circle[title=Notifications]", null)]
    [TestCase(typeof(Breadcrumbs), "nav.breadcrumbs.text-sm > ul > li > a[href='#']", "Projects")]
    [TestCase(typeof(Breadcrumbs), "nav.breadcrumbs > ul > li > span[aria-current=page]", "Checkout redesign")]
    [TestCase(typeof(Dock), "div[style*=translateZ] > div.dock.relative > button.dock-active > span.dock-label", "Home")]
    [TestCase(typeof(Dropdown), "details.dropdown > summary.btn.btn-outline", "Options")]
    [TestCase(typeof(Dropdown), "details.dropdown > div.dropdown-content.menu > li > a", "Archive")]
    [TestCase(typeof(Menu), "div[style*=translateZ] > div.dropdown.dropdown-open > button.btn[aria-expanded=true]", "Actions")]
    [TestCase(typeof(Menu), "div.dropdown > div.fixed.inset-0 + ul.dropdown-content.menu[role=menu] > li", "Delete")]
    [TestCase(typeof(NavMenuLink), "ul.menu > li > a[href='#'] > span.material-symbols-outlined", "home")]
    [TestCase(typeof(NavMenuLink), "ul.menu > li > a", "Dashboard")]
    [TestCase(typeof(Pagination), "nav.join[aria-label=Pagination] > button.join-item.btn-neutral[aria-current=page]", "5")]
    [TestCase(typeof(Steps), "ul.steps.steps-horizontal > li.step.step-primary[data-content='✓']", "Register")]
    [TestCase(typeof(Steps), "ul.steps > li.step.step-primary[aria-current=step]", "Choose plan")]
    [TestCase(typeof(Tabs), "div.w-96 > div[role=tablist].tabs.tabs-box > button.tab.tab-active", "Overview")]
    [TestCase(typeof(Tabs), "div.w-96 > div[role=tabpanel].tab-content.block", "Born in London")]
    [TestCase(typeof(TextLink), "a.link.link-hover.link-primary[href='https://daisyui.com'][target=_blank]", "Read the daisyUI docs")]
    [TestCase(typeof(MockupBrowser), "div.mockup-browser.border.border-base-300.w-96 > div.mockup-browser-toolbar > div.input", "acme.cloud")]
    [TestCase(typeof(MockupBrowser), "div.mockup-browser > div.border-t p.text-2xl", "Hello!")]
    [TestCase(typeof(MockupCode), "div.mockup-code > pre[data-prefix='$'] > code", "Done!")]
    [TestCase(typeof(MockupPhone), "div.mockup-phone.w-48 > div.mockup-phone-display > div.bg-primary", "api-gateway")]
    [TestCase(typeof(MockupWindow), "div.mockup-window.border.w-96 span.font-medium", "Deployments")]
    [TestCase(typeof(MockupWindow), "div.mockup-window > div > p", "3 deployments")]
    [TestCase(typeof(PageHeader), "div.max-w-full > div.flex.flex-row.justify-between h4", "Invoices")]
    [TestCase(typeof(PageHeader), "div.flex-row button.btn-primary", "New invoice")]
    [TestCase(typeof(SectionCard), "div.rounded-box.p-4.w-96 > h6", "Deployment")]
    [TestCase(typeof(SectionCard), "div.w-96 > div.divider + p", "rolling out")]
    [TestCase(typeof(DetailCard), "div.w-96 table.table > tbody > tr > td.wrap-anywhere strong", "Ana Silva")]
    [TestCase(typeof(DetailRow), "div.w-96 table > tbody > tr > td.wrap-anywhere strong", "Ana Silva")]
    [TestCase(typeof(DetailRow), "div.w-96 table > tbody > tr + tr", "EU West")]
    [TestCase(typeof(DetailLayout), "div.max-w-full a.btn[href='#']", "Back")]
    [TestCase(typeof(DetailLayout), "div.max-w-full div.col-span-12 > div.border > p", "renewed")]
    [TestCase(typeof(QuickAccessButton), "div.w-80 > button.btn.btn-outline.btn-block", "New invoice")]
    [TestCase(typeof(FeatureHero), "div.max-w-full > section.feature-hero h1.feature-hero__title", "Billing")]
    [TestCase(typeof(FeatureHero), "section.feature-hero .feature-hero__eyebrow", "Finance")]
    [TestCase(typeof(FeatureHomePage), "div.max-w-full > h4", "Billing")]
    [TestCase(typeof(FeatureHomePage), "div.grid.equal-height-cards > div.col-span-6 > div.card.nav-card", "Payments")]
    [TestCase(typeof(FeatureNavCard), "div.w-80 > div.card.nav-card .nav-card__eyebrow", "Billing")]
    [TestCase(typeof(FeatureNavCard), "div.card.nav-card .nav-card__desc", "Create, send")]
    [TestCase(typeof(AppCard), "div.app-card > div.app-icon[style*='#0ea5e9'] > span.material-symbols-outlined", "receipt_long")]
    [TestCase(typeof(AppCard), "div.app-card > .app-name", "Invoices")]
    public void LayoutPreset_RendersItsDaisyMarkup(Type component, string selector, string? text)
        => Preset_RendersItsDaisyControl(component, selector, text);

    [TestCase(typeof(Grid), "div.col-span-4, div.col-span-6", 5)]
    [TestCase(typeof(StackOverlay), "div.stack > div", 3)]
    [TestCase(typeof(Breadcrumbs), "nav.breadcrumbs li", 3)]
    [TestCase(typeof(Dock), "div.dock > button", 3)]
    [TestCase(typeof(Menu), "ul[role=menu] > li", 3)]
    [TestCase(typeof(Pagination), "span.btn-disabled[aria-hidden=true]", 2)]
    [TestCase(typeof(Steps), "li.step", 4)]
    [TestCase(typeof(Tabs), "button.tab[role=tab]", 3)]
    [TestCase(typeof(MockupCode), "pre[data-prefix]", 3)]
    [TestCase(typeof(DetailCard), "tbody > tr", 3)]
    [TestCase(typeof(FeatureHomePage), "div.nav-card", 2)]
    public void LayoutContainer_RendersEveryChild(Type component, string selector, int count)
        => Preset_RendersEveryOption(component, selector, count);

    [TestCase(typeof(Grid), "Widest safelisted gap", "div.grid.gap-12")]
    [TestCase(typeof(Stack), "Row", "div.flex.flex-row.gap-4")]
    [TestCase(typeof(Stack), "Centered", "div.flex.flex-col.items-center")]
    [TestCase(typeof(StackOverlay), "Top", "div.stack.stack-top")]
    [TestCase(typeof(Container), "Small", "div.mx-auto.max-w-screen-sm")]
    [TestCase(typeof(Container), "No gutters", "div.mx-auto.max-w-screen-lg:not(.px-4)")]
    [TestCase(typeof(Spacer), "Dotted leader", "div.flex-1.border-dotted")]
    [TestCase(typeof(Hero), "Background image", "div.hero[style^='background-image:url'] > div.hero-overlay + div.hero-content.text-neutral-content")]
    [TestCase(typeof(Footer), "Neutral", "footer.footer.bg-neutral.text-neutral-content")]
    [TestCase(typeof(Footer), "Centered", "footer.footer.footer-center.footer-horizontal")]
    [TestCase(typeof(Navbar), "Primary", "div.navbar.bg-primary.text-primary-content")]
    [TestCase(typeof(Navbar), "Shadow", "div.navbar.shadow-md")]
    [TestCase(typeof(Dock), "Extra large", "div.dock.dock-xl")]
    [TestCase(typeof(Dropdown), "Open", "details.dropdown.dropdown-open[open]")]
    [TestCase(typeof(Dropdown), "Opens on hover", "details.dropdown.dropdown-hover")]
    [TestCase(typeof(Dropdown), "Opens upward", "details.dropdown.dropdown-top.dropdown-open")]
    [TestCase(typeof(Menu), "Icon activator", "div.dropdown > button.btn-circle[aria-haspopup=menu][aria-label=Actions]")]
    [TestCase(typeof(Menu), "Dense", "ul.dropdown-content.menu.menu-sm")]
    [TestCase(typeof(Menu), "Horizontal list", "ul.dropdown-content.menu.menu-horizontal")]
    [TestCase(typeof(Menu), "Aligned to the end", "div.dropdown.dropdown-end")]
    [TestCase(typeof(NavMenuLink), "Disabled", "ul.menu > li.menu-disabled > span[aria-disabled=true]")]
    [TestCase(typeof(Pagination), "Primary", "button.btn-primary[aria-current=page]")]
    [TestCase(typeof(Pagination), "First and last buttons", "button[aria-label='First page'] ~ button[aria-label='Last page']")]
    [TestCase(typeof(Steps), "Vertical", "ul.steps.steps-vertical")]
    [TestCase(typeof(Tabs), "Lift", "div.tabs.tabs-lift ~ div.tab-content.border")]
    [TestCase(typeof(Tabs), "Border", "div.tabs.tabs-border")]
    [TestCase(typeof(Tabs), "Second tab active", "button.tab.tab-active[id$='-tab-1']")]
    [TestCase(typeof(TextLink), "Always underlined", "a.link.underline:not(.link-hover)")]
    [TestCase(typeof(TextLink), "Accent", "a.link.link-accent")]
    [TestCase(typeof(MockupBrowser), "Primary border", "div.mockup-browser.border-primary")]
    [TestCase(typeof(MockupCode), "Chevron prompt", "pre[data-prefix='>']")]
    [TestCase(typeof(MockupWindow), "Primary border", "div.mockup-window.border-primary")]
    [TestCase(typeof(PageHeader), "Level 2 heading", "h2")]
    [TestCase(typeof(PageHeader), "With icon", "h4.flex > span.material-symbols-outlined")]
    [TestCase(typeof(QuickAccessButton), "Loading", "button.btn[disabled] span.loading")]
    [TestCase(typeof(QuickAccessButton), "With badge", "div.tooltip span.indicator button.btn[disabled]")]
    [TestCase(typeof(FeatureHero), "Dense", "section.feature-hero.feature-hero--dense")]
    [TestCase(typeof(FeatureHero), "Ocean gradient", "section.feature-hero[style*='--hero-start:#0ea5e9']")]
    [TestCase(typeof(FeatureHomePage), "Hero banner", "section.feature-hero h1")]
    [TestCase(typeof(FeatureHomePage), "With error", "div.alert.alert-error")]
    [TestCase(typeof(FeatureNavCard), "Gradient avatar", "div.nav-card > span.nav-card__accent")]
    [TestCase(typeof(AppCard), "With badge", "div.app-card .app-badge")]
    public void LayoutVariant_RendersItsDaisyModifier(Type component, string variant, string selector)
        => DisplayVariant_RendersItsDaisyModifier(component, variant, selector);

    [TestCaseSource(nameof(LayoutCurated))]
    public void EveryLayoutVariant_ChangesTheRenderedMarkup(Type component)
    {
        var variants = Configured().GetVariants(component);
        variants.Count.Should().BeInRange(2, 6);

        foreach (var variant in variants)
        {
            using var context = NewContext();
            var cut = Bench(context, component);
            var before = Normalized(cut.Find(".pb-specimen").InnerHtml);

            Variant(cut, variant.Name);

            Normalized(cut.Find(".pb-specimen").InnerHtml).Should().NotBe(
                before, $"{component.Name} / \"{variant.Name}\" should change what the bench shows");
        }
    }

    [TestCaseSource(nameof(LayoutCurated))]
    public void EveryLayoutVariant_ChangesTheParameterDictionary(Type component)
        => EveryVariant_ChangesTheParameterDictionary(component);

    [Test]
    public void NavMenuLink_CurrentPageVariant_IsHighlightedOnTheExplorer()
    {
        // NavLink matches against the live URL: on /explorer?pb-..., Href="explorer" Prefix-matches.
        using var context = NewContext();
        context.Services.GetRequiredService<NavigationManager>().NavigateTo("explorer?pb-NavMenuLink");
        var cut = Bench(context, typeof(NavMenuLink));
        cut.FindAll(".pb-specimen a.menu-active").Should().BeEmpty();

        Variant(cut, "Current page");

        cut.Find(".pb-specimen ul.menu > li > a.menu-active[aria-current=page]").TextContent.Should().Contain("Dashboard");
    }

    [Test]
    public void FeatureHomePage_NeverRequiresAnAccessCheck()
    {
        // True wraps the page in AuthorizeView, which needs a cascading AuthenticationState the
        // demo does not provide. The default must stay false and no variant may flip it.
        new FeatureHomePage().RequireAccessCheck.Should().BeFalse();
        Configured().GetVariants(typeof(FeatureHomePage))
            .Should().NotContain(v => v.Values.Any(kv => kv.Key == nameof(FeatureHomePage.RequireAccessCheck)));
    }

    [TestCase(typeof(Grid))]
    [TestCase(typeof(Stack))]
    public void SpacingVariants_StayWithinTheSafelistedGaps(Type component)
    {
        // gap-{n} is safelisted only up to gap-12 at 0.6.0; a higher Spacing renders no gap.
        Configured().GetVariants(component)
            .SelectMany(v => v.Values).Where(kv => kv.Key == "Spacing")
            .Should().OnlyContain(kv => (int)kv.Value! <= 12);
    }

    /// <summary>
    /// The classes these presets make DaisyBlazor compose at runtime must exist in the compiled
    /// app.css. The last assertion pins the known upstream hole: when gap-16 appears, Stack and
    /// Grid can offer a wider Spacing (and the comment beside them can go).
    /// </summary>
    [Test]
    public void LayoutClasses_AreCompiledIntoAppCss()
    {
        var css = AppCss();
        string[] composed =
        [
            "gap-0", "gap-2", "gap-4", "gap-6", "gap-12", "col-span-4", "col-span-6", "col-span-12", "md\\:col-span-8", "md\\:col-span-4",
            "flex-row", "flex-col", "flex-wrap", "items-center", "flex-1",
            "stack", "stack-top", "stack-bottom", "stack-start", "stack-end",
            "max-w-screen-sm", "max-w-screen-md", "max-w-screen-lg", "mx-auto", "px-4",
            "hero", "hero-content", "hero-overlay", "min-h-64", "min-h-96",
            "footer", "footer-center", "footer-horizontal", "p-10", "p-4",
            "navbar", "navbar-start", "navbar-center", "navbar-end", "bg-base-100", "bg-primary", "text-primary-content",
            "bg-neutral", "text-neutral-content", "border-b", "border-base-300", "shadow-md",
            "breadcrumbs", "opacity-70",
            "dock", "dock-xs", "dock-sm", "dock-lg", "dock-xl", "dock-active", "dock-label", "relative",
            "dropdown", "dropdown-content", "dropdown-open", "dropdown-hover", "dropdown-top", "dropdown-end", "dropdown-right",
            "z-\\[1\\]", "z-\\[2\\]", "w-52", "w-56", "list-none", "fixed", "inset-0", "menu-sm", "menu-horizontal", "menu-active", "menu-disabled",
            "join", "join-item", "btn-sm", "btn-disabled", "btn-neutral", "btn-primary", "btn-circle", "btn-lg", "btn-block",
            "steps", "steps-horizontal", "steps-vertical", "step", "step-primary",
            "tabs", "tabs-box", "tabs-border", "tabs-lift", "tabs-sm", "tabs-md", "tabs-lg", "tab", "tab-active", "tab-content", "rounded-ss-none",
            "link", "link-hover", "link-primary", "link-secondary", "link-accent", "underline", "no-underline",
            "mockup-browser", "mockup-browser-toolbar", "mockup-code", "mockup-phone", "mockup-phone-camera", "mockup-phone-display",
            "mockup-window", "border-primary", "border-t",
            "wrap-anywhere", "self-start", "text-base-content\\/70", "equal-height-cards", "nav-card", "nav-card__accent",
        ];

        composed.Where(c => !Regex.IsMatch(css, $@"\.{Regex.Escape(c)}[\s{{:,.>\[)]"))
            .Should().BeEmpty("each class must be emitted for the component or variant that composes it to restyle anything");
        Regex.IsMatch(css, @"\.gap-16\b").Should().BeFalse("upstream hole at 0.6.0: gap-{n} is safelisted only up to 12");
    }

    private static string AppCss()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "PlayBlazor.slnx")))
        {
            root = root.Parent;
        }

        root.Should().NotBeNull("the tests run inside the repository");
        return File.ReadAllText(Path.Combine(root!.FullName, "demo", "PlayBlazor.Demo.Daisy", "wwwroot", "css", "app.css"));
    }
}
