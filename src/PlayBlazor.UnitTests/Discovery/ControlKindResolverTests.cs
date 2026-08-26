using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using NUnit.Framework;
using PlayBlazor.Discovery;
using PlayBlazor.Model;

namespace PlayBlazor.UnitTests.Discovery;

public class ControlKindResolverTests
{
    [TestCase(typeof(bool), ControlKind.Bool, false)]
    [TestCase(typeof(bool?), ControlKind.Bool, true)]
    [TestCase(typeof(DayOfWeek), ControlKind.Enum, false)]
    [TestCase(typeof(DayOfWeek?), ControlKind.Enum, true)]
    [TestCase(typeof(string), ControlKind.Text, false)]
    [TestCase(typeof(int), ControlKind.Number, false)]
    [TestCase(typeof(int?), ControlKind.Number, true)]
    [TestCase(typeof(long), ControlKind.Number, false)]
    [TestCase(typeof(short), ControlKind.Number, false)]
    [TestCase(typeof(byte), ControlKind.Number, false)]
    [TestCase(typeof(double), ControlKind.Number, false)]
    [TestCase(typeof(float), ControlKind.Number, false)]
    [TestCase(typeof(decimal), ControlKind.Number, false)]
    [TestCase(typeof(RenderFragment), ControlKind.Slot, false)]
    [TestCase(typeof(RenderFragment<string>), ControlKind.Slot, false)]
    [TestCase(typeof(EventCallback), ControlKind.Event, false)]
    [TestCase(typeof(EventCallback<string>), ControlKind.Event, false)]
    [TestCase(typeof(Uri), ControlKind.Unsupported, false)]
    [TestCase(typeof(Dictionary<string, object>), ControlKind.Unsupported, false)]
    public void Resolve_MapsTypeToKind(Type parameterType, ControlKind expectedKind, bool expectedNullable)
    {
        var (kind, isNullable) = ControlKindResolver.Resolve(parameterType);

        kind.Should().Be(expectedKind);
        isNullable.Should().Be(expectedNullable);
    }
}
