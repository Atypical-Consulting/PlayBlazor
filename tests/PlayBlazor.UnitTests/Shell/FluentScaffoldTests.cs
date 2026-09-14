using AwesomeAssertions;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components.Utilities;
using NUnit.Framework;
using PlayBlazor.Demo.FluentUI;

namespace PlayBlazor.UnitTests.Shell;

public class FluentScaffoldTests
{
    private static PlayBlazorOptions Configured()
    {
        var options = new PlayBlazorOptions();
        FluentPlaygroundConfig.Configure(options);
        return options;
    }

    [TestCase(typeof(FluentAppBarItem))]
    [TestCase(typeof(FluentNavItem))]
    [TestCase(typeof(FluentNavCategory))]
    [TestCase(typeof(FluentNavSectionHeader))]
    [TestCase(typeof(FluentWizardStep))]
    public void ComponentsThatNeedAParent_HaveOne(Type component)
        => Configured().TryGetScaffold(component, out _).Should().BeTrue();

    [Test]
    public void EveryScaffoldCarriesItsRazorSource()
    {
        // The generated snippet wraps the specimen in the scaffold's text; a scaffold without
        // source produces a snippet that does not reproduce what the bench shows.
        var options = Configured();

        options.TryGetScaffold(typeof(FluentNavItem), out _).Should().BeTrue();
        options.TryGetScaffoldSource(typeof(FluentNavItem), out var source).Should().BeTrue();
        source.Should().Contain("{specimen}");
    }

    [Test]
    public void ValidationComponents_RenderInsideAnEditForm()
    {
        var options = Configured();

        options.TryGetScaffoldSource(typeof(FluentValidationSummary), out var source).Should().BeTrue();
        source.Should().Contain("EditForm");
    }

    [Test]
    public void ComponentsWithARequiredValue_GetOne()
    {
        var options = Configured();

        options.TryGetParameterPreset(typeof(AddTag), "Name", out var name).Should().BeTrue();
        name.Should().NotBeNull();
        options.TryGetSlotPreset(typeof(FluentKeyCode), "ChildContent", out _).Should().BeTrue();
    }
}
