using AwesomeAssertions;
using NUnit.Framework;

namespace PlayBlazor.UnitTests;

public class SmokeTest
{
    [Test]
    public void TestInfrastructure_Works()
    {
        true.Should().BeTrue();
    }
}
