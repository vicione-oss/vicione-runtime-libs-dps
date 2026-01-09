using System.Collections.Generic;
using AwesomeAssertions;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class InfluxDbComparer_TryGetMissingStructure
{
    [Fact]
    public void Returns_true_if_structure_is_missing()
    {
        var expected = new List<Bucket> { new("test") };
        var current = new List<Bucket>();

        var result = InfluxDbComparer.TryGetMissingStructure(current, expected, out var missingBuckets);

        result.Should().BeTrue();
        missingBuckets.Should().ContainSingle().Which.Name.Should().Be("test");
    }

    [Fact]
    public void Returns_false_if_no_structure_is_missing()
    {
        var expected = new List<Bucket> { new("test") };
        var current = new List<Bucket> { new("test") };

        var result = InfluxDbComparer.TryGetMissingStructure(current, expected, out var missingBuckets);

        result.Should().BeFalse();
        missingBuckets.Should().BeEmpty();
    }
}
