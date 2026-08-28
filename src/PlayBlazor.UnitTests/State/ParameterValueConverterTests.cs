using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using MudBlazor.Utilities;
using NUnit.Framework;
using PlayBlazor.Discovery;
using PlayBlazor.Model;
using PlayBlazor.State;

namespace PlayBlazor.UnitTests.State;

public class ParameterValueConverterTests
{
    private static ParameterDescriptor Describe(Type type)
    {
        var (kind, isNullable) = ControlKindResolver.Resolve(type);
        return new ParameterDescriptor("P", type, kind, isNullable, null, false, null);
    }

    private static object? RoundTrip(Type type, object value)
    {
        var parameter = Describe(type);
        var text = ParameterValueConverter.Format(parameter, value);
        text.Should().NotBeNull();
        ParameterValueConverter.TryParse(parameter, text!, out var parsed).Should().BeTrue();
        return parsed;
    }

    [Test]
    public void DateTime_RoundTrips()
        => RoundTrip(typeof(DateTime?), new DateTime(2026, 8, 28, 14, 30, 5))
            .Should().Be(new DateTime(2026, 8, 28, 14, 30, 5));

    [Test]
    public void TimeSpan_RoundTrips()
        => RoundTrip(typeof(TimeSpan), new TimeSpan(1, 2, 3)).Should().Be(new TimeSpan(1, 2, 3));

    [Test]
    public void StringArray_RoundTripsAsCsv()
    {
        var parameter = Describe(typeof(string[]));
        ParameterValueConverter.Format(parameter, new[] { "Jan", "Feb" }).Should().Be("Jan, Feb");
        ParameterValueConverter.TryParse(parameter, "Mar, Apr ,May", out var parsed).Should().BeTrue();
        parsed.Should().BeEquivalentTo(new[] { "Mar", "Apr", "May" });
    }

    [Test]
    public void IntArray_RoundTripsAsCsv()
    {
        var parameter = Describe(typeof(int[]));
        ParameterValueConverter.TryParse(parameter, "10, 25, 50", out var parsed).Should().BeTrue();
        parsed.Should().BeEquivalentTo(new[] { 10, 25, 50 });
    }

    [Test]
    public void Char_And_MarkupString_RoundTrip()
    {
        RoundTrip(typeof(char), 'x').Should().Be('x');
        RoundTrip(typeof(MarkupString), new MarkupString("<b>hi</b>"))
            .Should().Be(new MarkupString("<b>hi</b>"));
    }

    [Test]
    public void MudColor_ParsesFromCssText()
    {
        var parameter = Describe(typeof(MudColor));
        ParameterValueConverter.TryParse(parameter, "#ff8800", out var parsed).Should().BeTrue();
        parsed.Should().BeOfType<MudColor>().Which.R.Should().Be(0xff);
    }

    [Test]
    public void Garbage_FailsGracefully()
    {
        ParameterValueConverter.TryParse(Describe(typeof(DateTime)), "not a date", out _).Should().BeFalse();
        ParameterValueConverter.TryParse(Describe(typeof(int[])), "a,b", out _).Should().BeFalse();
    }
}
