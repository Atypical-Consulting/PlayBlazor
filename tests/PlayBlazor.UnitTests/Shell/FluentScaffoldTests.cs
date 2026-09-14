using AwesomeAssertions;
using Microsoft.FluentUI.AspNetCore.Components;
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
}
