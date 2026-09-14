using AwesomeAssertions;
using Microsoft.FluentUI.AspNetCore.Components;
using NUnit.Framework;
using PlayBlazor.Demo.FluentUI;
using PlayBlazor.Discovery;

namespace PlayBlazor.UnitTests.Shell;

public class FluentCurationTests
{
    private static (PlayBlazorOptions Options, IReadOnlyList<PlayBlazor.Model.ComponentDescriptor> Listed) Curated()
    {
        var options = new PlayBlazorOptions();
        FluentPlaygroundConfig.Configure(options);
        var listed = new ReflectionCatalogProvider(options: options)
            .Discover(typeof(FluentButton).Assembly)
            .Where(c => !options.IsExcluded(c.Type) && (options.ComponentFilter?.Invoke(c.Type) ?? true))
            .ToList();
        return (options, listed);
    }

    [Test]
    public void ProvidersAndOptionsObjects_AreNotListed()
    {
        var names = Curated().Listed.Select(c => c.DisplayName).ToList();

        names.Should().NotContain("FluentToastProvider");
        names.Should().NotContain("FluentTooltipProvider");
        names.Should().NotContain("FluentProviders");
        names.Should().NotContain("ColumnReorderOptions");
        names.Should().NotContain("ColumnResizeOptions");
        names.Should().NotContain("Defer");
    }

    [Test]
    public void TheRealComponentsSurvive()
    {
        var names = Curated().Listed.Select(c => c.DisplayName).ToList();

        names.Should().Contain("FluentButton");
        names.Should().Contain("FluentDataGrid");
        names.Should().Contain("FluentNav");
        names.Count.Should().BeGreaterThan(80);
    }

    [Test]
    public void GenericsAreClosedWithATypeTheyAccept()
    {
        var options = Curated().Options;

        // Discovery closes an open generic with string first; all four reject it at construction.
        options.ResolvePreferredClosing(typeof(FluentCalendar<>).MakeGenericType(typeof(string)))
            .Should().Be(typeof(FluentCalendar<DateTime?>));
        options.ResolvePreferredClosing(typeof(FluentDatePicker<>).MakeGenericType(typeof(string)))
            .Should().Be(typeof(FluentDatePicker<DateTime?>));
        options.ResolvePreferredClosing(typeof(FluentTimePicker<>).MakeGenericType(typeof(string)))
            .Should().Be(typeof(FluentTimePicker<DateTime?>));
        options.ResolvePreferredClosing(typeof(FluentNumberInput<>).MakeGenericType(typeof(string)))
            .Should().Be(typeof(FluentNumberInput<int>));
    }
}
