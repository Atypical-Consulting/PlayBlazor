using AwesomeAssertions;
using Microsoft.FluentUI.AspNetCore.Components;
using NUnit.Framework;
using PlayBlazor.Demo.FluentUI;

namespace PlayBlazor.UnitTests.Shell;

/// <summary>
/// One case per curated component: a missing preset is named in the failure rather than
/// hidden behind a passing count. Milestone 3's preset tasks (form inputs, buttons and
/// actions, data display, layout and navigation) all add their cases to this one file.
/// </summary>
public class FluentPresetTests
{
    private static PlayBlazorOptions Configured()
    {
        var options = new PlayBlazorOptions();
        FluentPlaygroundConfig.Configure(options);
        return options;
    }

    // --- Task 5: form inputs ---
    [TestCase(typeof(FluentTextInput))]
    [TestCase(typeof(FluentTextArea))]
    [TestCase(typeof(FluentNumberInput<int>))]
    [TestCase(typeof(FluentSelect<string, string>))]
    [TestCase(typeof(FluentCombobox<string, string>))]
    [TestCase(typeof(FluentAutocomplete<string, string>))]
    [TestCase(typeof(FluentListbox<string, string>))]
    [TestCase(typeof(FluentOption<string>))]
    [TestCase(typeof(FluentCheckbox))]
    [TestCase(typeof(FluentSwitch))]
    [TestCase(typeof(FluentRadio<string>))]
    [TestCase(typeof(FluentRadioGroup<string>))]
    [TestCase(typeof(FluentSlider<int>))]
    [TestCase(typeof(FluentDatePicker<DateTime?>))]
    [TestCase(typeof(FluentTimePicker<DateTime?>))]
    [TestCase(typeof(FluentCalendar<DateTime?>))]
    [TestCase(typeof(FluentColorPicker))]
    [TestCase(typeof(FluentColorPickerInput))]
    [TestCase(typeof(FluentField))]
    [TestCase(typeof(FluentLabel))]
    [TestCase(typeof(FluentInputFile))]
    public void ComponentHasCuration(Type component)
    {
        var options = Configured();

        var curated = options.GetVariants(component).Count > 0
            || options.TryGetSlotPreset(component, "ChildContent", out _)
            || options.TryGetScaffold(component, out _);

        curated.Should().BeTrue($"{component.Name} should carry a preset, slot or variant");
    }
}
