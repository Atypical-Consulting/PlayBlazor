using System.Text.RegularExpressions;
using AwesomeAssertions;
using Bunit;
using DaisyBlazor;
using NUnit.Framework;
using PlayBlazor.Demo.Daisy;
using PlayBlazor.Discovery;
using PlayBlazor.Rendering;
using PlayBlazor.State;

namespace PlayBlazor.UnitTests.Shell;

/// <summary>
/// DaisyBlazor's actions and form inputs, rendered on the bench as the explorer renders them. Each
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

    [TestCase(typeof(Button), "button.btn", "Click me")]
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

    /// <summary>
    /// VariantLivenessTests' check, applied to this batch while DaisyBlazor sits outside
    /// <c>ExploredLibraries.Curated</c>: each variant must change the parameter dictionary.
    /// </summary>
    [TestCaseSource(nameof(Curated))]
    public void EveryVariant_ChangesTheParameterDictionary(Type component)
    {
        var options = Configured();
        var descriptor = new ReflectionCatalogProvider(options: options).Describe(component);
        var baseline = ParameterDictionaryBuilder.Build(descriptor, new PlaygroundState(), options);

        foreach (var variant in options.GetVariants(component))
        {
            var state = new PlaygroundState();
            foreach (var (name, value) in variant.Values)
            {
                if (value is not null)
                {
                    state.Set(name, value);
                }
            }

            var applied = ParameterDictionaryBuilder.Build(descriptor, state, options);
            var same = applied.Count == baseline.Count
                && baseline.All(kv => applied.TryGetValue(kv.Key, out var other) && Equals(kv.Value, other));
            same.Should().BeFalse($"{component.Name} / \"{variant.Name}\" should change the parameters");
        }
    }

    private static void Variant(IRenderedComponent<PlaygroundView> cut, string name)
        => cut.FindAll("button.pb-variant").Single(b => b.TextContent == name).Click();

    // Field ids are minted per instance, and Blazor renumbers its event-handler attributes.
    private static string Normalized(string html) => Noise().Replace(html, "{noise}");

    [GeneratedRegex("[0-9a-f]{32}|[0-9a-f]{8}(?=-)|(?<=dz-field-)[0-9a-f]+|(?<=dz-date-)[0-9a-f]{8}|blazor:\\w+=\"\\d+\"")]
    private static partial Regex Noise();
}
