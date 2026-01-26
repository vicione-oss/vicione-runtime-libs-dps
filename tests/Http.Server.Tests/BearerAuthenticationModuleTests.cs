using AwesomeAssertions;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class BearerAuthenticationModule_FixedTimeEquals
{
    [Fact]
    public void Returns_True_For_Equal_Strings()
    {
        var result = BearerAuthenticationModule.FixedTimeEquals("testToken", "testToken");
        result.Should().BeTrue();
    }

    [Fact]
    public void Returns_False_For_Different_Strings()
    {
        var result = BearerAuthenticationModule.FixedTimeEquals("testToken", "differentToken");
        result.Should().BeFalse();
    }
}
