using System.Reflection;
using AwesomeAssertions;
using NUnit.Framework;
using PlayBlazor.Discovery;
using PlayBlazor.Model;
using PlayBlazor.UnitTests.Fixtures;

namespace PlayBlazor.UnitTests.Discovery;

/// <summary>
/// Two parameter shapes exist on nearly every component of every library and can never be driven
/// from a generated control: Blazor's splatting dictionary, and an opaque object payload. They are
/// recognised structurally, so the rule holds for a library this repo has never seen.
/// </summary>
public class UndrivableParameterTests
{
    private static ParameterDescriptor Parameter(string name)
        => new ReflectionCatalogProvider()
            .Describe(typeof(SplattingFixture))
            .Parameters.Single(p => p.Name == name);

    [Test]
    public void SplattingParameter_IsUndrivable()
        => Parameter("Extra").Kind.Should().Be(ControlKind.Undrivable);

    [Test]
    public void OpaqueObjectPayload_IsUndrivable()
        => Parameter("Payload").Kind.Should().Be(ControlKind.Undrivable);

    [Test]
    public void AnOrdinaryStringIsUntouched()
        => Parameter("Label").Kind.Should().Be(ControlKind.Text);

    [Test]
    public void SplattingIsRecognisedByItsAttribute_NotItsName()
    {
        // MudBlazor calls it UserAttributes and types it Dictionary<,>; Fluent calls it
        // AdditionalAttributes and types it IReadOnlyDictionary<,>. Only the attribute is common.
        var property = typeof(SplattingFixture).GetProperty("Extra")!;
        var attribute = property.GetCustomAttribute<Microsoft.AspNetCore.Components.ParameterAttribute>()!;

        attribute.CaptureUnmatchedValues.Should().BeTrue();
        property.Name.Should().NotBe("AdditionalAttributes");
    }
}
