using System;
using AwesomeAssertions;
using AwesomeAssertions.Extensions;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class HttpClientDataPortProperties_BaseAddress
{
    [Theory]
    [MemberData(nameof(Conversion))]
    public void Getter_returns_value(string baseAddress)
    {
        HttpClientDataPortProperties properties = new(new() { BaseAddress = baseAddress, });

        properties.BaseAddress.Should().Be(baseAddress);
    }

    [Theory]
    [MemberData(nameof(Conversion))]
    public void Setter_sets_value(string baseAddress)
    {
        HttpClientDataPortCommunication communication = new();

        _ = new HttpClientDataPortProperties(communication)
        {
            BaseAddress = baseAddress,
        };

        communication.BaseAddress.Should().Be(baseAddress);
    }

    public static TheoryData<string> Conversion() => new()
    {
        { string.Empty },
        { "api.sample.io" },
    };
}

public class HttpDataPortProperties_PollInterval
{
    [Theory]
    [MemberData(nameof(Conversion))]
    public void Getter_returns_value(int? value, TimeSpan? timeSpanValue)
    {
        HttpClientDataPortProperties properties = new(new() { PollInterval = value, });

        properties.PollInterval.Should().Be(timeSpanValue);
    }

    [Theory]
    [MemberData(nameof(Conversion))]
    public void Setter_sets_value(int? value, TimeSpan? timeSpanValue)
    {
        HttpClientDataPortCommunication communication = new();

        _ = new HttpClientDataPortProperties(communication)
        {
            PollInterval = timeSpanValue,
        };

        communication.PollInterval.Should().Be(value);
    }

    public static TheoryData<int?, TimeSpan?> Conversion() => new()
    {
        { null, null },
        { 0, TimeSpan.Zero },
        { 2000, 2.Seconds() },
    };
}
