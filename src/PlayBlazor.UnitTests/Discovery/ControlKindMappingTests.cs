using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using MudBlazor.Utilities;
using NUnit.Framework;
using PlayBlazor.Discovery;
using PlayBlazor.Model;

namespace PlayBlazor.UnitTests.Discovery;

public class ControlKindMappingTests
{
    [TestCase(typeof(DateTime), ControlKind.Date, false)]
    [TestCase(typeof(DateTime?), ControlKind.Date, true)]
    [TestCase(typeof(DateOnly), ControlKind.Date, false)]
    [TestCase(typeof(TimeSpan), ControlKind.Time, false)]
    [TestCase(typeof(TimeSpan?), ControlKind.Time, true)]
    [TestCase(typeof(TimeOnly), ControlKind.Time, false)]
    [TestCase(typeof(char), ControlKind.Text, false)]
    [TestCase(typeof(MarkupString), ControlKind.Text, false)]
    [TestCase(typeof(string[]), ControlKind.Text, false)]
    [TestCase(typeof(int[]), ControlKind.Text, false)]
    [TestCase(typeof(MudColor), ControlKind.Color, false)]
    [TestCase(typeof(Func<string, bool>), ControlKind.Unsupported, false)]
    [TestCase(typeof(Dictionary<string, object>), ControlKind.Unsupported, false)]
    public void Resolve_MapsRichTypes(Type type, ControlKind expectedKind, bool expectedNullable)
    {
        var (kind, isNullable) = ControlKindResolver.Resolve(type);

        kind.Should().Be(expectedKind);
        isNullable.Should().Be(expectedNullable);
    }
}
