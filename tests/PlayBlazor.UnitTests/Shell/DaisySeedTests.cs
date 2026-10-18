using AwesomeAssertions;
using Bunit;
using DaisyBlazor;
using DaisyBlazor.Theming;
using NUnit.Framework;
using PlayBlazor.Demo.Daisy;
using PlayBlazor.Discovery;
using PlayBlazor.Model;

namespace PlayBlazor.UnitTests.Shell;

/// <summary>
/// The DaisyBlazor showcase is a seed until milestone 4, so it is not held to the curation gates.
/// These pin what the seed does promise: the icon picker is fed, providers and service-bound helpers stay off the list, and
/// the flagship bench renders a real daisyUI button.
/// </summary>
public class DaisySeedTests
{
    private static PlayBlazorOptions Configured()
    {
        var options = new PlayBlazorOptions();
        DaisyPlaygroundConfig.Configure(options);
        return options;
    }

    [Test]
    public void IconCatalogue_OffersMaterialLigatures_AndDrivesIconParameters()
    {
        DaisyIconCatalogue.Material.Should().HaveCountGreaterThan(1000);
        DaisyIconCatalogue.Material["Check"].Should().Be("check");

        var button = new ReflectionCatalogProvider(options: Configured()).Describe(typeof(Button));
        button.Parameters.Single(p => p.Name == nameof(Button.StartIcon)).Kind.Should().Be(ControlKind.Icon);
    }

    [TestCase(typeof(ThemeProvider))]
    [TestCase(typeof(DialogProvider))]
    [TestCase(typeof(SnackbarProvider))]
    [TestCase(typeof(PageBreadcrumb))]
    public void InfrastructureComponents_AreNotListed(Type provider)
        => Configured().ComponentFilter!(provider).Should().BeFalse();

    [Test]
    public void FlagshipButton_RendersAFilledDaisyButton()
    {
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddDaisyBlazor();
        context.Services.AddPlayBlazor(DaisyPlaygroundConfig.Configure);

        var cut = context.Render<PlaygroundView>(ps => ps.Add(c => c.Component, typeof(Button)));

        var button = cut.Find("button.btn");
        button.TextContent.Should().Contain("Click me");
        button.ClassList.Should().NotContain("btn-ghost");
    }
}
