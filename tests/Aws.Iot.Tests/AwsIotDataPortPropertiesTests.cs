using System.Diagnostics.CodeAnalysis;
using Amazon;
using AwesomeAssertions;
using Xunit;

namespace ViciOne.Suite.DataPort;

public class AwsIotDataPortProperties_AccessKey
{
    [Theory]
    [MemberData(nameof(Conversion))]
    public void Getter_returns_value(string accessKey)
    {
        AwsIotDataPortProperties properties = new(new() { AccessKey = accessKey, });

        properties.AccessKey.Should().Be(accessKey);
    }

    [Theory]
    [MemberData(nameof(Conversion))]
    public void Setter_sets_value(string accessKey)
    {
        AwsIotDataPortCommunication communication = new();

        _ = new AwsIotDataPortProperties(communication)
        {
            AccessKey = accessKey,
        };

        communication.AccessKey.Should().Be(accessKey);
    }

    public static TheoryData<string> Conversion() =>
    [
        string.Empty,
        "Key:12:34"
    ];
}

public class AwsIotDataPortProperties_SecretAccessKey
{
    [Theory]
    [MemberData(nameof(Conversion))]
    public void Getter_returns_value(string secretAccessKey)
    {
        AwsIotDataPortProperties properties = new(new() { SecretAccessKey = secretAccessKey, });

        properties.SecretAccessKey.Should().Be(secretAccessKey);
    }

    [Theory]
    [MemberData(nameof(Conversion))]
    public void Setter_sets_value(string secretAccessKey)
    {
        AwsIotDataPortCommunication communication = new();

        _ = new AwsIotDataPortProperties(communication)
        {
            SecretAccessKey = secretAccessKey,
        };

        communication.SecretAccessKey.Should().Be(secretAccessKey);
    }

    public static TheoryData<string> Conversion() =>
    [
        string.Empty,
        "Key:12:34"
    ];
}

public class AwsIotDataPortProperties_Region
{
    [Theory]
    [MemberData(nameof(Conversion))]
    [SuppressMessage("Usage", "xUnit1045:Avoid using TheoryData type arguments that might not be serializable", Justification = "Nicht möglich")]
    public void Getter_returns_value(string region, RegionEndpoint regionEndpoint)
    {
        AwsIotDataPortProperties properties = new(new() { Region = region, });

        properties.Region.Should().Be(regionEndpoint);
    }

    [Theory]
    [MemberData(nameof(Conversion))]
    [SuppressMessage("Usage", "xUnit1045:Avoid using TheoryData type arguments that might not be serializable", Justification = "Nicht möglich")]
    public void Setter_sets_value(string region, RegionEndpoint regionEndpoint)
    {
        AwsIotDataPortCommunication communication = new();

        _ = new AwsIotDataPortProperties(communication)
        {
            Region = regionEndpoint,
        };

        communication.Region.Should().Be(region);
    }

    public static TheoryData<string, RegionEndpoint> Conversion() => new()
    {
        { "eu-central-1", RegionEndpoint.EUCentral1 }, { "us-east-1", RegionEndpoint.USEast1 },
    };
}

public class AwsIotDataPortProperties_CertificateId
{
    [Theory]
    [MemberData(nameof(Conversion))]
    public void Getter_returns_value(string certificateId)
    {
        AwsIotDataPortProperties properties = new(new() { CertificateId = certificateId, });

        properties.CertificateId.Should().Be(certificateId);
    }

    [Theory]
    [MemberData(nameof(Conversion))]
    public void Setter_sets_value(string certificateId)
    {
        AwsIotDataPortCommunication communication = new();

        _ = new AwsIotDataPortProperties(communication)
        {
            CertificateId = certificateId,
        };

        communication.CertificateId.Should().Be(certificateId);
    }

    public static TheoryData<string> Conversion() =>
    [
        string.Empty,
        "Certificate:12:34"
    ];
}

public class AwsIotDataPortProperties_EndpointPort
{
    [Theory]
    [MemberData(nameof(Conversion))]
    public void Getter_returns_value(ushort endpointPort)
    {
        AwsIotDataPortProperties properties = new(new() { EndpointPort = endpointPort, });

        properties.EndpointPort.Should().Be(endpointPort);
    }

    [Theory]
    [MemberData(nameof(Conversion))]
    public void Setter_sets_value(ushort endpointPort)
    {
        AwsIotDataPortCommunication communication = new();

        _ = new AwsIotDataPortProperties(communication)
        {
            EndpointPort = endpointPort,
        };

        communication.EndpointPort.Should().Be(endpointPort);
    }

    public static TheoryData<ushort> Conversion() =>
    [
        8883,
        443
    ];
}
