using System;
using System.Diagnostics.CodeAnalysis;
using AwesomeAssertions;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class InfluxDbDataPortProperties_Token
{
    [Theory]
    [MemberData(nameof(Conversion))]
    public void Getter_returns_value(string token)
    {
        InfluxDbDataPortProperties properties = new(new() { Token = token, });

        properties.Token.Should().Be(token);
    }

    [Theory]
    [MemberData(nameof(Conversion))]
    public void Setter_sets_value(string token)
    {
        InfluxDbDataPortCommunication communication = new();

        _ = new InfluxDbDataPortProperties(communication)
        {
            Token = token,
        };

        communication.Token.Should().Be(token);
    }

    public static TheoryData<string> Conversion() => new()
    {
        { string.Empty },
        { "12:34:ab" },
    };
}

[SuppressMessage("Design", "CA1054:URI-ähnliche Parameter dürfen keine Zeichenfolgen sein", Justification = "Influx API erwartet einen string.")]
public class InfluxDbDataPortProperties_Url
{
    [Theory]
    [MemberData(nameof(Conversion))]
    public void Getter_returns_value(string url)
    {
        InfluxDbDataPortProperties properties = new(new() { Url = url, });

        properties.Url.Should().Be(url);
    }

    [Theory]
    [MemberData(nameof(Conversion))]
    public void Setter_sets_value(string url)
    {
        InfluxDbDataPortCommunication communication = new();

        _ = new InfluxDbDataPortProperties(communication)
        {
            Url = url,
        };

        communication.Url.Should().Be(url);
    }

    public static TheoryData<string> Conversion() => new()
    {
        { string.Empty },
        { "influxdb" },
    };
}

public class InfluxDbDataPortProperties_Organization
{
    [Theory]
    [MemberData(nameof(Conversion))]
    public void Getter_returns_value(string organization)
    {
        InfluxDbDataPortProperties properties = new(new() { Organization = organization, });

        properties.Organization.Should().Be(organization);
    }

    [Theory]
    [MemberData(nameof(Conversion))]
    public void Setter_sets_value(string organization)
    {
        InfluxDbDataPortCommunication communication = new();

        _ = new InfluxDbDataPortProperties(communication)
        {
            Organization = organization,
        };

        communication.Organization.Should().Be(organization);
    }

    public static TheoryData<string> Conversion() => new()
    {
        { string.Empty },
        { "https://cloud.amazon.com" },
    };
}

public class BulkSize
{
    [Theory]
    [MemberData(nameof(Conversion))]
    public void Getter_returns_value(int bulkSize)
    {
        InfluxDbDataPortProperties properties = new(new() { BulkSize = bulkSize, });

        properties.BulkSize.Should().Be(bulkSize);
    }

    [Theory]
    [MemberData(nameof(Conversion))]
    public void Setter_sets_value(int bulkSize)
    {
        InfluxDbDataPortCommunication communication = new();

        _ = new InfluxDbDataPortProperties(communication)
        {
            BulkSize = bulkSize,
        };

        communication.BulkSize.Should().Be(bulkSize);
    }

    public static TheoryData<int> Conversion() => new()
    {
        { 1_000 },
    };
}

public class InfluxDbDataPortProperties_MinimumPublishInterval
{
    [Theory]
    [MemberData(nameof(Conversion))]
    public void Getter_returns_value(uint minimumPublishIntervalInMs, TimeSpan minimumPublishInterval)
    {
        InfluxDbDataPortProperties properties = new(new() { MinimumPublishIntervalInMs = minimumPublishIntervalInMs, });

        properties.MinimumPublishInterval.Should().Be(minimumPublishInterval);
    }

    [Theory]
    [MemberData(nameof(Conversion))]
    public void Setter_sets_value(uint minimumPublishIntervalInMs, TimeSpan minimumPublishInterval)
    {
        InfluxDbDataPortCommunication communication = new();

        _ = new InfluxDbDataPortProperties(communication)
        {
            MinimumPublishInterval = minimumPublishInterval,
        };

        communication.MinimumPublishIntervalInMs.Should().Be(minimumPublishIntervalInMs);
    }

    public static TheoryData<uint, TimeSpan> Conversion() => new()
    {
        { 0, TimeSpan.Zero },
        { 2_000, TimeSpan.FromSeconds(2) },
    };
}
